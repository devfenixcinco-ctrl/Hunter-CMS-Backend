<#
.SYNOPSIS
    Despliega MreHunter.Api en la EC2 con versiones, verificación de salud y vuelta atrás automática.

.DESCRIPTION
    1. Verifica la rama y que no haya cambios sin commit.
    2. Publica en Release y empaqueta el resultado en un solo .tgz.
    3. Sube el paquete y remote-deploy.sh a /tmp del servidor.
    4. En el servidor: valida /etc/miapi.env, crea una versión nueva, cambia el enlace ~/miapi,
       reinicia el servicio y comprueba que la API responda 200. Si no responde, vuelve sola
       a la versión anterior.

    El sitio solo queda sin servicio durante el reinicio (1-2 s), no durante la copia de archivos.
    Las imágenes subidas y las hojas de vida viven en ~/miapi-shared y nunca se sobrescriben.

.EXAMPLE
    .\deploy\deploy.ps1                 # despliega la rama actual
.EXAMPLE
    .\deploy\deploy.ps1 -Rollback       # vuelve a la versión anterior
.EXAMPLE
    .\deploy\deploy.ps1 -Status         # muestra la versión activa y las disponibles
#>
[CmdletBinding()]
param(
    # Llave .pem de la instancia. También se puede definir con la variable de entorno HUNTER_SSH_KEY.
    [string]$KeyPath = $(if ($env:HUNTER_SSH_KEY) { $env:HUNTER_SSH_KEY } else { "C:\Users\DevSenior1\Documents\Desarrollos\Hunter Institucional\api-cms-hunter.pem" }),
    [string]$Server = "ubuntu@77.113.141.45",
    [string]$ExpectedBranch = "main",
    [int]$KeepReleases = 5,
    [switch]$Rollback,
    [switch]$Status,
    # Permite desplegar desde otra rama o con cambios sin commit (solo para casos excepcionales).
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

function Write-Step($text) { Write-Host "`n==> $text" -ForegroundColor Cyan }

function Invoke-Native {
    param([string]$File, [string[]]$Arguments, [string]$ErrorMessage)
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$ErrorMessage (código $LASTEXITCODE)" }
}

if (-not (Test-Path $KeyPath)) { throw "No se encontró la llave SSH en '$KeyPath'. Usa -KeyPath o la variable HUNTER_SSH_KEY." }

# --- Subir el script remoto (siempre con finales de línea LF, aunque git lo haya convertido a CRLF) ---
$stamp = (Get-Date).ToUniversalTime().ToString("yyyyMMddHHmmss")
$remoteScript = "/tmp/remote-deploy-$stamp.sh"
$localScript = Join-Path $env:TEMP "remote-deploy-$stamp.sh"
$content = (Get-Content (Join-Path $PSScriptRoot "remote-deploy.sh") -Raw) -replace "`r`n", "`n"
[System.IO.File]::WriteAllText($localScript, $content, (New-Object System.Text.UTF8Encoding($false)))

function Send-RemoteScript {
    Invoke-Native "scp" @("-q", "-i", $KeyPath, $localScript, "${Server}:$remoteScript") "No se pudo subir remote-deploy.sh"
}

try {
    if ($Status -or $Rollback) {
        $action = if ($Rollback) { "rollback" } else { "status" }
        Write-Step "Ejecutando '$action' en $Server"
        Send-RemoteScript
        Invoke-Native "ssh" @("-i", $KeyPath, $Server, "bash $remoteScript $action") "Falló '$action' en el servidor"
        return
    }

    # --- 1. Verificaciones locales ---
    Write-Step "Verificando el repositorio"
    $branch = (git rev-parse --abbrev-ref HEAD).Trim()
    $dirty = git status --porcelain | Where-Object { $_ -notmatch '\s(bin|obj)/' }
    Write-Host "Rama: $branch | Commit: $((git log --oneline -1).Trim())"

    if (-not $Force) {
        if ($branch -ne $ExpectedBranch) { throw "Estás en la rama '$branch', no en '$ExpectedBranch'. Usa -Force si es intencional." }
        if ($dirty) { throw "Hay cambios sin commit:`n$($dirty -join "`n")`nHaz commit o usa -Force." }
    } elseif ($dirty -or $branch -ne $ExpectedBranch) {
        Write-Warning "Desplegando con -Force desde '$branch' y/o con cambios sin commit."
    }

    # --- 2. Publicar ---
    Write-Step "Publicando en Release"
    $publishDir = Join-Path $repoRoot "publish"
    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    Invoke-Native "dotnet" @("publish", "-c", "Release", "-o", $publishDir, "--nologo", "-v", "q") "Falló dotnet publish"

    if (Test-Path (Join-Path $publishDir "wwwroot\uploads")) { throw "El publish incluye wwwroot\uploads: sobrescribiría las imágenes del servidor." }

    # --- 3. Empaquetar y subir ---
    Write-Step "Empaquetando"
    $package = Join-Path $env:TEMP "miapi-$stamp.tgz"
    Invoke-Native "tar" @("-czf", $package, "-C", $publishDir, ".") "No se pudo crear el paquete"
    Write-Host ("Paquete: {0:N1} MB" -f ((Get-Item $package).Length / 1MB))

    Write-Step "Subiendo a $Server"
    Send-RemoteScript
    Invoke-Native "scp" @("-q", "-i", $KeyPath, $package, "${Server}:/tmp/miapi-$stamp.tgz") "No se pudo subir el paquete"

    # --- 4. Activar en el servidor ---
    Write-Step "Activando la versión $stamp en el servidor"
    Invoke-Native "ssh" @("-i", $KeyPath, $Server, "bash $remoteScript deploy $stamp /tmp/miapi-$stamp.tgz $KeepReleases") "El despliegue falló en el servidor (la versión anterior se restauró si existía)"

    Write-Host "`nDespliegue completado: versión $stamp" -ForegroundColor Green
    Write-Host "Prueba: https://api.gpsrolandoplazas.com/api/site/layout"
}
finally {
    Remove-Item $localScript -ErrorAction SilentlyContinue
    if ($package) { Remove-Item $package -ErrorAction SilentlyContinue }
}
