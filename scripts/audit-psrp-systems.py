"""Static evidence for the phase comparison; never executes either GTA script.

Pass an ILSpy project export and `ilspycmd -l c` outputs for both DLLs.
Method references demonstrate wiring, not successful gameplay or feature parity.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import zipfile

p = argparse.ArgumentParser(description=__doc__)
for name in ['reference-source', 'psrp-dll', 'rpcore-dll', 'psrp-types', 'rpcore-types', 'baseline-package', 'output']:
    p.add_argument('--' + name, type=Path, required=True)
a = p.parse_args()
def digest(data):
    return hashlib.sha256(data).hexdigest()
def identity(path):
    data = path.read_bytes()
    return {'file': path.name, 'bytes': len(data), 'sha256': digest(data)}
main = (a.reference_source / 'PSRP/Main.cs').read_text()
files = []
for path in sorted(a.reference_source.rglob('*.cs')):
    data = path.read_bytes()
    source = data.decode('utf-8-sig')
    lines = source.splitlines()
    classes = re.findall(r'^\s*(?:public |internal |private |static |sealed |abstract )*class (\w+)', source, re.M)
    methods = [{'line': n, 'name': m.group(1)} for n, line in enumerate(lines, 1)
               if (m := re.match(r'\s*(?:public|private|protected|internal)\s+(?:static\s+)?(?:[\w<>\[\],.?]+)\s+(\w+)\s*\(', line))]
    root = classes[0] if classes else path.stem
    calls = [{'line': n, 'method': m.group(1)} for n, line in enumerate(main.splitlines(), 1)
             if (m := re.search(r'\b' + re.escape(root) + r'\.Instance\.(\w+)\(', line))]
    files.append({'file': str(path.relative_to(a.reference_source)), 'sha256': digest(data),
                  'lines': len(lines), 'classes': classes, 'methods': methods, 'main_references': calls,
                  'payment_lines': [n for n, line in enumerate(lines, 1) if re.search(r'Increase(?:Cash|Bank)Balance\(', line)]})
with zipfile.ZipFile(a.baseline_package) as archive:
    baseline = archive.read('payload/scripts/RPCore.dll')
shop_source = (a.reference_source / 'PSRP/NewInventorySystem.cs').read_text()
shop_block = shop_source.split('public List<Vector3> shopLocations =', 1)[1].split('\n\t};', 1)[0]
reference_points = [list(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)', shop_block)]
shipped_shops = json.loads((Path(__file__).resolve().parents[1] / 'config/Shops.json').read_text())
shipped_points = [[s['X'], s['Y'], s['Z']] for s in shipped_shops]
report = {'scope': 'Static metadata and decompiled source only; neither DLL was executed in GTA.',
          'shop_reference': {'count': len(reference_points), 'points': reference_points, 'all_shipped_points_match_reference': shipped_points == reference_points},
          'psrp': identity(a.psrp_dll), 'rpcore': identity(a.rpcore_dll),
          'rpcore_previous_tested': {'package': a.baseline_package.name, 'dll_bytes': len(baseline), 'dll_sha256': digest(baseline)},
          'psrp_binary_classes': a.psrp_types.read_text().splitlines(),
          'rpcore_binary_classes': a.rpcore_types.read_text().splitlines(),
          'reference_files': files}
post_source = a.reference_source / 'PSRP/_JOB__Post_Op.cs'
post_config = Path(__file__).resolve().parents[1] / 'config/Jobs/PostOp.json'
if post_source.exists() and post_config.exists():
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)', post_source.read_text())}
    config = json.loads(post_config.read_text())
    report['postop_addresses'] = {'count': len(config['Addresses']),
        'all_points_in_reference': all(tuple(p[k] for k in ['X', 'Y', 'Z']) in reference for p in config['Addresses']),
        'reference_source': 'PSRP/_JOB__Post_Op.cs', 'configuration_sha256': digest(post_config.read_bytes())}
for name, source_name, keys in [
    ('Uber', '_JOB__UberDriver.cs', ['Locations']),
    ('Valet', '_JOB__Valet.cs', ['Stand', 'Arrival', 'Spawn', 'Parking', 'Exit'])]:
    source = a.reference_source / 'PSRP' / source_name
    config_file = Path(__file__).resolve().parents[1] / 'config/Jobs' / (name + '.json')
    if source.exists() and config_file.exists():
        reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)', source.read_text())}
        config = json.loads(config_file.read_text())
        points = config['Locations'] if name == 'Uber' else [config[k] for k in keys]
        report[name.lower() + '_points'] = {'count': len(points),
            'all_points_in_reference': all(tuple(p[k] for k in ['X', 'Y', 'Z']) in reference for p in points),
            'reference_source': 'PSRP/' + source_name, 'configuration_sha256': digest(config_file.read_bytes())}
freight_source = a.reference_source / 'PSRP/JobDeliveryDriver.cs'
freight_config = Path(__file__).resolve().parents[1] / 'config/Jobs/StoreDelivery.json'
if freight_source.exists() and freight_config.exists():
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)', freight_source.read_text())}
    config = json.loads(freight_config.read_text())
    points = config['Addresses'] + [config['Start'], config['Depot']]
    report['store_delivery_points'] = {'store_count': len(config['Addresses']),
        'all_points_in_reference': all(tuple(p[k] for k in ['X', 'Y', 'Z']) in reference for p in points),
        'reference_source': 'PSRP/JobDeliveryDriver.cs', 'configuration_sha256': digest(freight_config.read_bytes())}
tanker_source = a.reference_source / 'PSRP/_JOB__Gas_Delivery.cs'
tanker_config = Path(__file__).resolve().parents[1] / 'config/Jobs/FuelDelivery.json'
if tanker_source.exists() and tanker_config.exists():
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f?, ([-\d.]+)f?, ([-\d.]+)f?\)', tanker_source.read_text())}
    config = json.loads(tanker_config.read_text())
    points = config['Addresses'] + [config['Start'], config['Depot'], config['Trailer']]
    report['fuel_delivery_points'] = {'station_count': len(config['Addresses']),
        'all_points_in_reference': all(tuple(p[k] for k in ['X', 'Y', 'Z']) in reference for p in points),
        'reference_source': 'PSRP/_JOB__Gas_Delivery.cs', 'configuration_sha256': digest(tanker_config.read_bytes())}
a.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(f'{len(files)} reference files indexed; {a.output.name}')
