param([Parameter(Mandatory = $true)][string]$GtaPath)
$ErrorActionPreference = 'Stop'
$GtaPath = (Resolve-Path -LiteralPath $GtaPath).Path
$scripts = Join-Path $GtaPath 'scripts'
$payload = Join-Path $PSScriptRoot 'payload'
if (!(Test-Path -LiteralPath (Join-Path $GtaPath 'GTA5.exe'))) { throw 'Selecione a pasta do GTA V Legacy que contem GTA5.exe.' }
if (!(Test-Path -LiteralPath (Join-Path $GtaPath 'ScriptHookVDotNet3.dll'))) { throw 'Instale ScriptHookVDotNet Enhanced 1.1.0.6 antes do RPCore.' }
if (!(Test-Path -LiteralPath (Join-Path $scripts 'LemonUI.SHVDN3.dll'))) { throw 'Instale LemonUI 2.2 SHVDN3 na pasta scripts antes do RPCore.' }
if (!(Test-Path -LiteralPath (Join-Path $payload 'scripts/RPCore.dll'))) { throw 'Execute o instalador do ZIP de distribuicao, junto da pasta payload.' }
$jsonDll = Join-Path $scripts 'Newtonsoft.Json.dll'
if (Test-Path -LiteralPath $jsonDll) {
    $jsonVersion = [Reflection.AssemblyName]::GetAssemblyName($jsonDll).Version
    if ($jsonVersion.Major -ne 13) { throw "Newtonsoft.Json instalado e $jsonVersion. RPCore requer assembly 13.x; resolva a compatibilidade dos outros mods antes de instalar." }
}
# Detect duplicate script copies before changing anything. A renamed copy still runs in SHVDN.
$duplicates = @(Get-ChildItem -LiteralPath $scripts -File -Filter 'RPCore*.dll' | Where-Object { $_.Name -ne 'RPCore.dll' })
if ($duplicates.Count -gt 0) { throw ('Remova copias duplicadas do RPCore da pasta scripts primeiro: ' + ($duplicates.Name -join ', ')) }
$backup = Join-Path $GtaPath ('RPCore-backups/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
if (Test-Path -LiteralPath (Join-Path $scripts 'RPCore.dll')) { Copy-Item -LiteralPath (Join-Path $scripts 'RPCore.dll') -Destination $backup }
if (Test-Path -LiteralPath (Join-Path $scripts 'RPCore')) { Copy-Item -LiteralPath (Join-Path $scripts 'RPCore') -Destination $backup -Recurse }
# Only the RPCore binary is replaced; existing data and other runtime DLLs are preserved.
Get-ChildItem -LiteralPath (Join-Path $payload 'scripts') -File -Recurse | ForEach-Object {
    $relative = $_.FullName.Substring((Join-Path $payload 'scripts').Length).TrimStart([char[]]@('\', '/'))
    $destination = Join-Path $scripts $relative
    if ($relative -eq 'RPCore.dll' -or !(Test-Path -LiteralPath $destination)) {
        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
    }
}
Write-Host "RPCore instalado. Backup: $backup"
Write-Host 'Abra GTA Legacy no modo historia. J: radial. F11: configuracoes. F2: garagens. F10: perfis/banco. Leia docs/INTERFACE-0.4.0.md.'
