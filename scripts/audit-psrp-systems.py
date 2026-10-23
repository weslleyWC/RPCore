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
prison_source = a.reference_source / 'PSRP/JobDeliveryDriver.cs'
prison_config = Path(__file__).resolve().parents[1] / 'config/Jobs/PrisonTransport.json'
if prison_source.exists() and prison_config.exists():
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f?, ([-\d.]+)f?, ([-\d.]+)f?\)', prison_source.read_text())}
    config = json.loads(prison_config.read_text())
    points = [config[k] for k in ['Start', 'Spawn', 'Destination']]
    report['prison_transport_points'] = {'count': len(points),
        'all_points_in_reference': all(tuple(p[k] for k in ['X', 'Y', 'Z']) in reference for p in points),
        'reference_source': 'PSRP/JobDeliveryDriver.cs', 'configuration_sha256': digest(prison_config.read_bytes())}
pilot_source = a.reference_source / 'PSRP/_JOB__Pilot.cs'
pilot_config = Path(__file__).resolve().parents[1] / 'config/Jobs/Pilot.json'
if pilot_source.exists() and pilot_config.exists():
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f?, ([-\d.]+)f?, ([-\d.]+)f?\)', pilot_source.read_text())}
    config = json.loads(pilot_config.read_text())
    points = [config[k] for k in ['Start', 'PlaneSpawn', 'PlaneDestination', 'HeliSpawn']] + config['Helipads']
    destinations = [config['PlaneDestination']] + config['Helipads']
    report['pilot_points'] = {'count': len(points), 'helipad_count': len(config['Helipads']),
        'all_points_in_reference': all(tuple(p[k] for k in ['X', 'Y', 'Z']) in reference for p in points),
        'destinations_match_reference': all(tuple(p[k] for k in ['X', 'Y', 'Z']) in reference for p in destinations),
        'public_base': {'origin': 'RPCore public airfield access fix; these three base points are not PSRP coordinates',
                        'points': {k: config[k] for k in ['Start', 'PlaneSpawn', 'HeliSpawn']}},
        'reference_source': 'PSRP/_JOB__Pilot.cs', 'configuration_sha256': digest(pilot_config.read_bytes())}
hunting_source = a.reference_source / 'PSRP/NewInventorySystem.cs'
hunting_config = Path(__file__).resolve().parents[1] / 'config/Jobs/Hunting.json'
if hunting_source.exists() and hunting_config.exists():
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f?, ([-\d.]+)f?, ([-\d.]+)f?\)', hunting_source.read_text())}
    config = json.loads(hunting_config.read_text())
    report['hunting'] = {'vendor_matches_reference': tuple(config['Vendor'][k] for k in ['X','Y','Z']) in reference,
        'reference_source': 'PSRP/NewInventorySystem.cs + PSRP/_Activity__Hunting.cs (static behavior comparison)',
        'forest_origin': 'RPCore expedition area; not asserted as a PSRP point', 'forest': config['Forest'],
        'material_count': len(config['Materials']), 'wallet_checkpoint': 'Wallet.json/Hunting',
        'configuration_sha256': digest(hunting_config.read_bytes())}
fishing_source = a.reference_source / 'PSRP/_Activity__Hunting.cs'
fishing_config = Path(__file__).resolve().parents[1] / 'config/Jobs/Fishing.json'
if fishing_source.exists() and fishing_config.exists():
    source = fishing_source.read_text()
    config = json.loads(fishing_config.read_text())
    report['fishing'] = {
        'reference_source': 'PSRP/_Activity__Hunting.cs',
        'reference_has_rod': 'prop_fishing_rod_02' in source,
        'reference_hook_phase_empty': bool(re.search(r'void SetUpHookPhase\(\)\s*\{\s*\}', source)),
        'reference_check_fishing_occurrences': len(re.findall(r'\bCheckFishing\(', source)),
        'reference_note': 'Only the method declaration was found; not a complete working fishing cycle.',
        'rpcore_cycle': 'Own implementation: paid equipment/bait, native water/ground checks, hook/reel timing, atomic catch/sale.',
        'point_origin': 'RPCore configurable public shore/pier points; not PSRP coordinates',
        'spots': config['Spots'], 'fish_count': len(config['Fish']),
        'wallet_checkpoint': 'Wallet.json/Fishing',
        'configuration_sha256': digest(fishing_config.read_bytes())}
craft_source = a.reference_source / 'PSRP/NewInventorySystem.cs'
craft_config = Path(__file__).resolve().parents[1] / 'config/Jobs/Crafting.json'
if craft_source.exists() and craft_config.exists():
    source = craft_source.read_text()
    config = json.loads(craft_config.read_text())
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f?, ([-\d.]+)f?, ([-\d.]+)f?\)', source)}
    report['crafting'] = {
        'reference_source': 'PSRP/NewInventorySystem.cs',
        'reference_recipe_entries': len(re.findall(r'new CraftingRecipe\(', source)),
        'reference_has_recipe_selection': 'GetMaxCraftAmount(CurrentRecipe)' in source,
        'station_matches_reference_point': tuple(config['Station'][k] for k in ['X','Y','Z']) in reference,
        'station_origin': 'Existing burger stand location from reference; RPCore assigns its own crafting/market service here.',
        'recipe_origin': 'RPCore hunting/fishing products and chained field kit; not copied PSRP recipes or a complete legacy item catalog.',
        'recipe_count': len(config['Recipes']), 'product_count': len(config['Products']),
        'wallet_checkpoint': 'Wallet.json/Crafting + Hunting/Fishing bags; no Inventory.json rewrite',
        'configuration_sha256': digest(craft_config.read_bytes())}
dealer_source = a.reference_source / 'PSRP/_JOB__Drug_Dealing.cs'
dealer_config = Path(__file__).resolve().parents[1] / 'config/Jobs/Dealer.json'
if dealer_source.exists() and dealer_config.exists():
    source = dealer_source.read_text()
    config = json.loads(dealer_config.read_text())
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f?, ([-\d.]+)f?, ([-\d.]+)f?\)', source)}
    report['dealer'] = {
        'reference_source': 'PSRP/_JOB__Drug_Dealing.cs',
        'supplier_matches_reference': tuple(config['Supplier'][k] for k in ['X','Y','Z']) in reference,
        'supplier_origin': 'External Stash House entrance in reference; RPCore stock contact, no interior teleport.',
        'reference_has_zone_reputation': 'AdjustZoneRep' in source and 'SaveZoneReps' in source,
        'reference_has_police_risk': 'Game.Player.WantedLevel = 1' in source,
        'reference_categories': ['Weed','Cocaine','MDMA','Lean','Crack'],
        'rpcore_cycle': 'Own stock/bank transactions, five abstract game products, owned sidewalk buyers, normal/premium negotiation, fixed offer rolls, timed handover, zone reputation and native one-star report.',
        'scope_limits': 'No synthesis recipes, interiors, rivals, robbery, plate resale or gang wars in this delivery.',
        'product_count': len(config['Products']), 'sales_per_session': config['SalesPerSession'],
        'wallet_checkpoint': 'Wallet.json/Dealer + Bank',
        'configuration_sha256': digest(dealer_config.read_bytes())}
streamer_source = a.reference_source / 'PSRP/_JOB__LiveStreamer.cs'
streamer_phone = a.reference_source / 'PSRP/CustomPhone.cs'
streamer_config = Path(__file__).resolve().parents[1] / 'config/Jobs/Streamer.json'
if streamer_source.exists() and streamer_phone.exists() and streamer_config.exists():
    source = streamer_source.read_text(); phone = streamer_phone.read_text(); config = json.loads(streamer_config.read_text())
    report['streamer'] = {
        'reference_source': 'PSRP/_JOB__LiveStreamer.cs + PSRP/CustomPhone.cs',
        'reference_has_bank_events': 'IncreaseBankBalance(7)' in source and 'BitsThisStream' in source and 'DonationMessages' in source,
        'reference_has_context_chat': 'StreamContext' in source and 'DrivingUber' in source and 'PizzaDeliveryJob' in source,
        'reference_phone_controls': 'Start/Stop Stream' in phone and 'Change Title' in phone and 'Change Username' in phone,
        'original_overlay': 'PSRP_PHONE/TwitchOverlay', 'overlay_alpha_bounds': [21,233,346,661],
        'original_phone_icon': 'PSRP_PHONE/TwitchIcon',
        'rpcore_cycle': 'Own local simulation: parallel broadcast, app/channel/title, context chat, followers/subscriptions/bits/donations, bank checkpoints, statistics and completion.',
        'duration_seconds': config['DurationSeconds'], 'block_seconds': config['TickSeconds'],
        'work_gate': 'Observed, never acquired or released; End remains reserved for physical jobs.',
        'scope_limits': 'No external Twitch connection or video recording; own bounded event economy and finite broadcast duration; detailed legacy contexts deferred to phase 9, full interface/mechanics review to phase 8.',
        'wallet_checkpoint': 'Wallet.json/Streamer + Bank; Phone.json history is secondary',
        'configuration_sha256': digest(streamer_config.read_bytes())}
a.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(f'{len(files)} reference files indexed; {a.output.name}')
