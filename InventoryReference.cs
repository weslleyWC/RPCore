using System.Collections.Generic;
namespace RPCore
{
    public static class InventoryReference
    {
        public static T Load<T>(string name)
        {using(var stream=typeof(InventoryReference).Assembly.GetManifestResourceStream("RPCore.Inventory."+name))using(var reader=new System.IO.StreamReader(stream))return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(reader.ReadToEnd());}
        // Presentation data read from the supplied PSRP ItemDatabase; runtime logic is RPCore.
        public static readonly Dictionary<string,string[]> Weapons=new Dictionary<string,string[]>
        {
            {"Knife",new[]{"Knife","knife"}},
            {"Nightstick",new[]{"Nightstick","nightstick"}},
            {"Bat",new[]{"Bat","bat"}},
            {"Crowbar",new[]{"Crowbar","crowbar"}},
            {"BattleAxe",new[]{"Battle Axe","battleaxe"}},
            {"KnuckleDuster",new[]{"Knuckle Duster","knuckleduster"}},
            {"Machete",new[]{"Machete","machete"}},
            {"Dagger",new[]{"Dagger","dagger"}},
            {"Hatchet",new[]{"Hatchet","hatchet"}},
            {"SwitchBlade",new[]{"Switchblade","switchblade"}},
            {"Pistol",new[]{"Taurus PT92AF","pistol"}},
            {"CombatPistol",new[]{"Beretta Px4","combatpistol"}},
            {"APPistol",new[]{"Colt Scamp","appistol"}},
            {"Pistol50",new[]{"Desert Eagle","pistol50"}},
            {"HeavyPistol",new[]{"Enterprise 1911","heavypistol"}},
            {"SNSPistol",new[]{"H&K P7M10","snspistol"}},
            {"VintagePistol",new[]{"FN Model 1922","vintagepistol"}},
            {"MarksmanPistol",new[]{"Center Contender","marksmanpistol"}},
            {"Revolver",new[]{"MP412 REX","revolver"}},
            {"DoubleActionRevolver",new[]{"Colt M1892","doubleaction"}},
            {"CeramicPistol",new[]{"H&K P7","ceramicpistol"}},
            {"NavyRevolver",new[]{"1851 Colt Navy","navyrevolver"}},
            {"StunGun",new[]{"Stun Gun","stungun"}},
            {"FlareGun",new[]{"Flare Gun","flaregun"}},
            {"MicroSMG",new[]{"Mini Uzi","microsmg"}},
            {"SMG",new[]{"MP5","smg"}},
            {"AssaultSMG",new[]{"P90 Bullpup","assaultsmg"}},
            {"MiniSMG",new[]{"SKorpion","minismg"}},
            {"MachinePistol",new[]{"TEC-9","machinepistol"}},
            {"CombatPDW",new[]{"SIG MPX","combatpdw"}},
            {"TacticalSMG",new[]{"B&T MP9","tacticalsmg"}},
            {"PumpShotgun",new[]{"Mossberg 590","pumpshotgun"}},
            {"SawnOffShotgun",new[]{"Mossberg 500","sawnoffshotgun"}},
            {"BullpupShotgun",new[]{"Kel-Tec KSG","bullpupshotgun"}},
            {"HeavyShotgun",new[]{"Saiga-12","heavyshotgun"}},
            {"DoubleBarrelShotgun",new[]{"Ranger 12","doublebarrelshotgun"}},
            {"CombatShotgun",new[]{"SPAS-12","combatshotgun"}},
            {"AssaultShotgun",new[]{"UTS-15","assaultshotgun"}},
            {"SweeperShotgun",new[]{"Armsel Striker","sweepershotgun"}},
            {"AssaultRifle",new[]{"AK-47","assaultrifle"}},
            {"AdvancedRifle",new[]{"CTAR-21","advancedrifle"}},
            {"BattleRifle",new[]{"FN FAL","battlerifle"}},
            {"BullpupRifle",new[]{"QBZ-95","bullpuprifle"}},
            {"CarbineRifle",new[]{"AR-15","carbinerifle"}},
            {"CompactRifle",new[]{"Draco AK","compactrifle"}},
            {"SpecialCarbine",new[]{"G36C","specialcarbine"}},
            {"HeavyRifle",new[]{"FN SCAR-16","heavyrifle"}},
            {"SniperRifle",new[]{"L96A1","sniperrifle"}},
            {"HeavySniper",new[]{"Barrett M82","heavysniper"}},
            {"MarksmanRifle",new[]{"M39 EMR","marksmanrifle"}},
            {"PrecisionRifle",new[]{"Remington 700","precisionrifle"}},
            {"RPG",new[]{"RPG-7","rpg"}},
            {"Minigun",new[]{"M134 Minigun","minigun"}},
            {"GrenadeLauncher",new[]{"M79","grenadelauncher"}},
            {"HomingLauncher",new[]{"FIM-92 Stinger","hominglauncher"}},
            {"Grenade",new[]{"Frag Grenade","grenade"}},
            {"StickyBomb",new[]{"C4","c4"}},
            {"SmokeGrenade",new[]{"Smoke Grenade","smokegrenade"}},
            {"BZGas",new[]{"Tear Gas","bzgas"}},
            {"ProximityMine",new[]{"Proximity Mine","proximitymine"}},
            {"Molotov",new[]{"Molotov Cocktail","molotov"}},
            {"PipeBomb",new[]{"Pipe Bomb","pipebomb"}},
            {"Flare",new[]{"Flare","flare"}},
        };
        public static readonly Dictionary<string,float> Energy=new Dictionary<string,float>
        {
            {"redbull",10f},
            {"pepsi",2f},
            {"sprite",2f},
            {"monster",10f},
            {"coffee",12f},
        };
        public static readonly Dictionary<string,string> Returns=new Dictionary<string,string>
        {
            {"water","psrp_emptybottle"},
            {"milk","psrp_emptybottle"},
        };
    }
}
