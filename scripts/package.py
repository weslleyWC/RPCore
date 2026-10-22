"""Build the distribution without deploying player saves over an installed game."""
from pathlib import Path
import hashlib
import json
import shutil
import zipfile

root = Path(__file__).resolve().parents[1]
dist = root / 'dist'
stage = dist / 'RPCore-update'
if stage.exists():
    shutil.rmtree(stage)
runtime = stage / 'payload/scripts'
data = runtime / 'RPCore'
data.mkdir(parents=True)
for name in ['RPCore.dll', 'Newtonsoft.Json.dll']:
    shutil.copy2(root / 'bin/scripts' / name, runtime / name)
# Provided PSRP files are retained verbatim. Live defaults are copied separately.
shutil.copytree(root / 'assets/RPCore', data / 'Legacy', ignore=shutil.ignore_patterns('Audio', 'Gangs'))
for name in ['Audio', 'Gangs']:
    shutil.copytree(root / 'assets/RPCore' / name, data / name)
for name in ['Achievements.json', 'Keys.ini', 'CustomRadioStream.xml']:
    shutil.copy2(root / 'assets/RPCore' / name, data / name)
shutil.copytree(root / 'config', data, dirs_exist_ok=True)
for name in ['Inventory', 'SaveData', 'Storage', 'StockMarket']:
    (data / name).mkdir(exist_ok=True)
shutil.copy2(root / 'scripts/Install-RPCore.ps1', stage / 'Install-RPCore.ps1')
shutil.copy2(root / 'README.md', stage / 'README.md')
shutil.copytree(root / 'docs', stage / 'docs')
manifest = {str(p.relative_to(stage)).replace('\\', '/'): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in sorted(stage.rglob('*')) if p.is_file()}
(stage / 'SHA256SUMS.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
dist.mkdir(exist_ok=True)
with zipfile.ZipFile(dist / 'RPCore-update.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted(stage.rglob('*')):
        # Preserve the empty original directories, too.
        archive.write(path, path.relative_to(stage))
with zipfile.ZipFile(dist / 'RPCore-source.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted(root.rglob('*')):
        relative = path.relative_to(root)
        if not path.is_file() or any(part in {'.git', '.deps', 'bin', 'obj', 'dist', 'TestResults', '__pycache__'} for part in relative.parts):
            continue
        archive.write(path, relative)
print(dist / 'RPCore-update.zip')
print(dist / 'RPCore-source.zip')
