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
        'scope_limits': 'Port collection/external display in 6.1B, staff/payroll/upgrades in 6.1C, safe cash collection in 6.1D, regular-customer installment sales in 6.1E. Default and parked-car repossession added in 6.1F. Showroom assignment/physical displays/onsite buyer added in 6.1H. Remaining staff AI, driving/combat repo variants, interior safe mechanics and other parity differences stay pending.',
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
        'scope_limits': 'This class covers two-car port collection. No five-car TR2, manual ramps or full physical showroom. Staff, safe cash collection and regular finance are separate modules; parked-car repossession is a separate 6.1F module; driving/combat variants remain pending; rental supply is a separate 6.1G module. Phases 8/9 retained.',
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
        'scope_limits': 'Physical mechanic/cleaner services added in 6.1I; Local defensive security posts added in 6.1J; visual building modifications, automatic sales and transport ownership gates remain pending. Safe cash collection added in phase 6.1D; regular-customer finance added in phase 6.1E; parked-car repossession added in 6.1F; driving/combat variants, rental supply orders added in 6.1G; interior safe mechanics and remaining systems stay pending. Phases 8/9 retained.',
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
        'scope_limits': 'Collection from existing company cash, not a new safe balance or interior safe/robbery/insurance simulation. Clearing follows confirmed departure, not a simulated full trip to the bank. Regular-customer installment sales added in phase 6.1E; default/parked-car repossession added in 6.1F; rental supply added in 6.1G; other variants/subsystems remain pending.',
        'configuration_sha256': digest(safe_config.read_bytes())}
finance_config = Path(__file__).resolve().parents[1] / 'config/Jobs/PdmFinance.json'
if pdm_source.exists() and finance_config.exists():
    source = pdm_source.read_text(); config = json.loads(finance_config.read_text())
    report['pdm_finance'] = {
        'reference_source': 'PSRP/PDMCompany.cs',
        'reference_has_finance_generation': 'GenerateCarFinance' in source and 'IsOnFinance = true' in source,
        'reference_has_finance_cash_receipts': 'TransactionType.Finance' in source and 'PDMBusinessBankCurrency += pDMSavedStockVehicle.repoMoneyOwed' in source,
        'reference_has_default_and_repo': 'repoIndex = 1' in source and 'HandleRepoEvent' in source,
        'rpcore_cycle': 'Own optional financed physical customer delivery; downpayment + remove stock + loan + sold cost atomically; snapshot interest/terms/condition; active gameplay installment credits to company cash; exact last remainder and settled history.',
        'defaults': config,
        'wallet_checkpoint': 'Pdm.Active.Financed and Pdm.Loans. Only cash received increases Revenue; loan opening counts one sale and full stock cost. Each time/receipt checkpoint is atomic and idempotent.',
        'compatibility': 'Absent Loans means empty book; absent Financed means existing cash sale. Regular cash sales, stock/logistics/staff/safe and personal Transport retained. Installments preserve safe-refund cash headroom.',
        'intentional_differences': 'Reference finance increases outstanding amounts and randomly resolves payment/default by game-day logic. RPCore 6.1E implements amortizing regular customers with own interest/downpayment and active-time periods, no offline accrual.',
        'scope_limits': '6.1F adds deterministic per-installment risk only for new contracts and physical parked-car repossession. Old 6.1E contracts remain zero-risk. Rental supply added in 6.1G; no negotiation, early payoff or personal player loans; global refinement remains phases 8/9.',
        'configuration_sha256': digest(finance_config.read_bytes())}
repo_config = Path(__file__).resolve().parents[1] / 'config/Jobs/PdmRepo.json'
if pdm_source.exists() and repo_config.exists():
    source = pdm_source.read_text(); config = json.loads(repo_config.read_text())
    block = source.split('private Dictionary<Vector3, float> NormalRepoLocations =', 1)[1].split('\n\t};', 1)[0]
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\),\s*([-\d.]+)f', block)}
    points = [tuple(row[k] for k in ['X','Y','Z','Heading']) for row in config['Locations']]
    report['pdm_repossession'] = {
        'reference_source': 'PSRP/PDMCompany.cs',
        'all_locations_and_headings_match_reference': all(row in reference for row in points) and len(points) == len(reference),
        'reference_has_normal_repo': 'if (onNormalRepo)' in source and 'HandleRepoEvent' in source,
        'reference_has_truck_trailer_attachment': 'repoVehicle.AttachTo(repoTrailer, offsetFront' in source and 'VehicleHash.Packer' in source and 'VehicleHash.TRFlat' in source,
        'reference_has_return_to_pdm': 'repoIndex = 0' in source,
        'locations': len(points), 'default_percent_per_installment': config['DefaultPercent'],
        'rpcore_cycle': 'New loan risk snapshots; stable per-contract/payment default at maturity, payments stop, reserved recovery operation, Packer/TRFlat coupling, exact collateral spawn, empty stopped assisted loading, return/timed receiving, durable stock restoration and receivable closure.',
        'wallet_checkpoint': 'Pdm.Repo + loan Asset condition; return adds original-cost inventory, reverses that cost from SoldCost, marks Repossessed, adds unpaid nominal amount to FinanceWriteOff, closes Repo. No extra cash/bonus.',
        'compatibility': 'Old missing DefaultPercent remains zero; regular finance API retained. New risk-enabled native sales only. Existing Pdm/Finance/Logistics/Staff/Safe JSON defaults unchanged. No personal Transport conversion.',
        'recovery': 'Handle/model/plate on rig and collateral. Occupied/moving/foreign-connected assets wait. Confirmed arrival receives stock; earlier interruption leaves default open, condition saved before own cleanup.',
        'intentional_differences': 'No invincibility, teleporting, arbitrary world vehicle retrieval, global cleanup or occupied loading. Own deterministic risk and stock-cost accounting; damaged cargo supports assisted winch-style attachment.',
        'scope_limits': 'Normal parked-car variant only; driving borrowers, combat encounters, negotiable collections, precise ramp/winch physics, rental and other PDM systems remain subsequent work/refinement. Phases 8/9 retained.',
        'configuration_sha256': digest(repo_config.read_bytes())}
rental_config = Path(__file__).resolve().parents[1] / 'config/Jobs/PdmRental.json'
if pdm_source.exists() and rental_config.exists():
    source = pdm_source.read_text(); config = json.loads(rental_config.read_text())
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f?, ([-\d.]+)f?, ([-\d.]+)f?\)', source)}
    points = [config[k] for k in ['Delivery','Worker','Return']] + config['Pickups']
    report['pdm_rental_supply'] = {
        'reference_source': 'PSRP/PDMCompany.cs',
        'reference_has_two_car_orders': 'GenerateRentalOrder' in source and 'RentalOrder item = new RentalOrder(modelHash, 2, displayName)' in source,
        'reference_has_physical_delivery': 'HandleRentalDelivery' in source and 'Control.VehicleHorn' in source and 'RentalWorkerPed' in source,
        'reference_has_return_then_payment': 'priceForDeliveryRental' in source and 'RentalOrders.RemoveAll' in source and 'received ~g~' in source,
        'all_points_match_reference': all(tuple(p[k] for k in ['X','Y','Z']) in reference for p in points),
        'point_count': len(points), 'worker_model': config['WorkerModel'], 'models': config['Models'], 'price_percent': config['PricePercent'],
        'rpcore_cycle': 'Own two matching ready-stock units reserved, Packer/TRFlat coupling and assisted loading, external airport lot, horn summons buyer, stopped timed acceptance and own cargo cleanup, return rig to PDM, atomic stock/cost/revenue/payment.',
        'wallet_checkpoint': 'Pdm.Rental with exact stock IDs/cargo/rig handles, price snapshots and Delivered/Returned. Before acceptance cancel unlocks paid stock; after acceptance preserve payable order, normal payout after returned checkpoint.',
        'recovery': 'Authenticate exact rig/cargo. Before acceptance return reservation to stock; accepted with healthy rig resumes return; accepted with unavailable world rig recovers payable receipt once. Cash/counter limits retain pending order; available cash withdrawal at desk after acceptance can free capacity.',
        'compatibility': 'Absent Rental means no order. Existing Pdm/Finance/Repo/Logistics/Staff/Safe configs and personal Transport unchanged; only new config installed if absent.',
        'intentional_differences': 'Own catalogue models/rate, always-available offers conditional on two matching paid ready stock instead of random legacy daily queue. No screen fades, disabled player controls, occupied cargo attachment, invincibility or global cleanup. Not consumer vehicle rental.',
        'scope_limits': 'One two-car fleet supply contract at a time; no consumer rental/deposit/return cycle, random daily order queue, custom dialogue/audio, detailed ramp physics or full remaining PDM parity. Phases 8/9 retained.',
        'configuration_sha256': digest(rental_config.read_bytes())}
showroom_config = Path(__file__).resolve().parents[1] / 'config/Jobs/PdmShowroom.json'
if pdm_source.exists() and showroom_config.exists():
    source = pdm_source.read_text(); config = json.loads(showroom_config.read_text())
    reference = {}
    for method, prefix in [('SpawnVehicleInSlot','room'), ('SpawnVehicleInParkingLot','lot')]:
        block = source.split('private void ' + method + '(', 1)[1].split('Vehicle vehicle = World.CreateVehicle', 1)[0]
        for row in re.findall(r'case (\d+):\s*position = new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\);\s*heading = ([-\d.]+)f;', block):
            reference[prefix + row[0]] = tuple(map(float, row[1:]))
    report['pdm_showroom'] = {
        'reference_source': 'PSRP/PDMCompany.cs',
        'all_slot_positions_and_headings_match_reference': len(config['Slots']) == len(reference) and all(tuple(row['Point'][k] for k in ['X','Y','Z','Heading']) == reference.get(row['Id']) for row in config['Slots']),
        'reference_has_slot_management': 'ShowroomSpots.Add(Slot12)' in source and 'slotIndex = 1' in source,
        'reference_has_showroom_sales': 'SellRandomPDMVehicleOnShowroomFloor' in source,
        'slot_count': len(config['Slots']), 'showroom_numbered_slots': 12, 'parking_slots': 7,
        'rpcore_cycle': 'Persistent exact stock/slot/vehicle assignment, fixed collision/ground/interior checks, protected reserved temporary displays with price cards, selective recollection, own public-desk buyer/handover/sale and departure cleanup.',
        'wallet_checkpoint': 'Pdm.Showroom and ShowroomSale; onsite sale credits cash/revenue/cost, removes exact stock/slot and closes intent together. No charge for assignment/removal and no free stock.',
        'compatibility': 'Missing fields initialize empty. Existing cash/loans/repo/rental/staff/safe and personal Transport preserved. Delivery/repair recollect only selected displayed stock; rental supply excludes assigned stock.',
        'recovery': 'Handle/model/plate authentication; nearby respawn only if old entity absent and bay free; far cleanup retains slot metadata. Interrupted unpaid buyer cancels intent and preserves inventory/assignment; paid sale cannot return to stock.',
        'intentional_differences': 'No global showroom/parking generator deletion or forced doors/IPL activation. Numbered slots 9-11 use exterior checks per their lower reference level. Own explicit buyer event at public desk instead of random automatic showroom sale.',
        'scope_limits': 'No preview cameras, cover-cloth system, automatic reputation/random sales, forced interior access or full building/staff upgrades. Interior/floor readiness and occupied bays can leave display pending; phases 8/9 retained.',
        'configuration_sha256': digest(showroom_config.read_bytes())}
premises_config = Path(__file__).resolve().parents[1] / 'config/Jobs/PdmPremises.json'
if pdm_source.exists() and premises_config.exists():
    source = pdm_source.read_text(); config = json.loads(premises_config.read_text())
    reference = {tuple(map(float, point)) for point in re.findall(r'new Vector3\(([-\d.]+)f?, ([-\d.]+)f?, ([-\d.]+)f?\)', source)}
    report['pdm_premises'] = {
        'reference_source': 'PSRP/PDMCompany.cs',
        'reference_has_mechanic_cleaner': 'SpawnMechanicPed' in source and 'SpawnCleanerPed' in source,
        'reference_has_cleaner_sequence': 'CreateCleanerSequence' in source and 'amb@world_human_maid_clean@idle_a' in source,
        'reference_has_stock_dirt': 'DirtLevel += num2' in source,
        'all_points_match_reference': all(tuple(config[n][k] for k in ['X','Y','Z']) in reference for n in ['Mechanic','Cleaner']),
        'rpcore_cycle': 'Own hired/paid mechanic and cleaner NPCs, selected displayed-stock service request, walk/animation, timed atomic repair or cleaning, company expense only on repair completion, preserved slot/stock and interrupted-intent recovery.',
        'wallet_checkpoint': 'Additive Pdm.PremisesService, DirtSeconds and Stock.Dirt. Snapshot employee/price/handle; commit stock condition, expense and close intent together; no charge or partial condition change on cancellation.',
        'compatibility': 'Old stock defaults to zero dirt; prior contracts retain saved prices. Existing four-role staff config is cloned and cleaner offer merged in memory; no rewrite/reset of custom wages, saves or visual resources.',
        'defaults': config,
        'intentional_differences': 'Own active-time dirt: +1 per 60 seconds to displayed units, max 15, 0.5 percent quote penalty per level. Explicit car cleaning/repair requests, not a port of legacy floor/trash chore AI. Paid cleaner salary but no per-clean fee. Reference points/models/cleaner animation guide own code; no copying source.',
        'scope_limits': 'Defensive guards added in 6.1J. No global relationship changes, forced interiors, arbitrary pedestrian removal, automatic floor/bin cleaning or full staff parity. Native paths/animations and interior access require GTA testing. Phases 8/9 retained.',
        'configuration_sha256': digest(premises_config.read_bytes())}
security_config = Path(__file__).resolve().parents[1] / 'config/Jobs/PdmSecurity.json'
if pdm_source.exists() and security_config.exists():
    source = pdm_source.read_text(); config = json.loads(security_config.read_text())
    reference = {}
    for n in [1, 2]:
        block = source.split('public void SpawnSecurity' + str(n) + 'Ped()', 1)[1].split('catch (', 1)[0]
        m = re.search(r'World.CreatePed\(PedHash.(\w+), new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\), ([-\d.]+)f\)', block)
        if m: reference['security' + str(n)] = tuple(map(float, m.groups()[1:]))
    report['pdm_security'] = {
        'reference_source': 'PSRP/PDMCompany.cs',
        'reference_has_two_guard_posts': 'SpawnSecurity1Ped' in source and 'SpawnSecurity2Ped' in source,
        'reference_has_armed_area_defense': 'TASK_COMBAT_HATED_TARGETS_IN_AREA' in source and 'WeaponHash.CarbineRifle' in source,
        'reference_has_guard_payroll': 'PDMSecurity1WagesOwed += 1300' in source and 'PDMSecurity2WagesOwed += 1300' in source,
        'all_points_and_headings_match_reference': len(reference) == 2 and all(tuple(p['Point'][k] for k in ['X','Y','Z','Heading']) == reference.get(p['Offer']['Id']) for p in config['Posts']),
        'rpcore_cycle': 'Two optional staff contracts, own NPC guard posts, paid-duty weapon state, visible local ambient attackers only, bounded engagement and walking return, recall from menu, delayed own replacement after death.',
        'compatibility': 'No new wallet operation or automatic guard hire; existing staff payroll/checkpoints/severance reused. Old staff configuration cloned and missing offers merged in memory. No rewriting old wages, sales or stock.',
        'defaults': config,
        'intentional_differences': 'Own hire price 1300 and wage 260 per default active 300 seconds. Finite 120-round rifle, no dropped weapon, no invincibility, no arbitrary pedestrian deletion or global relationship groups. Stops on wanted/mission/cutscene/death, no police/SWAT/army/player/script-mission targets and no pursuit outside per-post radius.',
        'scope_limits': 'Does not spawn attackers or simulate theft/insurance. Physical CCTV/own live viewer added in 6.1K; administrative safe exposure and business insurance added in 6.1L. Does not clear wanted or replace police. Combat, line of sight, safe spawn and navmesh require GTA testing. No claim of complete PDM parity; phases 8/9 retained.',
        'configuration_sha256': digest(security_config.read_bytes())}
surveillance_config = Path(__file__).resolve().parents[1] / 'config/Jobs/PdmSurveillance.json'
if pdm_source.exists() and surveillance_config.exists():
    source = pdm_source.read_text(); config = json.loads(surveillance_config.read_text())
    block = source.split('private Dictionary<Vector3, Vector3> camSpawnpos =', 1)[1].split('\n\t};', 1)[0]
    vectors = [tuple(map(float, m)) for m in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)', block)]
    reference = [(vectors[n][0], vectors[n][1], vectors[n][2], vectors[n+1][2]) for n in range(0,len(vectors),2)]
    report['pdm_surveillance'] = {
        'reference_source': 'PSRP/PDMCompany.cs',
        'reference_has_camera_purchase_and_props': 'PurchasePDMCamera' in source and 'SpawnPDMCamerasOnLoad' in source and 'prop_cctv_cam_04c' in source,
        'reference_has_15000_company_purchase': 'PDMBusinessBankCurrency -= 15000L' in source,
        'reference_has_preview_camera': 'OwnsPDMCameras.Selected' in source and 'SlotCamera.PointAt' in source,
        'all_mount_positions_and_rotations_match_reference': len(config['Cameras']) == len(reference) and all(tuple(c['Mount'][k] for k in ['X','Y','Z','Heading']) == r for c,r in zip(config['Cameras'],reference)),
        'rpcore_cycle': 'One-time optional company purchase, three own physical CCTV props, preservation of existing equivalent map props, office-only live scripted camera viewer, feed cycling and bounded modal session with cleanup.',
        'wallet_checkpoint': 'Additive Pdm.Surveillance Purchased/Paid; company cash, existing staff UpgradeSpent and receipt committed atomically. No monthly charge, insurance payout, stock change or financial bonus.',
        'compatibility': 'Missing state means unpurchased. New configuration only; previous staff/security/showroom configs unchanged. Only owned props/camera removed. Refuses opening over another camera and does not stop another identified rendering camera on exit.',
        'defaults': config,
        'intentional_differences': 'Reference camera purchase/props/preview used as static evidence. Multi-feed live viewer and framing are own additions; no claim that legacy preview was a CCTV gameplay viewer. No world prop sweep, forced player relocation, invincibility, timecycle effects or persistent control changes.',
        'scope_limits': 'No recording/playback or remote phone access. 6.1L adds explicit administrative safe exposure, insurance and contractual guard/camera risk reduction; no physical theft mission. Native render transitions, framing and interaction with other camera mods require GTA testing. Phases 8/9 retained.',
        'configuration_sha256': digest(surveillance_config.read_bytes())}
risk_config = Path(__file__).resolve().parents[1] / 'config/Jobs/PdmRisk.json'
if pdm_source.exists() and risk_config.exists():
    source = pdm_source.read_text(); config = json.loads(risk_config.read_text())
    report['pdm_risk_insurance'] = {
        'reference_source': 'PSRP/PDMCompany.cs',
        'reference_has_open_safe_away_risk': 'if (safeOpen && Game.Player.Character.Position.DistanceTo(PDMCenterSpot) >= 150f)' in source and 'TransactionType.SafeRobbery' in source,
        'reference_has_twenty_percent_loss_and_threshold': 'num18 > 50000' in source and '(double)num18 * 0.2' in source,
        'reference_has_full_loss_payout_only_if_covered': 'num20 >= (int)num19' in source and 'TransactionType.InsurancePayout' in source,
        'reference_has_renewal_and_insufficient_cash_lapse': 'TransactionType.InsurancePayment' in source and 'Insurance Cancelled due to lack of funds for payment.' in source,
        'reference_has_security_30_35_35': 'securityRating += 30' in source and source.count('securityRating += 35') == 2,
        'rpcore_cycle': 'Optional open/close administrative safe, consecutive active away exposure, stable per-check roll, contractual CCTV/paid-guard risk reduction, real loss and covered reimbursement atomically, safe closes after event, bounded history. Optional paid policy snapshots, active-time renewal, cancellation and lapse.',
        'wallet_checkpoint': 'Additive Pdm.Risk; same existing company cash, no second safe balance. Premiums, Stolen/Reimbursed totals and policy/incident/check receipt checkpoint together. Claim cannot create profit or be requested without a loss.',
        'compatibility': 'Old state means safe closed and no policy, with no new risk or auto-premium. Unknown fields retained. Old staff/security/surveillance/finance/stock configs unchanged; PdmRisk.json only new config.',
        'defaults': config,
        'intentional_differences': 'Own 120-second consecutive exposure and 1800 active-second policy periods, no offline accrual. Initial premium paid immediately; stable roll instead of legacy reused random variable. Tier 5 uses displayed 80000 premium and explicit 2-million cap rather than legacy 85000 recurring fee/200-million internal cap. 30/35/35 rating drives own risk multiplier and requires guards paid, not just hired.',
        'scope_limits': 'Administrative open-safe risk only. No safe prop/interior animation, spawned robber, stock theft, general vehicle insurance or physical heist. No teleport, wanted manipulation or new police AI. Phases 8/9 and other PDM differences remain.',
        'configuration_sha256': digest(risk_config.read_bytes())}
housing_source = a.reference_source / 'PSRP/Apartments.cs'
housing_config = Path(__file__).resolve().parents[1] / 'config/Jobs/Housing.json'
if housing_source.exists() and housing_config.exists():
    source = housing_source.read_text(); config = json.loads(housing_config.read_text())
    points = {tuple(map(float, p)) for p in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)', source)}
    report['housing_eclipse'] = {
        'reference_source': 'PSRP/Apartments.cs',
        'reference_has_purchase_and_bank_debit': 'DecreaseBankBalance(1425000)' in source and 'OwnEclipseApartment = true' in source,
        'reference_has_entry_exit_and_ipl': 'insideEclipse = true' in source and 'insideEclipse = false' in source and 'apa_v_mp_h_01_c' in source,
        'all_positions_match_reference': all(tuple(config[n][k] for k in ['X','Y','Z']) in points for n in ['Entrance','Interior','Exit']),
        'reference_wiring': 'No external Apartments.Instance call found in the 75-file export. Class logic is static evidence; RPCore explicitly registers its own HousingSystem in Main.',
        'rpcore_cycle': 'Own F10 housing menu and safehouse map icon, physical entrance interaction, optional atomic personal-bank purchase, profile ownership snapshot, asynchronous interior/floor probe, free entry/exit and public escape for non-owner profile.',
        'wallet_checkpoint': 'Additive Housing.Properties and Receipts. Purchase debits Bank and ownership together, rollback on failed save; no changes to Pdm, personal vehicles, needs or installed visual resources.',
        'defaults': config,
        'compatibility': 'Missing Housing means unowned. Existing saves/configs retained. Only own blip removed on dispose. Requested IPL is not globally unloaded; no forced interior entity-set changes, invincibility or persistent player control changes.',
        'intentional_differences': 'Reference purchase invokes IPL loader but does not automatically enter; RPCore requires a separate entry action and verifies floor readiness. Ownership checks prevent repeat charges. Exit remains available to a profile already inside, including with wanted level unchanged.',
        'scope_limits': 'Eclipse ownership and access only. Wardrobe/clothing editor added in 6.2B. Apartment storage added in 6.2C. No rental/hotels, furniture, garage ownership or needs changes. Native IPL/floor/framing and entry/exit require GTA testing. PDM differences and phases 8/9 remain.',
        'configuration_sha256': digest(housing_config.read_bytes())}
wardrobe_source = a.reference_source / 'PSRP/MultiplayerCharacter.cs'
wardrobe_config = Path(__file__).resolve().parents[1] / 'config/Jobs/Wardrobe.json'
if wardrobe_source.exists() and wardrobe_config.exists():
    source = wardrobe_source.read_text(); apartment = housing_source.read_text(); config = json.loads(wardrobe_config.read_text())
    report['wardrobe'] = {
        'reference_source': 'PSRP/MultiplayerCharacter.cs; PSRP/Apartments.cs',
        'reference_has_capture_apply_delete': 'SaveOutfitForCharacter' in source and 'ApplyOutfit_NoSave' in source and 'DeleteOutfit' in source,
        'reference_has_components_props_and_menu': 'GET_PED_DRAWABLE_VARIATION' in source and 'GET_PED_PROP_INDEX' in source and 'RefreshOutfitMenu' in source,
        'point_matches_reference': tuple(config['Point'][k] for k in ['X','Y','Z']) == (-797.761,328.302,190.7137) and '-797.761f, 328.302f, 190.7137f' in apartment,
        'rpcore_cycle': 'Owned Eclipse wardrobe interaction/menu, per-profile/per-model saved snapshots, explicit save/replace/rename/confirmed deletion, native variation availability validation, reversible clothing/prop editor and rollback on failed apply.',
        'wallet_checkpoint': 'Additive Wallet.Wardrobe with outfits and bounded receipts. Clone/mutate/save rollback, no money changes. Unknown library/outfit/piece metadata retained on explicit replacement.',
        'compatibility': 'No automatic outfit application at startup/profile change. Never changes ped model, head component 0 or hair component 2. Keeps native palette, normalizes absent prop texture, preserves existing Housing/Pdm/needs/vehicles and configs.',
        'defaults': config,
        'intentional_differences': 'Reference captures 12 components and 8 prop slots and supports last-outfit auto-load. RPCore deliberately captures 10 clothing slots and 5 usable prop anchors, binds exact model and preflights every drawable/texture. Manual dressing only. Editor is own implementation using current ped variations.',
        'scope_limits': 'No face/head-blend, hair, tattoos, ped replacement, camera rig or clothes store economy. Shared-inventory apartment storage added in 6.2C. Native clothing previews and cancellation require GTA tests; external scripts changing clothes can still conflict. Phases 8/9 retained.',
        'configuration_sha256': digest(wardrobe_config.read_bytes())}
home_storage_config = Path(__file__).resolve().parents[1] / 'config/Jobs/HomeStorage.json'
if home_storage_config.exists():
    source = (a.reference_source / 'PSRP/NewStorageSystem.cs').read_text()
    inventory_source = (a.reference_source / 'PSRP/NewInventorySystem.cs').read_text()
    config = json.loads(home_storage_config.read_text())
    report['home_storage'] = {
        'reference_source': 'PSRP/NewStorageSystem.cs; PSRP/NewInventorySystem.cs',
        'reference_has_persistent_storage_manager': 'StorageManager' in source and 'OpenStorage(string name)' in source and 'vehicle_storages.json' in source,
        'reference_has_single_and_stack_transfer': 'MoveOneToStorage' in inventory_source and 'MoveStackToStorage' in inventory_source,
        'reference_has_stash_house_storage': 'new NewStorageSystem("Stash House Storage", stashHouseStorage)' in inventory_source,
        'rpcore_cycle': 'Owned Eclipse private container through existing StorageSystem/InventorySystem; one, up-to-five or full-stack transfer, capacities and profile/access guards, atomic candidate save with live rollback, same Inventory.json.',
        'inventory_checkpoint': 'Inventory.Items, Containers and GridSlots committed together; save failure restores original live data. Opening an empty container does not create/save data. Existing trunk/stash transfer routes use the same rollback fix, quantity one UI retained.',
        'defaults': config,
        'compatibility': 'No parallel item bags, free items, money changes, furniture deletion or save migration. Existing container IDs and unknown item IDs/fields preserved. Private session closes on lost ownership/access/profile/modal and releases activity.',
        'intentional_differences': 'PSRP stash house point is a separate interior, not Eclipse. RPCore adds its own Eclipse convenience marker and F10 access anywhere inside owned apartment, capacity 200kg and configurable home:eclipse ID. No claim that this point/capacity comes from PSRP.',
        'scope_limits': 'No chest prop/animation, household furnishing, shared multi-character stash, hotel rental or automatic legacy stash migration. Native menus/access/quantities and old F8 regressions require GTA tests. Phases 8/9 retained.',
        'configuration_sha256': digest(home_storage_config.read_bytes())}
barber_config = Path(__file__).resolve().parents[1] / 'config/Jobs/Barber.json'
if barber_config.exists():
    source = (a.reference_source / 'PSRP/MultiplayerCharacter.cs').read_text(); config = json.loads(barber_config.read_text())
    block = source.split('public List<Vector3> BarberMarkers =',1)[1].split('\n\t};',1)[0]
    reference = {tuple(map(float,p)) for p in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',block)}
    report['barber'] = {
        'reference_source': 'PSRP/MultiplayerCharacter.cs',
        'reference_has_barber_menu_and_hair_application': 'BuildBarberMenu' in source and 'private void ApplyHair()' in source and 'SET_PED_COMPONENT_VARIATION, Game.Player.Character, 2' in source,
        'reference_has_tint_and_beard': 'SET_PED_HAIR_TINT' in source and 'ApplyFacialHair' in source,
        'all_shop_positions_match_reference': len(config['Shops']) == len(reference) and all(tuple(p[k] for k in ['X','Y','Z']) in reference for p in config['Shops']),
        'rpcore_cycle': 'Seven physical barbers, F10 GPS/access, model-compatible hair/texture preview plus freemode eyes, optional heading rotation, explicit one-fee bank confirmation with saved last-look record, cancellation restoration and manual free reapply at shop.',
        'wallet_checkpoint': 'Additive Wallet.Barber Looks/Spent/Services/Receipts; bank and appearance record committed together, rollback/replay guards. Preview itself has no money or save mutations.',
        'compatibility': 'Never replaces ped, head blend, beard, tint, tattoo or clothing. Hair component 2 is independent of existing Wardrobe slots. No automatic appearance on load/profile; no persistent control/camera changes and no override of vanilla E barber interaction.',
        'native_review': 'Pinned Enhanced Hash.GET_PED_HAIR_TINT_COLOR corresponds to 0x4852FC386E2E1BB5, a palette-index-to-RGB function, not a ped tint getter. GET_PED_HEAD_OVERLAY returns overlay index only. No guessed memory offsets or incorrect getter signatures used; tint/beard kept for later subphases.',
        'defaults': config,
        'intentional_differences': 'Reference offers brows/tints/beard and MP-only styling with camera/player control changes. RPCore 6.3A limits mutable fields to safely captured component 2 and valid freemode eye color, supports available protagonist hair variations, normal camera/rotation and own configurable 150 bank fee.',
        'scope_limits': '6.3B adds confirmation-only tint/beard/brows; 6.3C extends the same service with skin/makeup overlays. 6.3D adds optional heritage and 20 facial morphs. 6.3E adds explicit freemode creation/loading in isolated slots. No tattoo library or dedicated camera. Native appearance/texture/cancellation and coexistence with vanilla barbers need GTA testing. Phases 8/9 retained.',
        'configuration_sha256': digest(barber_config.read_bytes())}
grooming_config = Path(__file__).resolve().parents[1] / 'config/Jobs/Grooming.json'
if grooming_config.exists():
    source = (a.reference_source / 'PSRP/MultiplayerCharacter.cs').read_text(); config = json.loads(grooming_config.read_text())
    report['grooming'] = {
        'reference_source': 'PSRP/MultiplayerCharacter.cs',
        'reference_has_hair_tint': 'SET_PED_HAIR_TINT' in source and 'character.HairHighlight' in source,
        'reference_has_beard_brows_style_opacity_color': 'ApplyFacialHair()' in source and 'ApplyBrows()' in source and 'SET_PED_HEAD_OVERLAY_TINT' in source,
        'rpcore_cycle': 'Freemode/ready-head-blend only, optional independent hair-tint/beard/brows groups, no character preview during selection, correct palette RGB swatches, explicit paid confirmation, durable pending/started/completed checkpoints and free resume/reapply.',
        'wallet_checkpoint': 'Additive Wallet.Grooming. Begin reserves bank fee/pending; Start is durably saved before any appearance setter; Complete merges selected groups and clears pending with no extra debit. Unstarted refund restores bank/counters; started abandonment is explicit and has no refund or invented original-color restoration.',
        'compatibility': 'Does not read RGB palette function as current ped tint. No memory offsets, forced default head blend, ped replacement, clothing or auto-load. Preserves unknown record fields and unselected live groups. Existing Barber.json and cuts/eyes service untouched.',
        'defaults': config,
        'intentional_differences': 'Reference live-preview overwrites character appearance fields. RPCore adds own 120 configurable bank fee and no live tint/overlay preview because Legacy natives do not provide complete reversible color/opacity capture. Exact previously recorded selected values are free to reapply.',
        'scope_limits': '6.3C adds skin/makeup overlays; 6.3D adds optional heritage and 20 facial morphs. 6.3E adds explicit freemode creation/loading in isolated slots; no tattoos or automatic startup appearance. Rendering/silent native setters cannot be verified in this cloud. Paid-started failure remains recoverable; never claims to refund and undo unknown colors. Phases 8/9 retained.',
        'configuration_sha256': digest(grooming_config.read_bytes())}
if grooming_config.exists():
    source = (a.reference_source / 'PSRP/MultiplayerCharacter.cs').read_text()
    report['cosmetics'] = {
        'reference_source': 'PSRP/MultiplayerCharacter.cs',
        'reference_has_skin_and_makeup': all('private void ' + name + '()' in source for name in ['ApplyBlemishes','ApplyAgeing','ApplyComplexion','ApplySunDamage','ApplyMolesFreckles','ApplyMakeup','ApplyBlush']),
        'rpcore_overlay_ids': [0,3,4,5,6,7,8,9,10,11,12],
        'rpcore_cycle': 'Extends existing Grooming draft/fee/pending lifecycle; optional independent style, none, opacity, color. All groups start unselected; no live preview or automatic startup setters. Same-model saved groups merge without dropping previous hair/beard/brows or unknown fields.',
        'palette_types': {'makeup_blush_lipstick': 2, 'chest_hair': 1, 'other_skin_overlays': 0},
        'runtime_preflight': 'Freemode/ready blend, native per-overlay style count and palette count; cosmetics use makeup RGB getter instead of hair RGB. No default face, new model or guessed current opacity/color.',
        'wallet_checkpoint': 'Existing single configurable 120 bank fee for any combination, no additional per-overlay charge. Durable begin/start/complete, free resume/reapply, unstarted refund only; old 0.35 paid orders remain readable.',
        'intentional_differences': 'Own implementation uses confirmation-only selection. Lipstick, chest hair and body marks are additional native overlay controls; reference Apply methods above evidence the seven skin/makeup groups, not complete equivalence for all additions.',
        'scope_limits': 'Freemode must already exist. Heritage and facial morph controls added in 6.3D. 6.3E adds explicit profile/model loading; no tattoos or dedicated camera. Body overlays can be hidden by clothes. No complete native opacity/color readback; rendered result needs GTA testing. Phases 8/9 retained.',
        'configuration_sha256': digest(grooming_config.read_bytes())}
if grooming_config.exists():
    source = (a.reference_source / 'PSRP/MultiplayerCharacter.cs').read_text()
    report['face_editor'] = {
        'reference_source': 'PSRP/MultiplayerCharacter.cs',
        'reference_has_heritage': 'private void ApplyHeadBlend()' in source and 'character.ShapeMix / 100f' in source and 'character.SkinMix / 100f' in source,
        'reference_has_twenty_morphs': 'MorphNames = new string[20]' in source and 'private void ApplyMorph(int index, float scale)' in source and 'Hash.SET_PED_MICRO_MORPH' in source,
        'rpcore_cycle': 'Optional Heritage and independent Morphs in the same Grooming look/order. Draft starts with no face selection. New origin/shape/skin mixtures and signed -100..100 morph values apply only after confirmation. Same-model saved values merge; free resume/reapply and one existing Grooming fee.',
        'runtime': 'Native parent first/count ranges for all four base/DLC groups; no guessed fixed 45 maximum. After durable Start, SET_PED_HEAD_BLEND_DATA with third contribution zero, then tick-driven HAS_PED_HEAD_BLEND_FINISHED wait (at least next tick, max 10s), selected morph setters and surface overlays, then durable Complete. Keeps ShiftGate lease until finish/interruption, without blocking/freeze/camera.',
        'compatibility': 'Old look/pending deserialization adds empty Morphs and null Heritage without setters or migration; preserves previous cosmetics, unknown fields, profiles and configuration. No face capture by guessed offsets. No model or profile switching. Interrupted started order stays paid and resumable.',
        'intentional_differences': 'Reference creates a male freemode and previews face directly; RPCore requires an existing freemode and makes changes explicit. Independent shape/skin origin pairs, signed native morph range and native parent catalog replace reference fixed 0..45 sliders and 0..1 morph clamp.',
        'scope_limits': '6.3E adds explicit freemode creation/loading. No live reversible preview, full current morph readback, third-parent contribution, tattoo library or dedicated camera. Heritage explicitly replaces the base face/skin blend; resulting native detail composition needs GTA testing. Full creator parity/refinement and phases 8/9 remain pending.',
        'configuration_sha256': digest(grooming_config.read_bytes())}
if grooming_config.exists():
    source = (a.reference_source / 'PSRP/MultiplayerCharacter.cs').read_text()
    report['characters'] = {
        'reference_source': 'PSRP/MultiplayerCharacter.cs',
        'reference_has_model_gender_and_name': 'Gender_ItemChanged' in source and 'mp_f_freemode_01' in source and 'PlayerNameOption_Activated' in source,
        'reference_has_character_load': 'public void LoadCharacterToPed' in source and 'Game.Player.ChangeModel(model)' in source,
        'rpcore_cycle': 'F10 create named male/female freemode slot with zero balances, atomic new directory and no overwrite; separate explicit load confirmation. Nonblocking model/head-blend wait with shared lease, flush old systems/wallet and persist selected profile before native model change, then reload new profile exactly once.',
        'appearance': 'Default two-parent base plus own confirmed Grooming groups and saved eye color. All 12 component slots and 5 props saved with body. External unknown face state is not captured or claimed reversible. Loading is explicit, never on startup or profile selection alone.',
        'equipment': 'Per-character health/armor/weapons/active components/tints/clips and one count per shared ammo type. Native resource preflight and post-restore readback. Pending card blocks autosave replacement on failure; explicit retry. Separate durable protagonist return snapshot with pending/retry flag.',
        'compatibility': 'New RP_ plus 16 lowercase hex slot IDs cannot address old profile names or escape the save root. Existing profile files never imported/copied into a new slot. Unknown card/body/component/weapon metadata retained. No starting money grant, teleport destination, map cleanup, forced camera or .oiv/.rpf changes.',
        'intentional_differences': 'Reference creator previews/replaces ped while selecting and initializes starting money/gang/location. RPCore separates profile creation from confirmed loading, uses current position, zero balances and protected equipment snapshots; no automatic load or forced gang/property/vehicle changes.',
        'scope_limits': 'No complete external ped/face/stat/movement-state capture, protagonist mission task restoration, creation camera, arbitrary add-on ped catalog, default gang/start-money choices or automatic PSRP appearance import. Model/weapon restoration and story scripts must be tested in GTA. Phases 8/9 retained.'}
insurance_config = Path(__file__).resolve().parents[1] / 'config/Jobs/VehicleInsurance.json'
if insurance_config.exists():
    source = (a.reference_source / 'PSRP/Vehicle_Insurance.cs').read_text()
    report['vehicle_insurance'] = {
        'reference_source': 'PSRP/Vehicle_Insurance.cs',
        'reference_has_enroll_cancel_management': 'SaveVehicleToPersistence' in source and 'Cancel Insurance' in source and 'InsuredVehiclesMenu' in source,
        'reference_has_10000_bank_recovery': 'DecreaseBankBalance(10000)' in source and 'IsConsideredDestroyed' in source and 'vehicle.Repair();' in source,
        'reference_preserves_fuel_oil': 'float fuelLevel = vehicle.FuelLevel;' in source and 'float oilLevel = vehicle.OilLevel;' in source and 'vehicle.FuelLevel = fuelLevel;' in source and 'vehicle.OilLevel = oilLevel;' in source,
        'rpcore_cycle': 'Optional free insurance for owned registered garage road vehicles, per-policy excess snapshot default 10000 bank, GPS, explicit policy cancellation, existing destroyed vehicle repair in place. No spawn, deletion, towing, cash payout or enrollment on update.',
        'wallet_checkpoint': 'Additive VehicleInsurance. Begin reserves excess/pending; Start saved before repair; Complete atomically updates existing Transport.Vehicles entry and claim count while retaining Transport root reference. Resume has no second debit, unstarted refund only, explicit started abandonment without refund. All save failures roll back record/book/bank.',
        'compatibility': 'Requires road/model/plate/registered ownership, unique loaded identity, empty stopped detached non-job vehicle nearby on foot. Resume also exact original handle. Garage storage/retrieval/autosave skip that pending vehicle to prevent overwrite/duplicate. Other vehicles retain old behavior. Policy cancellation never deletes garage/car/fuel/storage.',
        'resources': 'RPCore tank is authoritative when present; otherwise native fuel is snapshotted. Reapply capped by current tank, completion refuses fuel increase. Oil preserved. Existing vehicle mods/paint remain on same entity, fresh snapshot with old unknown metadata saved. InsuranceBanner already exists in installed PSRP_Banners; fallback in memory without rewriting old bindings.',
        'defaults': json.loads(insurance_config.read_text()),
        'configuration_sha256': digest(insurance_config.read_bytes()),
        'intentional_differences': 'Reference permits registering current vehicle persistence and moves/repares wreck into fixed free parking. RPCore only covers garage-owned road vehicles and repairs at current position; deliberately no deletion of nearby cars, no arbitrary persistence/duplicate replacement or forced delivery.',
        'scope_limits': 'Missing/despawned vehicle has no new replacement claim in this module. Existing garage locate/recover behavior remains. No towing actor, remote delivery, insurance payments, full policy expiry/renewal, automatic PSRP policy import, oil mechanics or general license/police changes. Native burned-wreck repair/fuel/customization needs GTA tests. Phases 8/9 retained.'}
dmv_config = Path(__file__).resolve().parents[1] / 'config/Jobs/Dmv.json'
if dmv_config.exists():
    source = (a.reference_source / 'PSRP/TheoryTests.cs').read_text(); config = json.loads(dmv_config.read_text())
    block = source.split('private List<Vector3> ClassCPracticalTestPoints',1)[1].split('private int waitTimerTest',1)[0]
    points = [[float(v) for v in match] for match in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',block)]
    report['dmv'] = {
        'reference_source': 'PSRP/TheoryTests.cs',
        'reference_has_theory_30_threshold_12': 'DecreaseBankBalance(30)' in source and 'correctAnswers >= 12' in source,
        'reference_has_practical_60_and_examiner': 'Cost: $60' in source and 'VehicleHash.Asterope' in source and 'PedHash.Bevhills01AFY' in source,
        'reference_practical_handler_empty': bool(re.search(r'ClassCPracticalTest.Activated \+= delegate\s*\{\s*\};',source)),
        'reference_practical_start_has_no_callers_in_class': source.count('StartClassCPracticalTest(')==1,
        'reference_route_points': len(points),
        'all_route_positions_match_reference': len(points)==len(config['Route']) and all(all(abs(a-b)<0.00001 for a,b in zip(p,[c['X'],c['Y'],c['Z']])) for p,c in zip(points,config['Route'])),
        'rpcore_cycle': 'F10 physical DMV class C: own 15-question Portuguese theory, 12/15 pass, persistent answer receipts and theory certificate. Prepared Asterope/non-invincible examiner before practical fee, belt/right-left indicators, all 36 ordered points, three required stops, speed/belt/damage/time assessment, return/park/exit and explicit desk license issue.',
        'wallet_checkpoint': 'Additive Dmv book. Bank fee/counters/pending/config snapshot atomic, start authorization before exam, every answer/progress saved with rollback; paid resume has no new charge and keeps points/faults/time. Before start refund, after start explicit abandonment without refund. Theory certificate survives practical failure. Final license requires report/all points/within limits.',
        'compatibility': 'Advisory document: no new gating of existing jobs/garage/insurance/pilot qualification. Shared activity lease. No teleport/freeze/player control lock/police ignore/global relationship changes. Own temporary test car reserved against garage/insurance and retired only empty/stopped; own examiner cleanup. No automatic resume, paid attempt result or license on load.',
        'intentional_differences': 'Reference practical button is empty, starter has no call in class, and route switch stalls at point 2. RPCore implements own complete assessment/finish. Uses own questions instead of source texture pages, no invincible car/examiner or forced end teleport. A/M1 remain future in reference; 6.4C implements M1 independently, not as an empty menu.',
        'defaults': config,
        'configuration_sha256': digest(dmv_config.read_bytes()),
        'scope_limits': 'This section covers class C; 6.4C adds separate M1. No automatic PSRP document import, physical document inventory item, traffic-light AI/sign recognition or new enforcement. Native route/parking/examiner/seatbelt/indicator gameplay needs GTA tests; UI phase 8 and work refinement phase 9 retained.'}
m1_config = Path(__file__).resolve().parents[1] / 'config/Jobs/DmvM1.json'
if m1_config.exists():
    source = (a.reference_source / 'PSRP/TheoryTests.cs').read_text()
    character = (a.reference_source / 'PSRP/MultiplayerCharacter.cs').read_text()
    report['dmv_m1'] = {
        'reference_source': ['PSRP/TheoryTests.cs','PSRP/MultiplayerCharacter.cs'],
        'reference_declares_m1_price_30': 'Class M1 Theory Test' in source and 'Cost: $30' in source,
        'reference_m1_is_future_placeholder': 'ClassM1TheoryTest.Description = "Coming In Later Update";' in source and 'ClassM1TheoryTest.Activated' not in source,
        'reference_has_m1_document_fields': 'HasClassM1PassCertificate' in character and 'HasClassM1DrivingLicense' in character,
        'rpcore_cycle': 'M1 selected in the same DMV menu, own motorcycle 15-question bank, separate optional DmvM1 book, PCJ 600/examiner pillion, helmet instead of seatbelt, indicators, first three road segments limited to 15 km/h then 40 km/h, all 36 ordered points/stops/return/parking/exit/issue.',
        'wallet_checkpoint': 'Shared verified transaction implementation with explicit class selector; default API and old Dmv root remain class C. Separate receipts/pending/certificates/licenses/fees, shared real bank; class-mismatched configs/pending rejected. Paid M1 resume keeps price/rules/points/faults/time, no second debit.',
        'compatibility': 'Absent class in old paid options defaults C; old Dmv.json remains byte-identical. New DmvM1.json only when missing. Class C tests/receipts/documents retained. No enforcement or jobs/garage/insurance/pilot restriction. Invalid one-class book can be left to select the other class.',
        'helmet': 'Native helmet readback gates protection; optional stopped-rider loan button uses GIVE_PED_HELMET regular flag 4096. Tracks original head prop and restores only same ped/model/exact loan head prop. Character snapshot filter excludes this supplied temporary prop while preserving all other body/equipment fields; external changes bypass filter.',
        'intentional_differences': 'Reference has M1 names/flags and a future-description menu only. Entire M1 exam/questions/PCJ/passenger/speed rules are own implementation; street coordinates adapted from reference class C, not an invented claim of a complete reference M1 route.',
        'defaults': json.loads(m1_config.read_text()),
        'configuration_sha256': digest(m1_config.read_bytes()),
        'scope_limits': 'No closed slalom/cone course, remote instructor AI, class A, full physical document inventory, PSRP certificate import or enforcement. M1 pillion/helmet/handling/15 km/h segments and hat restoration need GTA testing. Phases 8/9 retained.'}
a_config = Path(__file__).resolve().parents[1] / 'config/Jobs/DmvA.json'
if a_config.exists():
    source = (a.reference_source / 'PSRP/TheoryTests.cs').read_text()
    report['dmv_a'] = {
        'reference_source': 'PSRP/TheoryTests.cs',
        'reference_declares_a_price_30': 'ClassATheoryTest = new NativeItem("Class A Theory Test", "Cost: $30' in source,
        'reference_a_is_future_placeholder': 'ClassATheoryTest.Description = "Coming In Later Update"' in source and 'ClassATheoryTest.Activated' not in source,
        'reference_has_a_document_fields': 'HasClassAPassCertificate' in character and 'HasClassADrivingLicense' in character,
        'rpcore_cycle': 'Separate DmvA theory 30 / own 15 questions / threshold 12. Own practical 120 with Phantom, trailers2 and passenger examiner. Manual coupling, 16 ordered industrial anchors resolved to road nodes before payment, belt/signals/stops/speed/damage/time, physical reverse 5m near bay, stopped aligned trailer 2s, explicit own-pair uncouple, exit and desk issue.',
        'physical_proof': 'Stage values 0..6 preserved; appended Coupled/Reversed/Detached flags. A route gated on own rig. Bay checks XY <=4m, vertical <=3m, heading <=20 degrees, both speeds <0.5m/s. Reverse uses actual displacement <=10m/frame, negative local velocity, driver/examiner/belt and near-bay requirement. Retirement protects occupied/moving/foreign connections.',
        'compatibility': 'C and M1 configs unchanged; three books separate pending/receipts/certificates/licenses, shared bank. Restart resets physical proof for replacement rig but preserves paid rules, points, faults and elapsed time. No license enforcement added to existing jobs. Missing A config only installed when absent.',
        'intentional_differences': 'Reference only advertises future A with 30 theory price and document flags. Entire A practical/fee/questions/route/rig/assessment are own code, not a recovered functional PSRP exam. Yard base matches previously used LS GAS area; road anchors and maneuver need GTA validation.',
        'defaults': json.loads(a_config.read_text()),
        'configuration_sha256': digest(a_config.read_bytes()),
        'scope_limits': 'No traffic-sign/light recognition, physical license inventory/import, police enforcement, cone course, mirror camera or collision-free route guarantee. No subagent/game execution. Coupling, industrial road nodes/clearance/traffic, reverse/bay and mods coexistence require GTA testing. Phases 8/9 retained.'}
presence_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Presence.json'
if presence_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    creator=(a.reference_source / 'PSRP/GangCreators.cs').read_text()
    report['gang_presence']={
        'reference_source':['PSRP/GangSpawner.cs','PSRP/GangCreators.cs'],
        'reference_has_hq_distance_streaming':'if (num <= 80f)' in source and 'num > 160f' in source and 'BeginSpawn(customGang, vector)' in source,
        'reference_has_leader_and_member_queue':'SpawnPed(active, leaderModel' in source and 'foreach (GangCreators.SpawnPointData pedSpawnSpot in gang.PedSpawnSpots)' in source and 'private void ProcessQueue()' in source,
        'reference_has_post_editor':'Set As Ped Spawn Spot' in creator and 'Leader Spawn Position' in creator,
        'reference_wired_in_main':'GangSpawner.Instance.Update();' in main,
        'rpcore_cycle':'F4 explicit per-profile presence, off by default for old saves. Shared existing HQ/model/post definitions; leader and deterministic member slots, 80m start/160m retire, caps 6 per HQ/12 total, asynchronous model request and conservative floor/space checks. Owned NPCs return on foot after straying, death cooldown, leader blip and live count. Cleanup on profile/reload/disable/distance/death/mission. New spawns pause during wanted/combat; existing actors keep native responses. Owned NPCs are explicitly unarmed and do not drop weapons.',
        'created_gang_editor':'Affiliated gang in RPCoreCreated.json only; set leader, append member or remove last member in F4, on foot within 70m. Atomic definition write before replacing runtime data; clone preserves unknown gang/spot/model/vehicle/relationship metadata. Existing fields and CustomGangs.json not rewritten.',
        'intentional_differences':'Owned human pedestrians use per-ped civilian relationship group without global hostility/weapon grants. No gang war, capture/economy, initiation, convoy/dogs, recruitment follower AI, dialogue or police integration in 6.5A. This is an initial HQ presence delivery, not complete GangSpawner parity.',
        'configuration_sha256':digest(presence_config.read_bytes()),
        'defaults':json.loads(presence_config.read_text()),
        'scope_limits':'Native model/ground/props/path/ambient response and mods coexistence require GTA tests. Definition files are shared across profiles; presence preference is per-profile. No automatic conversion of reference turf ownership or spawning in uncertain interiors. Phases 8/9 retained.'}
crew_source=Path(__file__).resolve().parents[1] / 'GangCrew.cs'
if crew_source.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    report['gang_crew']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_has_recruit_dismiss':'private void RecruitExistingGangMember(Ped ped)' in source and 'private void DismissRecruitedGangMember(Ped ped)' in source,
        'reference_limits_three_members':'MaxRecruitedGangMembers = 3' in source and 'recruitedGangMembers.Count >= 3' in source,
        'reference_excludes_hq_leader':'value.LeaderPed != ped' in source,
        'reference_detaches_recruit_from_hq_posts':'value.Posts.Remove(ped)' in source,
        'rpcore_cycle':'F4 recruit nearest own same-affiliation member within 3m, max 3, no leader/duplicate/dead/foreign recruitment. Existing actor/post key is retained and marked crew, preventing replacement while recruited. Session crew remains outside HQ radius while <=180m from player and same affiliation. Follow/wait/dismiss orders, own moving blips, three offsets, physical passenger boarding/exit.',
        'vehicle_safety':'Only stopped driveable car driven by player, within 25m, no JobVehicles reservation. Distinct vacant non-driver seats reserved 15s; DontJackAnyone and BlockSeatShuffling flags, no warp/unlock/seat setter. Cancels invalid or newly occupied seat. Exit waits stopped; dismissed moving passengers wait up to 60s then marked no longer needed rather than deleting vehicle. Anti-commandeer attribute is per owned recruit; no global group changes.',
        'persistence':'No saved ped/vehicle handles, crew membership or new monetary ledger. Existing affiliation/reputation/presence options/metadata unchanged. Crew ends on reload/profile switch/disable/death/mission/cutscene, affiliation mismatch or >180m; no teleport or auto-respawn into crew. Existing source post becomes eligible only as ordinary HQ member afterward.',
        'intentional_differences':'PSRP uses player native group, separation 9999 and combat attributes. RPCore uses scoped tasks on its own existing unarmed HQ NPCs, no takeover of player group or armed escort orders. Waiting/passenger seat allocation/exit are own additions. Driver shuffle prevention flag is documented MP-only, so no claim of game-level prevention of every native/third-party AI shuffle in Legacy.',
        'scope_limits':'No gang wars/territory/economy, armed protection, initiation/leader dialogue or individual command wheel. Native paths, doors, moving seat changes and coexistence with other followers/mods require GTA testing. Phases 8/9 retained.'}
loyalty_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Loyalty.json'
if loyalty_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    points=[[float(v) for v in m] for m in re.findall(r'AddLoyaltyPickupLocation\(new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\), ([-\d.]+)f\)',source)]
    config=json.loads(loyalty_config.read_text())
    report['gang_loyalty']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_has_two_packages_handover':'_loyaltyPackagesCollected < 2' in source and 'num >= 2500' in source and 'private void StartLoyaltyHandover()' in source,
        'reference_awards_affiliation_on_return':'PlayerGangAffiliation = _loyaltyGang.Name' in source and 'private void CompleteLoyaltyInitiation()' in source,
        'reference_contact_count':len(points),
        'all_contact_positions_headings_match_reference':len(points)==len(config['Contacts']) and all(all(abs(a-b)<0.00001 for a,b in zip(p,[c['X'],c['Y'],c['Z'],c['Heading']])) for p,c in zip(points,config['Contacts'])),
        'rpcore_cycle':'F4 optional loyalty initiation near live owned HQ leader while unaffiliated. Shared ShiftGate, two different reference contacts, own mortal unarmed contact spawned near route, E on foot within 2.5m and continuous eligible 2.5s handover, both durable pickups, return to live leader and explicit E atomic affiliation. No cash, inventory item or forced cutscene.',
        'persistence':'Optional GangState.Loyalty book with pending immutable coordinate snapshots, count 0..2 and bounded receipts. Clone/save-before-publish transaction, wrong attempt/index rejected, completion replay cannot rejoin after leaving. Suspend/END/reload/death/wanted/mission keeps confirmed packages; resume near same leader. Contact death explicitly cancels; failed save suspends preserving last persisted state.',
        'compatibility':'Legacy Join/Leave remains available when no initiation pending; no retroactive prerequisite on existing affiliation. Active/pending attempt blocks Join and post edits until complete/cancel. New Loyalty.json only copied if missing; old presence/crew/configs and all jobs/DMV retained.',
        'intentional_differences':'Reference routes return to HQ and uses victory cutscene plus invincible contact. RPCore returns to real leader, keeps contact mortal, has no forced camera/control/cutscene, persists receipts/resume and uses optional F4 for any gang with a configured leader. It does not enforce the reference Strength/Driving initiation type or replace legacy Join.',
        'configuration_sha256':digest(loyalty_config.read_bytes()),
        'scope_limits':'Native contact floor/space/animation, leader availability/input and coexistence with mods need GTA testing. No Strength/Driving initiation, full leader dialogue, gang territories/war/economy or police changes. Phases 8/9 retained.'}
strength_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Strength.json'
if strength_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    report['gang_strength']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_has_three_unarmed_fighters':'for (int i = 0; i < 3; i++)' in source and 'ped.Health = 150' in source and 'REMOVE_ALL_PED_WEAPONS' in source,
        'reference_has_health_and_arena_limits':'character.Health <= 100' in source and 'position) > 35f' in source,
        'reference_has_distinct_defeats':'_strengthDefeatedFighters.Add(i)' in source and '_strengthDefeatedFighters.Count >= 3' in source,
        'reference_has_final_membership':'private void CompleteStrengthInitiation()' in source and 'PlayerGangAffiliation = _strengthGang.Name' in source,
        'rpcore_cycle':'F4 optional Strength near live owned leader, unaffiliated, fists/health>100. Shared ShiftGate. Asynchronous model preparation and floor/space search before durable attempt and combat. Three own mortal unarmed challengers, no money/weapon drops or global relations. Distinct player-attributed knockouts, 35m center/rules, 300 active seconds, persisted approved report then explicit E at live leader to award membership once.',
        'persistence':'Optional GangState.Strength book, attempt/options/models/center/proof snapshots and bounded receipts. Clone/save-before-publish, no duplicate slot, rollback, mutually exclusive pending with loyalty, other affiliation never overwritten. Partial suspension restarts all three physical opponents on resume, no extra attempt; approved report persists for issue without respawning opponents. Failure/cancel changes only its book.',
        'intentional_differences':'Source forces unarmed selection and victory cutscene and measures 35m from HQ. RPCore preserves player weapons/controls, fails if player switches weapon, uses start position as arena center, own 300s limit and explicit leader confirmation. No forced camera/health/armor reset, native police/wanted unchanged. Damage attribution uses native recent damage flags, not a complete forensic record against third-party scripts.',
        'configuration_sha256':digest(strength_config.read_bytes()),
        'defaults':json.loads(strength_config.read_text()),
        'scope_limits':'Native hand combat/health thresholds/surrender/damage flag attribution, ped paths/floor and NPC interactions need GTA tests. No Driving initiation, full leader dialogue/territories/war/economy or police integration. Phases 8/9 retained.'}
driving_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Driving.json'
if driving_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    block=source.split('private readonly Dictionary<string, Vector3> drivingInitiationLocations',1)[1].split('private ',1)[0]
    points=[[float(v) for v in m] for m in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',block)]
    config=json.loads(driving_config.read_text())
    report['gang_driving']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_target_count':len(points),
        'all_target_positions_match_reference':len(points)==len(config['Targets']) and all(all(abs(a-b)<0.00001 for a,b in zip(p,[c['X'],c['Y'],c['Z']])) for p,c in zip(points,config['Targets'])),
        'reference_has_armed_passenger':'WeaponHash.MicroSMG, 1500' in source and 'private void UpdateDrivingCrewBoarding()' in source,
        'reference_requires_crew_kill':'GET_PED_SOURCE_OF_DEATH' in source and 'private bool WasDrivingTargetKilledByCrew' in source and 'You attacked or hit the target.' in source,
        'reference_has_escape_and_return':'DistanceTo(_drivingTargetPosition) < 150f' in source and 'DistanceTo(_drivingHQPosition) > 20f' in source and 'private void CompleteDrivingInitiation()' in source,
        'rpcore_cycle':'F4 optional driving initiation near live leader while unaffiliated. Borrow player current car without ownership/mutation/deletion; own gang passenger boards front seat physically, 25 reference target locations and rival-model target, passenger MicroSMG only against owned target. Exact killer handle plus rejection of player/car damage, escape >=150m, driver/passenger return home stopped and no wanted, durable report then E at leader membership.',
        'vehicle_and_actor_safety':'DontJackAnyone/BlockSeatShuffling, no warp/unlock/driver seat or company JobVehicles. Pin car after actual boarding; same driver/car/front passenger required until physical return. Only own NPCs/blips/models and two unique temporary relationship groups cleaned. Hostility only between private crew/target groups; no native group matrix/police manipulation. No player car reservation/deletion, player weapon grant, money or loot drop.',
        'persistence':'Optional GangState.Driving book and sequential Boarding/Target/Escape/Return/Report, clone/save-before-publish, bounded receipts/attempts/completions, immutable rules/models/home/target. Partial resume replays all physical stages with new actors and same attempt; approved report reload issues without actors. Mutual exclusion with loyalty/strength and shared jobs/DMV gate.',
        'intentional_differences':'Reference permits some exits and uses victory cutscene. RPCore requires same driver/passenger vehicle until home, parked home return, wanted cleared naturally before report, 1200 active seconds and explicit E at leader. No forced camera/cutscene or police immunity. Attribution relies on native damage/death fields and requires GTA tests with other scripts.',
        'configuration_sha256':digest(driving_config.read_bytes()),
        'scope_limits':'Native passenger doors/window fire/aim, target floor/streaming/clearance and exact kill attribution need GTA validation. No full leader dialogue, territories/war/economy or police integration. Phases 8/9 retained.'}
turf_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Territories.json'
if turf_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text();creator=(a.reference_source / 'PSRP/GangCreators.cs').read_text();config=json.loads(turf_config.read_text())
    refs=[]
    for block in source.split('private void LoadTurfs()',1)[1].split('GangTurfs.Add(new Turf')[1:]:
        ident=int(re.search(r'ID = (\d+)',block)[1]);center=[float(v) for v in re.search(r'Center = new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',block).groups()]
        pb=block.split('PedSpawnSpots =',1)[1].split('VehicleSpawnPostitions =',1)[0]
        posts=[[float(v) for v in m] for m in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\),\s*([-\d.]+)f',pb)]
        refs.append((ident,center,posts))
    report['gang_territories']={
        'reference_source':['PSRP/GangSpawner.cs','PSRP/GangCreators.cs'],
        'reference_territory_count':len(refs),
        'all_centers_and_posts_match_reference':len(refs)==len(config['Areas']) and all(ident==c['Id'] and xyz==[c['Center'][k] for k in ['X','Y','Z']] and posts==[[p[k] for k in ['X','Y','Z','Heading']] for p in c['Posts']] for (ident,xyz,posts),c in zip(refs,config['Areas'])),
        'reference_owner_bands_match': 'i <= 5' in creator and '3rd Avenue Hustlas' in creator and 'i <= 10' in creator and all(c['DefaultOwner']==('3rd Avenue Hustlas' if c['Id']<=5 else 'Eastside Kings' if c['Id']<=10 else 'Blacktop Locos') for c in config['Areas']),
        'reference_has_defender_capture':'private bool AreTurfDefendersDead(Turf turf)' in source and 'private void CaptureTurf(Turf turf)' in source and 'TurfOwnership[turf.ID] = playerGangAffiliation' in source,
        'rpcore_cycle':'F4 territory submenu, opt-in map per profile, 15 default owners/coordinates and all ped posts retained. Explicit on-foot area attack, actual previous-owner valid ped models, asynchronous preparation and safe original posts. Default 3 own armed defenders, no loot, native combat only on player. All distinct deaths persist ready checkpoint; E in area atomically captures ownership with expected-previous-owner and affiliation checks, refreshes colored owner blips.',
        'persistence':'Optional GangState.Turf Owners override dictionary plus snapshot pending/defeats/ready/receipts/counts. Initial owners resolved in memory, no autosave migration/grants on load. Partial restart requires fresh defenders, no extra attempt; ready proof persists across reload. Changed owner cannot be overwritten, failures conserve old owner, receipts prevent replay. Per-profile ownership persists after Leave; old/unknown owner IDs and metadata retained.',
        'intentional_differences':'Initial capture module only: default three pistol defenders versus full PSRP ambient forces/weapons/reinforcements. Explicit F4 attack and E capture, own 600s limit, on-foot45m center, optional map; no automatic attack trigger/cutscene/relations changes. Combat deaths of all owned defenders count, disappearance does not. No money award; 6.5H adds reputation at confirmed outcomes. No turf stock/commerce, gang AI battles or automatic counterattack.',
        'configuration_sha256':digest(turf_config.read_bytes()),
        'scope_limits':'Native combat, paths/floor, airport/restricted access, wanted, coexistence with crew/police/mods require GTA tests. No automatic PSRP TurfOwnership import, complete gang economy, reinforcement/convoy or police integration. Phases 8/9 retained.'}
defense_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Defense.json'
if defense_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    report['gang_defense']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_has_incoming_attackers':'public void StartGangTurfAttack' in source and 'private void SpawnGangTurfAttackers' in source,
        'reference_has_defense_win_and_loss':'TURF DEFENDED' in source and 'DefenseWin' in source and 'TURF LOST' in source and 'DefenseFail' in source,
        'reference_transfers_ownership_to_attacker':'TurfOwnership[turf.ID] = attackingGang.Name' in source,
        'rpcore_cycle':'Explicit F4 Defend owned area only, known affiliated gang/map/safe foot/area/shared gate. Choose valid rival models, prefer original owner if different. Own configurable two waves x three on-foot attackers from outer ring, existing pistol/mortal/no-loot combat. Next wave only after current batch dead, distinct indices/checkpoints, E confirms defense and ownership remains same. Death/vehicle/abandonment/timer during active defense transfer expected-owned area to snapshot attacker atomically.',
        'persistence':'Append-only Attack=0/Defense=1 mode, attacker/wave-size and Defenses/LostDefenses counters. Legacy pending remains Attack without fields. Snapshot preserves counts/models/radius/attacker; partial restart repeats all waves without extra attempt, ready result reload confirms without actors. Cancellation/suspension/technical model/slot/save failure, mission/cutscene/ped switch preserve owner. Changed owner/affiliation blocks loss overwrite; success/loss replay cannot recount.',
        'intentional_differences':'Reference schedules AI attacks, vehicles, allied defenders and gang-vs-gang war. RPCore 6.5G is manually requested player defense, own 1-2 waves capped at six actors, no automatic attacks/offline simulation/convoy or armed allied garrison. Loss conditions explicit in menu. 6.5G has no cash grant, police changes or old-state migration; 6.5H adds reputation and explicit funded treasury.',
        'configuration_sha256':digest(defense_config.read_bytes()),
        'defaults':json.loads(defense_config.read_text()),
        'scope_limits':'Native perimeter floor/path/combat/wave transition, actual death timing and coexistence with police/mods require GTA tests. Full automatic wars, territory economy/rank/payday, leader dialogue/police remain pending. Phases 8/9 retained.'}
treasury_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Treasury.json'
if treasury_config.exists():
    creators=(a.reference_source / 'PSRP/GangCreators.cs').read_text();source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    report['gang_progression_treasury']={
        'reference_source':['PSRP/GangCreators.cs','PSRP/GangSpawner.cs'],
        'reference_has_250_750_1500_thresholds':'playerGangReputation >= 1500' in creators and 'playerGangReputation >= 750' in creators and 'playerGangReputation < 250' in creators,
        'reference_keeps_highest_rank':'GangRank = Math.Max(num, val)' in creators,
        'reference_has_capture_defense_loss_rewards':'num = 50' in creators and 'num = 25' in creators and 'num = -20' in creators,
        'reference_has_salary_shares_and_day':'1 => 0.05' in source and '2 => 0.1' in source and '3 => 0.2' in source and '4 => 0.35' in source and 'TotalHours < 24.0' in source,
        'rpcore_progression':'Remove legacy 100 cap on progression/reload, preserve existing positive reputation. Rank1..4 with reference thresholds and no demotion within same affiliation, including legacy implicit rank. Future capture+50/defense+25/lost-defense-20 saved atomically with ownership/outcome receipt. Existing pizza contribution kept without cap. Join/Leave/new initiation reset rank with existing reputation reset, no historical rewards backfill.',
        'rpcore_treasury':'Optional Wallet.GangTreasury, per-profile/per-gang accounts. Explicit HQ bank deposit/withdrawal and manual timed rank share 5/10/20/35; default24 game hours. Treasury balance plus bank conserved, strict deposited-withdrawn-salary conservation, overflow/metadata/receipt/rollback guards. Original bank/book references restored if save fails; other wallet references untouched. Read native game clock without modifying it, rewind cannot advance payday, no offline compounding.',
        'intentional_differences':'Reference daily automatic salary comes from owned turf CleanCash. This delivery uses an own explicit bank-funded treasury until territory business cash is connected; no automatic salary, laundering, clean-cash migration or income generation. Rank affects share only, no extra ownership/crew/admin grants. No silent debits, transfers or promotions written at startup.',
        'configuration_sha256':digest(treasury_config.read_bytes()),
        'scope_limits':'Native HQ access/game calendar, protagonist bank integration and coexistence with UI/mods need GTA tests. No turf commerce/rent/passive income, laundering, global gang bank sharing between profiles, AI war or police changes. Phases 8/9 retained.'}
business_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Business.json'
if business_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    report['gang_territory_business']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_has_separate_stock_and_cash':'Dictionary<DrugTypes, int> Drugs' in source and 'Dictionary<TurfWeaponType, int> Weapons' in source and 'public int CleanCash' in source,
        'reference_has_owned_turf_collection':'private bool CollectTurfCash(Turf turf)' in source and '!PlayerOwnsTurf(turf)' in source,
        'reference_has_cafe_supply_work':'NutCafeSupplyRun' in source and 'CafeSupplyGoal = 3' in source,
        'reference_has_laundering_efficiency':'TurfLaunderingEfficiency = 0.85' in source,
        'rpcore_cycle':'Own original legal local commerce at configured owned territory centers. Explicit bank purchase stocks gang/area ledger; safe on-foot service for45 active seconds then E sells5 units into gang treasury. Shared shift gate, current owner/affiliation/profile/player checks, stock/capacity guards. Menus/pause/frame gaps do not add time; cancellation/death/vehicle/combat/wanted/abandonment/profile change discard only unconfirmed runtime time. No stock consumed until successful sale save.',
        'persistence':'Optional Wallet.GangBusiness accounts by case-insensitive gang plus area, purchased/sold/stock/revenue conservation and bounded receipts. Optional treasury BusinessIncome supports legacy zero and decimal-exact deposited+income-withdrawn-salary=balance validation without long sum overflow. Sale clones both books and commits in one Wallet save; bank and original book references restored on exception. Existing payday anchor unchanged; first new treasury income starts game-calendar interval.',
        'intentional_differences':'Foundation for territory business income, not complete PSRP economy. Generic legal merchandise and active management; no drug/weapon stock, buyers/props, cafe truck delivery, automatic sales, dirty cash, laundering or offline income. No conversion of PSRP Cash/CleanCash. Stock belongs to funding gang/profile; changed owner cannot sell rival stock. Ownership loss locks old stock until same gang regains control; no confiscation or free transfer.',
        'configuration_sha256':digest(business_config.read_bytes()),
        'defaults':json.loads(business_config.read_text()),
        'scope_limits':'Actual centers/ground access, wanted/combat detection, menu/keyboard/native timing and coexistence with installed mods need GTA validation. Work/visual refinements remain phases8/9; full criminal commerce, supply runs, laundering, automatic wars/dialogue/police pending.'}
supply_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Supply.json'
if supply_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    block=source.split('private readonly Dictionary<Vector3, float> cafeSupplyParcelLocations',1)[1].split('private const int CafeSupplyGoal',1)[0]
    points=[[float(v) for v in m] for m in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\),\s*([-\d.]+)f',block)]
    config=json.loads(supply_config.read_text())
    report['gang_supply']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_supplier_count':len(points),
        'all_supplier_positions_headings_match_reference':len(points)==len(config['Suppliers']) and all(all(abs(a-b)<0.00001 for a,b in zip(p,[c['X'],c['Y'],c['Z'],c['Heading']])) for p,c in zip(points,config['Suppliers'])),
        'reference_has_pickup_load_unload_delivery':all(t in source for t in ['PickupCafeSupplyParcel','LoadCafeSupplyParcel','UnloadCafeSupplyParcel','DeliverCafeSupplyParcel','CafeSupplyGoal = 3']),
        'reference_completion_is_laundering_boost':'turfData.LaunderingBoostMultiplier = 2f' in source and 'DateTime.UtcNow.AddHours(1.0)' in source,
        'rpcore_cycle':'Optional physical commerce freight for any owned configured territory. Driver binds own stopped unreserved car/van at destination, reserves discounted bank cost once. GPS three reference suppliers, foot E pickup with borrowed vehicle nearby, box carry using existing optional DeliveryCargo and E at rear records distinct load. Must drive bound vehicle to destination then foot rear pickup and three dropoffs; final E credits complete stock lot, no second debit, salary or treasury credit.',
        'persistence':'Optional Wallet.GangBusiness.Supply order snapshots gang/area, suppliers, cost/units/capacity/time, distinct loads and sequential delivery checkpoints. One Wallet save per bank/stock checkpoint, clone/save/publish and rollback. Shared gate and JobVehicles reservation, existing WorkContext shop input reservation around active point with owned cleanup, model/plate/handle/profile/actor/owner checks. Suspension never refunds automatically; explicit safe refund restores reserved cost once including changed gang/ownership. Partial resume resets physical proof and checkpoint receipts but preserves paid reservation; fully delivered order resumes ready without car. No entity handles resurrected from save.',
        'intentional_differences':'PSRP supplies a spawned Burrito for turf4 cafe and rewards2x laundering for60 real minutes plus MissionWin. RPCore borrows player car/van, does not spawn/delete/repair/move it, grants a discounted paid merchandise lot instead, permits configured owned areas and has no laundering or reputation reward. Carry prop respects existing Visuals flags and ups_box/vanilla fallback; no supplier world crates or vehicle-mounted crate props. Direct purchase and service preserved but blocked while freight reservation exists.',
        'configuration_sha256':digest(supply_config.read_bytes()),
        'defaults':config,
        'scope_limits':'Actual supplier/destination access, vehicle rear offsets/parking, GPS, E inputs, optional props/animations and native vehicle identity/mod coexistence need GTA testing. Automatic criminal sales/laundering, cafe-specific simulation, wars/dialogue/police and phases8/9 pending.'}
laundering_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Laundering.json'
if laundering_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    report['gang_illicit_laundering']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_street_sale_consumes_stock_and_credits_dirty':'private bool CompleteStreetDrugSale' in source and 'turfData.Drugs[drug] -= quantity' in source and 'turfData.Cash += earnings' in source,
        'reference_has_85_percent_processing':'TurfLaunderingEfficiency = 0.85' in source and 'double num4 = (double)num3 * 0.85' in source,
        'reference_has_rate_500_per_minute':'TurfLaunderingPerMinute = 500.0' in source,
        'reference_is_automatic_real_time':'private void UpdateAutomaticTurfLaundering()' in source and 'DateTime utcNow = DateTime.UtcNow' in source and 'totalSeconds = Math.Min(totalSeconds, 7200.0)' in source,
        'rpcore_cycle':'Explicit F4 owned-area territorial dealer mode reuses existing buyer/stock/quote/refusal/police/handover/session rules. Sale stock/statistics/reputation and dirty proceeds commit in one Wallet save without legacy bank payout. Normal dealer mode remains bank-funded payout, no historical sales migration. Recheck gang/profile/actor/ownership/area45m/cutscene and supply reservation; shared shift gate and owned shop-input context.',
        'processing':'Explicit safe on-foot owned-area batch up to500 dirty over60 active seconds then E credits85% to gang treasury. Fixed cumulative integer/decimal rounding gives equal net across batch sizes, exact processed=net+fee and proceeds-processed=dirty. No deposits of bank/cash as dirty source. Optional LaunderedIncome treasury field defaults0; deposits+business+laundered-withdrawn-salary=balance. First new treasury anchors game-calendar payday; existing anchor preserved. Dealer/dirty and dirty/treasury cloned separately with single real Wallet save and original references restored on exception.',
        'intentional_differences':'PSRP stores criminal stock per turf, automatic UTC laundering with7200s catchup into turf CleanCash, separate collection/salary and cafe boost. RPCore uses existing personal Dealer stock only in opt-in territorial mode, per gang/area/profile dirty ledger, active manual batch directly to treasury, no catchup/offline wages, weapons/auto dealing/cafe boost or 1:1 PSRP cash migration. Pausing/menu gaps do not add work; suspension/reload drops only unconfirmed time. Existing legal commerce/freight untouched.',
        'configuration_sha256':digest(laundering_config.read_bytes()),
        'defaults':json.loads(laundering_config.read_text()),
        'scope_limits':'Real buyer paths/offer/input/animation, safety/area/native clock, police encounters and compatibility require GTA validation. Full automatic turf economy, separate criminal warehouse/convoys, war/dialogue/police and phases8/9 remain pending.'}
warehouse_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Warehouse.json'
if warehouse_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    report['gang_criminal_warehouse']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_has_per_turf_drug_stock':'public Dictionary<DrugTypes, int> Drugs' in source,
        'reference_deposit_checks_inventory_and_removes_items':'private bool DepositDrugIntoTurf' in source and 'int inventoryAmountForDrug = GetInventoryAmountForDrug(drug)' in source and '!RemoveDrugFromInventory(drug, amount)' in source and 'turfData.Drugs[drug] += amount' in source,
        'reference_street_sale_debits_local_stock':'private bool CompleteStreetDrugSale' in source and 'turfData.Drugs[drug] -= quantity' in source and 'turfData.Cash += earnings' in source,
        'rpcore_cycle':'F4 Commerce > Criminal warehouse for owned area. Optional per gang/area/profile storage; deposit/withdraw1 or5 packages moves actual existing Dealer stock with no cash/reputation/income. Default30kg warehouse, fixed999/product and20/API transfer limit,40 keys, personal weight/stock caps checked on withdrawal. Unknown IDs preserved and weighted conservatively, need matching catalog to move. Shared gate/safe foot12m/known gang/current owner/no supply reservation for physical actions; remote consultation/GPS.',
        'warehouse_sale':'Explicit Sell from warehouse reuses Dealer buyers/quote/reputation/refusal/police/handover, checks local stock for start/offers/commit and never fills shortage from personal stock. Accounting composes validated territorial dirty sale in temporary view then commits warehouse+Dealer stats+dirty proceeds in one real Wallet save, restores personal Stock reference unchanged. Normal bank mode and previous personal-stock territorial mode preserved; mode resets on end/reload and disposed Dealer cannot restart through callback.',
        'persistence':'Optional Wallet.GangWarehouse accounts with Stock/Deposited/Withdrawn/Sold per product, exact deposited+supplied+raided-withdrawn-sold=stock using decimal counters (Supplied since6.5M, Raided since6.5N, both optional). Bounded receipts, case-insensitive gang+area keys, no automatic stock migration/grant/confiscation. Losing ownership locks old account until same gang regains it. Transfer rollback restores both original books; sale rollback restores all three references and never changes Bank/Cash/treasury/legal business/freight. Global Dealer receipts prevent cross-mode double payout.',
        'intentional_differences':'Reference uses main inventory drug types for turf deposit, fixed turf stock and separate NPC systems. RPCore uses Dealer product catalog/stock, adds explicit withdrawal and optional warehouse-sale mode while preserving old sale modes. No automatic cargo resupply, weapons, rival confiscation or automated sales. Money laundering remains the previous active85% cycle, not PSRP UTC simulation/boost.',
        'configuration_sha256':digest(warehouse_config.read_bytes()),
        'defaults':json.loads(warehouse_config.read_text()),
        'scope_limits':'Actual UI/GPS/area/ownership/native buyers/police/input and coexistence with mods need GTA tests. Rival-raiding logistics/weapons/auto trading, cafe boost, wars/dialogue/police and phases8/9 remain pending.'}
criminal_supply_config=Path(__file__).resolve().parents[1] / 'config/Gangs/CriminalSupply.json'
if criminal_supply_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    block=source.split('DrugSupplyRunLocations = new List<Vector3>',1)[1].split('WorkVanSpawnPosition',1)[0]
    points=[[float(v) for v in m] for m in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',block)]
    config=json.loads(criminal_supply_config.read_text())
    report['gang_criminal_supply']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_first_turf_supply_location_count':len(points),
        'all_route_positions_match_first_turf_supply_locations':len(points)==len(config['Route']['Suppliers']) and all(all(abs(a-b)<0.00001 for a,b in zip(p,[c['X'],c['Y'],c['Z']])) for p,c in zip(points,config['Route']['Suppliers'])),
        'reference_has_supply_package_and_return':'ItemDatabase.Create(ItemID.DrugPackage, 1)' in source and 'private void UpdateDrugSupplyRunReturn()' in source and 'private void CompleteDrugSupplyRun()' in source,
        'reference_supply_credits_turf_stock':'turfData.Drugs[supplyRunDrug.Key] += supplyRunDrug.Value' in source,
        'rpcore_cycle':'Own paid criminal freight selected per Dealer catalog product from F4 criminal warehouse. Default10 units and10% discount on Buy price, bank reserved once. Shared existing borrowed-car three pickup/load/drive-back/unload/confirm runtime with separate criminal route; completion credits only criminal Warehouse.Stock and Supplied, not personal Dealer/Business legal stock or money. Kind0 legacy legal unchanged; Kind1 stores SKU, price/quantity/route/capacity/radius/weight snapshots.',
        'persistence':'Same single GangBusiness.Supply slot prevents parallel legal/criminal reservations. Resume/refund/checkpoints use existing receipts and one Wallet save; partial resume resets physical proof without charging, ready remains ready. Criminal completion clones Business+Warehouse together, adds supplied+stock, clears order, rollback restores both references. Source bank/cash/Dealer/dirty/treasury untouched after reservation. Warehouse invariant becomes deposited+supplied-withdrawn-sold=stock, absent Supplied defaults empty. Existing public completion overload retained, criminal completion additionally needs current valid catalog for weight checks.',
        'capacity_and_catalog':'Validate stock999, total weight and640 accounts before bank debit and again before final credit; counters/IDs/config/unknown kind protected. Price/units/capacity/radius/points captured when paid. Completion checks current catalog weight against captured capacity; removed SKU preserved with conservative1kg weight and blocked management until catalog restores it. Over-limit/changed weight keeps ready order and explicit refund available.',
        'intentional_differences':'Reference DrugSupplyRun steals random rival dealer supplies, scales two drug types by rival turf count, consumes DrugPackage and awards MissionWin. RPCore delivery is a paid catalog purchase of one SKU with three stops, uses own car/van and recoverable financial reservation; no ambush/rival relationship penalty, random35-90 loot, weapons or reputation reward. Route reuses three first-turf candidate coordinates, not a claim of identical encounter logic.',
        'configuration_sha256':digest(criminal_supply_config.read_bytes()),
        'defaults':config,
        'scope_limits':'Native access/ground at three candidate spots, parking/rear/carry/keys/GPS, actual vehicle identity and coexistence need GTA validation. Rival raids/guards/criminal convoys, weapons/automatic restock, cafe boost, wars/dialogue/police and phases8/9 remain pending.'}
raid_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Raids.json'
if raid_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    block=source.split('DrugSupplyRunLocations = new List<Vector3>',1)[1].split('WorkVanSpawnPosition',1)[0]
    points=[[float(v) for v in m] for m in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',block)]
    config=json.loads(raid_config.read_text())
    report['gang_rival_supply_raid']={
        'reference_source':'PSRP/GangSpawner.cs',
        'all_site_positions_match_first_turf_candidates':len(points)==len(config['Sites']) and all(all(abs(a-b)<0.00001 for a,b in zip(p,[c['X'],c['Y'],c['Z']])) for p,c in zip(points,config['Sites'])),
        'reference_has_dealer_and_two_to_four_guards':'private void SpawnDrugSupplyRunEncounter()' in source and 'int num = turfRandom.Next(2, 5)' in source and 'supplyRunDealer.Armor = 25' in source and 'ped.Armor = 35' in source,
        'reference_has_two_second_body_search':'supplyRunSearchCompleteTime = Game.GameTime + 2000' in source and 'player_search' in source and 'private void CompleteDrugSupplyRunDealerSearch()' in source,
        'reference_has_two_random_drug_types':'for (int i = 0; i < 2; i++)' in source and 'turfRandom.Next(35, 90)' in source,
        'reference_return_credits_turf_stock':'private void CompleteDrugSupplyRun()' in source and 'turfData.Drugs[supplyRunDrug.Key] += supplyRunDrug.Value' in source,
        'rpcore_cycle':'Explicit F4 owned-area rival supply raid, current known gang/free gate/no freight. Snapshot rival/models, one candidate location, two catalog SKUs and loot35..90 each, default900 active seconds, capacity. Own dealer plus two guards asynchronously spawned near site, safe ground/space checks, armor25/35 accuracy30 pistol45/no weapon-money drops. Immediate player-target combat with no global relationship edits. All three distinct player-damaged deaths required, E body search2s with named optional animation, quest cargo checkpoint then escape native police and return foot E within3m of owned area.',
        'runtime_ownership':'Three own actors only, transient random decorator token validates ownership on tick/search/cleanup, no handles resurrected from saves. Model/tag/space/streaming/actor loss or foreign kill proof suspend without stock. First spawn waits for menus/modal to close. Search timing stops on distance/menu/vehicle/ragdoll; pause/frame gaps cannot complete offline. No player health/weapon/car/clock or wanted-level resets.',
        'persistence':'Optional Wallet.GangRaid pending snapshot/counters/receipts. Distinct defeat checkpoints, package only after all three; partial resume resets physical proof and receipts without reroll/extra attempt, package resume has no respawn. Completion clones Raid+Warehouse and credits Raided+stock/clears pending/completed in one Wallet save. Exact warehouse deposited+supplied+raided-withdrawn-sold=stock, missing Raided defaults0. Overflow/capacity/catalog/owner/profile guards, rollback/replay. Death/active encounter abandonment/timeout fail and discard unissued quest cargo; suspension/cancel preserve all old stock, cancel has no fee/refund. Pending raid excludes territorial commerce/freight/warehouse/dealing/processing until closed.',
        'intentional_differences':'Reference uses dealer plus2..4 guards, gang weapon pool/relationships, aim/damage-triggered aggro, inventory DrugPackage, scaled random loot and MissionWin. RPCore fixes cap at3 pistol NPCs, all three attributable defeats, immediate combat, no inventory item/carry prop, no rival-count scaling/permanent relationship penalty/reputation reward or automatic gang war. Loot35..90 inclusive in own implementation. Free combat loot distinct from paid criminal supply.',
        'configuration_sha256':digest(raid_config.read_bytes()),
        'defaults':config,
        'scope_limits':'Actual coordinates/ground/AI/combat attribution/decor registration/body access/animation/input/wanted behavior and coexistence require GTA tests. Automatic wars/convoys/weapons/cafe boost/full leader dialogue/police and phases8/9 remain pending.'}
diplomacy_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Diplomacy.json'
if diplomacy_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    worsen=source.split('private void WorsenGangRelationship(string gangName)',1)[1].split('private void ImproveGangRelationship',1)[0]
    improve=source.split('private void ImproveGangRelationship(string gangName)',1)[1].split('private static string GroupName',1)[0]
    search=source.split('private void CompleteDrugSupplyRunDealerSearch()',1)[1].split('private void ',1)[0]
    report['gang_diplomacy']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_worsens_after_body_search':'WorsenGangRelationship(activeSupplyRunGang)' in search,
        'reference_worsen_chain':all(t in worsen for t in ['"Respect" => "Neutral"','"Neutral" => "Hate"','"Hate" => "War"','"War" => "War"']),
        'reference_improve_chain':all(t in improve for t in ['"War" => "Hate"','"Hate" => "Neutral"','"Neutral" => "Respect"','"Respect" => "Respect"']),
        'reference_directed_rival_attitude':'customGang.Relationships[playerGangAffiliation] = text2' in worsen,
        'reference_bulk_sale_improvement_chance_30':'bulkDrugRelationshipRandom.Next(100) < 30' in source,
        'rpcore_cycle':'F4 relationships query; per-profile directed rival attitude toward own gang, initialized from rival Relationships field or Neutral. Three defeats do not penalize; confirmed body search atomically saves quest cargo plus one deterioration, capped at War, once per raid. Cancellation/failure after theft does not undo offense; existing collected legacy cargo is not retroactively penalized. Original gang definitions, global AI and police are untouched.',
        'rpcore_own_negotiation':'Explicit paid agreement at own HQ within5m, human on foot/safe/no wanted/activity/pending raid or freight. Default bank1000 buys exactly one upward tier per pair per24game hours, no payment at Respect, no offline compounding or clock rewind bypass. Stable receipt and book+bank rollback in one Wallet save. No treasury, cash, stock or gang reputation changes.',
        'persistence':'Optional Wallet.GangDiplomacy accounts/receipts with bounded pair uniqueness/history and overflow protection; unknown fields survive copy/save, legacy absence stays absent until action. Existing attitude overrides later definition edits. Directed pairs remain distinct across affiliations and profiles; configs never overwrite installed definitions.',
        'configuration_sha256':digest(diplomacy_config.read_bytes()),
        'defaults':json.loads(diplomacy_config.read_text()),
        'intentional_differences':'Paid negotiation/cooldown is an RPCore mechanic, not a PSRP feature. PSRP improves relation with30% chance after bulk drug deal and applies native relationship groups. Bulk deals, automatic war/peace/AI hostility, alliances blocking raids and mission reputation reward are not implemented here. War is a ledger status only. Interface phase8 and jobs phase9 remain pending.',
        'scope_limits':'HQ input/GPS, native clock/safe conditions, combat/body-search integration and coexistence need real GTA testing; static tests cannot prove gameplay.'}
    if 'gang_rival_supply_raid' in report:
        report['gang_rival_supply_raid']['persistence']+=' From0.57 body-search checkpoint also atomically worsens the directed rival attitude, with rollback of both books.'
        report['gang_rival_supply_raid']['intentional_differences']=report['gang_rival_supply_raid']['intentional_differences'].replace('permanent relationship penalty/reputation reward','reputation reward')
bulk_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Bulk.json'
if bulk_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    block=source.split('weaponSaleBuyerLocations =',1)[1].split('\n\t};',1)[0]
    points=[list(map(float,p)) for p in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\),\s*([-\d.]+)f',block)]
    config=json.loads(bulk_config.read_text())
    report['gang_bulk_sale']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_quantities_25_50_100_200_500':'new int[5] { 25, 50, 100, 200, 500 }' in source,
        'reference_value_one_and_half_and_cap_75000':'GetStreetDrugUnitPrice(item.Key) * (long)item.Value * 3 / 2' in source and 'if (num > 75000)' in source,
        'reference_prefers_meeting_at_least_1200m':'x.Key.DistanceTo(turf.Center) >= 1200f' in source and 'GetWeaponSaleBuyerLocation(turf, out var position' in source,
        'all_six_meeting_positions_headings_match_reference':len(points)==6 and len(points)==len(config['Sites']) and all(all(abs(a-b)<0.0001 for a,b in zip(p,[c['X'],c['Y'],c['Z'],c['Heading']])) for p,c in zip(points,config['Sites'])),
        'reference_completion_credits_turf_cash_and_chance_30':'turfSaveData.Cash += currentBulkDrugValue' in source and 'bulkDrugRelationshipRandom.Next(100) < 30' in source and 'ImproveGangRelationship(bulkDrugBuyerGang)' in source,
        'reference_has_spawned_van_buyer_crew_and_damage_value':'new Model(VehicleHash.GBurrito2)' in source and 'while (bulkDrugBuyerFriends.Count < 3)' in source and 'private int GetCurrentBulkDrugValue()' in source,
        'rpcore_cycle':'F4 commerce own territory bulk menu, one known SKU and quantity, explicit buyer gang with ped model, captured150% Dealer.Sell integer value capped75000. Uses borrowed player car/van stopped at area, E pick crate and E rear load, drive same vehicle to captured meeting and park within25m, E rear unload, own unarmed civilian-group buyer, E handover3 active seconds, final E at meeting credits dirty proceeds. Exactly one own tagged NPC, no supplied/modified player vehicle or global relationships. Source six positions/headings with preference >=1200m, async model/ground/space checks.',
        'persistence':'Optional Wallet.GangBulk pending snapshot/counters/revenue/receipts. Stock stays in warehouse book while contract locks territorial trade/transfer/sale/laundering/freight/raid/agreement. No escrow debit/refund. Only final completion atomically clones Bulk+Warehouse+Illicit+Diplomacy, debits local SKU/Sold, credits local Dirty/Proceeds, counts BulkDeals and improves one attitude tier only for captured roll<30. Roll/price/buyer/quantity/site/options survive reload; no reroll on partial restart. Personal stock/bank/cash/treasury/reputation untouched. Ready handover persists and resumes at meeting on foot without respawn/car; partial resume at area with car resets physical load. Cancellation unlocks undepleted stock even after lost affiliation/ownership. Full rollback/replay/overflow/legacy unknown-field guards.',
        'configuration_sha256':digest(bulk_config.read_bytes()),
        'defaults':config,
        'intentional_differences':'PSRP supports mixed SKU shipment, real upfront reservation, supplied GBurrito2, buyer car+three friends, multi-stage animations/vehicle transfer, cargo damage/price scaling and hijack attacks, turf cash and MissionWin. RPCore one SKU, one crate and one pedestrian buyer, borrowed existing car, logical stock lock until final debit, fixed quoted full value and dirty revenue processed85% separately. No ambush/damage pricing/permanent cargo loss, buyer car/crew or mission reputation reward. Death/wanted/vehicle loss/technical failure/time limit suspend without consuming stock; these are deliberate recoverable semantics. Native police remain active.',
        'scope_limits':'Actual access/ground at meetings, buyer tag/streaming/AI, input/rear handling/prop/clock/vehicle identity/coexistence need GTA tests. Further bulk realism, convoys/weapons/boost/dialogue/automatic wars/police and phases8/9 remain pending.'}
    if 'gang_diplomacy' in report:
        report['gang_diplomacy']['intentional_differences']=report['gang_diplomacy']['intentional_differences'].replace('Bulk deals, automatic war/peace/AI hostility','Advanced bulk scenes/damage/ambushes, automatic war/peace/AI hostility')
        report['gang_diplomacy']['persistence']+=' From0.58 confirmed bulk sale counts BulkDeals and applies30% snapshot roll for one improvement in the same Wallet save; pending bulk blocks paid negotiation.'
arms_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Arms.json'
if arms_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    prices=source.split('private int GetWeaponSalePrice(TurfWeaponType type)',1)[1].split('private int GetWeaponSaleValue()',1)[0]
    sale=source.split('private void CompleteWeaponSaleMission()',1)[1].split('private void ',1)[0]
    config=json.loads(arms_config.read_text())
    expected={'pistol':1200,'smg':2500,'rifle':4500,'shotgun':3200,'ammo':3}
    report['gang_arms_commerce']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_five_categories_and_prices':all(t in prices for t in ['TurfWeaponType.Pistol => 1200','TurfWeaponType.SMG => 2500','TurfWeaponType.AssaultRifle => 4500','TurfWeaponType.Shotgun => 3200','TurfWeaponType.Ammo => 3']),
        'all_default_sell_prices_match_reference':{p['Id']:p['Sell'] for p in config['Products']}==expected,
        'reference_quantities_1_5_10_25_50':'new int[5] { 1, 5, 10, 25, 50 }' in source and config['Delivery']['Quantities']==[1,5,10,25,50],
        'reference_ten_slots_per_owned_area_and_ammo_per_hundred':'return playerOwnedTurfCount * 10' in source and 'Math.Ceiling((double)item.Value / 100.0)' in source,
        'reference_arms_sale_credits_turf_cash':'turfData.Cash += currentWeaponSaleValue' in sale,
        'reference_has_rival_weapon_van_robbery':'private void GenerateWeaponSupplyLoot(string targetGang)' in source and 'private void SpawnWeaponSupplyCrates()' in source,
        'rpcore_cycle':'F4 commerce Arsenal at owned territory. Separate commercial pistol/SMG/rifle/shotgun/ammo accounts by gang/area/profile. Own paid purchase mechanic: safe human on foot within Warehouse radius, bank debit plus conserved stock/Purchased/Spent in one save. Stock is never player weapon/ammo inventory. Arms delivery uses existing GangBulk physical car/crate/rear/travel/one tagged civilian buyer/handover3s/final E engine, Kind=Arms1 with captured limit10 per owned area capped150 and ceil(ammo units/100) slot usage. Drug Kind0 remains default for legacy contracts.',
        'persistence':'Optional Wallet.GangArms stock=Purchased-Sold, max999/SKU, bounded accounts/keys/receipts and spent/revenue histories. Paid buy rollback/replay; pending arms/drug cargo share one slot and block purchases/territorial operations. Final arms delivery atomically clones Bulk+Arms+Illicit, debits commercial weapons only and credits local Dirty/Proceeds, no relation chance/BulkDeals or native player weapon/cash changes. Original drug warehouse/Dealer/diplomacy references remain exact. Captured quote/kind/limit/options survive reload/restart; ready resumes at meeting on foot, cancellation only unlocks unconsumed stock. Same recoverable suspension semantics as bulk base, no refund of earlier completed purchases. Invalid/unknown catalog/state conservatively preserved.',
        'configuration_sha256':digest(arms_config.read_bytes()),
        'defaults':config,
        'intentional_differences':'Paid local supply is RPCore code, not PSRP parity: rival weapon-van theft is still pending. One SKU, one buyer pedestrian, borrowed player car and logical stock lock until final debit; PSRP mixed shipment/GBurrito2/buyer crew/vehicle exchange/ambushes/cargo damage/permanent loss/automatic sales and MissionWin remain pending. PSRP credits turf cash; RPCore dirty proceeds require existing85% processing. Shared delivery cap75000 is RPCore bound. Sale slot limit is captured at contract, not continuously scaled during transit. Never equips, withdraws or captures native player weapons/ammo.',
        'scope_limits':'GTA access/ground/vehicle identity/rear/props/buyer tag/input/GPS/AI/coexistence still need real testing. Arms van theft/convoys/automatic trade/boost/dialogue/automatic wars/police and phases8/9 remain pending.'}
    if 'gang_bulk_sale' in report:
        report['gang_bulk_sale']['persistence']+=' From0.59 Kind0 explicitly selects drugs; Kind1 arms shares runtime/checkpoints but uses separate Arms transactions and no diplomacy bonus. Missing Kind stays0.'
weapon_raid_config=Path(__file__).resolve().parents[1] / 'config/Gangs/WeaponRaids.json'
if weapon_raid_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    loot=source.split('private void GenerateWeaponSupplyLoot(string targetGang)',1)[1].split('private void SpawnWeaponSupplyCrates()',1)[0]
    delivery=source.split('private void UpdateWeaponSupplyVanDelivery()',1)[1].split('private void KeepWeaponSupplyVanMoving()',1)[0]
    completion=source.split('private void CompleteWeaponSupplyRun()',1)[1].split('private void FailWeaponSupplyRun()',1)[0]
    block=source.split('DrugSupplyRunLocations = new List<Vector3>',1)[1].split('WorkVanSpawnPosition',1)[0]
    points=[[float(v) for v in m] for m in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',block)]
    config=json.loads(weapon_raid_config.read_text())
    report['gang_weapon_van_raid']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_has_gburrito2_and_guard_crew':'private void SpawnWeaponSupplyRunEncounter()' in source and 'new Model("gburrito2")' in source and 'private void SpawnWeaponSupplyVanCrew' in source and 'ped.Armor = 50' in source and 'ped.Accuracy = 35' in source,
        'reference_base_loot_ranges_and_optional_chances':all(s in loot for s in ['turfRandom.Next(2, 7)','turfRandom.Next(100) < 75','turfRandom.Next(100) < 55','turfRandom.Next(100) < 50','turfRandom.Next(100, 251)']),
        'all_three_sites_match_first_turf_supply_candidates':len(points)==len(config['Sites'])==3 and all(all(abs(a-b)<0.00001 for a,b in zip(p,[c['X'],c['Y'],c['Z']])) for p,c in zip(points,config['Sites'])),
        'reference_delivery_requires_driver_within_15m':'num > 15f' in delivery and 'driver.Handle == character.Handle' in delivery,
        'reference_completion_credits_weapon_stock_without_worsen':'turfData.Weapons[item.Key] += item.Value' in completion and 'WorsenGangRelationship' not in completion,
        'reference_uses_moving_driver_and_four_seats':'VehicleSeat[] array = new VehicleSeat[4]' in source and 'TASK_VEHICLE_DRIVE_WANDER' in source,
        'rpcore_cycle':'F4 owned-area rival weapon van raid: free slot shared with existing drug raid, explicit Kind1 vs missing/Kind0 legacy drugs. One own parked GBurrito2 at nearby source street and exactly3 tagged pistol guards, armor50 accuracy35. Three distinct player-damaged deaths before capture; player must take actual own-tagged van driver seat, evade native police, stop within15m of owned origin safe/wanted0 and then final foot E within3m. Vehicle return checkpoint persists before atomic arsenal credit. No personal/native weapons, bank/cash/dirty proceeds or reputation/diplomacy reward.',
        'persistence':'Captured rival/models/site/vehicle options/capacity and base-range loot: pistol2..6 plus ammo100..250, optionalSMG75%1..4/rifle55%1..3/shotgun50%1..3. Partial reload/resume even after stolen-but-not-returned resets all physical proofs/capture with same loot and attempt; returned VanDelivered resumes foot at home without van/guard respawn. Final save clones Raid+Arms, adds Stock+Recovered and completed/clears pending; rollback/replay/capacity/catalog/overflow/ownership guards. Arms law Purchased+Recovered-Sold=Stock, legacy Recovered missing0. Death/van destruction/encounter abandonment/active timeout fail unissued cargo; technical/profile/ownership/mission/cancel/suspend preserve old stock. Shared pending guards prevent concurrent territorial economy/contracts.',
        'entity_ownership':'Token decorator on each own NPC/vehicle; no saved handles restored, no deleting ambient actors or player cars. Own van reserved from other jobs. Cleanup deletes tagged guards, queues own van until empty/stopped; detached/recycled/tag-lost handles are not deleted. Queue capped8 for activation; profile reload continues cleanup, script dispose releases occupied resources without forced ejection. Model/ground/space/tag loss suspends, no loot.',
        'configuration_sha256':digest(weapon_raid_config.read_bytes()),
        'defaults':config,
        'intentional_differences':'Reference moves a van with driver+three passengers and gang weapon pool, reacts to aiming/shooting/hijack, adds wood crate prop, scales loot35% per extra rival territory and awards MissionWin. RPCore fixed parked van with3 outside pistol guards/immediate attack/all3 attributable defeats, no crate attached/no pursuit/scaling/global relationship changes/reputation reward. Return additionally requires stopped/safe/wanted0 plus explicit foot final confirmation, saved logical proof and recovery. Fixed first-turf candidate set rather than all turf-specific lists. No complete PSRP parity claim.',
        'scope_limits':'Street/ground/space at three source locations, native combat/damage flags/decorator, actual driver/van/parking/return and deferred empty-vehicle cleanup require GTA gameplay tests. Moving convoy/refinement, automatic sales/boost/dialogue/wars/police and phases8/9 remain pending.'}
    if 'gang_arms_commerce' in report:
        report['gang_arms_commerce']['persistence']+=' From0.60 free weapon-van raid credits optional Recovered history atomically; conservation becomes Purchased+Recovered-Sold=Stock.'
        report['gang_arms_commerce']['intentional_differences']=report['gang_arms_commerce']['intentional_differences'].replace('rival weapon-van theft is still pending','weapon-van theft base is delivered in0.60; moving convoy/refinement remains pending')
auto_arms_config=Path(__file__).resolve().parents[1] / 'config/Gangs/AutoArms.json'
if auto_arms_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    pick=source.split('private bool TryGetAutomaticWeaponSale(',1)[1].split('private void ProcessAutomaticWeaponDealing(',1)[0]
    cycle=source.split('private void ProcessAutomaticWeaponDealing(',1)[1].split('private void UpdateAutomaticWeaponDealing()',1)[0]
    config=json.loads(auto_arms_config.read_text())
    report['gang_automatic_arms']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_interval_180_and_price_500':'AutoWeaponSaleIntervalSeconds = 180' in source and 'AutoWeaponSaleDirtyCash = 500' in source and config['IntervalSeconds']==180 and config['UnitPrice']==500,
        'reference_excludes_ammo_and_selects_four_weapon_categories':'TurfWeaponType.Ammo' not in pick and all('TurfWeaponType.'+n in pick for n in ['Pistol','SMG','AssaultRifle','Shotgun']),
        'reference_consumes_one_weapon_and_credits_cash':'turfData.Weapons[weaponType]--' in cycle and 'turfData.Cash' in cycle and '+ 500L' in cycle,
        'reference_uses_utc_and_catchup_cap_40':'DateTime.UtcNow' in cycle and 'Math.Min(40.0' in cycle,
        'rpcore_cycle':'Explicit category opt-in in F4 arsenal, initially disabled/no book until user action. Fixed default500 dirty proceeds per one selected in-stock pistol/SMG/rifle/shotgun every180 active seconds per owned area; ammo excluded. Current catalog required, one random eligible selected category, no buyers/props/native player weapons. Settings show automatic price/current bank purchase/manual delivery quote before enabling. Activation safe on foot near owned area; disabling all available even after lost ownership/affiliation.',
        'persistence':'Optional Wallet.GangAutoArms settings/history/receipts per gang/area/profile. Sale atomically clones AutoArms+Arms+Illicit, Stock-1/Sold+1/arms revenue, auto Sales/Revenue/last product and Dirty/Proceeds+price; rollback/replay/overflow guards. Arms purchased+recovered-sold conservation retained. Personal cash/bank/Dealer/drug warehouse/diplomacy/GangState/treasury unchanged. Existing manual85% processing handles dirty proceeds. Invalid books or missing selected catalog/stock cause no replacement/free money.',
        'session_timing':'Runtime-only clock/receipt per area; no elapsed timestamps persisted. Only alive human/safe/wanted0/no mission/cutscene/free shift gate/no pending freight/raid/bulk/current affiliation+ownership. Menus/pause/modal freeze elapsed, gaps>2s ignored. Losing world/ownership conditions or stock resets elapsed; reload/profile/actor change/config toggles start fresh. One sale per interval, no offline/catch-up. Save exceptions fault that area for session until toggle/reload; never retry each frame.',
        'configuration_sha256':digest(auto_arms_config.read_bytes()),
        'defaults':config,
        'intentional_differences':'PSRP automatically processes all owned-area available weapon categories via UTC elapsed and up to40 catch-up sales; RPCore opt-in categories, active-session time and safe/free conditions, no offline catch-up or ambient buyer simulation. Both default fixed500, which can be below paid acquisition/manual sale value. RPCore writes dirty book and keeps laundering manual85%, not automatic PSRP CleanCash. Automatic laundering/cafe boost and other systems remain pending.',
        'scope_limits':'Native gate/menu/pause/safety/profile interaction, timing and coexistence with live jobs/territory activity require GTA testing. No claim of complete gang economy parity; cafe boost/dialogue/automatic wars/police and phases8/9 remain pending.'}
auto_launder_config=Path(__file__).resolve().parents[1] / 'config/Gangs/AutoLaunder.json'
if auto_launder_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    cycle=source.split('private void UpdateAutomaticTurfLaundering()',1)[1].split('private void StartTattooPromotion(',1)[0]
    config=json.loads(auto_launder_config.read_text())
    report['gang_automatic_processing']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_base_rate_500_per_minute':'500.0 * num * totalSeconds / 60.0' in cycle and config['BatchAmount']==500 and config['IntervalSeconds']==60,
        'reference_net_85_and_fraction_remainder':'* 0.85 + turfData.LaunderingCleanRemainder' in cycle and 'turfData.LaunderingCleanRemainder = num4 - (double)num5' in cycle,
        'reference_debits_dirty_cash_and_credits_clean_cash':'turfData.Cash -= num3' in cycle and 'turfData.CleanCash += num5' in cycle,
        'reference_utc_elapsed_cap_7200':'DateTime.UtcNow' in cycle and 'Math.Min(totalSeconds, 7200.0)' in cycle,
        'reference_optional_expiring_multiplier':'turfData.LaunderingBoostExpiresUtc' in cycle and 'turfData.LaunderingBoostMultiplier' in cycle,
        'rpcore_cycle':'Explicit initially-disabled F4 commerce per-owned-area automatic processing. Safe human on foot near area to enable; stop control remains available after lost gang/ownership. Default consumes up to500 existing Dirty per60 active session seconds, preserving existing cumulative85%/15% fee and crediting gang treasury LaunderedIncome, never player bank/cash. Handles min(Dirty,batch), interleaved manual processing and integer rounding, including zero-net tiny batches. No direct stock/sale or diplomacy effects.',
        'persistence':'Optional Wallet.GangAutoLaunder account Enabled/Batches/Processed/Net/Fees and receipts per gang/area/profile, bounded and extensible. Reuses validated GangIllicit.Process against a shadow wallet with no IO, then atomically publishes AutoLaunder+Illicit+Treasury in one real Wallet save. Original three references restored on failure; replay/no-op/owner/pending/capacity/overflow guards. Global Processed/Net cumulative conservation remains authoritative when manual and automatic modes mix. Treasury payday anchor preserved; no new salary or bank credit. Legacy absence stays unconfigured until explicit action.',
        'session_timing':'Reuses pure tested active clock with stable per-cycle receipt; no timestamps persisted or offline catch-up. Only safe/alive human/current affiliation+ownership/free gate/no pending raid/freight/bulk and positive dirty. Menus/pause/modal freeze; gaps>2s ignored. World/ownership/stock/contract/activity loss or reload/profile/actor/toggle resets elapsed. Independent automatic arms and processing clocks can feed proceeds sequentially, one confirmed transaction at a time. Save exception faults area until toggle/reload.',
        'configuration_sha256':digest(auto_launder_config.read_bytes()),
        'defaults':config,
        'intentional_differences':'PSRP continuously processes each second using UTC elapsed up to7200s and optional expiring multiplier, credits turf CleanCash. RPCore opt-in, whole batches after full active interval, no catch-up/offline, treasury instead of separate CleanCash, safer gating. Cafe supply/earned2X boost remains pending; configuring a rate is not that mission. Existing manual mode remains available.',
        'scope_limits':'Native safe/menu/gate/profile/clock and coexistence with automatic arms/manual processing/jobs require GTA testing. Cafe boost/other territorial activities/refinements/dialogue/wars/police and phases8/9 remain future.'}
    if 'gang_automatic_arms' in report:
        report['gang_automatic_arms']['intentional_differences']=report['gang_automatic_arms']['intentional_differences'].replace('keeps laundering manual85%, not automatic PSRP CleanCash. Automatic laundering/cafe boost','offers manual85% plus optional active-session automatic processing from0.62, not PSRP UTC CleanCash. Cafe boost')
cafe_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Cafe.json'
if cafe_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    start=source.split('private void StartCafeSupplyRun(Turf turf)',1)[1].split('private void UpdateCafeSupplyParcelBlip()',1)[0]
    complete=source.split('private void CompleteCafeSupplyContract()',1)[1].split('private void StartCafeSupplyRun(',1)[0]
    config=json.loads(cafe_config.read_text())
    coords=[(55.15895,-1737.911,29.57429),(-677.5007,-854.6947,24.0892),(-151.7666,-290.0251,40.10313)]
    report['gang_cafe_supply']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_requires_owned_turf_four':'turf.ID != 4 || !PlayerOwnsTurf(turf)' in start,
        'reference_three_boxes':'private const int CafeSupplyGoal = 3' in source and '3 supply parcels delivered.' in complete,
        'all_three_pickups_match_reference':all(all(abs(p[k]-v)<.0001 for k,v in zip(['X','Y','Z'],xyz)) for p,xyz in zip(config['Route']['Suppliers'],coords)) and len(config['Route']['Suppliers'])==3,
        'reference_parking_and_door_match':'new Vector3(-1270.415f, -1211.753f, 4.90133f)' in source and 'new Vector3(-1275.385f, -1200.205f, 5.370624f)' in source and abs(config['Parking']['X']+1270.415)<.0001 and abs(config['Door']['Y']+1200.205)<.0001,
        'reference_awards_two_x_for_one_utc_hour':'LaunderingBoostMultiplier = 2f' in complete and 'DateTime.UtcNow.AddHours(1.0)' in complete and config['BoostMinutes']==60,
        'rpcore_cycle':'Free opt-in supply contract only owned area4. Player own car/van starts at cafe yard, three distinct pickup/load checkpoints, drive same reserved borrowed vehicle to source yard, three rear unload/door deliveries, E final. Reuses physical GangSupply and DeliveryCargo, preserves paid legal/criminal routes and pending slot. Complete commits Business pending removal plus Cafe reward in one Wallet save; no money, stock, reputation, diplomacy or auto-enable. Resume partial repeats physical proof with same snapshots; ready resumes foot at door; cancel zero refund preserves any older expiry.',
        'persistence':'Optional bounded/extensible Wallet.GangCafe Accounts Gang/Area/Completed/StartedUtcTicks/ExpiresUtcTicks and256 receipts. Free order Kind2, Cafe config snapshot, cost0/units3/capacity3; legacy Kind0 legal/Kind1 criminal unchanged. Replay does not extend, new actual completion refreshes duration without stacking, rollback restores both books, owned/current gang/readiness/invalid books/overflow checks. UTC-only award, expiry no greater than180min, clock before award inactive.',
        'boost':'Multiplier2 accelerates only active automatic processing clock in matching profile/gang/area. Default batch500 per30s instead of60, unchanged cumulative85% net and treasury transaction. Menu/pause freeze production; risk/activity/pending/reload resets current interval. Reward expiry follows real UTC including menus/offline like PSRP; no offline production or automatic enable. Original three-argument shared clock keeps arms at normal rate; boosted overload tested with menus, gaps and expiry.',
        'configuration_sha256':digest(cafe_config.read_bytes()),'defaults':config,
        'intentional_differences':'Player own borrowed car/van instead of supplied Burrito, abstract load checkpoints instead of three persistent visible props in van, sequential GPS pickups, explicit persisted ready/E reward instead of immediate third box reward. Does not write cross-file GangState MissionWin. Advanced cargo visuals/van/dialogue/other work boosts/wars/police and phases8/9 remain future.',
        'scope_limits':'Yard/door access with installed resources, vehicle rear/carrying props/markers, E/END and saved partial/ready resume, live UTC expiry and coexistence with jobs and auto/manual/arms require GTA tests; cloud does not run GTA.'}
    report['gang_automatic_processing']['intentional_differences']='PSRP continuous UTC production with7200s cap; RPCore opt-in active whole batches to treasury, no offline/catch-up. Cafe earned2X integrated from0.63; expiry itself follows UTC, production still active only. Manual remains unchanged.'
    report['gang_automatic_processing']['scope_limits']='Native timing/safety/gate/profile and cafe expiry/coexistence require GTA tests. Other territorial activities/dialogue/wars/police/refinements and phases8/9 remain future.'
promotion_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Promotion.json'
if promotion_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    start=source.split('private void StartTattooPromotion(Turf turf)',1)[1].split('private bool FindTattooPromotionTarget()',1)[0]
    search=source.split('private bool FindTattooPromotionTarget()',1)[1].split('public void ClearTattooPromotionTarget()',1)[0]
    give=source.split('private void GiveTattooPromotionFlyer()',1)[1].split('private void CreateTattooPromotionReturnBlip()',1)[0]
    complete=source.split('private void CompleteTattooPromotion()',1)[1].split('private void CancelTattooPromotion()',1)[0]
    config=json.loads(promotion_config.read_text())
    report['gang_tattoo_promotion']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_requires_owned_turf_one':'turf.ID != 1 || !PlayerOwnsTurf(turf)' in start,
        'reference_goal_five':'private const int TattooPromotionFlyerGoal = 5' in source and config['Goal']==5,
        'reference_shop_matches':'new Vector3(1320.043f, -1647.478f, 52.14559f)' in source and all(abs(config['Shop'][k]-v)<.0001 for k,v in zip(['X','Y','Z'],[1320.043,-1647.478,52.14559])),
        'reference_search_annulus_50_to_120_and_initial_eight':'50f' in search and '120f' in search and 'num3 < 8f' in search and config['MinDistance']==50 and config['SearchRadius']==120,
        'reference_unique_human_foot_non_gang_customers':'!ped2.IsHuman' in search and 'ped2.IsInVehicle()' in search and 'tattooPromotionUsedPeds.Contains(ped2.Handle)' in search and 'IsTattooPromotionOwnGangMember' in search,
        'reference_handover_distance_anim_and_return':'2.2f' in give and '"mp_common", "givetake1_a"' in give and 'private const float TattooPromotionReturnDistance = 5f' in source and config['InteractionDistance']==2.2 and config['ReturnRadius']==5,
        'reference_reward_two_x_one_utc_hour':'LaunderingBoostMultiplier = 2f' in complete and 'DateTime.UtcNow.AddHours(1.0)' in complete and config['BoostMinutes']==60,
        'rpcore_cycle':'Explicit F4 commerce tattoo promotion at owned territory1 source shop. Free start on foot within5m, sequential closest real ambient civilian target in50-120m annulus, initially at least8m from player. E within2.2m begins asynchronous animation and1500ms active proximity handover; successful Wallet checkpoint only once per selected GUID and distinct runtime ped handle. After five, return shop foot/E within5m grants2X for60 real UTC minutes. No spawned fake customers or ambient-ped task, persistence, ownership, health, money, cleanup or relationship writes. Own independent moving position blip/marker only, never deletes customer.',
        'persistence':'Optional extensible bounded Wallet.GangPromotion with Pending Id/Gang/Area1/Options snapshot/Customers GUID proofs, Accounts per gang area1 Completed/StartedUtcTicks/ExpiresUtcTicks,256 receipts. No saved handles. Partial resume from shop explicitly resets proofs and receipts to ensure distinct customers in one activation; ready five persists and resumes foot/E without redoing. Cancel no owner requirement/no cost/no reward, preserves older earned expiry. Complete publishes pending removal and reward in one book/Wallet save; rollback/replay/overflow/ownership/UTC checks. New actual mission refreshes without stacking duration.',
        'integration':'Shared territorial Blocked helper includes pending/invalid promotion; explicit bulk and arms begin guards prevent bypass. OtherFree validates other books without recursive shared blocker. Automatic arms/processing stop while pending, including suspended. After reward max(cafe,promotion) multiplier2 applies only matching gang/profile/area; never4X, no auto-enable/offline production/cash/stock/rep/relations. Existing cumulative85% and treasury kept. Source cafe snapshots remain unchanged.',
        'configuration_sha256':digest(promotion_config.read_bytes()),'defaults':config,
        'intentional_differences':'RPCore waits1500ms active handover instead of source immediate counter at animation request. Stricter civilians4/5 only, nonpersistent/nonmission/non-group/nonconfigured-gang models, excludes ragdoll. No mutation of ambient customer to force standing. Search every3s/next after2.5s; no fallback population. Pausing/menu/vehicle cancels handover without proof; unsafe/world/profile/owner/actor/other pending or leaving radius+100 suspends. END and explicit resume/cancel instead of G/source cancellation. No paper prop or custom source HUD, no cross-file MissionWin. Mechanics and UI refinements remain phases8/9.',
        'scope_limits':'GTA ambient density, native flags/filters, live pathfinding/target movement, animation streaming, E/END, ready/partial reload and live bonus timing/coexistence require user testing; cloud does not run GTA. Other territorial work/mechanic/tow/police/dialogue/automatic wars and phases8/9 remain future.'}
    report['gang_automatic_processing']['intentional_differences']+=' Tattoo promotion earned2X area1 integrated from0.64; cafe area4 preserved; maximum rather than multiplied boosts.'
mechanic_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Mechanic.json'
if mechanic_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    start=source.split('private void StartMechanicRepair(Turf turf)',1)[1].split('private bool SpawnMechanicRepairVehicle()',1)[0]
    spawn=source.split('private bool SpawnMechanicRepairVehicle()',1)[1].split('private void UpdateMechanicRepair()',1)[0]
    skill=source.split('private void UpdateMechanicSkillMinigame()',1)[1].split('private void DrawMechanicSkillMeter(',1)[0]
    complete=source.split('private void CompleteMechanicRepairContract()',1)[1].split('private void CancelMechanicRepair()',1)[0]
    models=re.findall(r'"([a-z0-9_]+)"',source.split('private readonly string[] mechanicCustomerModels',1)[1].split('private bool mechanicHoodOpened',1)[0])
    config=json.loads(mechanic_config.read_text())
    report['gang_mechanic_contract']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_requires_owned_turf_two':'turf.ID != 2 || !PlayerOwnsTurf(turf)' in start,
        'reference_three_cars_three_hits':'private const int MechanicRepairGoal = 3' in source and 'private const int MechanicSkillHitsRequired = 3' in source and config['Goal']==3 and config['RequiredHits']==3,
        'reference_full_120_model_catalog_matches':models==config['Models'] and len(models)==120,
        'reference_shop_spawn_bay_exit_match':all(all(abs(config[name][k]-v)<.0001 for k,v in zip(['X','Y','Z'],values)) for name,values in [('Shop',[542.1745,-187.7679,54.50751]),('Spawn',[514.5953,-279.2764,46.28008]),('Bay',[540.6617,-188.0396,53.80798]),('Exit',[542,-260,54.3])]),
        'reference_damaged_driving_customer':'"a_m_y_business_01"' in spawn and 'vehicle.EngineHealth = 450f' in spawn and 'vehicle.BodyHealth = 550f' in spawn and 'vehicle.PetrolTankHealth = 800f' in spawn and 'TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE' in spawn,
        'reference_sine_green_458_542_and_cooldown_400':'Math.Sin(num2 * 3.5f)' in skill and 'num3 >= 0.458f && num3 <= 0.542f' in skill and 'Game.GameTime + 400' in skill,
        'reference_hood_front_repair_animation':'new Vector3(0f, 2.5f, 0f)' in skill and 'num > 2.8f' in skill and 'OpenMechanicRepairHood()' in skill and '"mini@repair", "fixing_a_ped"' in source,
        'reference_reward_two_x_one_utc_hour':'LaunderingBoostMultiplier = 2f' in complete and 'DateTime.UtcNow.AddHours(1.0)' in complete and config['BoostMinutes']==60,
        'rpcore_cycle':'Owned area2 F4 commerce free mechanic contract on foot at shop. Captures three random source120 catalog models once. Async creates own tagged damaged car and business driver on source street, drives directly to source bay, stops/handbrake/engine off, E opens hood at front<=2.8m. Native repair animation and active sine3.5 meter, green[.458,.542], first500ms/next400ms cooldown, three real timed hits. Saves vehicle completion before repair own vehicle, closes hood, driver departs. Three saved vehicles and final on-foot E shop confirmation grant2X/60 UTC real minutes, no financial/stock/rep/diplomacy or automatic enable.',
        'persistence':'Optional extensible Wallet.GangMechanic with Pending Id/Gang/Area2/Options/captured Models/Repaired. No saved handles or partial skill state. Resume retains completed cars and next model, resets only current unconfirmed repair; ready3 resumes foot/E without respawn. Cancel no owner requirement/no cost/no reward and preserves older expiry. One cloned-book Wallet write per begin/repair/cancel/complete, final pending removal+bonus atomic, rollback/replay/overflow/capacity640/receipts256 guards. UTC strict award/window<=180min, refresh not stack. Legacy absence stays unconfigured.',
        'integration':'Shared territorial blocker includes pending/invalid mechanic; bulk/arms begin and promotion OtherFree guard it. Mechanic OtherFree directly validates other books to avoid recursion/self-blocking. Max of mechanic area2/promotion area1/cafe area4 multiplier2, no4X; active safe production only, expiry itself real UTC/offline. Existing85% cumulative processing/treasury/manual and autoarms unchanged.',
        'native_ownership_cleanup':'Unique DECOR token for own car/driver, company vehicle reserved, no borrowed/player car repair. Yard/spawn checks every3s while blocked, no clear-area or deletion of ambient vehicles/props. Never teleports arriving car/player. Input/animation paused safely; actor/profile/owner/death/wanted/combat/vehicle intrusion/150m departure/arrival90 active seconds suspend. Own cleanup uses MarkAsNoLongerNeeded and releases reservation, never Delete/eject. If player occupies own car, defers release in bounded queue and drains on inactive ticks; dispose only marks/releases, no forced removal.',
        'configuration_sha256':digest(mechanic_config.read_bytes()),'defaults':config,
        'intentional_differences':'PSRP teleports arriving car to fixed bay and awards after third departure; RPCore drives to bay without warping, checkpoints completed cars and asks final E, supports resume/cancel. Timing/cooldowns count active near-front gameplay; menu/far gaps freeze. No custom source HUD, uses native meter/subtitle; no cross-file MissionWin. Retains120 source models, locations, damage, hood, animation, three hits/cars and2X. Actual traffic/navmesh/arrivals/cleanup/animations need GTA test.',
        'scope_limits':'GTA-only validation of workshop resources/access/traffic/pathfinding/free bay, client arrival/hood/smoke/animation/green meter and departure, E/END/partial/ready reload, stolen/occupied client car cleanup and bonus timing/coexistence. Tow/other work/dialogue/wars/police and user phases8/9 remain future.'}
    report['gang_automatic_processing']['intentional_differences']+=' Mechanic area2 earned2X from0.65 is combined by max with cafe/promotion, not multiplication.'
    report['gang_tattoo_promotion']['scope_limits']=report['gang_tattoo_promotion']['scope_limits'].replace('Other territorial work/mechanic/tow/','Other territorial work/tow/')
tow_config=Path(__file__).resolve().parents[1] / 'config/Gangs/TowRecovery.json'
if tow_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    start=source.split('private void StartTowTruckRecovery(Turf turf)',1)[1].split('private bool SpawnTowRecoveryTruck()',1)[0]
    delivery=source.split('private void CompleteTowRecoveryDelivery()',1)[1].split('private void CompleteTowTruckRecoveryContract()',1)[0]
    reward=source.split('private void CompleteTowTruckRecoveryContract()',1)[1].split('private void UpdateTowTruckRecovery()',1)[0]
    controls=source.split('private void UpdateTowRecoveryControls()',1)[1].split('private void CreateTowRecoveryYardBlip()',1)[0]
    drop=source.split('private bool IsTowRecoveryAtDropOff()',1)[1].split('private void CheckTowRecoveryAttachment()',1)[0]
    target=source.split('private bool SpawnTowRecoveryTarget()',1)[1].split('private void UpdateTowRecoveryControls()',1)[0]
    models=re.findall(r'"([a-z0-9_]+)"',source.split('private readonly string[] towRecoveryVehicleModels',1)[1].split('private readonly Vector3 cafeSupplyDropOff',1)[0])
    people=[m for m in re.findall(r'"([a-z0-9_]+)"',target.split('Model model2',1)[0]) if m.startswith('a_')]
    points=re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\),\s*([-\d.]+)f',source.split('private readonly Dictionary<Vector3, float> towRecoveryLocations',1)[1].split('private readonly string[] towRecoveryVehicleModels',1)[0])
    config=json.loads(tow_config.read_text())
    report['gang_tow_recovery']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_requires_owned_turf_three':'turf.ID != 3 || !PlayerOwnsTurf(turf)' in start,
        'reference_actual_two_deliveries_despite_toast_three':'towRecoveryCompleted >= 2' in delivery and '3 vehicles recovered.' in reward and config['Goal']==2,
        'reference_full_126_models_24_customers_match':models==config['Models'] and people==config['Customers'] and len(models)==126 and len(people)==24,
        'all_fifteen_sites_positions_headings_match':len(points)==len(config['Sites'])==15 and all(all(abs(float(v)-p[k])<.0001 for k,v in zip(['X','Y','Z','Heading'],q)) for p,q in zip(config['Sites'],points)),
        'reference_truck_spawn_heading_and_yard_match':all(abs(config['TruckSpawn'][k]-v)<.0001 for k,v in zip(['X','Y','Z','Heading'],[-48.79256,-1690.796,29.41298,294.2766])) and all(abs(config['Yard'][k]-v)<.0001 for k,v in zip(['X','Y','Z'],[-44.64281,-1683.975,29.38501])),
        'reference_physical_towing_arm_rear_and_hook_bounds':'SET_VEHICLE_TOW_TRUCK_ARM_POSITION' in controls and 'new Vector3(0f, -4f, 0f)' in controls and 'num <= 3f && speed < 1f' in controls and 'towRecoveryCraneHeight <= 0.2f' in controls and 'ATTACH_VEHICLE_TO_TOW_TRUCK' in controls and 'DETACH_VEHICLE_FROM_TOW_TRUCK' in controls,
        'reference_drop_both_vehicle_distances_12_16':'num <= 12f && num2 <= 16f' in drop,
        'reference_damage_grade_75_200_and_initial_800':'num2 < 75f' in delivery and 'num2 < 200f' in delivery and 'towRecoveryTarget.BodyHealth = 800f' in target and 'towRecoveryTarget.EngineHealth = 200f' in target,
        'reference_reward_two_x_one_utc_hour':'LaunderingBoostMultiplier = 2f' in reward and 'DateTime.UtcNow.AddHours(1.0)' in reward and config['BoostMinutes']==60,
        'reference_terminates_vanilla_towing_script':'TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME' in source and 'controller_towing' in source,
        'rpcore_cycle':'F4 owned area3 free two-call contract at source yard. Provided towtruck, snapshot of model/customer/site per call from126/24/15 source catalogs, target streams near player<=120m, own NPC on safe sidewalk and broken car Engine200/Body800/hazards. NUM8/NUM2 arm rate0.025 per35ms, E own pair hook rear4m/proximity3m/speed<1/arm<=.2; confirms native attachment and routes yard. Only commanded E detach in yard with truck<=12/target<=16/low arm/stopped driver, exact own empty pair, detached/settled target can checkpoint delivery. Accidental detach anywhere, even yard, requires rehook; no false delivery. Damage loss0..800 classification matches source. Two saved deliveries and foot/E final award2XUTC60min.',
        'persistence':'Optional extensible Wallet.GangTow Pending Id/Gang/Area3/Options/Dispatches snapshots/Damage results; no handles/partial hook/timer saved. Same call/model/customer/site on resume, prior deliveries and grades retained; ready2 resumes foot/E no respawn. One cloned-book Wallet write per begin/deliver/cancel/complete, index receipts/replay/rollback/overflow/640 accounts/256 receipts. Final pending removal+reward atomic, no cash/bank/stock/rep/diplomacy or auto-enable. New actual completed contract refreshes UTC window without stacking.',
        'native_ownership_cleanup':'Only tagged own towtruck/target/customer; reserve company vehicles. Scope Context control suppression while driving active own truck, leaves H hazards intact and never terminates vanilla/controller_towing or other scripts. Exact native tow pair checks; refuses occupied targets and foreign connections. Own empty stopped(<.5) vehicles retired only after checking no foreign tow coupling; own pair detach only then, never occupied delete/ejection/foreign detach. Deferred groups capped8 with reserved slot and inactive tick drain; Dispose marks/releases occupied/foreign without forced removal. Target/customer spawning waits free spot/models and safe sidewalk, no clear-area or teleport.',
        'integration':'Shared territorial blocker includes pending/invalid tow; bulk/arms and promotion/mechanic OtherFree prevent overlap without recursion. Max tow area3/mechanic2/promotion1/cafe4 multiplier2; no4X.85% cumulative treasury/manual/autoarms unaffected. Production active/safe only, UTC expiry offline but no offline production.',
        'configuration_sha256':digest(tow_config.read_bytes()),'defaults':config,
        'intentional_differences':'Two follows actual PSRP counter/HUD, fixing source completion toast3 inconsistency. E unload instead of H to preserve hazards, NUM controls preserved. Never kills vanilla scripts or forces player out/deletes occupied truck like reference cleanup. Requires commanded detach and stable exact-pair yard proof instead of any accidental yard detachment; final foot/E save rather than immediate source award. Resume snapshots/prior delivery proofs preserved, native HUD/help instead of source custom panel; no cross-file MissionWin. Stronger empty/foreign/ownership guards. Quality informational, not direct pay or boosted multiplier.',
        'scope_limits':'GTA tests needed for native tow hook geometry/arm/key controls, damage/physics, target streaming/navmesh, traffic/yard/props, accidental/released/foreign/occupied cleanup, vanilla towing coexistence, E/END/reload/profile and bonus timing. Other work/dialogue/wars/police/refinements and phases8/9 remain future.'}
    report['gang_automatic_processing']['intentional_differences']+=' Tow recovery area3 earned2X from0.66 is also combined by max, never stacked.'
    report['gang_mechanic_contract']['scope_limits']=report['gang_mechanic_contract']['scope_limits'].replace('Tow/other work/','Other work/')
cash_config=Path(__file__).resolve().parents[1] / 'config/Gangs/CashRun.json'
if cash_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    start=source.split('private void StartGruppeCashRun(Turf turf)',1)[1].split('private void UpdateGruppeCashRun()',1)[0]
    collect=source.split('private void CollectGruppeBag(Ped guard)',1)[1].split('private Vector3 GetGruppeStockadeRear()',1)[0]
    load=source.split('private void LoadGruppeBag()',1)[1].split('private void UpdateGruppeCollectionBlip()',1)[0]
    hand=source.split('private void HandOverGruppeStockade()',1)[1].split('private void DrawGruppeCashHud()',1)[0]
    points=re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\),\s*([-\d.]+)f',source.split('private readonly Dictionary<Vector3, float> gruppeCollectionLocations',1)[1].split('private const int GruppeBagGoal',1)[0])
    config=json.loads(cash_config.read_text())
    report['gang_gruppe_cash_run']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_requires_owned_turf_five':'turf.ID != 5 || !PlayerOwnsTurf(turf)' in start,
        'reference_five_collection_sites_match':len(points)==len(config['Sites'])==5 and all(all(abs(float(v)-p[k])<.0001 for k,v in zip(['X','Y','Z','Heading'],q)) for p,q in zip(config['Sites'],points)),
        'reference_spawn_depot_receiver_positions_headings_match':all(all(abs(config[name][k]-v)<.0001 for k,v in zip(['X','Y','Z','Heading'],values)) for name,values in [('Spawn',[-1330.259,-663.4227,26.0517,127.8134]),('Depot',[-680.2075,-615.2822,25.30618,264.3309]),('Receiver',[-675.5438,-613.2014,25.30774,131.4234])]),
        'reference_models_stockade_security_money_bag':'new Model("stockade")' in source and 'new Model("s_m_m_security_01")' in source and 'new Model("prop_money_bag_01")' in collect and config['VehicleModel']=='stockade' and config['GuardModel']=='s_m_m_security_01' and config['BagModel']=='prop_money_bag_01',
        'reference_carry_then_five_attached_bags':'prop.AttachTo(Game.Player.Character.Bones[Bone.PHRightHand]' in collect and 'gruppeLoadedBags.Add(prop)' in load and 'ATTACH_ENTITY_TO_ENTITY' in load and 'gruppeLoaded >= 5' in load,
        'reference_rear_3_8_and_load_2_5':'new Vector3(0f, -3.8f, 0f)' in source and 'DistanceTo(GetGruppeStockadeRear()) <= 2.5f' in source,
        'reference_receiver_gets_truck_and_two_x_hour':'TASK_ENTER_VEHICLE' in hand and 'LaunderingBoostMultiplier = 2f' in hand and 'DateTime.UtcNow.AddHours(1.0)' in hand and config['BoostMinutes']==60,
        'rpcore_cycle':'Owned area5 F4 cash transport free start on foot at source stockade spawn. Provide tagged stockade, choose closest remaining source site from truck, stream own unarmed security guard near player, E<=2m active1500ms handover, source money bag attached right hand. E rear<=2.5m/truck stopped within35m source site saves distinct loaded index and attaches visible bag inside stockade at source offsets. Five bags, player drives same truck to depot<=6m/stopped; footE<=2m receiver with truck<=10m begins active handover, saves Delivered checkpoint. E final confirms bonus2XUTC60min, no direct wallet money/stock. Receiver enters own empty stockade and drives away; failure to board safely falls back to deferred own cleanup.',
        'persistence':'Optional extensible Wallet.GangCashRun Pending Id/Gang/Area5/Options/Loaded unique site indices/Delivered and per-gang area5 Completed/StartedUtcTicks/ExpiresUtcTicks,640 accounts/256 receipts. No handles/props/timers saved. Begin/Load/Deliver/Restart/Cancel/Complete clone one book then one Wallet save, rollback/replay/capacity/overflow/ownership/UTC guards. Loaded alone cannot grant bonus; Delivered requires all site indices, then atomic pending removal+award. Partial resume resets all physical cargo and load receipts, same config/sites/id; delivered-ready resumes foot at receiver without truck/NPC respawn. Cancel free/no owner requirement and preserves prior expiry, completed new run refreshes not stacks.',
        'native_ownership':'DECOR token for all own truck/peds/bags; reserves company truck. Rejects foreign attachment/unexpected occupants/150m abandonment/carrying into vehicle/lost visible cargo. No ambient actor tasks/clear-area/teleport/global relation or police writes. Prevents truck spawning within4m of player or occupied space; guard/receiver need free1.2m and player>=1.5m, retries3s with help. Models/animation async10s; pause/menu cancels unconfirmed handover. Props follow captured Visuals.Enabled/UseProps preference: cosmetic off still requires same physical actions/proofs. Cleanup never removes occupied/moving/foreign-linked truck, bounded deferred queue drained inactive; on completed handover only own receiver is allowed to board before drive/natural release, otherwise empty-safe cleanup after30 active seconds. Dispose marks/releases protected entities, no forced ejection.',
        'integration':'Shared territorial blocker includes pending/invalid cash run, explicit bulk/arms and other work guards avoid overlap/recursive validation. Max of five earned activity multipliers2 at one UTC observation per active area, never stack, preserves85% cumulative treasury/manual/autoarms and no offline production. No personal inventory cash item, bank/cash/stock/reputation/diplomacy or auto-enable from bags/reward.',
        'configuration_sha256':digest(cash_config.read_bytes()),'defaults':config,
        'intentional_differences':'Streams one nearest remaining collection guard rather than all five at start;1500ms active handover before bag transfer. Requires own truck stopped/within35m for pickup/loading, active player-driver arrival, empty safe handover and saved Delivered plus finalE instead of immediate source award. Partial resume repeats physical cargo; ready proof persists. Receiver drives away/natural cleanup instead of unconditional source12s deletion; own safe cleanup and optional props respect installed preferences. Native HUD/help, not complete source custom panel; no cross-file MissionWin.',
        'scope_limits':'GTA tests required for stockade space/props/bone offsets/bag loading, streaming guards/receiver/animations/depot access, drive-away/boarding/occupied cleanup, E/END/profile/reload, automatic timing/expiry and native armored-truck coexistence. Medical courier/other work/dialogue/wars/police/refinements and phases8/9 remain future.'}
    report['gang_automatic_processing']['intentional_differences']+=' Gruppe cash run area5 earned2X from0.67 uses maximum with prior activities, never multiplication.'
medical_config=Path(__file__).resolve().parents[1] / 'config/Gangs/MedicalCourier.json'
if medical_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    spawn=source.split('private bool SpawnMedicalCourierDoctor()',1)[1].split('private void CollectMedicalCourierDocuments()',1)[0]
    collect=source.split('private void CollectMedicalCourierDocuments()',1)[1].split('private void CompleteMedicalCourierDelivery()',1)[0]
    complete=source.split('private void CompleteMedicalCourierDelivery()',1)[1].split('private void StartMedicalCourierRun(',1)[0]
    start=source.split('private void StartMedicalCourierRun(Turf turf)',1)[1].split('private void UpdateMedicalCourierRun()',1)[0]
    update=source.split('private void UpdateMedicalCourierRun()',1)[1].split('private void DrawMedicalCourierHud()',1)[0]
    config=json.loads(medical_config.read_text())
    report['gang_medical_courier']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_requires_owned_turf_six':'turf.ID == 6 && PlayerOwnsTurf(turf)' in start,
        'reference_doctor_document_models_match':'new Model("s_m_m_doctor_01")' in spawn and 'new Model("prop_cs_documents_01")' in collect and config['DoctorModel']=='s_m_m_doctor_01' and config['DocumentModel']=='prop_cs_documents_01',
        'reference_doctor_position_heading_and_drop_match':all(abs(config['Doctor'][k]-v)<.0001 for k,v in zip(['X','Y','Z','Heading'],[-940.9167,-337.9953,38.97752,109.513])) and all(abs(config['Destination'][k]-v)<.0001 for k,v in zip(['X','Y','Z'],[342.6898,-1397.448,32.50928])),
        'reference_clipboard_scenario_and_right_hand_documents':'WORLD_HUMAN_CLIPBOARD' in spawn and 'character.Bones[Bone.PHRightHand]' in collect and 'new Vector3(0.1f, 0f, 0f)' in collect,
        'reference_pickup_and_delivery_on_foot_within_two':'medicalCourierDoctor.Position) <= 2f' in update and 'medCenterDropOff) <= 2f' in update and '!character.IsInVehicle()' in update,
        'reference_reward_two_x_one_utc_hour':'LaunderingBoostMultiplier = 2f' in complete and 'DateTime.UtcNow.AddHours(1.0)' in complete and config['BoostMinutes']==60,
        'rpcore_cycle':'F4 owned area6 free medical document courier. Begin on foot near source doctor, async own unarmed doctor clipboard scenario with source model/heading; E foot<=2m active1500ms doctor gives/player receives. Own source document prop right hand offset.1, collision off, respects captured Visuals.UseProps/Enabled and hidden inside vehicles/restored foot; no vehicle creation/reservation/modification. Personal vehicle or on-foot travel allowed. E foot<=2m destination active handover saves Delivered, then finalE awards2XUTC60min, never money/stock/auto-enable.',
        'persistence':'Optional extensible Wallet.GangMedical Pending Id/Gang/Area6/Options/Collected/Delivered; per-gang area6 Completed/StartedUtcTicks/ExpiresUtcTicks,640 accounts/256 receipts. No handles/props/timers. Begin/Collect/Deliver/Cancel/Complete clone one book then one Wallet save, rollback/replay/ownership/overflow/UTC checks. Delivered implies Collected, final pending removal+reward atomic. Collected reload/END resume on foot anywhere safe with same snapshot and reconstructed own document, no repeat doctor/colect or proof reset; delivered-ready resumes foot at destination without NPC/prop respawn. Cancel free without ownership preserves prior expiry; real new completion refreshes, not stacks.',
        'native_ownership':'DECOR only own doctor/documents, no ambient-ped mutation or player-car ownership/position/health/money work. Model/animation async10s. Spawn waits near actor and free point/player>=1.5m, help to step2m back. Scene gestures own doctor/player only; pause/menu/range/vehicle interruption cancels unconfirmed transfer. Death/wanted/combat/ragdoll/profile/actor/owner/other pending or lost visible doc suspends, saved Collected preserved. Owned doc removed at delivery/suspension; own doctor removed only if outside vehicle, otherwise marked natural release, never eject. No global police/relations/script changes.',
        'integration':'Shared territorial blocker includes pending/invalid medical, explicit bulk/arms and other activity OtherFree guards avoid overlap and recursion. Six activity multipliers observed at sameUTC and combined by max2, no stacking.85% cumulative treasury/manual/autoarms and active-only/nooffline production preserved. No personal inventory item or direct wallet/treasury/stock/reputation/diplomacy credit.',
        'configuration_sha256':digest(medical_config.read_bytes()),'defaults':config,
        'intentional_differences':'Active1500ms handovers and saved Collect/Delivered plus finalE instead of source instantaneous flag/award. Resumable collected document survives without native handles; ready no respawn; props optional/hidden in vehicle instead of clipping in hand while driving. Own safe spawn and NPC cleanup, native simple HUD, no cross-file MissionWin. No required/provided vehicle or personal car modifications.',
        'scope_limits':'GTA tests required for hospital resources/doctor access/clipboard/handovers/prop bone/render preferences, car entry/exit hidden paper, foot delivery, lost prop, E/END/reload/profile, other jobs and live UTC bonus/coexistence. Pop-up shop/other work/dialogue/wars/police/refinements and phases8/9 remain future.'}
    report['gang_automatic_processing']['intentional_differences']+=' Medical courier area6 earned2X from0.68 is combined by max with all earlier activities.'
    report['gang_gruppe_cash_run']['scope_limits']=report['gang_gruppe_cash_run']['scope_limits'].replace('Medical courier/other work/','Other work/')
popup_config=Path(__file__).resolve().parents[1] / 'config/Gangs/PopUpShop.json'
if popup_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    config=json.loads(popup_config.read_text())
    names=re.findall(r'"([A-Z -]+)"',source.split('private readonly string[] legionClothingItems',1)[1].split('private readonly int[] legionClothingPrices',1)[0])
    prices=[int(x) for x in re.findall(r'\b\d+\b',source.split('private readonly int[] legionClothingPrices = new int[30]',1)[1].split('private string legionRequestedItem',1)[0])]
    people=re.findall(r'"([a-z0-9_]+)"',source.split('private void SpawnLegionCustomer()',1)[1].split('Model model =',1)[0])
    section=source.split('public void SpawnLegionShopProps()',1)[1].split('public void CleanupLegionShopProps()',1)[0]
    props=re.findall(r'\((-?\d+), ([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',section)
    report['gang_pop_up_shop']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_catalog_30_matches':len(names)==len(prices)==len(config['Products'])==30 and all(p['Name']==n and p['Price']==v for p,n,v in zip(config['Products'],names,prices)),
        'reference_customer_models_52_match':len(people)==52 and config['Customers']==people,
        'reference_nine_props_match':len(props)==len(config['Props'])==9 and all(p['Model']==int(q[0]) and all(abs(p['Point'][k]-float(v))<.0001 for k,v in zip(['X','Y','Z'],q[1:4])) and all(abs(p[k]-float(v))<.0001 for k,v in zip(['Pitch','Roll','Yaw'],q[4:])) for p,q in zip(config['Props'],props)),
        'reference_integer_dirty_income':'turfData.Cash += legionItemPrice * (100 - legionRequiredDiscount) / 100' in source,
        'reference_goal_and_meter':config['Goal']==10 and 'num * 0.85f % 2f' in source and 'legionCheckoutTarget) <= 0.09f' in source,
        'reference_vouchers':'turfRandom.Next(0, 4) * 5' in source,
        'reference_owned_area_seven':'turf.ID != 7' in source or 'turf.ID == 7' in source,
        'reference_utc_boost':config['BoostMinutes']==60 and 'private void CompleteLegionShop()' in source and 'DateTime.UtcNow.AddHours(1.0)' in source,
        'configuration_sha256':digest(popup_config.read_bytes()),'defaults':config,
        'implementation':'F4 owned territory7, free start on foot at Legion source shop. Nine optional source props, own tagged customer walks from street. E starts checkout, arrows select exact item then voucher, E stops triangle meter speed.85 tolerance.09. Confirmed sale atomically updates PopUp and GangIllicit in one Wallet save; integer discounted price credits only territory dirty proceeds. Failed meter no income. Ten sales plus finalE grant2XUTC60min. Maximum of seven bonuses, never stacking or auto-enable.',
        'persistence':'Pending captures catalog/options and immutable customer model/item/voucher/target. Confirmed Sales/Revenue and dirty credit persist through END/reload/cancel. Contracts0 history permits partial confirmed revenue without bonus. No native handles or transient selection/meter saved. Bounded receipts, rollback both books, replay/ownership/overflow/capacity guards; worst-case contract revenue checked before start.',
        'native_limits':'Only own DECOR-tagged props/customer; skip matching installed props, no ambient entities deleted, collision off cosmetics. Pause/menu freezes checkout. Unsafe/lost owner/35m departure/path failure suspends with saved order and sales. No clothing inventory debit, personal cash/bank/treasury credit, vehicle/global police modifications. GTA pathfinding, rendering, gestures, input and timing need gameplay validation.',
        'intentional_differences':'Persistent per-sale credit and request, END suspension/resume, optional props and native simple help/meter. FinalE confirms earned bonus instead of immediate source award; no cross-file MissionWin. Phase8 HUD/needs and phase9 job realism remain future.'}
    report['gang_automatic_processing']['intentional_differences']+=' Pop-up territory7 bonus from0.69 uses maximum2X with prior six activities.'
mowing_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Mowing.json'
if mowing_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    config=json.loads(mowing_config.read_text())
    start=source.split('private void StartMirrorParkMowing(Turf turf)',1)[1].split('private void UpdateMirrorParkMowing()',1)[0]
    update=source.split('private void UpdateMirrorParkMowing()',1)[1].split('private void DrawMirrorParkMowingHud(',1)[0]
    complete=source.split('private void CompleteMirrorParkMowing()',1)[1].split('public void CleanupMirrorParkMowing()',1)[0]
    section=source.split('private readonly float[,] mirrorGrassLocations',1)[1].split('private const string TheatreIpl',1)[0]
    rows=re.findall(r'\{ ([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f \}',section)
    report['gang_mirror_park_mowing']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_requires_owned_territory_eight':'turf.ID != 8 || !PlayerOwnsTurf(turf)' in start,
        'reference_fifty_seven_patches_match':len(rows)==len(config['Patches'])==57 and all(all(abs(p[k]-float(v))<.0001 for k,v in zip(['X','Y','Z','Heading'],q)) for p,q in zip(config['Patches'],rows)),
        'reference_mower_and_grass_models':'new Model("mower")' in start and 'new Model(992644101)' in start and config['VehicleModel']=='mower' and config['GrassModel']==992644101,
        'reference_spawn_heading_matches':all(abs(config['Spawn'][k]-v)<.0001 for k,v in zip(['X','Y','Z','Heading'],[1140.882,-671.9385,56.51244,102.5347])),
        'reference_own_driver_and_one_meter':'currentVehicle.Handle == mirrorMower.Handle && mirrorMower.Driver == character' in update and 'num2 * num2 + num3 * num3 > 1f' in update and config['MowRadius']==1,
        'reference_nearest_remaining_blip':'mirrorNextBlipUpdate = Game.GameTime + 1200' in update and 'foreach (Vector3 item in mirrorGrassRemaining)' in update,
        'reference_reward_two_x_one_utc_hour':'LaunderingBoostMultiplier = 2f' in complete and 'DateTime.UtcNow.AddHours(1.0)' in complete and config['BoostMinutes']==60,
        'configuration_sha256':digest(mowing_config.read_bytes()),'defaults':config,
        'rpcore_cycle':'F4 owned territory8 free start on foot near source mower spawn. Own DECOR-tagged/reserved mower and source grass props (optional Visuals.Enabled/UseProps), full57 source coordinates/headings. Only own player-driver passes within1mXY, within3mZ and at most8m/s save distinct index; props removed only after durable cut. Nearest remaining patch GPS refreshed1200ms. All57 saved then footE near empty stopped own mower or original point confirms2XUTC60min. Ready resume at source point needs no new mower.',
        'persistence':'Optional extensible Wallet.GangMowing Pending Id/Gang/Area8/Options/Mowed unique indices, per-gang area8 Completed/StartedUtcTicks/ExpiresUtcTicks. No native handles/timers. Cloned one-book single Wallet save with rollback/replay/ownership/limits/overflow checks;256 receipts/640 accounts. Partial resume reconstructs only remaining grass and own new mower, preserving all cuts. Cancel clears pending without partial reward; prior bonus expiry retained. Final pending removal plus reward atomic, no direct bank/cash/treasury/stock/dirty/reputation/diplomacy or automatic opt-in.',
        'integration':'Shared territorial blocker and direct OtherFree/bulk/arms guards include pending or invalid mowing without recursive validation. Eight earned activity bonuses observed at sameUTC, maximum2X, no stacking.85% cumulative treasury/15% fee and manual/autoarms unchanged; active-only production, no offline income.',
        'native_ownership':'Only own tagged props/mower; async10s models, eight props per tick, free spawn requires player>=3m and no nearby vehicles/peds. No removal of installed props or ambient cars/peds, no global relation/police/script changes. Unsafe/wanted/owner/profile/actor loss/300m departure/mower loss or150m separation suspends. Cleanup defers deleting occupied/moving/attached mower until empty/stopped/unconnected, keeps job reservation until release, drains inactive, blocks new mower while old one awaits release. Dispose releases naturally without ejection. Cosmetic props collision off, fixed saved geometry remains required if props disabled.',
        'intentional_differences':'Per-cut durable proof and partial resume, optional props, height/speed guard and explicit finalE. END suspension instead of G discard; no source forced exit/teleport/delete occupied mower or cross-file MissionWin. Simple native progress/help/nearest marker instead of finished custom HUD.',
        'scope_limits':'GTA tests required for source grass asset render/positions, mower spawn and driving/access, actual one-meter hit radius/height/speed, route markers, E/END/reload/profile, occupied/moving cleanup and bonus expiry. Further territorial activities and phases8(interface/needs)/9(job realism) remain future.'}
    report['gang_automatic_processing']['intentional_differences']+=' Mirror Park territory8 earned2X from0.70 uses maximum with all seven prior activities.'
    report['gang_medical_courier']['scope_limits']=report['gang_medical_courier']['scope_limits'].replace('Pop-up shop/other work/','Other work/')
theatre_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Theatre.json'
if theatre_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text()
    config=json.loads(theatre_config.read_text())
    section=source.split('private readonly List<TheatreItem> theatreItems',1)[1].split('private bool theatreSetupActive',1)[0]
    rows=re.findall(r'new TheatreItem\("([^"]+)", "([^"]+)", new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\), ([-\d.]+)f\)',section)
    start=source.split('private void StartRedCarpetSetup(Turf turf)',1)[1].split('private void UpdateTheatreSetupBlip()',1)[0]
    collect=source.split('private void PickupTheatreEquipment(Ped player)',1)[1].split('private void InstallTheatreEquipment(Ped player)',1)[0]
    install=source.split('private void InstallTheatreEquipment(Ped player)',1)[1].split('private void UpdateRedCarpetSetup()',1)[0]
    update=source.split('private void UpdateRedCarpetSetup()',1)[1].split('private void DrawTheatreSetupHud()',1)[0]
    complete=source.split('private void CompleteRedCarpetSetup()',1)[1].split('public void CleanupRedCarpetSetup()',1)[0]
    report['gang_theatre_premiere']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_requires_owned_territory_nine':'turf.ID == 9 && PlayerOwnsTurf(turf)' in start,
        'reference_eleven_equipment_names_models_positions_match':len(rows)==len(config['Items'])==11 and all(p['Name']==q[0] and p['Model']==q[1] and all(abs(p['Point'][k]-float(v))<.0001 for k,v in zip(['X','Y','Z','Heading'],q[2:])) for p,q in zip(config['Items'],rows)),
        'reference_crate_model_point_heading_match':config['CrateModel']=='prop_mb_crate_01a' and 'CreateTheatreProp("prop_mb_crate_01a", theatreCratePosition, -88.3205f, placeOnGround: false)' in start and all(abs(config['Crate'][k]-v)<.0001 for k,v in zip(['X','Y','Z','Heading'],[308.4233,197.2859,103.1828,-88.3205])),
        'reference_right_hand_crate_attachment':'57005' in collect and '0.12f, 0.04f, -0.03f, 0f, 90f, 0f' in collect,
        'reference_collect_then_install_in_order':'theatreCarrying = true' in collect and 'theatreCarrying = false' in install and 'theatreSetupStep++' in install,
        'reference_foot_radius_2_3_and_area_115':'character.IsInVehicle()' in update and 'DistanceTo(vector) <= 2.3f' in update and 'DistanceTo(theatreCratePosition) > 115f' in update and config['InteractionDistance']==2.3,
        'reference_red_carpet_ten_minutes':'IS_IPL_ACTIVE, "redCarpet"' in complete and 'REQUEST_IPL, "redCarpet"' in complete and 'Game.GameTime + 600000' in complete,
        'reference_bonus_two_x_one_utc_hour':'LaunderingBoostMultiplier = 2f' in complete and 'DateTime.UtcNow.AddMinutes(60.0)' in complete and config['BoostMinutes']==60,
        'configuration_sha256':digest(theatre_config.read_bytes()),'defaults':config,
        'rpcore_cycle':'F4 owned territory9 free start on foot near equipment crate. Eleven source equipment pieces in captured order. E foot<=2.3m active1500ms collect, own source crate attached right hand57005 with source offsets and box carry animation; walk to current install point, E1500ms installs, repeat. Saved Carrying and Installed reconstruct on resume. After11, finalE last point or crate awards2XUTC60min and requests redCarpet IPL only if inactive. Does not auto-enable production or create cash/dirty/stock.',
        'persistence':'Optional extensible Wallet.GangTheatre Pending Id/Gang/Area9/Options/Installed/Carrying; per-gang area9 Completed/StartedUtcTicks/ExpiresUtcTicks. Installed0..Items.Count, Carrying cannot coexist with ready. Captured items/model/points/options survive config edits; no handles/timers/IPL ownership persisted. Begin/Collect/Install/Cancel/Complete clone one book/one Wallet save with rollback/replay/ownership/limits/overflow,640 accounts/256 receipts. Partial resume preserves pickup and prior installs; ready resume at crate no respawn. Cancel free without owner keeps prior expiry; final removal+award atomic, replay no IPL refresh.',
        'integration':'Direct blockers and shared territorial guard include invalid/pending theatre without recursion. Nine earned activity multipliers observed at sameUTC use maximum2X, never stack;85/15 cumulative processing/manual/autoarms preserved. No personal inventory, money, dirty, treasury, reputation/diplomacy or MissionWin mutation.',
        'native_ownership':'DECOR only own crate/carried crate/installed props. Models/anim async10s. Optional Visuals.Enabled/UseProps cosmetics; same physical collection/install/foot proof if disabled. Reuses matching nearby installed prop visually without modifying/deleting it. Safe own cleanup and carry animation stop only, no vehicles/NPCs/global police/relationships/scripts. Unsafe/lost owner/profile/actor/115m departure/entering vehicle while carrying/lost own carry suspends retaining proofs. Pause/menu interrupts unconfirmed handover. Native simple HUD/markers.',
        'ipl_lifecycle':'Session-only tested lease. Already-active redCarpet is borrowed and never removed by this activity. Only after successful new final save, inactive IPL request starts own600000ms gameplay lease; active tick includes other activities/menus, paused or >2s discontinuity excluded. New successful own completion refreshes, replay/cancel/suspend do not. Inactive ticks expire own lease, Dispose removes own lease only. No lease restoration across reload, no claimed detection of later requests from external scripts; native API exposes active status, not ownership/refcounts. Bonus UTC independent of visual lease.',
        'intentional_differences':'1500ms active handovers and durable collected/installed proofs, END suspension/resume and finalE instead of instant source completion. No forced NPC/vehicle changes, optional collision-free cosmetics, borrowed installed props. Last source item named Red Carpet actually uses prop_premier_fence_01; actual carpet is IPL after final confirmation. UI and job realism remain phases8/9.',
        'scope_limits':'GTA tests needed for crate carry/bone offsets/animation, eleven placements/ground height/access, collect/install input, END/reload/profile, preexisting IPL and own expiry, and processing bonus coexistence. Further territorial activities and phases8/9 remain future.'}
    report['gang_automatic_processing']['intentional_differences']+=' Theatre territory9 earned2X from0.71 is combined by maximum with all eight prior activities.'
chauffeur_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Chauffeur.json'
if chauffeur_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text();config=json.loads(chauffeur_config.read_text())
    section=source.split('private readonly Dictionary<string, Vector3> chauffeurLocations',1)[1].split('private Vector3 chauffeurWalkAwayTarget',1)[0]
    rows=re.findall(r'"([^"]+)",\s*new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',section)
    start=source.split('private void StartPrivateChauffeur(Turf turf)',1)[1].split('private void UpdateChauffeurBlip()',1)[0]
    difficulty=source.split('private bool UpdateChauffeurDifficulty()',1)[1].split('private void DrawPrivateChauffeurHud()',1)[0]
    complete=source.split('private void CompletePrivateChauffeur()',1)[1].split('private void FailPrivateChauffeur(',1)[0]
    update=source.split('private void UpdatePrivateChauffeur()',1)[1].split('private bool UpdateChauffeurDifficulty()',1)[0]
    report['gang_vip_chauffeur']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_owned_territory_ten':'turf.ID != 10 || !PlayerOwnsTurf(turf)' in start,
        'reference_seven_cars_ten_passengers_match':config['Vehicles']==['stingergt','stinger','Ztype','f620','adder','bullet','monroe'] and config['Passengers']==['a_m_y_business_01','a_m_m_business_01','a_m_y_business_02','a_f_y_business_01','a_f_y_business_02','a_f_m_business_02','a_m_y_vinewood_01','a_m_y_vinewood_02','a_m_y_vinewood_03','a_m_y_vinewood_04'] and all('"'+n+'"' in start for n in config['Vehicles']+config['Passengers']),
        'reference_fifteen_named_destinations_match':len(rows)==len(config['Destinations'])==15 and all(p['Name']==q[0] and all(abs(p[k]-float(v))<.0001 for k,v in zip(['X','Y','Z'],q[1:])) for p,q in zip(config['Destinations'],rows)),
        'reference_car_pickup_points_headings_match':all(abs(config[n][k]-v)<.0001 for n,values in [('Spawn',[-83.87819,905.1129,234.9008,41.74263]),('Pickup',[-132.5137,963.5354,236.0194,160.8435])] for k,v in zip(['X','Y','Z','Heading'],values)),
        'reference_three_personalities_deadline_and_limits':'turfRandom.Next(3)' in start and '150000 + (int)(num2 * 95f)' in start and 'Math.Max(180000, Math.Min(300000' in start and 'num3 - 20000' in start and '75f : (chauffeurVIPIsLate ? 135f : 105f)' in difficulty,
        'reference_damage_wait_speed_acceleration_penalties':'num2 * 0.12f' in difficulty and 'num3 *= 1.5f' in difficulty and 'chauffeurStartTime > 30000' in difficulty and 'chauffeurLastDrivingSample >= 250' in difficulty and 'num7 > 8f' in difficulty,
        'reference_actual_board_stop_exit_wait':'num <= 12f && chauffeurVehicle.Speed < 1.5f' in update and 'num2 <= 20f && chauffeurVehicle.Speed < 1f' in update and 'Game.GameTime - chauffeurWalkStartedTime >= 5000' in update,
        'reference_quality_bonus_thresholds_and_utc':'chauffeurSatisfaction >= 85f && chauffeurVehicleCondition >= 85f' in complete and 'chauffeurSatisfaction >= 60f && chauffeurVehicleCondition >= 65f' in complete and 'chauffeurRewardMultiplier = 1.25f' in complete and 'chauffeurRewardMultiplier = 1.5f' in complete and 'DateTime.UtcNow.AddMinutes(60.0)' in complete and config['BoostMinutes']==60,
        'configuration_sha256':digest(chauffeur_config.read_bytes()),'defaults':config,
        'rpcore_cycle':'Owned territory10 F4 free start on foot by source car spawn. Persist chosen one of7 cars/10 VIP models/15 destinations/3 personalities. Own tagged reserved car and unarmed VIP. Drive supplied car, stop<=12m/<1.5m/s VIP boards actual passenger seat without warp, deliver<=20m/<1m/s, wait exit and5active seconds walking, persist Delivered; finalE destination<=20m on foot or stopped own driver awards quality boostUTC60min. Excellent sat>=85/body>=850 gives2, good>=60/650 gives1.5, otherwise acceptable1.25 only above failure15/450.',
        'difficulty':'Pure meter source250ms acceleration over8m/s2 outside25m, penalties1.8 strict/.8 others. Source1s body loss>=8 at.12 or.18 satisfaction perhealth, waiting after30s .8/1.5 persecond, overspeed105/75/135kmh at1/2.2/1, late idle<3kmh outside35m .5/s. Clamp0..100; active deadline180..300s from150s+distance3D*95ms/m, late reduces20s with150s floor. Failure atelapsed>=limit, sat<=15, body<=450. Native unavailable car/VIP, passenger left early or drive-away before exit sets score0 and durable failure.',
        'persistence':'Optional extensible Wallet.GangChauffeur Pending Id/Gang/Area10/Options/Vehicle/Passenger/Destination/Personality/Progress(Elapsed,Satisfaction,BodyHealth)/Boarded/Delivered. Accounts Completed/Bonus1.25|1.5|2/UTC window. Begin/Update/Board/Deliver/Cancel/Complete clone one book and save Wallet atomically with rollback/replay,640 accounts/256 receipts. Sample snapshots each1active second and flush at suspension; same-profile/id checks avoid writing another profile. Partial resume repeats physical pickup/trip in new own car but preserves chosen request, elapsed, nonincreasing satisfaction/body, so no grade/time reset or free repair. Delivered resumes at destination no entities; no direct money/stock/dirty/treasury/reputation/diplomacy/opt-in. Abnormal process loss can lose at most last unsaved sample interval; no native handles/timers/offline progress saved.',
        'integration':'Direct/blocker guards include invalid/pending chauffeur without recursion. Automatic processing now accepts finite double speed1..2 in shared clock with retained fractional milliseconds, keeps old arms1X and all prior integer bonuses unchanged. Ten activity multipliers combined by maximum at sameUTC, no stacking;1.25/1.5/2X make60s interval48/40/30active seconds.85/15 cumulative processing unchanged.',
        'native_ownership':'Async10s models; free car/VIP spawn, player>=3m car point; never clears ambient entities. Only tagged own car/ped and own passenger tasks. No personal vehicle mutations, police immunity/global relationship/script changes or forced player teleport/ejection. Pause/menu freezes meter. Unsafe/owner/profile/actor loss, foreign occupant/attachment or150m car separation suspends. Failure retains pending for explicit cancel. Car cleanup waits empty/stopped/unconnected; own VIP inside own stopped car is asked to exit naturally, retries4s, never deleted inside vehicle. Deferred cleanup drains inactive, next begin/resume waits previous entities release. Dispose marks natural release/reservation cleanup.',
        'intentional_differences':'Persistent performance snapshots/monotonic condition, END suspension and partial physical replay, no offline timer, no boarding warp, explicit saved delivery/finalE rather than instant reward. Spawn wait/model load excluded from timer, menu/pause freezes. HUD native simple, no cross-file MissionWin/MissionFail. Phase8 UI/needs and phase9 job realism remain reserved.',
        'scope_limits':'GTA tests needed for hill car/VIP access, actual boarding/exit and5s walk, all destinations/traffic, performance grades/deadline/penalties, END/reload/failed cancel/profile, occupied cleanup, fractional production rates and old jobs/bonuses. Further activities/refinements remain future.'}
    report['gang_automatic_processing']['intentional_differences']+=' VIP area10 from0.72 grants graded1.25/1.5/2X, max with prior nine; fractional clock preserves submillisecond accumulation, normal autoarms unchanged.'
dock_config=Path(__file__).resolve().parents[1] / 'config/Gangs/DockCargo.json'
if dock_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text();config=json.loads(dock_config.read_text())
    start=source.split('private void StartDockCargoContract(Turf turf)',1)[1].split('private void UpdateDockCargoBlip()',1)[0]
    load=source.split('private void UpdateDockAutomaticLoading()',1)[1].split('private void UpdateDockCargoContract()',1)[0]
    unload=source.split('private void UpdateDockAutomaticUnloading()',1)[1].split('private float GetDockTrailerCondition()',1)[0]
    condition=source.split('private float GetDockTrailerCondition()',1)[1].split('private int GetDockCargoTimeRemaining()',1)[0]
    complete=source.split('private void CompleteDockCargoContract()',1)[1].split('private void FailDockCargoContract(',1)[0]
    section=source.split('private readonly Vector3[] dockTrailerCargoSlots',1)[1].split('private readonly Dictionary<int, int> dockCargoSlotAssignments',1)[0]
    rows=re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',section)
    report['gang_dock_cargo']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_owned_territory_eleven':'turf.ID != 11 || !PlayerOwnsTurf(turf)' in start,
        'reference_models_and_seven_slot_offsets_match':config['TruckModel']=='phantom' and config['TrailerModel']=='trflat' and 'new Model("phantom")' in start and 'new Model("trflat")' in start and config['CargoModelA']=='prop_cementbags01' and config['CargoModelB']=='prop_conc_blocks01a' and config['CargoModelA'] in load and config['CargoModelB'] in load and len(rows)==len(config['Slots'])==7 and all(all(abs(p[k]-float(v))<.0001 for k,v in zip(['X','Y','Z'],q)) for p,q in zip(config['Slots'],rows)),
        'reference_truck_trailer_bay_destination_points_match':all(abs(config[n][k]-v)<.0001 for n,values in [('Truck',[-779.6579,-2662.85,13.88363,59.62956]),('Trailer',[-816.874,-2627.552,13.88067,239.2787]),('Bay',[-765.6149,-2603.296,13.31192]),('Destination',[1377.557,-743.3366,66.7329])] for k,v in zip(['X','Y','Z','Heading'],values)),
        'reference_ten_minute_deadline':'Game.GameTime + 600000' in start and config['SecondsLimit']==600,
        'reference_auto_load_three_seconds_five_meters':'num > 5f' in load and 'dockNextAutoLoadTime = Game.GameTime + 3000' in load and 'dockTruck.Speed < 0.5f && dockTrailer.Speed < 0.5f' in load,
        'reference_auto_unload_two_seconds_nine_meters':'num > 9f' in unload and 'dockNextAutoUnloadTime = Game.GameTime + 2000' in unload and 'dockTruck.Speed < 0.5f && dockTrailer.Speed < 0.5f' in unload,
        'reference_condition_scales_damage_one_third':'float num4 = num3 / 3f' in condition and '100f - num4 / num * 100f' in condition,
        'reference_quality_tiers_and_utc':'dockTrailerCondition >= 90f' in complete and 'dockTrailerCondition >= 70f' in complete and 'dockCargoRewardMultiplier = 1.25f' in complete and 'dockCargoRewardMultiplier = 1.5f' in complete and 'DateTime.UtcNow.AddMinutes(60.0)' in complete and config['BoostMinutes']==60,
        'configuration_sha256':digest(dock_config.read_bytes()),'defaults':config,
        'rpcore_cycle':'Owned territory11 F4 free start on foot near source truck spawn. Own tagged reserved phantom and trflat, actual exact-trailer native coupling; natural reverse or safe aligned stopped E aid via existing TankerRules. Trailer center<=5m bay, both speeds<.5 and player-driver/correct coupling:3continuous active seconds per pallet. Seven alternating source models attached at source slots. Trailer center<=9m destination same stopped/driver/coupling conditions:2seconds per pallet unload in reverse order. FinalE foot<=9m destination or stopped own driver with trailer<=9m awards quality1.25/1.5/2XUTC60min. No direct cash/dirty/stock or opt-in.',
        'condition_and_deadline':'Source display100-(1000-minBody)/30, nominal1000 baseline;damage scaled one third, not raw health percentage. With nonnegative body standard minimum display66.67 so source50 cutoff is unreachable via body alone; destruction/unavailable equipment also fails explicitly. Tiers90/70 correspond body700/100; poor valid below100 gives1.25. Deadline600active seconds, sample every1s and normal suspension flush; pause/menu/load preparation/offline do not count. Nonincreasing saved body, elapsed never rewinds, Faulted cannot clear.',
        'persistence':'Optional extensible Wallet.GangDock Pending Id/Gang/Area11/Options/Progress(Elapsed,BodyHealth,Faulted)/Loaded/Unloaded, accounts Completed/Bonus/UTC. Bounds0<=Unloaded<=Loaded<=SlotsCount; unload requires full load, ready allunloaded and nonfailed. Begin/Update/Load/Unload/Cancel/Complete clone one book/one Wallet save, rollback/replay/owner/limits/overflow,640 accounts/256 receipts. Partial resume at depot recreates own rig and only remaining physical cargo, preserves counts/time/damage; repeats coupling/travel, not confirmed load/unload. Ready resumes at destination without spawn. Normal sampling1s, abnormal process loss may lose last unsaved interval. No handles/timers/native cargo saved.',
        'native_ownership':'Models async10s; large free spawn radii7/9m and player>=4m, no ambient vehicle/ped/prop deletion. Captured Visuals.Enabled/UseProps optional cosmetics but same seven transfers/proofs/timing. Only own DECOR-tagged truck/trailer/pallets. Exact GET_VEHICLE_TRAILER_VEHICLE verification prevents wrong trailer credit. Unexpected occupants/foreign connection/150m separation or unsafe/world/owner/profile/actor loss suspends. Missing/dead/undriveable truck, lost trailer/dead trailer or deadline faults contract for explicit cancellation; lost own visible pallet suspends preserving proofs. Cleanup never deletes occupied/moving/foreign-connected rig, detaches only verified owned pair once empty/stopped, includes changed-token safeguards. Inactive cleanup queue and reservation until release; Dispose marks natural release, no ejection/teleport/global police/script mutation.',
        'integration':'Direct and shared territorial blockers include invalid/pending dock, no recursive validation. Eleven earned activity multipliers observed sameUTC combine by maximum, never stack; shared fractional clock and85/15 cumulative processing, manual commerce/autoarms unchanged. No personal inventory/bank/cash/treasury/stock/dirty/reputation/diplomacy/MissionWin credit.',
        'intentional_differences':'Persistent per-pallet proof, performance snapshots, END partial resume, explicit finalE, active-only deadline excludes preparation/menus, optional props and protected cleanup. Requires player-driver during both loading/unloading and exact own trailer rather than broad native any trailer. Nominal1000 condition baseline persisted via minbody. Native simple HUD/markers; phases8UI/needs and9job realism remain reserved.',
        'scope_limits':'GTA tests required for port spawn/road and source delivery access, native physical coupling/E aid, exact trailer-center bays, pallet attachment/visibility,3s/2s interruption,10min route, condition tiers, partial/ready END/reload, occupied/foreign cleanup and bonus coexistence. Other territorial activities/refinements remain future.'}
    report['gang_automatic_processing']['intentional_differences']+=' Dock area11 from0.73 graded1.25/1.5/2X uses max with prior ten activities; no financial split changes.'
bmx_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Bmx.json'
if bmx_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text();config=json.loads(bmx_config.read_text())
    start=source.split('private void StartBmxPromotion(Turf turf)',1)[1].split('private void UpdateBmxPromotionBlip()',1)[0]
    update=source.split('private void UpdateBmxPromotion()',1)[1].split('private void CompleteBmxPromotion()',1)[0]
    complete=source.split('private void CompleteBmxPromotion()',1)[1].split('private void FailBmxPromotion(',1)[0]
    section=source.split('private readonly Vector3[] bmxPromotionCheckpoints',1)[1].split('private int bmxPromotionInputTime',1)[0]
    rows=re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',section)
    report['gang_bmx_promotion']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_owned_territory_twelve':'turf.ID != 12 || !PlayerOwnsTurf(turf)' in start,
        'reference_model_spawn_heading_match':'new Model("bmx")' in start and config['VehicleModel']=='bmx' and all(abs(config['Spawn'][k]-v)<.0001 for k,v in zip(['X','Y','Z','Heading'],[-1375.902,-1407.651,5.033009,352.8152])),
        'reference_eleven_checkpoint_positions_match':len(rows)==len(config['Checkpoints'])==11 and all(all(abs(p[k]-float(v))<.0001 for k,v in zip(['X','Y','Z'],q)) for p,q in zip(config['Checkpoints'],rows)),
        'reference_seventy_five_second_deadline':'bmxPromotionDeadline = Game.GameTime + 75000' in start and config['SecondsLimit']==75,
        'reference_specific_bike_and_sequential_checkpoints':'character.CurrentVehicle.Handle == bmxPromotionBike.Handle' in update and 'bmxPromotionCheckpoint++' in update,
        'reference_radius_2_5_height_3_and_ring':'num3 > 2.5f' in update and 'num4 > 3f' in update and 'MarkerType.Ring' in update and config['Radius']==2.5 and config['Height']==3,
        'reference_reward_two_x_utc_hour':'LaunderingBoostMultiplier = 2f' in complete and 'DateTime.UtcNow.AddMinutes(60.0)' in complete and config['BoostMinutes']==60,
        'configuration_sha256':digest(bmx_config.read_bytes()),'defaults':config,
        'rpcore_cycle':'F4 owned territory12 free begin on foot near source skatepark spawn. Own tagged reserved BMX, player>=2m/free spawn, async model10s. Deadline75active seconds starts when bicycle spawns, includes mounting/off-bike time, excludes menu/pause/preparation/offline. Only player riding own BMX can confirm current sequential checkpoint within source2.5mXY and3mZ. Eleven source rings/GPS in exact order; same-frame flush elapsed before saved checkpoint. All11 proof freezes deadline; finalE at last point on foot or stopped own bicycle grants2XUTC60min. No added stunt/jump criteria unsupported by reference.',
        'persistence':'Optional extensible Wallet.GangBmx Pending Id/Gang/Area12/Options/Progress(Elapsed,Faulted)/Cleared, per-gang area12 Completed/UTC window. Monotonic elapsed/fault and sequential index; failed cannot become ready, replay no duplicate progress/reward/expiry. Begin/Update/Pass/Cancel/Complete clone one book/one Wallet save with rollback/replay/owner/geometry/limits/overflow,640 accounts/256 receipts. Snapshots each1active second, normal suspension flush, checkpoints durable before advancing. Partial resume at source reconstructs own bicycle, retains checkpoints and elapsed, targets next point; ready resume last point no bicycle. Abnormal process loss may lose last unsaved interval. No handles/timers/vehicle references persisted.',
        'native_ownership':'DECOR only own bicycle, JobVehicles reservation. Free spawn does not delete ambient cars/peds/props. No personal bike modification, force mounting/teleport/ejection, police/global relationship/script changes. Shared safe/world/owner/profile/actor guards suspend; ragdoll suspends. Other occupants/attachments,100m bike separation or250m course departure suspend with proofs. Lost/dead own bike or deadline faults for explicit cancel. Cleanup drains inactive, only own tagged empty/stopped/not airborne/unattached bicycle deleted; next begin/resume waits prior release. Dispose releases naturally.',
        'integration':'Pending/invalid BMX added to direct/shared territorial blockers without recursive validation. Twelve activity multipliers observed at sameUTC combine by maximum2X; prior fractional bonuses/85-15 processing/manual/autoarms unchanged. No bank/cash/stock/dirty/treasury/inventory/reputation/diplomacy/automatic opt-in or MissionWin/MissionFail mutation.',
        'intentional_differences':'Persisted checkpoints/time, END suspension and retained-time resume, explicit finalE rather than instant source reward. Timer excludes menus/pause/load prep, riding requires player-driver. Shared safety suspends ragdoll/wanted etc, bounded course separation and protected own cleanup. Native simple HUD/rings, no complete source custom panel. Phase8 UI/needs and phase9 job realism remain reserved.',
        'scope_limits':'GTA tests needed for skatepark source coordinates/ramps/rings, BMX mount/control, all11 sequential points within75s, XY/height tolerance, END/reload partial/ready, timeout/cancel/profile, occupied/moving/airborne cleanup and coexistence with prior jobs/bonuses. Further territorial activities/refinements remain future.'}
    report['gang_automatic_processing']['intentional_differences']+=' BMX territory12 earned2X from0.74 uses maximum with all eleven prior activities, no stacking.'
offshore_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Offshore.json'
if offshore_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text();config=json.loads(offshore_config.read_text())
    start=source.split('private void StartOffshoreDeal(Turf turf)',1)[1].split('private void UpdateOffshoreDealBlip()',1)[0]
    update=source.split('private void UpdateOffshoreDeal()',1)[1].split('private void StartOffshoreDealCinematic()',1)[0]
    cinematic=source.split('private void StartOffshoreDealCinematic()',1)[1].split('private void UpdateOffshoreDealCinematic()',1)[0]
    complete=source.split('private void CompleteOffshoreDeal()',1)[1].split('private void FailOffshoreDeal(',1)[0]
    report['gang_offshore_delivery']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_owned_territory_thirteen':'turf.ID != 13 || !PlayerOwnsTurf(turf)' in start,
        'reference_models_match':all('new Model("'+config[k]+'")' in start for k in ['JetModel','BoatModel','BuyerModel']) and [config[k] for k in ['JetModel','BoatModel','BuyerModel']]==['seashark','squalo','a_m_y_business_01'],
        'reference_all_points_headings_match':all(abs(config[n][k]-v)<.0001 for n,values in [('Pickup',[-1065.109,-1158.42,1.383517,232.1834]),('Jet',[-1103.408,-1152.185,-.5149324,27.82469]),('Buyer',[-2279.427,-959.0692,-.5984672,36.92299])] for k,v in zip(['X','Y','Z','Heading'],values)),
        'reference_deadline_pickup_own_jet_and_handover':'Game.GameTime + 300000' in start and config['SecondsLimit']==300 and 'DistanceTo(offshorePackagePickup) <= 2f' in update and 'character.CurrentVehicle.Handle == offshoreSeashark.Handle' in update and 'num <= 5f' in update and 'offshoreSeashark.Speed < 1.5f' in update,
        'reference_five_second_transfer_and_frozen_buyer':'offshoreDealCinematicEnd = Game.GameTime + 5000' in cinematic and 'FREEZE_ENTITY_POSITION, offshoreBuyerBoat.Handle, true' in start,
        'reference_two_x_utc_hour':'LaunderingBoostMultiplier = 2f' in complete and 'DateTime.UtcNow.AddMinutes(60.0)' in complete and config['BoostMinutes']==60,
        'configuration_sha256':digest(offshore_config.read_bytes()),'defaults':config,
        'rpcore_cycle':'Owned territory13 F4 free begin on foot at source pickup. Own tagged/reserved seashark at source water point, E foot<=2m collects abstract package, board own jet then navigate source offshore buyer point. Stream own squalo and unarmed business buyer within300m, frozen own buyer boat/engine off, ped seated driver. Own jet driver within5m at speed<1.5m/s for5continuous active seconds saves Delivered. FinalE near buyer in own jet or foot at original pickup grants2XUTC60min. No personal cash/inventory/stock/dirty/opt-in.',
        'persistence':'Optional extensible Wallet.GangOffshore Pending Id/Gang/Area13/Options/Progress(Elapsed,Faulted)/Collected/Boarded/Delivered; accounts Completed/UTC. Collected before Boarded, both before Delivered; ready nonfailed. Clone one-book one-Wallet save rollback/replay/owner/limits/overflow,640 accounts/256 receipts. Samples1s plus suspend/stage flush, monotonic time/fault, captured models/points/options. Partial resume foot shore pickup preserves collected package/time, reconstructs own jet and repeats physical boarding/travel. Delivered resumes shore without entities/travel. Abnormal process loss may lose last unsaved interval. No native handles/camera/timers persisted.',
        'native_ownership':'Async10s models, free jet4m and buyer boat7m spawn, buyer streamed within300m instead of spawning distant entities. Only DECOR own vehicles/ped, no deletion of ambient actors. No jet freeze/control disabling/camera changes; five-second stationary transfer can be interrupted. Own buyer boat frozen only while active; cleanup unfreezes even if occupied, never deletes NPC inside boat. Own buyer and boat released naturally together if only own buyer aboard; foreign occupants/attachments defer. Own jet deleted only empty/slow/not airborne/unattached, inactive queue and reservations until release, Dispose naturally releases protected entities. No police/global relations/script changes or player ejection/teleport.',
        'integration':'Thirteen activity bonuses use max2X at sameUTC, prior fractional boosts and85/15/manual/autoarms unchanged. Invalid/pending offshore blocks other territorial economics without recursive validation. Unsafe/world/profile/actor/owner changes and foreign occupants/attachment or150m jet separation suspend. Missing/dead jet/buyer/boat or deadline fault for explicit cancel. No reputation/diplomacy/MissionWin/MissionFail mutations.',
        'intentional_differences':'Active-only300s deadline excludes prepare/menu/offline, includes full5s transfer instead of source stage4 deadline bypass. No source cinematic camera/freeze jet/disabled controls. Foot pickup required; abstract package as source, no extra inventory item. Buyer streamed late, persistent proofs and finalE/END/retained-time resume. Ready shore resume avoids requiring on-foot position at sea. UI/needs phase8 and job realism phase9 remain reserved.',
        'scope_limits':'GTA tests needed for water jet spawn/boarding, offshore buyer streaming/boat access, actual5m/low speed5s transfer/waves,300s deadline, E/END/reload shore partial/ready, timeout/cancel/profile, occupied boat/natural release and old bonuses. Other activities/refinements remain future.'}
    report['gang_automatic_processing']['intentional_differences']+=' Offshore area13 earned2X from0.75 uses max with prior twelve activities, no stacking.'
pier_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Pier.json'
if pier_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text();config=json.loads(pier_config.read_text())
    section=source.split('private readonly Vector3[] pierMaintenanceLocations',1)[1].split('private readonly float[] pierMaintenanceHeadings',1)[0]
    rows=re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',section)
    headings=re.findall(r'([-\d.]+)f',source.split('private readonly float[] pierMaintenanceHeadings',1)[1].split('private const int PierMaintenanceTaskDuration',1)[0])
    update=source.split('private void UpdatePierMaintenance()',1)[1].split('private void StartPierMaintenanceTask()',1)[0]
    water=source.split('private void StartPierWateringAnimation(Ped player)',1)[1].split('private void FinishPierMaintenanceTask()',1)[0]
    complete=source.split('private void CompletePierMaintenance()',1)[1].split('private void FailPierMaintenance(',1)[0]
    report['gang_pier_maintenance']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_owned_territory_fourteen_dispatch':'TurfWorkType.PierMaintenance' in source.split('ID = 14,',1)[1].split('GangTurfs.Add',1)[0] and 'turf == null || !PlayerOwnsTurf(turf)' in source.split('private void StartTurfWork(Turf turf, TurfWorkType selectedWork)',1)[1].split('private ',1)[0],
        'reference_twenty_one_points_headings_match':len(rows)==len(headings)==len(config['Points'])==21 and all(all(abs(p[k]-float(v))<.0001 for k,v in zip(['X','Y','Z'],q)) and abs(p['Heading']-float(h))<.0001 for p,q,h in zip(config['Points'],rows,headings)),
        'reference_two_restocks_then_nineteen_waterings':'pierMaintenanceIndex < 2' in update and 'pierMaintenanceIndex >= 2' in source and len(config['Points'])-2==19,
        'reference_three_second_tasks_and_five_minute_deadline':'PierMaintenanceTaskDuration = 3000' in source and 'pierMaintenanceDeadline = Game.GameTime + 300000' in source and config['TaskMilliseconds']==3000 and config['SecondsLimit']==300,
        'reference_foot_two_meter_interaction':'character.Position.DistanceTo(vector) <= 2f && !character.IsInVehicle()' in update,
        'reference_animations_watering_can_and_offsets':'mini@repair' in source and 'fixing_a_ped' in source and 'amb@world_human_gardener_plant@male@base' in water and 'new Model("prop_wateringcan")' in water and '57005' in water and '0.1f, 0.02f, -0.03f, -80f, 0f, 20f' in water and config['WaterModel']=='prop_wateringcan',
        'reference_two_x_utc_hour':'LaunderingBoostMultiplier = 2f' in complete and 'DateTime.UtcNow.AddMinutes(60.0)' in complete and config['BoostMinutes']==60,
        'reference_incorrect_toast_mentions_seven':'All 7 maintenance tasks finished.' in complete,
        'configuration_sha256':digest(pier_config.read_bytes()),'defaults':config,
        'rpcore_cycle':'F4 owned area14 free start on foot near first source point.21 sequential source points/headings, first2 restock mini@repair/fixing_a_ped, next19 water gardener base with optional own prop_wateringcan right hand57005 source offsets/rotation. E foot<=2m begins; wait actual anim start then3continuous active seconds in range/foot/playing anim, save next Done and remove own can. No player freeze/teleport/ClearAll.300active seconds includes walking/actions; all21 ready then finalE foot last point grants2XUTC60min. Source toast says7 but actual source array/loop is21 and RPCore follows21.',
        'persistence':'Optional extensible Wallet.GangPier Pending Id/Gang/Area14/Options/Progress(Elapsed,Faulted)/Done; accounts Completed/UTC. Clone one-book single Wallet save rollback/replay/owner/sequence/duration/limits/overflow,640 accounts/256 receipts. Samples1s, flush before Work and normal suspension; time/fault monotonic. Partial resume foot within250m first point keeps Done/time, targets next, only transient unconfirmed action repeats. Ready resume foot last point no work. Abnormal process loss may lose last unsaved interval; no prop/timer/anim handle saved. No bank/cash/stock/dirty/treasury/inventory/reputation/diplomacy/opt-in.',
        'native_ownership':'Own DECOR watering can, captured Visuals.Enabled/UseProps optional cosmetic but same3s animated actions required. Async10s model/animation loading; actual animation startup grace1500ms then full work duration. Pause/menu/range/foot or animation interruption stops only tracked specific clip, deletes own can, no proof. No global animation dictionary removal, player freeze, ClearAll, ped/vehicle/camera/police/relations/script changes. Unsafe/profile/actor/owner/other pending or250m departure suspends retaining progress; deadline faults for explicit cancel. Props/models safely cleaned on suspension/disposal.',
        'integration':'Fourteen earned activity bonuses combined by maximum2X at sameUTC; prior fractional boosts/85-15 processing/manual/autoarms unchanged. Invalid/pending pier added direct/shared blockers without recursion. No cross-file MissionWin/MissionFail writes.',
        'intentional_differences':'Persisted per-task proof/time, END/partial resume, finalE, active deadline excludes prepare/menu/offline, no player freeze/ClearAll, optional prop and safe specific animation cleanup. First-point8m begin, owned14 explicitly enforced rather than broad helper invoked through parent source dispatcher. UI/needs phase8 and job realism phase9 remain reserved.',
        'scope_limits':'GTA tests needed for all21 positions/headings/access, restock/gardener animations/watering-can bone,3s range/animation interruption,300s route, END/reload partial/ready, deadline/cancel/profile, optional props and coexistence with previous jobs/bonuses. Further activities/refinements remain future.'}
    report['gang_automatic_processing']['intentional_differences']+=' Pier area14 earned2X from0.76 uses maximum with all thirteen prior activities.'
cashier_config=Path(__file__).resolve().parents[1] / 'config/Gangs/Cashier.json'
if cashier_config.exists():
    source=(a.reference_source / 'PSRP/GangSpawner.cs').read_text();config=json.loads(cashier_config.read_text())
    section=source.split('private readonly ItemID[] cashierShopItems',1)[1].split('private List<Ped> recruitedGangMembers',1)[0];items=re.findall(r'ItemID\.(\w+)',section)
    price_source=source.split('private int GetCashierItemPrice(ItemID item)',1)[1].split('private int GetCashierBasketTotal()',1)[0];prices={};pending=[]
    for line in price_source.splitlines():
        m=re.search(r'case ItemID\.(\w+):',line)
        if m:pending.append(m[1])
        m=re.search(r'return (\d+);',line)
        if m:
            for name in pending:prices[name]=int(m[1])
            pending=[]
    spawn=source.split('private void SpawnNextCashierCustomer()',1)[1].split('private void SendCashierCustomerToNextShelf()',1)[0]
    models=re.findall(r'"([a-z0-9_]+)"',spawn.split('string[] array = new string[40]',1)[1].split('Model model =',1)[0])
    shelf_section=source.split('private readonly Vector3[] cashierCustomerWalkspots',1)[1].split('private readonly float[] cashierCustomerWalkspotHeadings',1)[0]
    rows=re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',shelf_section)
    headings=re.findall(r'([-\d.]+)f',source.split('private readonly float[] cashierCustomerWalkspotHeadings',1)[1].split('private readonly Vector3 cashierCustomerSpawnPosition',1)[0])
    checkout=source.split('private void UpdateCashierCheckout()',1)[1].split('private void DrawCashierShiftHud()',1)[0]
    complete=source.split('private void CompleteCashierShift()',1)[1].split('private void FailCashierShift(',1)[0]
    report['gang_cashier_shift']={
        'reference_source':'PSRP/GangSpawner.cs',
        'reference_fifty_two_catalog_ids_prices_match':len(items)==len(config['Products'])==52 and all(p['Id']==name and p['Price']==prices[name] for p,name in zip(config['Products'],items)),
        'reference_forty_customer_models_match':len(models)==40 and config['Customers']==models,
        'reference_six_shelf_positions_headings_match':len(rows)==len(headings)==len(config['Shelves'])==6 and all(all(abs(p[k]-float(v))<.0001 for k,v in zip(['X','Y','Z'],q)) and abs(p['Heading']-float(h))<.0001 for p,q,h in zip(config['Shelves'],rows,headings)),
        'reference_counter_customer_exit_spawns_match':all(abs(config[n][k]-v)<.0001 for n,values in [('Stand',[-3040.46,583.7421,7.908928,26.97503]),('Till',[-3041.154,585.3275,7.908927,205.6297]),('Exit',[-3043.564,592.288,7.908928])] for k,v in zip(['X','Y','Z','Heading'],values)) and all(all(abs(p[k]-v)<.0001 for k,v in zip(['X','Y','Z'],q)) for p,q in zip(config['Spawns'],[[-3034.21,582.9148,7.801618],[-3041.081,597.0467,7.664927]])),
        'reference_two_to_seven_items_two_to_four_shelves':'cashierRandom.Next(2, 8)' in source and 'cashierRandom.Next(2, 5)' in source,
        'reference_five_customers_eighty_wage_ten_minute_shift':config['Goal']==5 and config['Wage']==80 and config['SecondsLimit']==600 and 'cashierCustomersServed >= 5' in checkout and 'cashierEarned += 80' in checkout and 'cashierShiftDeadline = Game.GameTime + 600000' in source,
        'reference_actual_salary_no_processing_bonus':'Game.Player.Money += num' in complete and 'LaunderingBoostMultiplier =' not in complete and 'LaunderingBoostExpiresUtc =' not in complete,
        'reference_scan_then_payment_and_450ms':'cashierCurrentItem++' in checkout and 'cashierInputTime = Game.GameTime + 450' in checkout and 'cashierCurrentItem < cashierBasket.Count' in checkout,
        'configuration_sha256':digest(cashier_config.read_bytes()),'defaults':config,
        'rpcore_cycle':'Owned territory15 F4 free cashier shift. Start E foot<=2.5m source counter (widened from source1.2 to allow exterior counter space while preserving original clerk). Own unarmed customer chooses captured2..4distinct source shelves/pauses1.5..3s, actual navmesh then till<=.65m. E scans each of2..7captured product indices,450ms active cooldown, E separately pays basket and increments served without personal credit. Five paid customers then finalE counter<=2.5 grants400cash salary at80/customer, no processing boost despite source misleading toast.600active seconds,1s snapshots, normal suspension flush.',
        'persistence':'Optional extensible Wallet.GangCashier Pending Id/Gang/Area15/Options/Progress(Elapsed,Faulted)/Served/Customer(Id,Model,Basket,Route,Scanned); per-gang account Completed/Customers/Earned. Snapshot52 prices/40models/shelves and request immutable through reload; basket duplicates allowed, route2..4unique. Optional bounded640accounts/256receipts. Begin/Update/Customer/Scan/Pay/Cancel clone one book and save once, rollback/replay/owner/capacity/overflow. Partial resume repeats current customer physical shelf route but preserves basket/scanned/served/time; ready no customer. Cancel/fail discard unpaid accrued wage, earlier paid history untouched.',
        'salary_atomicity':'Complete clones book, adds salary to actual Cash via Wallet delegates, clears pending/increments history/adds receipt then one Wallet save. MP Cash and book share durable Wallet JSON. Native protagonist cash is external GTA state: rollback for synchronous setter/save failure, replay guarded by receipt, but no cross-file atomic guarantee against abrupt process/game-save loss between GTA cash write and Wallet save; follows existing DeliveryPayments model. Bank/dirty/stock/treasury/inventory/reputation/diplomacy and all14 earned laundering multipliers unchanged. No opt-in or invented bonus.',
        'native_ownership':'Only own DECOR-tagged customer; async10s model request, free source spawns, precise .5shelf/.35till navmesh stop radii, retry5s and suspend after90active seconds unable to arrive. Arrival, browsing, till, checkout and leaving owned NPC only; no warp or ambient tasks. Original clerk not deleted/hidden/moved, no script termination or police immunity. Player may work exterior counter within2.5m. Shared gate suppresses RPCore purchase menu/marker only while gang_cashier owns activity, restores automatically after release; other jobs/shop behavior unchanged. Customer leaves and naturally releases after exit/12s; cleanup never deletes occupied NPC. No vehicles/camera/global relations changes.',
        'integration':'Pending/invalid cashier added direct/shared territorial blockers and invalid guard in auto processing. No new multiplier:14 earned activity bonuses stay max2X, fractional clock and85/15 split unchanged. Input/menu/pause freezes clock; unsafe/owner/profile/actor/50m departure suspends. Dead customer/deadline faults for explicit cancel. All account salary totals and choices/options/unknown fields preserved.',
        'intentional_differences':'Source HideOriginalCashier deletes nearby mp_m_shopkeep_01; RPCore preserves it and permits exterior stand with2.5m initial start radius (source1.2). No global shop-script manipulation. Durable scans/served/time, END resume, ready finalE instead of immediate final salary, active-only deadline. Human-friendly ID labels and native subtitle rather than full texture basket UI; phases8/9 remain reserved.',
        'scope_limits':'GTA tests needed for access/vanilla clerk coexistence/police response, exterior E and purchase-menu exclusion, customer shelf navigation/till proximity/path blockage, scans/sounds/checkout/leaving, all5 within10min, cash salary MP/protagonists, END/reload partial/ready/cancel/failure/profile and installed store assets. Other gang systems and phases8UI/needs/9job realism remain future.'}
war_source=(a.reference_source/'PSRP/GangSpawner.cs').read_text()
report['gang_warfare']={
    'reference_source':'PSRP/GangSpawner.cs: GetAiTurfAttackInterval, UpdateTurfAttackByAi, UpdateAiVsAiTurfAttacks, CheckTurfReinforcements',
    'reference_active_attack_intervals_780_to_240_seconds':all(x in war_source for x in ['GetAiTurfAttackInterval','780000','240000']),
    'reference_response_deadline_120_seconds':'GangTurfDefenceTime = 120000' in war_source,
    'reference_rival_interval_300_to_900_seconds':'turfRandom.Next(300000, 900001)' in war_source,
    'reference_rival_event_chance_85':'AiVsAiAttackChance = 85' in war_source,
    'reference_rival_capture_odds_55_50_45':all(x in war_source for x in ['int num4 = 50','num4 = 55','num4 = 45']),
    'reference_half_force_reinforcements':'num2 > 0.5f' in war_source and 'SpawnTurfReinforcements(turf)' in war_source,
    'configuration_sha256':digest((Path(__file__).resolve().parents[1]/'config/Gangs/Warfare.json').read_bytes()),
    'rpcore_cycle':'Independent per-profile opt-in personal attacks, rival simulation and capture reinforcements. Source attack intervals,120s alert,85%/5-15min distant rival simulation,45/50/55 odds. Directed declaration in existing diplomacy Wallet book; agreements remain paid. Alert and ownership/history one GangState checkpoint; responding clears warning and creates existing defense in one save. Source maximum domination exception and global relationships not ported. Remote battles cannot involve player faction.',
    'differences':'Own bounded32-entry history; active safe free-world clock, pauses on menus/jobs/pending/mission/wanted/combat; no offline simulation. Alert progress checkpoints1s. Source reinforcements use vehicle; RPCore three extra owned foot actors, total<=6, snapshot per attempt and trigger at half initial casualties including all killed in one frame. Defaults disabled on legacy saves. Declaration is own explicit HQ interaction, no fee/global faction rewrite.',
    'remaining':'Vehicle convoys/reinforcements, organized assaults, drive-bys and full dialogue remain unfinished; no claim of complete gang parity, police,phase7,8 or9. GTA navigation/combat/streaming/map and compatibility tests required.'}
assault_cfg=json.loads((Path(__file__).resolve().parents[1]/'config/Gangs/Assault.json').read_text())
assault_origins=[]
for m in re.finditer(r'\bID = (\d+),\s+TurfName =',war_source):
    part=war_source[m.end():]
    block=re.search(r'VehicleSpawnPostitions = new Dictionary<Vector3, float>\s*\{(.*?)\n\t\t\t\},',part,re.S)
    if not block:continue
    points=[dict(X=float(x),Y=float(y),Z=float(z),Heading=float(h)) for x,y,z,h in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\),\s*([-\d.]+)f',block.group(1))]
    assault_origins.append({'Area':int(m.group(1)),'Spawns':points[:4]})
report['gang_organized_assault']={
    'reference_source':'PSRP/GangSpawner.cs: OpenTurfAssaultSelection, StartTurfAssaultConvoy, FillTurfAssaultVehicle, UpdateTurfAssaultEscort, UpdateTurfAssaultMission, BeginConvoyTurfAttack, OrderTurfAssaultCrewToFight, CleanupTurfAssault',
    'reference_fifteen_origins_sixty_first_vehicle_positions_headings_match':len(assault_origins)==15 and assault_cfg['Origins']==assault_origins,
    'reference_four_cars_player_lead_three_drivers':'for (int num = 0; num < 4; num++)' in war_source and 'num == 0, out var driver' in war_source,
    'reference_granger_fallback':'Model model2 = new Model("granger")' in war_source,
    'reference_ten_minute_travel_and_sixty_five_meter_arrival':'Game.GameTime - turfAssaultMissionStartTime > 600000' in war_source and 'if (num <= 65f)' in war_source,
    'reference_escort_native_4500ms':'turfAssaultEscortUpdateTime = Game.GameTime + 4500' in war_source and 'Hash.TASK_VEHICLE_ESCORT' in war_source,
    'reference_crew_armor75_accuracy55':'ped.Armor = 75' in war_source and 'ped.Accuracy = 55' in war_source,
    'reference_owned_turf_launch_and_owned_defender_targets':'Four vehicle spawn spots are required.' in war_source and 'private void OrderTurfAssaultCrewToFight()' in war_source,
    'configuration_sha256':digest((Path(__file__).resolve().parents[1]/'config/Gangs/Assault.json').read_bytes()),
    'rpcore_cycle':'One existing Turf pending: snapshot origin/target/rules/vehicle/fifteen crew seats and defenders; four owned cars, player lead and three escort drivers, stopped65m+one real escort within100m, voluntary foot advance starts existing capture. Own survivors target only own defenders; optional0.78 reinforcement flow retained. Final E grants existing capture+50 reputation once, no cash or stock. END/reload partial travel resumes origin with surviving cars/crew and spent time; arrived partial resumes target with living foot crew and combat time. Ready resumes only confirmation.',
    'safety':'DECOR ownership, shared activity gate and job vehicle reservations; all four safe slots checked without clearing ambient traffic, source bays then nearby road nodes with actual heading and nine meter separation. Async bounded models, no player warp/ejection, no global relationships/police changes/script termination. Deferred cleanup only owned empty stopped unattached cars and foot crew, occupied actors wait; own entities naturally released on dispose.',
    'differences':'Source fills every passenger seat and may delete/warp actors. RPCore caps three passengers/car (15 crew), granger configurable model,22m/s careful escort, keeps casualties/wrecks/clocks, requires one escort arrival and player voluntary foot advance. Six defenders maximum, no faction vehicle tuning import. Travel clock stops on saved arrival; combat has separate saved limit. Native travel position/vehicle damage/crew surviving health/ammo not fully snapshotted; no offline simulation. These physical refinements belong to phase9.',
    'remaining':'Drive-bys, full leader dialogue, mobile weapon/supply robbery variants remain. Motorized enemy reinforcement variant not declared delivered;0.78 foot reinforcement stays. Police/phase7/8/9 still pending. GTA escort/navmesh/slots/combat/failures/cleanup/mod compatibility require actual playtest.'}
creator_source=(a.reference_source/'PSRP/GangCreators.cs').read_text()
report['gang_rival_drivebys_leaders']={
    'reference_source':'PSRP/GangSpawner.cs: IsRivalDriveByBlocked, UpdateRivalGangDriveBys, TryFindRivalDriveBySpawn, StartRivalGangDriveBy, AssignRivalDriveByDriverTask, AssignRivalDriveByPassengerTasks, UpdateActiveRivalDriveBy, BeginRivalDriveByEscape, CleanupRivalGangDriveBy, UpdateGangLeaderInteraction, SpeakToGangLeader, UpdateGangDialogue; PSRP/GangCreators.cs GangInitiationType',
    'reference_intervals_300_900_chance85':all(x in war_source for x in ['RivalDriveByMinInterval = 300000','RivalDriveByMaxInterval = 900000','RivalDriveByChance = 85']),
    'reference_approach45_shoot7500_escape20_range35':all(x in war_source for x in ['RivalDriveByAttackTime = 45000','RivalDriveByShootingDuration = 7500','RivalDriveByEscapeTime = 20000','RivalDriveByShootDistance = 35f']),
    'reference_spawn90_140_envelope75_170':all(x in war_source for x in ['turfRandom.Next(90, 141)','num3 < 75f','num3 > 170f']),
    'reference_driver_three_gunners_micro_smg':'Math.Min(3, vehicle.PassengerCapacity)' in war_source and 'WeaponHash.MicroSMG, 350' in war_source,
    'reference_hq40_and_territory_exclusion':'private bool IsRivalDriveByBlocked(Ped player)' in war_source and 'position.DistanceTo(position2) <= 40f' in war_source,
    'reference_leader_entry_squared225_exit4':'float num = 2.25f' in war_source and 'character.Position.DistanceTo(_dialogueGang.LeaderPed.Position) > 4f' in war_source,
    'reference_initiation_dispatch_loyalty_strength_driving':'switch (g.Data.InitiationType)' in war_source and 'public enum GangInitiationType' in creator_source,
    'reference_own_leave_other_recruitment_denied':'REQUEST INITIATION' in war_source and 'LEAVE GANG' in war_source and 'RECRUITMENT DENIED' in war_source,
    'configuration_sha256':digest((Path(__file__).resolve().parents[1]/'config/Gangs/DriveBy.json').read_bytes()),
    'rpcore_cycle':'Independent opt-in per profile, scheduled active5-15min/85% or explicit real test. One configurable car, own driver/three MicroSMG gunners, approach45s, <=35m one7.5s burst then20s escape/>220m. Initial human/outdoors/ground/no-water, HQ40m/territory exclusions, shared activity gate and pending economic/gang guards. Records outcomes only; no money/reputation/territory changes. Reload records valid pending as interrupted and never respawns attack. DECOR-protected cleanup/natural release, no occupied-car deletion or player ejection.',
    'leader_cycle':'E within1.5m of live presence-owned leader opens bound4m LemonUI conversation. Source InitiationType0/1/2 from preserved extension metadata routes existing loyalty/strength/driving, resumes matching suspended initiation. Own faction gets operational existing menus and explicit leave preserving wallet/stocks/owners/history; current RPCore leave reputation/rank reset retained. Other affiliation cannot request recruitment here. Actor/profile/faction/presence/safety/distance changes close conversation. Existing F4 shortcuts/Join retained.',
    'differences':'Strict Hate/War eligibility from directed saved diplomacy; source excludes Like/Neutral but may treat Respect as hostile. Own configurable granger rather than importing PSRP vehicle tuning. No driver warp during approach, no ammo refill each task, disarm own gunners on escape. Active encounter clock continues during non-pausing LemonUI menus; pause stops clock, modal/mission/safe-zone/actor/profile change interrupts. Native source lower dialogue rendering deferred to phase8; new dialogue offers own management links. Missing initiation type on created gangs defaults loyalty, malformed type retained/unavailable.',
    'remaining':'Police/prison and phase7 still absent. Gang mobile weapon/supply-raid variants, motorized enemy reinforcement/vehicle tuning and physical/detail differences remain in parity review/refinement; no claim of complete source parity. GTA drive-by firing/navmesh/LOS/escape/cleanup and leader proximity/source initiations require actual test.'}
traffic_ref=(a.reference_source/'PSRP/BetterPoliceSystem.cs').read_text()
prison_ref=(a.reference_source/'PSRP/PrisonSystem.cs').read_text()
traffic_cfg=json.loads((Path(__file__).resolve().parents[1]/'config/Police/Traffic.json').read_text())
street_part=traffic_ref[traffic_ref.index('public Dictionary<string, int> StreetNames'):traffic_ref.index('public static BetterPoliceSystem Instance')]
source_limits={k:int(v) for k,v in re.findall(r'\{ "([^"]+)", (\d+) \}',street_part)}
report['traffic_enforcement']={
    'reference_source':'PSRP/BetterPoliceSystem.cs CheckForViolations/EvaluateViolations/StartPullOver/HandleEscort/HandlePullOver/ResolveTrafficStop; PSRP/PrisonSystem.cs assessed separately',
    'reference_162_street_limits_match':len(source_limits)==162 and source_limits==traffic_cfg['SpeedLimits'],
    'reference_mph_multiplier':'currentVehicle.Speed * 2.2369363f' in traffic_ref,
    'reference_witness50_line_of_sight':'World.GetNearbyVehicles(player.Position, 50f)' in traffic_ref and 'driver.HasClearLineOfSightTo(player' in traffic_ref,
    'reference_speed_fines150_250_at30_over':'num2 >= 30f' in traffic_ref and '? 250 : 150' in traffic_ref,
    'reference_other_six_fines':all(x in traffic_ref for x in ['ViolationTypes.NoSeatbelt, 150','ViolationTypes.NoHeadlights, 100','ViolationTypes.RidingWithNoHelmet, 120','ViolationTypes.DoingABurnout, 100','ViolationTypes.SmokingEngine, 75','ViolationTypes.PoppedTire, 100']),
    'reference_night19_to6_and_moving10mph':'hour >= 19 || hour <= 6' in traffic_ref and 'num >= 10f' in traffic_ref,
    'reference_stop_inspection6500_to9000ms':'rng.Next(6500, 9000)' in traffic_ref,
    'reference_prison_disables_global_police_not_ported':all(x in prison_ref for x in ['Game.MaxWantedLevel = 0','SET_POLICE_IGNORE_PLAYER','TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME']),
    'configuration_sha256':digest((Path(__file__).resolve().parents[1]/'config/Police/Traffic.json').read_bytes()),
    'rpcore_cycle':'Independent opt-in Wallet.Traffic, actual witnessed seven offences, 162 source mph limits,2s continuous evidence, read-only ambient law-vehicle driver LOS50m or requested own observing patrol. Owned response cruiser/officer follow, stopped driver/officer proximity inspection7.5s, persisted itemized invoice. Exact bank payment/paid status one Wallet save with rollback/replay; no native cash clamp. Open/served/evaded/interrupted invoice preserved on exit/reload; no fabricated test fine, offline interest or licence/job gating.',
    'compatibility':'No commandeering/clearing ambient cop tasks or weapons, no dispatch/global relationship changes; native wanted unchanged in default mode, optional pursuit referral assessed separately, script termination, player teleport/ejection, forced car speed or weapon removal. Only own DECOR officer/cruiser cleanup with occupied/moving/attached resources protected. Shared gate/gang pending guard, current belt state and existing DMV documents read-only. Disabled in old saves; other traffic-controller mods not detected or integrated automatically.',
    'differences':'Source charges protagonist cash immediately, mutates ambient patrol, escalates wanted and offers bribe. RPCore records debt at witnessed response registration and voluntary later exact bank payment; officer/cruiser own, evidence2s, cooldown120active seconds. Seven base offences plus four optional offences and owned pursuit/referral in traffic_pursuit below. No bribe or automatic source-style vehicle insurance BOLO; custody assessed separately. Source PrisonSystem is global police suppression, not imported as prison gameplay.',
    'remaining':'Full LSPDFR dashboard/integration remains optional separate work; bribe/insurance BOLO and other deliberate differences listed for final parity review. All eleven active EvaluateViolations checks have RPCore counterparts with optional additional four; source bumper enum entries do not prove an implemented check; no claim of phase6/7/8/9 completion. GTA observation/road/node/LOS/follow/stop/inspection/cleanup/UI and compatibility tests required.'}
report['custody_prison']={
    'reference_source':'PSRP/PrisonSystem.cs complete source and BetterPoliceSystem.StartFootPursuit',
    'reference_prison_global_suppression':all(x in prison_ref for x in ['Game.MaxWantedLevel = 0','SET_POLICE_IGNORE_PLAYER','ENABLE_DISPATCH_SERVICE','TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME']),
    'reference_no_booking_sentence_release_cycle':all(x not in prison_ref for x in ['Bail','Sentence','Booking','ReleasePrisoner']),
    'reference_foot_pursuit_not_jail_booking':'StartFootPursuit' in traffic_ref and 'Get on the ground' in traffic_ref,
    'configuration_sha256':digest((Path(__file__).resolve().parents[1]/'config/Police/Custody.json').read_bytes()),
    'implemented':'Independent opt-in Wallet.Custody. Explicit surrender during native pursuit, on-foot unarmed stationary wanted player, living cop on foot LOS15m,3s eligibility. Valid ready interior and floor before saved booking, one-shot clear of accepted wanted level and explicit cell transfer. Config snapshot60seconds/star,500bank bail/star. Active in-cell time only, monotone absolute5s checkpoints, resume explicit, no offline credit. Full sentence release or exact atomic bank bail, rollback/idempotency,32 histories with lifetime counters, durable release exit recovery.',
    'compatibility':'No global suppression of dispatch/police/max wanted, ambient cop commandeering, invincibility, freezing player, inventory/weapon confiscation or script termination. Shared gate/gang pending checks. Native arrest/death can interrupt surrender. Clear wanted occurs only once after explicit accepted saved surrender; new wanted stops custody and is never cleared by resume or release. Existing traffic debts untouched. No auto teleport on load.',
    'remaining':'GTA must validate vanilla Mission Row cell interior/floor, native police interaction, release point, gate and UI. Modded/unavailable interior safely refuses before booking/clear/debit. Native pursuit integration delivered; owned traffic pursuit/referral and remaining four offences assessed in traffic_pursuit. Optional LSPDFR remains distinct. Phase7 missions/editor and phases8/9 unchanged.'}
report['traffic_pursuit']={
    'reference_source':'PSRP/BetterPoliceSystem.cs EvaluateViolations/StartPullOver/HandleEscort/StartFootPursuit/HandleFootPursuit/SetWanted',
    'reference_four_remaining_prices':all(x in traffic_ref for x in ['ViolationTypes.UsingAMobilePhone, 100','ViolationTypes.TailGaiting, 120','ViolationTypes.AimingAWeapon, 350','ViolationTypes.ExcessiveHornUse, 125']),
    'reference_horn_ten_seconds':'TotalSeconds >= 10.0' in traffic_ref,
    'reference_tailgate_ray_ten_meters':'10f, IntersectFlags.Vehicles, currentVehicle' in traffic_ref,
    'reference_vehicle35_foot45_distance300':all(x in traffic_ref for x in ['pursuitTimer + 35000','_footPursuitStartTime >= 45000','num >= 300f']),
    'reference_own_task_chase_and_foot_nav':all(x in traffic_ref for x in ['TASK_VEHICLE_CHASE','TASK_GO_TO_ENTITY','SetWanted(3, giveBolo: true)']),
    'configuration_sha256':digest((Path(__file__).resolve().parents[1]/'config/Police/TrafficPursuit.json').read_bytes()),
    'implemented':'Per-profile AdvancedOffences and PursuitEnabled optional false in legacy saves; existing numeric0..6 preserved, codes7..10 added, up to11 distinct invoice lines. Actual native phone task, vehicle ray10m/same heading/same height/moving>=10mph, weapon aiming, continuous active horn10s plus existing evidence hold. Owned cruiser vehicle chase, controlled stop inspection, owned officer foot follow/proximity LOS unarmed stationary inspection. Moving/noncompliant flee timers35/45active seconds or300m; waiting safely does not trigger stars. Explicit option permits persisted one-shot backup request up to3stars capped by native max and only current wanted0. Exact debt/payments untouched; reload/END/interruption never replay pursuit/request.',
    'durability':'Ticket snapshots pursuit configuration. BackupLevel recorded with evaded outcome and cleared Pending in one Wallet save before native wanted effect; rollback protects record and funds, identical replay reports unchanged and cannot reapply effect. Requested level is a request receipt, not proof another mod allowed native response. No offline chase, recurring backup, fake test offence or debt surcharge.',
    'intentional_differences':'Do not commandeer ambient police, force patrol speed, clear native wanted on300m escape, disable dispatch, remove player weapons or set global relationships. Conservative same-direction/height tailgate filter, active horn time rather than wall clock, optional four offences, active flight timers paused for safe waiting/menu rather than indiscriminate source timeout, unarmed stationary foot conference. Foot response issues citation, not automatic custody; native backup can be surrendered using0.82. No source bribe/vehicle insurance BOLO automatic side effect. Bumper enum entries have no active EvaluateViolations check in supplied source.',
    'remaining':'GTA tests needed for physics, ray, native phone, aiming/native police reaction, horn, chase/stop/foot pathing, backup, custody and other police-controller mods. This closes the grouped independent police core in the roadmap, not total PSRP parity. Optional integration and deliberate differences stay explicit for final review; phase7 missions/editor next,8/9 reserved.'}
studio_ref=(a.reference_source/'PSRP/_Heist__TheMovieStudio.cs').read_text()
studio_path=Path(__file__).resolve().parents[1]/'config/Missions/StudioHeist.json'
studio_cfg=json.loads(studio_path.read_text())
reference_points={tuple(float(n) for n in xyz) for xyz in re.findall(r'new Vector3\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)',studio_ref)}
objective_keys=['Van','Lockup','Documents','DocumentsDrop','Getaway','Garage','Truck','BoatDrop','BagContact','Plant','PreparedCar','Gate','Parking','DirectorWalk','DirectorCar','EscapeBoat','BuyerBoat']
all_studio_points=[studio_cfg[k] for k in objective_keys]+sum([studio_cfg[k] for k in ['Directors','SupplyGuards','DockGuards','StudioGuards']],[])
report['studio_heist']={
    'reference_source':'PSRP/_Heist__TheMovieStudio.cs complete2463line export; OnLoad/Update/final state machine/CompleteSetup/StateComplete/cleanup',
    'reference_six_ordered_acts':all(x in studio_ref for x in ['Act 1: Get some supplies','Act 2: Get Some Documents','Act 3: Getaway Vehicle','Act 4: Engine Without Wheels','Act 5: The Distraction','The Finale']),
    'reference_49_objective_guard_director_points_match':len(all_studio_points)==49 and all((p['X'],p['Y'],p['Z']) in reference_points for p in all_studio_points),
    'reference_actual_getaway_locust_not_method_name_baller':'World.CreateVehicle(VehicleHash.Locust' in studio_ref and studio_cfg['GetawayModel']=='locust',
    'reference_vehicle_models':all(('VehicleHash.'+model) in studio_ref for model in ['Burrito3','Jugular','Bison','BoatTrailer2','Schafter5','Dinghy3','Longfin']),
    'reference_document_night21_to5':'h >= 21 || h < 5' in studio_ref,
    'reference_three_lockpick_hits_five_failures':'minigamesComplete >= 3' in studio_ref and 'timesFailed >= 5' in studio_ref,
    'reference_director_spacing5_escape400':'DistanceTo(directorPed.Position) <= 5f' in studio_ref and '>= 400f' in studio_ref,
    'reference_twenty_second_fake_distraction':'bombTimer + 20000' in studio_ref,
    'reference_reward200000_to450000_and_bank_reset':'rng.Next(200000, 450001)' in studio_ref and 'PlayerBankBalance += num' in studio_ref and 'HollywoodSetupCounter = 0' in studio_ref,
    'configuration_sha256':digest(studio_path.read_bytes()),
    'implemented':'Full five-preparation plus finale cycle. Real owned supplies van/cargo/guards and stopped own-driver handover; night documents/trunk three timing hits/five failures/drop; owned Locust/guard/garage; Bison/trailer/owner key or intimidation/verified own hitch/combined delivery; hand-carried bag/contact/plant. Prepared car/gate guard ID/park/find randomly one of4directors/follow5-80m/quiet player takedown/body key/Schafter trunk AI device/400m escape/lose native wanted/Dinghy3/Longfin buyer/continuous5s own-driver stopped sea handover. Full reference18studio guards,4supply and6dock positions retained with local spawning. F11/Core and F2Lester contact.',
    'durability':'Optional Wallet.StudioHeist, sequential setup counter, per-attempt ID/options/reward snapshot/active elapsed, local ordered proof flow. Checkpoint5s and on suspend; restart only current physical act on resume with same elapsed/reward, no offline progress. Completed acts retained. Ready receipt before claim; bank reward and completion/history/reset in one Wallet save, replay protected and rollback/overflow retry. Final distraction request/use once saved;20active seconds before owned guards return to gate, no global wanted reduction.',
    'compatibility':'Shared activity gate and gang pending guards; profile/actor/mission/death/timeout checks. Asynchronous bounded model loading and near-only ground/water/space checked spawns. DECOR only own entities, reserved cars/trailers/boats; deferred occupied/moving/attached/towed cleanup, natural release of own corpses in vehicles. No ambient deletion/gang ped commandeering/barrier mutation, global relationships/dispatch/max-wanted changes, immunity, forced player outfit/position/exit, cash/inventory overwrite. Native wanted requested once when device taken, capped by existing max and never reduced/cleared.',
    'intentional_differences':'Independent functional implementation, not copied source. No source global police suppression/invincible vehicles/NPCs, ambient gang takeover, infinite model waits, blocking Script.Wait, forced cameras/control lock/outfit change or barrier rotation. Own guard replaces ambient gang takeover in getaway setup; dock allows close-range armed intimidation or looting own owner body. No remote fake explosion or native wanted-minus-one; distraction only diverts owned guards. Lockpick normalized time rather than FPS-dependent pixels, buyer handover12m/stopped/5s versus source35m, optional claim protects payout. Reference dock guard outlier near supplies depot retained in config but only spawns near that point. Mid-act resources not serialized/restored; current act restarts while earlier setups and time/reward persist.',
    'remaining':'GTA must validate all6acts, source coordinate heights, gates/map mods, model availability, trailer hitch, director navmesh/stealth, actual native police and boat/water/handover/cleanup. No claim of tested gameplay or exact PSRP parity. Halloween assessed in halloween_last_ride below; editor assessed in persistent_map_editor; final review still phase7; HUD/mechanics8 and all-work realism9 reserved.'}
halloween_ref=(a.reference_source/'PSRP/HalloweenHearseMission.cs').read_text()
halloween_path=Path(__file__).resolve().parents[1]/'config/Missions/Halloween.json'
halloween_cfg=json.loads(halloween_path.read_text())
report['halloween_last_ride']={
    'reference_source':'PSRP/HalloweenHearseMission.cs complete668line export; StartMission/Update/UpdateTraffic/UpdateDeathPulse/EndMission/cleanup',
    'reference_three_positions_heading_match':all(token in halloween_ref for token in ['232.1685f, -1389.492f, 29.99907f','242.2843f, -1394.68f, 30.01883f','-1655.124f, -221.2749f, 55.03468f','78.73449f']) and halloween_cfg['Start']['X']==232.1685 and halloween_cfg['Hearse']['Heading']==78.73449 and halloween_cfg['Destination']['X']==-1655.124,
    'reference_romero_three_minutes_start3_5_finish5':all(x in halloween_ref for x in ['new Model("romero")','TimeLimitMs = 180000','StartRadius = 3.5f','FinishRadius = 5f']),
    'reference_pulse8_seconds_radius12_speed27_visual650':all(x in halloween_ref for x in ['PulseCooldownMs = 8000','PulseRadius = 12f','PulseSpeed = 27f','PulseVisualDurationMs = 650']),
    'reference_chase1500_ram2800_range36':all(x in halloween_ref for x in ['RetaskMs = 1500','RamCooldownMs = 2800','RamRange = 36f','TASK_VEHICLE_CHASE','SET_DRIVER_AGGRESSIVENESS']),
    'reference_native_selfie_control_and_keyboard_e':'Control.PhoneCameraSelfie' in halloween_ref and '[E]' in halloween_ref,
    'reference_scare_ytd_audio_intervals':all(x in halloween_ref for x in ['JumpScareOne','Halloween1.wav','Next(30000, 75000)','Next(45000, 75000)']),
    'reference_no_monetary_reward':all(x not in halloween_ref for x in ['PlayerBankBalance','IncreaseBankBalance','IncreaseCashBalance','Game.Player.Money','Wallet']),
    'reference_ambient_takeover_and_global_environment_not_ported':all(x in halloween_ref for x in ['World.GetNearbyVehicles(player, 205f)','vehicle.IsPersistent = true','World.Weather = Weather.ThunderStorm','new TimeSpan(23, 0, 0)','vehicle.Velocity =']),
    'configuration_sha256':digest(halloween_path.read_bytes()),
    'implemented':'F11/Core menu, persistent Ghost map marker/GPS and on-foot E menu at source3.5m point. Explicit start/optional scares, bounded async models/local free-ground Romero, own actual driver starts180active-world-second clock, stop5m cemetery delivery. Own hidden unarmed human drivers/road-node cars only, chase/ram assistance,4active/12total default caps,260m retire, bounded off-camera55-140m road/ground/space candidates. PulseE/native selfie stick,8s cooldown12m source falloff27speed,650ms ring; only owned live-driver nonforeign/unattached/ground pursuers. Real installed PSRP_Banners/JumpScareOne and RPCore/Audio/Seasonal/Halloween1.wav, first30-75s/subsequent45-75s, per-profile disable, no gameplay failure if graphics missing.',
    'durability':'Optional Wallet.Halloween with attempt/options/progress snapshots, started/completed/failed/interrupted/pulse lifetime counts and32history. Exact own-driver proof and checkpoint before result; pulse checkpoint before own-car physics with local rollback on failed save, duplicate checkpoint/result idempotent. Save5s/explicit exit. Reload records interruption without spawning or resetting a partly damaged hearse; no resume/offline credit/duplicate result. No financial reward in reference or RPCore; wallet balances/inventory/jobs/other journals unchanged.',
    'compatibility':'Shared gate and gang-pending/profile/actor/mission/death/resource guards, model10s and occupied-ground5s bounds. No ambient traffic takeover, visibility/weapon changes to ambient drivers, player teleport/eject/repair/immunity, global wanted/dispatch/relationships/climate/clock/camera effects. Mutable physics only owned attacker pairs with no foreign occupant/attachment. Cleanup restores own drivers visible and releases/deletes only owned resources; occupied/moving/air/attached cars protected, own corpses and far own-only drivers naturally released. InstalledOIV/RPF/YTD and audio unchanged.',
    'intentional_differences':'Finite spawned pursuers instead of unbounded ambient205m takeover. Arrival requires actual driver and stop rather than any passenger/drive-by finish. Source forces23h/thunder and camera shake; RPCore preserves current environment/camera. Source E idle branch empty; RPCore opens its own functional menu and supports F11. Clock follows unpaused world including LemonUI/modal menus because physics/pursuers keep moving; ESC pause and >1s stalls do not catch up. No mid-run resume/health reset. Cosmetic scare fade800ms instead of a single full-alpha frame, optional fallback text, no camera shake. No cash reward added.',
    'remaining':'GTA test required for route/180s difficulty, Romero/ground/ghost driver rendering, chase/ram/own-only pulse, traffic collision, active-world menu timing, realYTD/audio/fallback and cleanup alongside installed mods. Map editor assessed in persistent_map_editor; final parity review remains phase7; HUD/mechanics8 and all-work/mission realism9 reserved.'}
editor_ref=(a.reference_source/'PSRP/MapBuilder.cs').read_text()
editor_path=Path(__file__).resolve().parents[1]/'config/Editor/Defaults.json'
report['persistent_map_editor']={
    'reference_source':'PSRP/MapBuilder.cs complete474line export; data classes/OnLoad/OnTick/SaveNew*/CreateBlip/LoadCustomBlips/DeleteAllCustomBlips',
    'reference_per_player_services_and_blips':all(x in editor_ref for x in ['TuningShopLocations','GasShopLocations','GroceryShopLocations','CustomBlipLocations','MultiplayerCharacter.SaveCharacter(character)']),
    'reference_five_sprite_choices_seven_colors':all(x in editor_ref for x in ['"House", "Garage", "Warehouse", "Convenience Store", "Gas Station"','BlipColor.Blue, BlipColor.White, BlipColor.Red, BlipColor.Orange, BlipColor.Yellow, BlipColor.Green, BlipColor.Pink']),
    'reference_point_duplicate_distance5_and_activation4_5':'minDistance = 5f' in editor_ref and 'num > 4.5f' in editor_ref,
    'reference_legend_short_range_settings':all(x in editor_ref for x in ['isShownOnLegend','isShortRange','creatorBlipIsShortRange']),
    'reference_no_prop_editor':'World.CreateProp' not in editor_ref and 'MapBuilderDataMechanicShop' in editor_ref,
    'reference_unsafe_suffix_blip_deletion_not_ported':'World.GetAllBlips()' in editor_ref and 'EndsWith("_C")' in editor_ref,
    'reference_fuel_points_stored_without_active_handler':'AddGasShopLocation' in editor_ref and 'foreach (MapBuilderGasShop' not in editor_ref,
    'configuration_sha256':digest(editor_path.read_bytes()),
    'implemented':'F11/Core Map Builder. Named map library per profile, one active map, optional autoload off legacy. New independent prop editor with async bounded models, rejects ped/vehicle/unavailable/oversized models, frozen owned props with collision-off preview, XYZ/pitch/roll/yaw/numpad world transform/ground/duplicate/remove, staged drafts, explicit save/cancel and revision conflict guard. Native markers/name/sprite/colors/legend/short range/GPS through own blip references. Grocery points use existing item catalog/retail cash+inventory; fuel points use existing tank/pumping/prices/exclusions; workshop points use existing stopped own-driver repair500/wash25 preserving fuel. No original location-definition rewrite.',
    'persistence':'Separate SaveData/<profile>/MapEditor.json; no Wallet schema/balance mutation by editor. Atomic JsonFiles .bak, invalid JSON/semantics protected, disk SHA conflict guard before writes, clone/revision CAS, unknown metadata retained. Export/import format1 JSON in RPCore/Maps/Exports with safe file names,1MB import cap/validated schema/model identifiers/limits; import remaps all IDs, copies metadata and never executes scripts or writes source. Default12maps/64objects/32points, proximity120/160m, absolute24/128/64 limits.',
    'compatibility':'DECOR ownership for props, only retained own blip handles, no global _C scans/deletions. Deferred attached-own prop cleanup verifies original token. No player teleport/global camera/weather/wanted/relationships/ambient deletion. Shared gate/profile/actor/mission guards while editing; world manipulation modal consumes controls/keys to avoid opening phone/radial/shop under cursor. Collision saved props spawn only in free area, no collision preview, large props over20m radius rejected. Unavailable DLC/model retained in JSON and logged rather than deleting data.',
    'intentional_differences':'Source only saves service locations/custom icons and globally deletes suffix_C blips, installs6default tuning points and enables burnout/invincibility/ignored-by-everyone for mod shop. RPCore adds own props/library/import/export and avoids these side effects/default migrations. Workshop point offers existing repair/wash, not full PSRP MechanicShop tuning. Grocery activation uses existing1.8m RPCore shop radius, fuel/workshop4.5m; new same-kind point duplicate guard5m. No automatic import/conversion of PSRP nested MapBuilderData or YMAP/XML/OIV/RPF editing. These differences stay explicit for final parity review.',
    'remaining':'GTA must validate placement/ground/axes/keyboard and ESC control, streaming/collision/free-area guard, model/DLC support, per-profile reload/autoload/own-only cleanup, marker styles and real purchases/refuel/repair with balances/fuel. Full vehicle customization is a functional gap distinct from editor repair/wash, to assess in final review. Phase7 final parity review next; global HUD/mechanics8 and all-work/mission realism9 reserved.'}
mechanic_ref=(a.reference_source/'PSRP/MechanicShop.cs').read_text()
report['vehicle_customization']={
 'reference_source':'PSRP/MechanicShop.cs: PopulateCustomsMenu, GetSlot0..48, paint/wheels/lights/extras/tyres; TaxiService/TaxiRide and WeatherSystem also inspected for final-review gaps',
 'reference_has_mods_paint_wheels_extras':all(x in mechanic_ref for x in ['GET_NUM_VEHICLE_MODS','GET_MOD_TEXT_LABEL','GetPaintMenu','GetAllExtras','PopulateWheelsSubmenu']),
 'reference_has_bank_costs': 'DecreaseBankBalance' in mechanic_ref,
 'implemented':'Owned garages and editor workshops open one reused menu with shared activity gate. Indexed native slots0..49 supported by model, stock option, turbo/smoke/xenon toggles, wheel categories/front-rear slots, primary/secondary/pearl/wheel/dashboard/interior palette, tint/plate styles/liveries, neon sides/colors/smoke colors, model extras and tyre burst protection. Selecting never previews/mutates; activation verifies change then debits bank and saves. Registered live vehicle snapshot saved with ID/garage/unknown metadata unchanged; ordinary failure restores native option/record/bank. Added nullable TiresCanBurst keeps old snapshot semantics.',
 'compatibility':'Stopped road driver, no passengers/attachments/job vehicles/native mission/wanted; profile/actor/location guards and shared gate. No player teleport, global police suppression, door prop changes, forced burnout/immunity/cameras. Same item is free no-op; unsupported indexed mods hidden. Bank/file and native world cannot be crash-atomic; gameplay and process-kill cases require GTA validation.',
 'differences':'No automatic6PSRP tuning locations or nested MapBuilderData migration. No hover previews/camera choreography, RGB text editor, plate text renaming, xenon color picker or drift tyre toggles; palette/styles and existing identity preserved. These are explicit customization refinements, not a claim of exact parity.',
 'remaining':'Physical vehicle/model support, receipts and garage/insurance recovery, native refusal/rollback, controls and installed map/mod coexistence need GTA testing. Taxi/limo passenger service and weather/time controls are confirmed whole-system gaps in final review; HUD/mechanics8 and job realism9 remain reserved.'}
report['persistent_map_editor']['remaining']='Vehicle customization now assessed in vehicle_customization. GTA editor and service checks remain pending; no PSRP nested-data migration.'
report['persistent_map_editor']['intentional_differences']+=' 0.87 extends workshop callbacks with vehicle_customization; the repair-only limitation above describes0.86 baseline.'
taxi_ref=(a.reference_source/'PSRP/TaxiRide.cs').read_text()
weather_ref=(a.reference_source/'PSRP/WeatherSystem.cs').read_text()
report['passenger_transport']={
 'reference_source':'PSRP/TaxiService.cs, TaxiRide.cs and TaxiVehicleData.cs; CallTaxi/CallLimo/SaveCurrentTaxi, spawn/entry/drive/arrival/payment/exit',
 'reference_has_pickup_waypoint_limo_and_custom_model':all(x in taxi_ref for x in ['SpawnTaxiAndDriver','SpawnLimoAndDriver','World.WaypointPosition','TaxiVehicleData.ApplyToVehicle']),
 'reference_has_fare_display_payment_mismatch':'farePrice * (float)num' in taxi_ref and 'DecreaseCashBalance((int)farePrice)' in taxi_ref,
 'configuration_sha256':digest((Path(__file__).resolve().parents[1]/'config/Services/Taxi.json').read_bytes()),
 'implemented':'F2Contacts index6 and F11/F10 menus. Async bounded own car/human driver, road/free-ground spawn, pickup marker, natural passenger entry, driver goes to road waypoint, arrival/new waypoint, normal20/faster30m/s, manual end/END, custom vehicle visual template with separate plate. Config defaults taxi500/limo2000 bank escrow, fare taxi2+5/15s/limo150+1/15s consistently displayed and settled. World continues under menus; actual pause/stall excluded. Shared gate/profile/actor/gang/native mission/wanted guards.',
 'durability':'Optional Wallet.Passenger, options snapshot/escrow before request, boarded receipt before ride, exact15s meter steps checkpointed with no offline catch-up, settlement fare+unused-refund+history in one wallet save. Failure rolls back bank/book, replay does not refund twice, int overflow leaves reservation pending. Reload interrupts/refunds unused escrow without respawning, restoring vehicle health or replaying money.32history plus lifetime spent/closed calls; metadata retained. Old saves stay absent until explicit use.',
 'differences':'Bank escrow instead of cash/bank/debt fallback, bounded ride duration/budget and fixed pickup, no teleport skip/reckless60mps/forced speed zero or player ejection. Custom missing model requires restore-default action; no automatic model fallback. Phone remains subject to existing passenger restrictions; F11/END available. Natural NPC traffic pathing is not proven by data tests.',
 'remaining':'GTA pickup/road heights, seats/AI pathing, boarding/cancel/multiple waypoints, native menus, money/refund and save/reload/custom model/cleanup need gameplay test.'}
report['world_controls']={
 'reference_source':'PSRP/WeatherSystem.cs OnLoad/Update/OpenMeteo conversion/transitions',
 'reference_has_real_dynamic_clock_and_units':all(x in weather_ref for x in ['api.open-meteo.com','DynamicWeather','RealTimeSync','UseFahrenheit','300000']),
 'reference_blocks_on_http_result':'GetWeatherWithCodeAsync(result, result2).Result' in weather_ref,
 'configuration_sha256':digest((Path(__file__).resolve().parents[1]/'config/World/Defaults.json').read_bytes()),
 'implemented':'F11/F10 global world menu, mutually exclusive Off/Manual/Dynamic/Real,12weather choices, optional Windows-local clock sync/manualHH:mm, coordinates/location name/C-F. Defaults Off/syncfalse. Dynamic gradual own transitions480active seconds; real OpenMeteo current_weather300active seconds; asynchronous bounded64KiB fetch/8s cancellation, strict schema/WMO/temp validation and stale-generation rejection. No network or natives in background beyond HTTP/data parse; game thread applies only completed requests when no mission/cutscene/activity/pause/death.',
 'durability':'Global World/Settings.json separate from Wallet/characters. JsonFiles atomic/bak, invalid semantics protected, SHA conflict detection and save-before-activate, unknown metadata preserved. Defaults never overwrite existing files; reload keeps global choices. Disabling/releases our weather persist only if current weather still matches last applied; no native clock freeze. Coordinates entered explicitly, no geolocation service.',
 'differences':'Corrected default LA longitude to west-negative; Celsius default. Simple gradual own weighted graph, no synchronous Result network blocking and no timeout freeze. Off stops automation/releases last own matching weather override, does not rewind weather/time. Automatic writes suspend during mission/shared activity; pending result applied later. Live HTTP smoke blocked by execution proxy403; no claim of online validation.',
 'remaining':'GTA weather/clock/Windows locale and online provider availability need test. Network failure retains current weather and retries next active interval; native visual weather behavior and coexistence with other weather mods untested.'}
report['vehicle_customization']['remaining']='GTA customization checks remain pending. Taxi/passenger and world controls now assessed in passenger_transport/world_controls. Remaining gang/police differences require final classification; phases8/9 remain reserved.'
police_ref=(a.reference_source/'PSRP/BetterPoliceSystem.cs').read_text()
insurance_ref=(a.reference_source/'PSRP/Vehicle_Insurance.cs').read_text()
report['police_resolution_coleader']={
 'reference_source':'BetterPoliceSystem.OfferBribe/SetWanted, Vehicle_Insurance.GiveBoloToVehicle and GangSpawner.BeginSpawn',
 'reference_has_bribe_cost_risk_and_failure':all(x in police_ref for x in ['BribeCostMultiplier = 1.5','BaseBribeChance = 0.6','flag ? 0.15 : 0.6','num3 *= 0.75','SetWanted(2, giveBolo: true)']),
 'reference_bolo_persisted_or_temporary_model_key':'vehicleModel.IdentityData.HasBolo = true' in insurance_ref and 'TempBoloList.Add(playerCar.Model.Hash, reason)' in insurance_ref,
 'reference_has_bolo_enum_without_active_check':police_ref.count('ViolationTypes.HasBolo')==1,
 'implemented':'Optional bribes/BOLO off legacy. Own live tagged on-foot officer within2.5m/stopped actual driver, E/F11, one durable attempt per ticket. Bank1.5x quote/source rounding,60%/15% risk with25% reduction for total>=300. Accepted settles current fine+bank+receipt in one save. Rejection retains fine, optional BOLO and up to2native stars only with pursuit mode. Receipt-before-native/changed-only effect/no replay, clearing or lowering stars. BOLO on traffic evasion/rejection keyed by model+plate, next-stop serious risk, administrative regularization after source fines resolved. Configured CoLeaderModel extension fallback only when primary missing, immutable presence plan/contacts/initiations.',
 'durability':'Optional ticket Bribe/BoloAtStop and Traffic flags/Bolos. Bank/book rollback, existing fine counters unchanged, agreement cannot debit through Pay and permits resolved-ticket pruning.256tickets/256BOLOs, no unpaid pruning, invalid data protected, metadata retained. No opt-in or old file migration.',
 'intentional_differences':'Bank instead of native cash; no remote/ambient bribes. Rejection respects opt-in/current/max stars. Full plate+model BOLO instead of temporary model-wide list. No automatic HasBolo fine because source enum alone has no active check. Own regularization with no extra fee/insurance changes. No invented default co-leader when both models missing, nor replacement of configured invalid/nonempty leader.',
 'remaining':'GTA inspection/wanted/BOLO/insurance/co-leader models and initiations pending. Base inventory closed with explicit differences; phase8 needs/interfaces, phase9 all job/gang/mission realism and variants. External LSPDFR integration not implemented or required.'}
report['traffic_pursuit']['remaining']='Resolution expanded0.89; see police_resolution_coleader. GTA pursuit/inspection/native response pending; presentation/realism phase8/9.'
report['gang_rival_drivebys_leaders']['remaining']='Configured co-leader fallback0.89. Moving targets/motorized reinforcement/gang tuning variants assigned to phase9, not claimed exact parity. GTA tests pending.'
report['vehicle_customization']['remaining']='GTA customization/models/garage/insurance pending. Cosmetic options and gang tuning interaction differences explicitly remain for phases8/9.'
report['phase7_base_closure']={'status':'Independent functional base inventory closed0.89; not exact PSRP parity or GTA homologation','next':'Phase8 interfaces/real needs mechanics, phase9 all job/activity/gang/mission realism','external_optional_not_implemented':['LSPDFR/RagePluginHook officer dashboard'],'variants_not_implemented_for_phase9':['moving supply/weapon theft targets','motorized territorial reinforcements versus existing foot defenders','gang tuning interactions versus existing workshop'],'evidence':'docs/FECHAMENTO-BASE-0.89.0.md; class indexing is not gameplay proof.'}
report['phase8_roleplay_needs_menus']={
 'reference_source':'PSRP HungerSystem/HydrationSystem OnTick, UiSystem sleep/stamina/gauges, NewInventorySystem DrawInventoryBackground/Grid/Slot and SettingsMenu categories',
 'reference_hunger_hydration_interval60s':all('Drain Rate", 60)' in (a.reference_source/'PSRP'/file).read_text() and '* 1000' in (a.reference_source/'PSRP'/file).read_text() for file in ['HungerSystem.cs','HydrationSystem.cs']),
 'reference_sleep90s_and_stamina':all(x in (a.reference_source/'PSRP/UiSystem.cs').read_text() for x in ['sleepDrainTimer + 90001','StaminaDrainRate = 0.35f','StaminaTimer + 5000','DrinkVisual']),
 'reference_inventory_six_columns_four_rows':all(x in (a.reference_source/'PSRP/NewInventorySystem.cs').read_text() for x in ['TotalCols = 6','VisibleRows = 4','new Point(140, 230)','col * 130']),
 'bugs_fixed':['list inventory use granted effects before any durable item removal','grid consumption could leave inventory/needs inconsistent across save failure','needs timer subtracted signed time and credited long stalls','sleep depletion too slow versus reference and recovery reset incomplete at respawn','lung gauge only polled native stamina instead of PSRP-style sleep-linked meter/oxygen','F11 duplicated map editors and mixed tools/services above reference categories','F10 mixed diagnostics/imports/services and bank transfers lacked ordinary-failure rollback','optional per-profile systems required repeated manual setup contrary to user roleplay request','hotkeys could act between native character change and profile reload','discard/move changed memory before durable save'],
 'implemented':'One-time RoleplayVersion1 wallet/gang upgrades outside active activities, retaining valid pending snapshots, enables traffic advanced offences/pursuit/bribe/BOLO/custody/presence/territories/war/rival simulation/reinforcement/driveby without changing funds/ownership/history. Subsequent explicit advanced choices retained. Runtime default legacy1.1/1.6 food/water rates normalized to1/min, other finite rates retained/capped; sleep1/90s, rest10/min, frame-rate independent sleep-linked stamina7*(1-sleep/200)/s, recovery15/s,5s cooldown with10/s recovery. Actual underwater oxygen, warnings/phoneFitness history, safe foot collapse, nonlethal HP fatigue,5s checkpoints/wrap/pause/stall handling. Durable inventory receipt before any benefit, idempotent needs recovery, no health replay, side-effect callbacks isolated. F11 eight reference categories, technical controls under UI advanced and test/teleport under CheatMenu; F10 four fixed groups. Inventory actual6x4 native source geometry/YTD,24slot pages with saved positions/unknown items preserved; legacy25slot API kept compatible.',
 'durability':'NeedsData extra metadata retained; optional Stamina/Cooldown/LastConsumptionId and inventory Consumption receipt; item quantities/containers and wallet money/licenses unchanged by preference upgrades. Inventory and needs files use recovery protocol, not cross-file atomic claim; native healing never replayed after reload. Bank deposit/withdraw ordinary failure restores cash+bank. Settings/Keys/Items/Visuals/Gangs JSON and OIV/RPF/YTD not rewritten by release. Runtime authorized one-time profile preference writes have bak and preserve later choices; active activities/corrupt journals defer; valid suspended journals are retained.',
 'intentional_differences':'No frame-dependent hunger double-drain or stamina regeneration, repeated warning spam or forced passout while driving/in native mission/job. Collapse uses bounded own ragdoll on foot only; no source indefinite drunk animation/forced camera/native stamina reset/swim-speed takeover. Manual weather/time and mission start still deliberate actions, not automatic. No 90item cap; old unknown items kept. Legacy5x5 helper retained but runtime uses6x4; UI remains custom rendering around installed reference assets, not exact claimed pixel parity.',
 'remaining':'Physical GTA testing still required: layouts/YTD/scaling and all input, hunger/thirst/rest/stamina/oxygen/HP, item use/cancel/reload/save failure, automatic profile upgrade and existing tickets/territory events, F2/teleport/bank/profiles. Phase8 remains ongoing for other service/job-specific menus/phone/inventory details; phase9 whole activity realism unchanged. No claim to have eliminated every game-only bug.'}
report['phase7_base_closure']['next']='Phase8 ongoing0.90 needs/consumption/profile defaults/F11/F10/6x4 inventory; then remaining service-specific UI; phase9 activity realism.'
report['sleep_recovery_hotfix']={
 'problem':'0.90 collapsed every30s for hunger/thirst/sleep without recovering energy, and did not correlate native bed sleep with RPCore needs. User confirmed repeated falling after food/Franklin bed.',
 'reference_source':'PSRP RadialMenu sleep phase fills Sleep100; HelperSystem passout animation and UiSystem sleep/stamina. New implementation independent; no game execution.',
 'implemented':'Sleep-only collapse once per durable10s episode; one bounded native ragdoll, actual-world pause/stall filtering, remaining-time checkpoints, actor/profile/shared gate binding and input guard. Completion saves Sleep100/Stamina100/cooldown0 before releasing run, preserves food/water/receipts/unknown metadata. No repeated30s collapse; food recovery no longer restores stale sleep snapshot. Native safehouse sleep detector correlates nearby-home/scene-control-fade with4-12h clock jump including midnight; RPCore manual/real-clock writes excluded, auto sync yields during native loss of control.',
 'vehicle_safety':'No start in vehicle/boarding/water/air/fall/climb/combat/ragdoll/native mission/wanted/menu/other activity. In-vehicle fatigue does not disable sprint control. Forced boarding/mission/death/profile change suspends own recovery without ejection/warp/freezing/vehicle natives; death hospital reset clears valid episode. Resume only in safe foot context.',
 'durability':'Optional Needs.SleepRecovery Id/Remaining0..10000, absent legacy; begin and wake clone/save-before-assignment with rollback on write failure. Failure after elapsed0 retains completed pending record without restarting ragdoll; native health never healed or replayed. Bed recognition is a contextual heuristic, not a definitive GTA sleep event; GTA validation pending especially custom beds/maps/clock mods.',
 'remaining':'GTA user must check single10s ground recovery, sprint after wake, native Franklin old/new house bed, save/reload and vehicle/boarding/no ejection. Hunger/thirst still require food/water; sleep does not refill them. Physical ragdoll/bed recognition not homologated by unit tests.'}
report['inventory_armory_refinement']={
 'reference_source':'PSRP/NewInventorySystem.cs: InventoryItem.Weapon, UseSelectedItem, SyncWeapons, SyncAmmo, attack-input decrement, SaveInventory/LoadInventory; SettingsMenu.cs: RequiresWeaponInInventory and VanillaWeaponWheelDisabled; ItemDatabase.cs weapon and ammo definitions.',
 'reference_defaults':'Require Weapon In Inventory true; Vanilla Weapon Wheel Disabled false. Independent flags; weapon item remains after equip. SyncWeapons/SyncAmmo called in inventory Open state; attack input removes one round without verifying actual native ammo delta; UseSelectedItem can Give250.',
 'implemented':'Always active RP inventory ownership, default hidden wheel with persisted F11GameSettings option, foot guards for equip/edits, personal ammo tracking continues in vehicles without taking over driving/weapon controls, and temporary activity/mission weapons excluded from import. Existing firearms/melee/heavy weapons imported once per profile into optional Inventory.Armory and existing Items; shared native ammo pools merged with MAX, never summed per gun. Native ammo delta persisted before acknowledging baseline, including automatic fire/reload/pickups; no 250-round equip bonus. Throwable stacks decrement their actual projectile count, no duplicate bullet item. Equip/unarm from inventory and storage/drop enforce owned inventory quantities; tint, clip bounds and components recorded, unknown JSON metadata retained.',
 'durability':'Clone/save-before-assignment for import/ammo/item transfer/drop; ordinary write failure retains previous object and retries ammo delta while attack input is held for that frame. Authoritative inventory quantity applied to native ammo on load/equip/edits; total ammo shared only by actual native ammo type. No native all-weapons removal. In-memory/native actions and JSON are not a single crash-atomic transaction; a crash before the next frame can lose the latest observation. HomeStorage gate explicitly allowed; other activities/native missions yield; vehicles keep native wheel available while personal ammo remains tracked.',
 'deliberate_differences':'Source wheel-hide default false changed to true as requested for RP; source flag requiring inventory is always active outside protected contexts. Actual native ammo types retained instead of merging all PSRP Light weapons into one incompatible GTA pool. No copying source Give250 or button-based ammo accounting. Utilities such as petrol can/extinguisher/parachute/scanner remain native. No separate new Ammu-Nation shop: native firearm/ammo purchases and pickups are observed in free roam; job loans excluded until absent again.',
 'remaining':'GTA tests required: migration, keyboard/controller wheel, actual automatic fire, reload, throw final grenade, native ammo pickups/purchases, stow/retrieve/accessories, save/reload, F2/J/F11, protagonist and MP profile transitions, mission/job loans and vehicles. No gameplay parity claim or claim all phase8 UI bugs are solved.'}
report['inventory_review_093']={
 'scope':'Grouped inventory refinement after user requested first-six-slot hotkeys and full review instead of individual small releases. Static decompiled reference, independent implementation, no GTA execution.',
 'reference_flows':{'PSRP/Main.cs':'onKeyDown Keys49..54 -> HotkeySlot0..5; polling IsKeyPressed on any keydown can repeat use','PSRP/NewInventorySystem.cs':'HotkeySlot/UseSelectedItem/HandleInput, 6x4 visible grid/15 rows, header/weight/scrollbar/craft UI, reference recipes/reusable items, held-item swap, stack/unit discard, transfers, ammo/throwables, save/load, Update timers/props and external activity actions','PSRP/ItemDatabase.cs':'weapon names/icons; food/drink/energy, returned containers, action tools and activity products','PSRP/NewStorageSystem.cs':'two grids, source storage begins1040/230, side switch and unit/stack transfers','PSRP/NewShopSystem.cs':'categories/grid/purchase from bank; purchase may partially fill before failure in source AddItem'},
 'implemented':['bare keyboard1..6 uses absolute slots0..5 once per keydown; gaps/NumPad/modifiers/configured bindings/modal/profile/activity guards','first-row numeric labels, rolling6-column/4-row viewport, weight bar, reference63 weapon names/icons incl C4=c4, truthful item details','inventory and deposits/trunks/apartment side-by-side with Tab/RB side switch, unit/stack transfers and close callback releasing only home-storage owner','31 additional non-alcohol food/drink/material/phone definitions; existing19 definitions/quantities/prices preserved; metadata fallback for energy/returned containers','9 personal recipes: four shake flavors x milk/water and dirty-shaker washing, three reference-only ingredient slots, exact recipe matching, batch1..100/weight/overflow validation, one saved candidate, no preview duplication','6.5s bounded food/drink use with async animation/one tagged own prop; native pause/stalls excluded, actor/profile/vehicle/combat/mission interruption, no replay/refund of consumed effect; original inventory reopens after normal completion','optional idempotent SleepBoost applies to current sleep instead of stale receipt snapshot; returned empty bottle/dirty shaker committed with item consumption','inventory transfers use24-slot runtime reconciliation and direct candidate write before assignment; high saved positions/unknown metadata retained','fresh native acquisition under continuous tracking may replace a fully discarded gun, no other copy in containers; new untracked ammo pool added once per actual type, MAX retained for initial/ambiguous observations','store purchase persists wallet and inventory immediately with ordinary-failure cash rollback; presentation callbacks after commit'],
 'intentional_differences':['RPCore aggregate Items dictionary and existing container/financial/job saves retained; no arbitrary per-slot stack split or destructive90-slot limit','legacy two-argument25-slot layout helper remains for compatibility, runtime transfers use24; one old test expectation updated accordingly','existing cash payment/prices maintained rather than switching all stores to source bank-card pricing; native cash plus JSON are not crash-atomic','UI displays controls actually implemented instead of source help textures with different bindings','native AmmoType pools and no Give250/button-based ammo accounting retained from0.92; utilities remain native'],
 'not_claimed_as_implemented':['source alcohol/drug intoxication/every medical condition','all source drug/bong recipes and every inventory-only hand tool action','source umbrella reusable prop, screwdriver parking-meter/plate theft and fuel-can fueling','source hunting bait/material drops/fish and all source shop/selling categories as one personal inventory; existing separate job stocks preserved','source queued InventoryBoxNoti notification skin versus existing RPCore feed/UI','every native Ammu-Nation reacquisition flow after interruption of player control; ambiguous/restored snapshots remain conservative','per-instance split stacks/durability/mouse drag-and-drop or exact visual/runtime parity'],
 'phase_boundaries':'No automatic conversion of hunting/fishing/dealer/gang/crafting Wallet stocks or changes to job rewards, permits and progress. Activity realism/inventory integration stays phase9; remaining distinct inventory tools/intoxication are explicitly listed for remaining phase8, not hidden by the hotbar implementation.',
 'verification':'3541 data tests (3482 baseline +59 new); Release net48 compiled. Unit coverage includes exact key mapping/gaps, scrolling, no preview mutation, recipe/batch/weight/overflow/tool/save-failure invariants, retained high storage slots/metadata, energy idempotence and container consumption, ordinary purchase failure rollback. GTA input/rendering/animations/native actions require user test.'}
report['inventory_mouse_094']={
 'reference':'PSRP/NewInventorySystem DrawInventoryGrid/DrawSlot uses6x4 visible cells140px on130px pitch, left140/top230; NewStorageSystem right1040. Source consulted uses control navigation; mouse gesture implementation is independent RPCore code requested by user.',
 'implemented':'Native GTA cursor while grid visible; mouse click selects, left hold/release moves a full personal inventory position or swaps with occupied destination using existing MoveSlotSaved save-before-assignment.6design-pixel movement threshold, ghost icon and source cue; wheel scrolls hovered grid during drag. Same scale/insets/aspect transform as Draw and reverse-order hit testing matching overlapping source sprites.',
 'safety':'No held item removed from counts. Outside/same-cell/stale-source release, right click, other key, close/reload/pause/controller switch cancel; opening with held button waits for fresh press. Stationary cursor does not override keyboard navigation. Mouse drag blocks hotbar actions and controller activation on handover. Cursor/camera/button suppression is frame-only inside view. Storage right side selectable; cross-grid drag never transfers/discards. Existing keyboard/controller/Enter transfer/recipes unchanged.',
 'remaining':'GTA test cursor native axes/rendering and button transitions in fullscreen/windowed/16:9/ultrawide/safe-zone/scale, drag/swap/cancel/wheel/hotbar persistence, controller handover, deposits and inventory close/reload. No claim of live GTA validation or mouse transfer/crafting/double-click use.'}
report['inventory_review_093']['not_claimed_as_implemented']=[x.replace('/mouse drag-and-drop','') for x in report['inventory_review_093']['not_claimed_as_implemented']]
report['inventory_burden_094']={
 'reference':'PSRP NewInventorySystem WeightInInventory/MaxWeightInventory75 used in UpdateWeight, DrawHeader and AnimateWeightBar. No movement restriction linked to inventory mass found in consulted source; own behavior requested by user before publication.',
 'implemented':'Existing item weight x quantity and MaxWeightKg retained; double accumulation prevents float overflow and1g tolerance prevents false overload. Above capacity, progressively lower per-frame SET_PED_MOVE_RATE_OVERRIDE from~0.8 to minimum0.45 and disable sprint/jump for that frame. Once normal or unsafe, stop applying, without global run/swim multipliers, control re-enable, entity velocity, health damage or ragdoll. Notify on condition transition and inventory status.',
 'context':'Only alive human current-profile controlled ped on foot; no vehicle/boarding, swimming, air/fall, ragdoll/climb, pause/cutscene/native mission. Separate job/container stocks not counted. Own job foot movement uses personal load; no job save/reward changes.',
 'offloading':'Ctrl+Enter computes maximum quantity that fits receiver, count/weight/counter caps revalidated by existing atomic candidate transfer. Shift+Enter still requests full stack; no auto discard/migration or deleted ammunition. Existing configured weights/capacity, including zero auto-registered weapon mass, retained.',
 'validation':'3602 total tests,61 new versus0.93:27 mouse and34 burden/capacity. Build net48 compiled. Actual GTA cursor and movement rate/restore still require user test.'}
report['inventory_fixes_095']={
 'reference':'NewInventorySystem UsingDrink/UsingFood/Closed explicitly deletes consumption model; SettingsMenu OnTick applies weapon controls independently per frame. Reference removal methods remove selected unit/stack; no recoverable ground-drop flow found in consulted inventory source.',
 'cleanup':'Own-tagged entity handle/model/token retained across deletion attempts; detach, mission ownership, DELETE_OBJECT and existence confirmation; retry250ms; recover only RPCoreInvUse leftovers after reload. No nearby-model deletion or global task clear.',
 'input':'Final main-tick input policy independent of armory IO/gate and inventory fault state; preserves existing preference. Foot activities no longer bypass wheel policy. Native missions, vehicles/boarding, pause/cutscene/no-control preserved.',
 'ground':'Own implementation: Delete/LB leave unit/stack, generic blue marker; keyboard R/Shift+R in personal inventory recovers unit/what fits within1.8m and same interior. Optional per-profile GroundItems in same Inventory.json candidate commit; no expiry, max128 refuses new pile, no ammo minting or weapon metadata loss. Invalid records/unknown definitions retained, no cross-profile pickup.',
 'validation':'3639 automated data/policy tests; net48 build. GTA native cleanup/controls/marker position remain pending user test. User confirmed0.94 mouse/weight mechanics before this update.'}
report['shops_and_consumable_presentation_096']={
 'reference':'NewShopSystem DrawCategories/DrawGrid/HandleInput/PurchaseSelectedItem:6x4 visible,140 sprites/130 pitch, catalogue1040/230. ItemDatabase model/fallback/left-hand attachment and drink/food flows reviewed.',
 'shops':'Existing conveniences and editor Grocery use categorized grid and read-only personal inventory; optional existing list preference retained. Mouse selects products, explicit Buy button release confirms, categories/quantity/max/feedback, keyboard/controller with fresh-button handover. No fixed7-category/42-product cutoff. Optional ItemDef.Category keeps custom grouping without rewriting configs.',
 'purchase':'Existing cash prices and BuySaved rollback retained. Max1..100 respects money/mass/counter; each confirmation revalidates context/item identity/quantity. No partial order after explicit confirmation, no automatic consumption or task stock migration. Per-shop gate/modal closes safely; no position freeze or warp.',
 'consumables':'Found two prop producers: InventorySystem BeginUsing plus subscribed PropPresentation.Used. Removed duplicate internal subscription; phone presentation stays separate. Single owned inventory prop uses cloned custom bindings or reference model/fallback plans; async bounded loading and0.95 tagged cleanup. Existing untagged historical leftovers require a full game restart; never deleted by nearby model.',
 'validation':'3686 automated data tests,47 new; net48 build. Native GTA drawing/cursor/controller/prop presentation remain pending user test. Existing111 payload JSON/INI are preserved.'}
report['global_menus_097']={
 'reference':'SettingsMenu hierarchy/native checkboxes; RadialMenu root/inventory pages,6-item pagination+2 navigation slots, type-based nearest GPS and dormant/WIP emote list reviewed.',
 'settings':'Eight F11 categories retained. Native checkbox/list preferences refresh and persist before apply; ScriptSettings.Save false handled. Same-directory staging+replace+backup; no new RP activation flags. Guards close own menus on unsafe context. Waypoint teleport unchanged.',
 'radial':'Original eight sectors/centers/textures retained. Mouse sector selection, same-sector release confirmation, center/outside/help exclusion, back/pagination and controller fresh-button handover. Owned inventory filters food/drink/health/weapons/reusable phone use actual UseSelected after modal closes; queued actions revalidate actor/profile/control/time. No duplicate inventory or direct native ammo grants.',
 'gps':'Actual runtime grocery/fuel/barber/garage/owned-workshop and active editor locations exposed without changing job/price/config data. Revalidate nearest on confirm. Jobs grouped; personal pins add/rename/delete candidate saved before assignment with second delete confirmation and stale-data check. Invalid or oversized pin lists retained without silent filter/truncation.',
 'emotes':'Two short hands-up gestures from source list plus existing hello/clap, asynchronous load and owned-animation stop; activity gate released on cancel/unsafe/timeout. Source root Emotes was WIP. Couch/wall-dependent poses deferred until support anchoring; no global task clear.',
 'differences':'Garage replaces source ClothingShop GPS because local wardrobe requires owned apartment. No third-eye/ped crimes, all radial car/clothing pages, or all reference store categories claimed. Other systems and all phase9 activity refinement explicitly remain in coverage checklist.',
 'validation':'3725 data tests,39 new. Previous0.96 stores/consumables confirmed working by user. Native cursor/menu/gestures/GPS integration require GTA test.'}
a.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(f'{len(files)} reference files indexed; {a.output.name}')
