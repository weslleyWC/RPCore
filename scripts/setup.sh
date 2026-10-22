#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
tool_root="${RPCORE_TOOL_ROOT:-/workspace/.tools}"
mkdir -p "$tool_root" "$repo_root/.deps"
export DOTNET_CLI_HOME="$tool_root/home"
export NUGET_PACKAGES="$tool_root/nuget"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
if [[ ! -x "$tool_root/dotnet/dotnet" ]]; then
  curl --fail --location --retry 2 https://dot.net/v1/dotnet-install.sh -o "$tool_root/dotnet-install.sh"
  bash "$tool_root/dotnet-install.sh" --version 8.0.419 --install-dir "$tool_root/dotnet" --no-path
fi
export DOTNET_ROOT="$tool_root/dotnet"
export PATH="$DOTNET_ROOT:$PATH"
python3 - "$repo_root" <<'PY'
import hashlib, pathlib, sys, urllib.request, zipfile, io
repo = pathlib.Path(sys.argv[1])
dependencies = [
 ('https://github.com/Chiheb-Bacha/ScriptHookVDotNetEnhanced/releases/download/v1.1.0.6/ScriptHookVDotNetEnhanced-v1.1.0.6.zip',
  'f10da8819fa6814fb0a04553567d2516934eac96b16af06ad8f315f0da00473a',
  {'ScriptHookVDotNet3.dll':'ScriptHookVDotNet3.dll', 'Docs/ScriptHookVDotNet3.xml':'ScriptHookVDotNet3.xml'}),
 ('https://github.com/LemonUIbyLemon/LemonUI/releases/download/v2.2/LemonUI.zip',
  'ef95724eaa3389f02d44a4c87bd9b21e4626ebf34201167b0abeba84ef810ed6',
  {'SHVDN3/LemonUI.SHVDN3.dll':'LemonUI.SHVDN3.dll', 'SHVDN3/LemonUI.SHVDN3.xml':'LemonUI.SHVDN3.xml'})]
for url, checksum, names in dependencies:
    data = urllib.request.urlopen(url, timeout=90).read()
    if hashlib.sha256(data).hexdigest() != checksum:
        raise RuntimeError('Release checksum mismatch: ' + url)
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        for member, name in names.items():
            (repo / '.deps' / name).write_bytes(archive.read(member))
PY
cd "$repo_root"
dotnet restore RPCore.csproj --locked-mode
dotnet build RPCore.csproj -c Release --no-restore
dotnet restore tests/RPCore.Tests.csproj --locked-mode
dotnet test tests/RPCore.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=DataTests.trx'
