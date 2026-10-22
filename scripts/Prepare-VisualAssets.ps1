param([Parameter(Mandatory = $true)][string]$GtaPath)
$ErrorActionPreference = 'Stop'
$GtaPath = (Resolve-Path -LiteralPath $GtaPath).Path
if (!(Test-Path -LiteralPath (Join-Path $GtaPath 'GTA5.exe'))) { throw 'Selecione a pasta do GTA V Legacy com GTA5.exe.' }
$props = Join-Path $PSScriptRoot 'psrp_props.zip'
$oiv = Join-Path $PSScriptRoot 'PSRP.oiv'
if (!(Test-Path -LiteralPath $props) -or !(Test-Path -LiteralPath $oiv)) { throw 'Extraia o pacote visual completo: este script precisa de PSRP.oiv e psrp_props.zip ao lado dele.' }
if ((Get-FileHash -LiteralPath $props -Algorithm SHA256).Hash.ToLowerInvariant() -ne 'e781c8a9039b2289635808f7dc080d3ef189f3dafe2fbd9042453ece817ebf1f') { throw 'SHA-256 do pacote de props diverge. Nao sera instalado.' }
if ((Get-FileHash -LiteralPath $oiv -Algorithm SHA256).Hash.ToLowerInvariant() -ne '5435e920c9bfa84c15b53a8d9206cf8a31c993b5f22d85fae895b71611053990') { throw 'SHA-256 do OIV diverge. Nao sera instalado.' }
$destination = Join-Path $GtaPath 'mods/update/x64/dlcpacks/psrp_props'
$target = Join-Path $destination 'dlc.rpf'
$expected = '2ada13065946fce703b3abce0b813d01c270d953cb085832d659f8686d100701'
if (Test-Path -LiteralPath $target) {
    if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) { throw 'Ja existe um psrp_props/dlc.rpf diferente. Ele foi preservado. Compare a versao instalada e faca backup antes de decidir substituir manualmente.' }
    Write-Host 'O DLC de props enviado ja esta copiado corretamente.'
} else {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($props)
    $temporary = $null
    try {
        $entry = $zip.GetEntry('psrp_props/dlc.rpf')
        if ($null -eq $entry -or $entry.Length -ne 127979008) { throw 'Entrada dlc.rpf ausente ou tamanho inesperado.' }
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        $temporary = Join-Path $destination ('dlc.rpf.' + [Guid]::NewGuid().ToString('N') + '.tmp')
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $temporary, $false)
        if ((Get-FileHash -LiteralPath $temporary -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) { throw 'DLC extraido com SHA-256 incorreto.' }
        Move-Item -LiteralPath $temporary -Destination $target
        Write-Host "DLC verificado e copiado para $target"
    } finally {
        $zip.Dispose()
        if ($temporary -and (Test-Path -LiteralPath $temporary)) { Remove-Item -LiteralPath $temporary }
    }
}
Write-Host ''
Write-Host 'Ainda no OpenIV, com o GTA fechado:'
Write-Host '1. Instale PSRP.oiv pelo Package Installer, escolhendo a pasta mods.'
Write-Host '2. Em mods/update/update.rpf/common/data/dlclist.xml, acrescente UMA entrada dentro de Paths, se ainda nao existir:'
Write-Host '   <Item>dlcpacks:/psrp_props/</Item>'
Write-Host 'Preserve todas as outras entradas. Este script nao modifica update.rpf nem dlclist.xml.'
Write-Host '3. Instale a DLL RPCore 0.3.0. No jogo, use F10 > Visual assets: diagnose.'
