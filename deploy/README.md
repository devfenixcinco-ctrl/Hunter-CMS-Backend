# Guía de despliegue — MreHunter.Api

Guía para desplegar la API en la EC2 `api-cms-hunter` (`api.gpsrolandoplazas.com`) con el script por versiones y aplicar la configuración de nginx.

> Sigue los pasos **en orden**. Cada paso indica cómo verificarlo y cómo volver atrás.

---

## Cómo queda el servidor

```
Navegador ─► nginx (80/443, HTTP/2)
               ├─ /uploads/*  ─► ~/miapi-shared/wwwroot-uploads   (nginx sirve las imágenes directo)
               └─ todo lo demás ─► Kestrel 127.0.0.1:5000 ─► RDS MySQL

/home/ubuntu/
├── miapi                      → enlace a la versión activa (lo usa el servicio systemd "miapi")
├── miapi-releases/
│   ├── 00000000000000-legacy/ → la instalación anterior al script (para volver atrás)
│   ├── 20261120153000/        → cada despliegue (se guardan las últimas 5)
│   └── ...
├── miapi-shared/
│   ├── wwwroot-uploads/       → imágenes subidas desde el CMS (públicas, las sirve nginx)
│   └── uploads/               → hojas de vida (privadas, permisos 700)
└── miapi-uploads-backup-*.tgz → respaldo creado en la primera migración
```

Los archivos subidos viven **fuera** de las versiones: ningún despliegue los vuelve a sobrescribir.

---

## 1. Antes del día del despliegue

| # | Tarea | Cómo verificar |
|---|-------|----------------|
| 1 | Ramas de Fase 1 y Fase 2 fusionadas en `main` en GitHub | `git log --oneline -3 origin/main` muestra los commits de Fase 1 y 2 |
| 2 | Equipo de Angular avisado de los cambios (ver abajo) | — |
| 3 | Contraseña de aplicación de Gmail **rotada** (la anterior está en el historial de git) | Nueva contraseña creada en la cuenta de Google |
| 4 | `/etc/miapi.env` completo (ver 1.1) | Comandos de 1.1 |
| 5 | Snapshot manual de RDS (precaución; estas fases no cambian la base) | RDS → `mrehunter-db` → Acciones → **Tomar instantánea** |
| 6 | La regla SSH del grupo de seguridad tiene tu IP actual | `Test-NetConnection 77.113.141.45 -Port 22` → `True` |

**Cambios que necesita el frontend Angular** (si no, el login deja de funcionar bien):

- `POST /api/auth/login` con credenciales incorrectas responde **401** con `{"message":"Correo o contraseña incorrectos."}` (antes 404/403 con `remainingAttempts`).
- `POST /api/auth/register` exige token (solo un administrador crea usuarios).
- `GET /api/newsletter` y `GET /api/work-with-us` exigen token.
- Opcional: reemplazar las llamadas a header, footer, settings y contact-header por una sola: `GET /api/site/layout`.
- Opcional: no enviar `Authorization` en los GET públicos (evita preflights CORS).

### 1.1 Verificar `/etc/miapi.env`

Conéctate por SSH:

```powershell
ssh -i "C:\Users\DevSenior1\Documents\Desarrollos\Hunter Institucional\api-cms-hunter.pem" ubuntu@77.113.141.45
```

En el servidor (muestra longitudes y nombres, **no** los valores):

```bash
sudo awk -F= '/^Jwt__Key=/{print "Jwt__Key: " length($2) " caracteres"}' /etc/miapi.env
sudo grep -oE '^(Email__SmtpUser|Email__SmtpPass|ConnectionStrings__DefaultConnection|ASPNETCORE_ENVIRONMENT|ASPNETCORE_URLS)=' /etc/miapi.env
```

Debe verse `Jwt__Key` con **32 o más** caracteres y las 5 variables listadas. Si falta algo:

```bash
sudo nano /etc/miapi.env
```

- Generar una clave JWT nueva: `openssl rand -base64 48`
  (cambiarla invalida las sesiones abiertas del CMS: los administradores deben volver a iniciar sesión).
- `Email__SmtpPass` = la contraseña de aplicación **nueva** de Gmail, sin espacios.

No hace falta reiniciar ahora: el despliegue reinicia la API.

---

## 2. Día del despliegue

### Paso 1 — Actualizar `main` en tu PC

```powershell
cd "C:\Users\DevSenior1\Documents\Desarrollos\Hunter Institucional\Hunter-CMS-Backend"
git checkout main
git pull
```

### Paso 2 — Desplegar la API

```powershell
.\deploy\deploy.ps1
```

Si Windows bloquea el script: `powershell -ExecutionPolicy Bypass -File .\deploy\deploy.ps1`

Lo que hace:

1. Verifica que estés en `main` sin cambios pendientes.
2. Publica en Release y sube un solo paquete `.tgz`.
3. En el servidor valida `/etc/miapi.env`; si `Jwt__Key` no sirve, **cancela sin tocar nada**.
4. **Solo la primera vez:** detiene la API unos segundos, respalda los archivos subidos en `~/miapi-uploads-backup-*.tgz`, los mueve a `~/miapi-shared` y conserva la instalación actual como `00000000000000-legacy`.
5. Activa la versión nueva, reinicia y espera a que `/api/footer` responda 200.
6. **Si no responde en 30 s, vuelve sola a la versión anterior** y muestra el log del servicio.

Resultado esperado al final:

```
[..] OK: la API responde 200 en http://localhost:5000/api/footer.
[..] Versión activa: 2026...
Despliegue completado: versión 2026...
```

### Paso 3 — Verificar la API

```powershell
(Invoke-WebRequest -UseBasicParsing https://api.gpsrolandoplazas.com/api/site/layout).StatusCode
```
→ `200`

```powershell
try { Invoke-WebRequest -UseBasicParsing -Method Post https://api.gpsrolandoplazas.com/api/home } catch { $_.Exception.Response.StatusCode.value__ }
```
→ `401` (las escrituras ya exigen token)

Luego, en el sitio web y en el CMS:

- [ ] El sitio carga (header, footer, home, productos).
- [ ] Iniciar sesión en el CMS funciona.
- [ ] Editar un texto en el CMS se refleja en el sitio **de inmediato** (la caché se invalida al guardar).
- [ ] El formulario de contacto envía y llega el correo.

Si algo falla: [Volver atrás](#3-volver-atrás).

### Paso 4 — Aplicar la configuración de nginx

> Hazlo **después** del Paso 2: nginx sirve las imágenes desde `~/miapi-shared`, que crea el primer despliegue.

**4.1 Subir el archivo** (desde tu PC):

```powershell
scp -i "C:\Users\DevSenior1\Documents\Desarrollos\Hunter Institucional\api-cms-hunter.pem" deploy\nginx\miapi.conf ubuntu@77.113.141.45:/tmp/miapi.conf
```

**4.2 En el servidor**, comparar las rutas de certificados con las actuales (deben coincidir con las de `deploy/nginx/miapi.conf`):

```bash
sudo grep -nE 'ssl_certificate|options-ssl|ssl_dhparam|server_name' /etc/nginx/sites-available/miapi
```

**4.3 Respaldar, reemplazar, validar y recargar:**

```bash
sudo cp /etc/nginx/sites-available/miapi /etc/nginx/sites-available/miapi.bak-$(date +%F)
sudo cp /tmp/miapi.conf /etc/nginx/sites-available/miapi
sudo nginx -t && sudo systemctl reload nginx
```

- Si `nginx -t` muestra un error, **no se recarga nada**: el sitio sigue con la configuración anterior. Restaura el respaldo (ver 3.2) y revisa el mensaje.
- `reload` no corta conexiones.

**4.4 Verificar** (desde tu PC):

```powershell
curl.exe -sI https://api.gpsrolandoplazas.com/api/footer
```
→ `HTTP/2 200`, `strict-transport-security`, sin versión en `server: nginx`.

```powershell
curl.exe -sI https://api.gpsrolandoplazas.com/uploads/settings/logo.jpeg
```
→ `HTTP/2 200` y `cache-control: max-age=604800` (usa una imagen que exista; si responde **403**, ver Problemas comunes).

En el navegador: DevTools → Red → columna **Protocolo** = `h2`.

**4.5 Confirmar que la renovación del certificado sigue funcionando:**

```bash
sudo certbot renew --dry-run
```

---

## 3. Volver atrás

### 3.1 La API (versión anterior)

```powershell
.\deploy\deploy.ps1 -Rollback
```

Cada ejecución retrocede una versión. Para ver la activa y las disponibles:

```powershell
.\deploy\deploy.ps1 -Status
```

Para regresar a como estaba **antes de todo el proceso**, ejecuta `-Rollback` hasta que la activa sea `00000000000000-legacy`.

### 3.2 nginx (configuración anterior)

```bash
sudo cp /etc/nginx/sites-available/miapi.bak-AAAA-MM-DD /etc/nginx/sites-available/miapi
sudo nginx -t && sudo systemctl reload nginx
```

Con la configuración anterior, Kestrel vuelve a servir `/uploads/` (las versiones tienen el enlace a `~/miapi-shared`), así que las imágenes siguen funcionando.

---

## 4. Operación diaria

| Acción | Comando |
|--------|---------|
| Desplegar | `.\deploy\deploy.ps1` |
| Volver a la versión anterior | `.\deploy\deploy.ps1 -Rollback` |
| Ver versión activa | `.\deploy\deploy.ps1 -Status` |
| Desplegar desde otra rama (excepcional) | `.\deploy\deploy.ps1 -Force` |
| Logs de la API (servidor) | `sudo journalctl -u miapi -n 50 --no-pager` |
| Logs en vivo | `sudo journalctl -u miapi -f` |
| Errores de nginx | `sudo tail -n 50 /var/log/nginx/error.log` |

El `deploy.ps1` anterior (el de Descargas, con `scp -r`) **ya no debe usarse**: copia archivo por archivo con la API detenida y puede sobrescribir imágenes.

---

## 5. Problemas comunes

| Síntoma | Causa | Solución |
|---------|-------|----------|
| `ssh: connect ... Connection timed out` | Cambió tu IP pública | EC2 → Grupo de seguridad → regla SSH (22) → Origen: **Mi IP** |
| `Jwt__Key falta o tiene menos de 32 caracteres` | `/etc/miapi.env` incompleto | Ver 1.1. No se tocó nada en el servidor |
| `Estás en la rama '...' , no en 'main'` | Protección del script | `git checkout main`, o `-Force` si es intencional |
| `La nueva versión NO respondió en 30 s` | La API no arranca (ver el log que imprime) | Ya volvió sola a la anterior. Corregir y desplegar de nuevo |
| `/uploads/...` responde **403** | nginx no tiene permiso de lectura | En el servidor: `chmod o+x ~ ~/miapi-shared && chmod -R o+rX ~/miapi-shared/wwwroot-uploads` (el deploy lo hace solo) |
| Subida desde el CMS falla con **413** | Archivo > 10 MB (o nginx sin la config nueva) | La API acepta hasta 5 MB; aplicar el Paso 4 |
| **502 Bad Gateway** | La API está detenida | `sudo systemctl status miapi --no-pager` y revisar el log |
| `Access denied` / no conecta a la base | Credenciales o grupo de seguridad de RDS | Revisar `ConnectionStrings__DefaultConnection` en `/etc/miapi.env` |
