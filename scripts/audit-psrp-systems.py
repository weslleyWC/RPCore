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
        'intentional_differences':'Initial capture module only: default three pistol defenders versus full PSRP ambient forces/weapons/reinforcements. Explicit F4 attack and E capture, own 600s limit, on-foot45m center, optional map; no automatic attack trigger/cutscene/relations changes. Combat deaths of all owned defenders count, disappearance does not. No money/reputation award, turf stock/commerce, gang AI battles or automatic counterattack.',
        'configuration_sha256':digest(turf_config.read_bytes()),
        'scope_limits':'Native combat, paths/floor, airport/restricted access, wanted, coexistence with crew/police/mods require GTA tests. No automatic PSRP TurfOwnership import, complete gang economy, reinforcement/convoy or police integration. Phases 8/9 retained.'}
a.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(f'{len(files)} reference files indexed; {a.output.name}')
