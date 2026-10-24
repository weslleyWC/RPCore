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
drag_source = a.reference_source / 'PSRP/_ACTIVITY__Drag_Racing.cs'
drag_config = Path(__file__).resolve().parents[1] / 'config/Jobs/Drag.json'
if drag_source.exists() and drag_config.exists():
    source = drag_source.read_text(); config = json.loads(drag_config.read_text())
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f?, ([-\d.]+)f?, ([-\d.]+)f?\)', source)}
    points = [config[k] for k in ['Entry','PlayerStart','RivalStart','PlayerFinish','RivalFinish']]
    report['drag'] = {
        'reference_source': 'PSRP/_ACTIVITY__Drag_Racing.cs',
        'all_points_match_reference': all(tuple(p[k] for k in ['X','Y','Z']) in reference for p in points),
        'reference_has_false_start': 'DISQUALIFED, FALSE START' in source,
        'reference_has_double_bet_payout': 'IncreaseCashBalance(Vehicle1DragRace.Value * 2)' in source,
        'reference_has_straight_line_ai': 'VehicleDrivingFlags.ForceStraightLine' in source,
        'rpcore_cycle': 'Own one-vs-one event, own player car/manual alignment, native rival AI, countdown/false start, interpolated finish gates, optional bank escrow/result/recovery.',
        'point_count': len(points), 'original_banner': 'PSRP_Banners/DragBanner',
        'scope_limits': 'No spectator bets, multi-racer meet, flare starter, tire warmup/grip changes, nitro, cameras or global vehicle cleanup in this delivery; refinement phase 9.',
        'wallet_checkpoint': 'Wallet.json/Drag + Bank; persisted result before settlement',
        'configuration_sha256': digest(drag_config.read_bytes())}
pdm_source = a.reference_source / 'PSRP/PDMCompany.cs'
pdm_config = Path(__file__).resolve().parents[1] / 'config/Jobs/Pdm.json'
if pdm_source.exists() and pdm_config.exists():
    source = pdm_source.read_text(); config = json.loads(pdm_config.read_text())
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f?, ([-\d.]+)f?, ([-\d.]+)f?\)', source)}
    points = [config['Office'], config['Spawn']] + config['Customers']
    report['pdm'] = {
        'reference_source': 'PSRP/PDMCompany.cs',
        'all_points_match_reference': all(tuple(p[k] for k in ['X','Y','Z']) in reference for p in points),
        'office_origin': 'Reference safe-collection NPC external point used as RPCore public company desk.',
        'reference_has_stock_delivery': 'UpdateDeliveryState' in source and 'PDMSavedStockVehicles' in source,
        'reference_has_business_cash': 'PDMBusinessBankCurrency' in source,
        'reference_purchase_price': 3200000, 'rpcore_purchase_price': config['PurchasePrice'],
        'rpcore_cycle': 'Own company acquisition, separate business cash, capital/withdrawals, paid virtual stock, physical NPC delivery/keys/sale, damage/repairs, recovery with handle/model/plate checks.',
        'customer_count': len(config['Customers']), 'catalog_count': len(config['Catalog']),
        'scope_limits': 'Port collection and one-car external display added in phase 6.1B; staff management/payroll and economic upgrades added in phase 6.1C; full showroom, remaining staff AI, safe cash collection added in phase 6.1D; finance/repossession, rentals and other safe mechanics remain subsequent subphases.',
        'wallet_checkpoint': 'Wallet.json/Pdm + Bank, personal Transport retained',
        'configuration_sha256': digest(pdm_config.read_bytes())}
logistics_config = Path(__file__).resolve().parents[1] / 'config/Jobs/PdmLogistics.json'
if pdm_source.exists() and logistics_config.exists():
    source = pdm_source.read_text(); config = json.loads(logistics_config.read_text())
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f?, ([-\d.]+)f?, ([-\d.]+)f?\)', source)}
    report['pdm_logistics'] = {
        'reference_source': 'PSRP/PDMCompany.cs',
        'all_points_match_reference': all(tuple(config[n][k] for k in ['X','Y','Z']) in reference for n in ['Trailer','Dock','Return','Showroom']),
        'all_slots_match_reference': all(tuple(slot[k] for k in ['X','Y','Z']) in reference for slot in config['Slots']),
        'reference_has_packer_trflat': 'VehicleHash.Packer' in source and 'VehicleHash.TRFlat' in source,
        'reference_has_port_collection': 'SpawnOrderVehicles' in source and 'TwoCarCollectionStates.ReturnToPDM' in source,
        'truck_model': config['TruckModel'], 'trailer_model': config['TrailerModel'], 'capacity': len(config['Slots']),
        'rpcore_cycle': 'Own pending paid orders, Packer/TRFlat coupling, drive each of 1-2 exact stock cars to trailer and exit/E attach, return/stopped timed receiving, durable arrival then ready stock, one-car external display.',
        'compatibility': 'Old stock defaults ready; existing customer deliveries unchanged. New UI orders pending. No extra collection payment or second purchase charge.',
        'recovery': 'Persist handle/model/plate of rig and exact stock cargo; occupied/moving assets wait. Confirmed arrival receives, interrupted unconfirmed orders stay paid/pending.',
        'scope_limits': 'No five-car TR2, manual ramps, full physical showroom, staffing, finance/repossession/rental/safe logistics; phases 8 and 9 retained.',
        'configuration_sha256': digest(logistics_config.read_bytes())}
staff_config = Path(__file__).resolve().parents[1] / 'config/Jobs/PdmStaff.json'
if pdm_source.exists() and staff_config.exists():
    source = pdm_source.read_text(); config = json.loads(staff_config.read_text())
    report['pdm_staff'] = {
        'reference_source': 'PSRP/PDMCompany.cs',
        'reference_has_staff_management': 'StaffManagement.Opening' in source,
        'reference_has_mechanic_and_marketing': all(x in source for x in ['OwnsPDMMechanic','OwnsPDMJuniourSMManger','OwnsPDMSeniorSMManger','OwnsPDMDirectorSMManger']),
        'reference_has_payroll_and_former_debt': 'RegisterWages' in source and 'PDMWagesOwedToFiredStaff' in source,
        'reference_payroll': 'Accrual after seven employed game days; company cash pays wages; junior/senior/director hierarchy and former staff debt.',
        'rpcore_cycle': 'Own four-role employment, snapshot terms, active-time payroll, company-cash payments, resignation accrual with proportional wage/severance, workshop discount and highest eligible marketing bonus on completed customer sale.',
        'period_seconds': config['PeriodSeconds'], 'roles': [r['Id'] for r in config['Roles']],
        'compatibility': 'Additive Pdm/Staff; absent field means no employees/upgrades/debt. No retroactive wages, no offline accrual, no changes to personal bank/vehicles/stock or logistics.',
        'intentional_differences': 'Own prices and 300-second gameplay payroll; bonus percentages and workshop discount are RPCore rules. No claim of identical legacy staff AI, automatic sales or weekly economy.',
        'scope_limits': 'No cleaner/security/collection staff AI, visual building modifications, automatic sales, transport ownership gates, safe cash collection added in phase 6.1D; finance/repossession/rentals and interior safe mechanics remain pending. Phases 8/9 retained.',
        'configuration_sha256': digest(staff_config.read_bytes())}
safe_config = Path(__file__).resolve().parents[1] / 'config/Jobs/PdmSafe.json'
if pdm_source.exists() and safe_config.exists():
    source = pdm_source.read_text(); config = json.loads(safe_config.read_text())
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f?, ([-\d.]+)f?, ([-\d.]+)f?\)', source)}
    report['pdm_safe'] = {
        'reference_source': 'PSRP/PDMCompany.cs',
        'reference_has_safe_collection': 'HandleSafeEvent' in source and 'SafeMoneyBeingCollected' in source,
        'reference_has_stockade_and_case': 'CreateVehicle("stockade"' in source and 'prop_security_case_01' in source,
        'reference_has_personal_bank_transfer': 'IncreaseBankBalance(safeMoneyToBeCollected)' in source,
        'all_points_match_reference': all(tuple(config[n][k] for k in ['X','Y','Z']) in reference for n in ['Spawn','Parking','Handover','Bank']),
        'rpcore_cycle': 'Own physical NPC collection: reserve company cash, Stockade arrival, collector walks to public desk, eligible timed handover, boarding/departure, bank clearing, durable refund-or-credit recovery.',
        'wallet_checkpoint': 'Pdm.SafeTransfer reservation, Handed flag, then Bank + Withdrawn + close operation atomically. No revenue or reward generated.',
        'compatibility': 'Additive state, old cash/stock/staff preserved. Cash headroom invariant permits refund even near integer limit. Shared physical work gate. Existing direct withdrawals retained.',
        'identity': 'Van handle/model/unique plate; collector marker or exact remembered identity associated with authenticated van; case marker or attachment to authenticated collector.',
        'intentional_differences': 'Reserve funds at request instead of spending unreserved balance at completion; no forced NPC/van teleports or invulnerability; arrival timeout refunds before handover. Handed funds remain payable across interruption.',
        'scope_limits': 'Collection from existing company cash, not a new safe balance or interior safe/robbery/insurance simulation. Clearing follows confirmed departure, not a simulated full trip to the bank. Finance/repossession/rentals and remaining PDM subsystems still pending.',
        'configuration_sha256': digest(safe_config.read_bytes())}
a.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(f'{len(files)} reference files indexed; {a.output.name}')
