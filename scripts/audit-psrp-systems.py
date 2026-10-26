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
a.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(f'{len(files)} reference files indexed; {a.output.name}')
