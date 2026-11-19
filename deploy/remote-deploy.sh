#!/usr/bin/env bash
# =============================================================================
#  Despliegue por versiones de MreHunter.Api en la EC2 (lo ejecuta deploy.ps1).
#
#  Estructura en el servidor:
#    ~/miapi                    -> enlace simbólico a la versión activa
#    ~/miapi-releases/<fecha>/  -> cada versión publicada (se guardan las últimas N)
#    ~/miapi-shared/wwwroot-uploads/ -> imágenes subidas desde el CMS (compartidas)
#    ~/miapi-shared/uploads/         -> hojas de vida (compartidas, privadas)
#
#  El servicio systemd sigue apuntando a /home/ubuntu/miapi.
#
#  Uso:
#    remote-deploy.sh deploy <timestamp> <paquete.tgz> [versiones_a_conservar]
#    remote-deploy.sh rollback
#    remote-deploy.sh status
# =============================================================================
set -euo pipefail

# El script se borra a sí mismo al terminar (deploy.ps1 lo sube a /tmp en cada ejecución).
trap 'rm -f "$0"' EXIT

APP_LINK="$HOME/miapi"
RELEASES="$HOME/miapi-releases"
SHARED="$HOME/miapi-shared"
SERVICE="miapi"
ENV_FILE="/etc/miapi.env"
HEALTH_URL="${HEALTH_URL:-http://localhost:5000/api/footer}"

log()  { echo "[$(date -u +%H:%M:%S)] $*"; }
fail() { echo "ERROR: $*" >&2; exit 1; }

health_check() {
    local code
    for _ in $(seq 1 30); do
        code=$(curl -s -o /dev/null -w '%{http_code}' "$HEALTH_URL" || true)
        if [ "$code" = "200" ]; then return 0; fi
        sleep 1
    done
    return 1
}

# Cambio atómico del enlace ~/miapi y reinicio del servicio.
switch_to() {
    ln -sfn "$1" "$APP_LINK.tmp"
    mv -Tf "$APP_LINK.tmp" "$APP_LINK"
    sudo systemctl restart "$SERVICE"
}

current_release() { readlink -f "$APP_LINK"; }

# Las carpetas de archivos subidos viven fuera de las versiones: un deploy nunca las pisa.
link_shared() {
    local rel="$1"
    mkdir -p "$rel/wwwroot"
    rm -rf "$rel/wwwroot/uploads" "$rel/uploads"
    ln -s "$SHARED/wwwroot-uploads" "$rel/wwwroot/uploads"
    ln -s "$SHARED/uploads" "$rel/uploads"
}

# La Fase 1 exige Jwt__Key >= 32 caracteres: sin ella la API no arranca. Se valida ANTES de tocar nada.
check_env() {
    sudo test -f "$ENV_FILE" || fail "No existe $ENV_FILE."

    local key
    key=$(sudo grep -E '^Jwt__Key=' "$ENV_FILE" | head -n 1 | cut -d= -f2- || true)
    key="${key%\'}"; key="${key#\'}"; key="${key%\"}"; key="${key#\"}"

    if [ "${#key}" -lt 32 ]; then
        fail "Jwt__Key falta o tiene menos de 32 caracteres en $ENV_FILE: la API no arrancaría. Despliegue cancelado. Genera una con: openssl rand -base64 48"
    fi
    case "$key" in
        CAMBIA_ESTA_CLAVE*) fail "Jwt__Key es la clave de ejemplo que estuvo publicada en el repositorio. Genera una nueva con: openssl rand -base64 48" ;;
    esac

    if ! sudo grep -qE '^Email__SmtpUser=.+' "$ENV_FILE"; then log "AVISO: falta Email__SmtpUser en $ENV_FILE: no se enviarán correos."; fi
    if ! sudo grep -qE '^Email__SmtpPass=.+' "$ENV_FILE"; then log "AVISO: falta Email__SmtpPass en $ENV_FILE: no se enviarán correos."; fi
    log "Variables de $ENV_FILE verificadas."
}

# Solo la primera vez: ~/miapi es una carpeta normal (despliegue antiguo por scp).
# Se respaldan y se mueven los archivos subidos a ~/miapi-shared y la carpeta queda como la versión "legacy".
migrate_legacy() {
    if [ ! -d "$APP_LINK" ] || [ -L "$APP_LINK" ]; then
        return 0
    fi

    log "Primera ejecución: migrando $APP_LINK al esquema de versiones..."
    mkdir -p "$SHARED/wwwroot-uploads" "$SHARED/uploads" "$RELEASES"

    local stamp backup legacy
    stamp=$(date -u +%Y%m%d%H%M%S)
    backup="$HOME/miapi-uploads-backup-$stamp.tgz"
    # Ceros al inicio: al ordenar, la instalación antigua siempre queda como la versión más vieja.
    legacy="$RELEASES/00000000000000-legacy"

    # Se detiene la API para que nadie suba archivos mientras se copian.
    sudo systemctl stop "$SERVICE"

    if ! (
        set -e
        items=()
        if [ -d "$APP_LINK/wwwroot/uploads" ]; then items+=("wwwroot/uploads"); fi
        if [ -d "$APP_LINK/uploads" ]; then items+=("uploads"); fi
        if [ "${#items[@]}" -gt 0 ]; then
            tar -czf "$backup" -C "$APP_LINK" "${items[@]}"
            echo "Respaldo de archivos subidos: $backup"
        fi

        if [ -d "$APP_LINK/wwwroot/uploads" ]; then cp -a "$APP_LINK/wwwroot/uploads/." "$SHARED/wwwroot-uploads/"; fi
        if [ -d "$APP_LINK/uploads" ]; then cp -a "$APP_LINK/uploads/." "$SHARED/uploads/"; fi
        chmod 700 "$SHARED/uploads"

        mv "$APP_LINK" "$legacy"
        link_shared "$legacy"
        ln -s "$legacy" "$APP_LINK"
    ); then
        # Pase lo que pase, la API vuelve a quedar en línea con lo que había.
        if [ ! -e "$APP_LINK" ] && [ -d "$legacy" ]; then ln -s "$legacy" "$APP_LINK"; fi
        sudo systemctl start "$SERVICE" || true
        fail "La migración falló. La API se reinició con la versión que tenía. Respaldo (si se creó): $backup"
    fi

    sudo systemctl start "$SERVICE"
    log "Versión anterior conservada como $(basename "$legacy")."
}

prune() {
    local keep="$1" current
    current=$(current_release)
    { ls -1d "$RELEASES"/[0-9]* 2>/dev/null || true; } | sort | head -n -"$keep" | while read -r dir; do
        if [ "$dir" = "$current" ]; then continue; fi
        rm -rf "$dir"
        log "Eliminada versión antigua $(basename "$dir")."
    done
}

do_deploy() {
    local ts="$1" pkg="$2" keep="${3:-5}"
    [ -f "$pkg" ] || fail "No se encontró el paquete $pkg."

    check_env
    migrate_legacy
    mkdir -p "$RELEASES" "$SHARED/wwwroot-uploads" "$SHARED/uploads"

    local rel="$RELEASES/$ts"
    [ -e "$rel" ] && fail "Ya existe la versión $ts."

    mkdir -p "$rel"
    tar -xzf "$pkg" -C "$rel"
    rm -f "$pkg"
    [ -f "$rel/MreHunter.Api.dll" ] || { rm -rf "$rel"; fail "El paquete no contiene MreHunter.Api.dll."; }
    link_shared "$rel"

    local previous=""
    if [ -L "$APP_LINK" ]; then previous=$(current_release); fi

    log "Activando versión $ts..."
    switch_to "$rel"

    if health_check; then
        log "OK: la API responde 200 en $HEALTH_URL."
    else
        log "La nueva versión NO respondió en 30 s. Últimos logs del servicio:"
        sudo journalctl -u "$SERVICE" -n 40 --no-pager || true
        if [ -n "$previous" ]; then
            log "Volviendo a la versión anterior $(basename "$previous")..."
            switch_to "$previous"
            if health_check; then log "Versión anterior restaurada y respondiendo."; else log "ATENCIÓN: la versión anterior tampoco responde. Revisa el servidor."; fi
        fi
        rm -rf "$rel"
        fail "Despliegue fallido."
    fi

    prune "$keep"
    do_status
}

do_rollback() {
    [ -L "$APP_LINK" ] || fail "Todavía no hay esquema de versiones: no hay a qué volver."

    local current prev
    current=$(current_release)
    prev=$({ ls -1d "$RELEASES"/[0-9]* 2>/dev/null || true; } | sort | awk -v c="$current" '$0==c { print p; exit } { p=$0 }')
    [ -n "$prev" ] || fail "No hay una versión anterior a $(basename "$current")."

    log "Volviendo de $(basename "$current") a $(basename "$prev")..."
    switch_to "$prev"
    if health_check; then log "OK: la API responde 200."; else fail "La versión $(basename "$prev") no responde. Revisa: sudo journalctl -u $SERVICE -n 50"; fi
    do_status
}

do_status() {
    if [ -L "$APP_LINK" ]; then
        log "Versión activa: $(basename "$(current_release)")"
    else
        log "Esquema de versiones aún no instalado (se instala en el primer deploy)."
    fi
    if [ -d "$RELEASES" ]; then
        log "Versiones en el servidor:"
        ls -1 "$RELEASES"
    fi
}

case "${1:-}" in
    deploy)   shift; do_deploy "$@" ;;
    rollback) do_rollback ;;
    status)   do_status ;;
    *)        fail "Uso: $0 deploy <timestamp> <paquete.tgz> [conservar] | rollback | status" ;;
esac
