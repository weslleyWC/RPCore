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
        'scope_limits': 'Eclipse ownership and access only. No wardrobe/editor, apartment storage, rental/hotels, furniture, garage ownership or needs changes. Native IPL/floor/framing and entry/exit require GTA testing. PDM differences and phases 8/9 remain.',
        'configuration_sha256': digest(housing_config.read_bytes())}
a.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(f'{len(files)} reference files indexed; {a.output.name}')
