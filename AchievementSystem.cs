using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public class AchDef
    {
        public string Id;
        public string Name;
        public string Desc;
        public string Stat;   // counter this achievement watches
        public int Target;
    }

    public class AchData : ExtensibleData
    {
        public Dictionary<string, int> Stats = new Dictionary<string, int>();
        public List<string> Unlocked = new List<string>();
    }

    public class AchievementSystem : ISystem
    {
        public string Name { get { return "achievements"; } }

        readonly ObjectPool pool;
        readonly NativeMenu menu = new NativeMenu("Achievements", "");
        readonly List<AchDef> defs;
        AchData d;
        public event Action<string, int> Progress;

        static List<AchDef> DefaultDefs()
        {
            return new List<AchDef>
            {
                new AchDef { Id = "first_drop",  Name = "First Delivery",   Desc = "Drop off your first pizza.",        Stat = "pizza_drops",  Target = 1 },
                new AchDef { Id = "courier",     Name = "Courier",          Desc = "Make 25 pizza drops.",              Stat = "pizza_drops",  Target = 25 },
                new AchDef { Id = "shift",       Name = "Clocked Out",      Desc = "Finish 5 full shifts.",             Stat = "shifts_done",  Target = 5 },
                new AchDef { Id = "pocket",      Name = "Pocket Money",     Desc = "Earn $5,000 from jobs.",            Stat = "cash_earned",  Target = 5000 },
                new AchDef { Id = "regular",     Name = "Regular",          Desc = "Buy 20 items from stores.",         Stat = "items_bought", Target = 20 },
                new AchDef { Id = "spender",     Name = "Big Spender",      Desc = "Spend $2,000 in stores.",           Stat = "cash_spent",   Target = 2000 },
                new AchDef { Id = "habit",       Name = "Creature of Habit",Desc = "Use 30 items.",                     Stat = "items_used",   Target = 30 },
                new AchDef { Id = "safety",      Name = "Safety First",     Desc = "Buckle up 10 times.",               Stat = "belts_on",     Target = 10 },
            };
        }

        public AchievementSystem(ObjectPool pool)
        {
            this.pool = pool;
            defs = Store.LoadDef("AchievementDefs.json", DefaultDefs).Where(a => a != null && !string.IsNullOrEmpty(a.Id) && a.Target > 0).GroupBy(a => a.Id).Select(g => g.First()).ToList();
            if (!defs.Any(d => d.Id == "pdm_owner")) defs.Add(new AchDef { Id="pdm_owner",Name="Minha empresa",Desc="Adquira a empresa PDM.",Stat="pdm_owned",Target=1 });
            if (!defs.Any(d => d.Id == "pdm_sale")) defs.Add(new AchDef { Id="pdm_sale",Name="Primeiro cliente",Desc="Conclua uma entrega com venda PDM.",Stat="pdm_sales",Target=1 });
            if (!defs.Any(d => d.Id == "drag_first")) defs.Add(new AchDef { Id="drag_first",Name="Na pista",Desc="Resolva uma corrida de arrancada.",Stat="drag_finished",Target=1 });
            if (!defs.Any(d => d.Id == "drag_win")) defs.Add(new AchDef { Id="drag_win",Name="Primeiro na chegada",Desc="Venca uma arrancada.",Stat="drag_wins",Target=1 });
            if (!defs.Any(d => d.Id == "stream_first")) defs.Add(new AchDef { Id="stream_first",Name="Ao vivo",Desc="Confirme o primeiro bloco da live.",Stat="stream_blocks",Target=1 });
            if (!defs.Any(d => d.Id == "stream_complete")) defs.Add(new AchDef { Id="stream_complete",Name="Live completa",Desc="Conclua uma transmissao.",Stat="stream_completed",Target=1 });
            if (!defs.Any(d => d.Id == "dealer_first")) defs.Add(new AchDef { Id="dealer_first",Name="Primeiro contato",Desc="Conclua uma venda de rua.",Stat="dealer_sales",Target=1 });
            if (!defs.Any(d => d.Id == "dealer_session")) defs.Add(new AchDef { Id="dealer_session",Name="Turno de rua",Desc="Conclua um turno de vendas.",Stat="dealer_sessions",Target=1 });
            if (!defs.Any(d => d.Id == "dealer_regular")) defs.Add(new AchDef { Id="dealer_regular",Name="Contatos pela cidade",Desc="Conclua vinte vendas de rua.",Stat="dealer_sales",Target=20 });
            if (!defs.Any(d => d.Id == "craft_first")) defs.Add(new AchDef { Id="craft_first",Name="Primeiro produto",Desc="Fabrique um produto na bancada.",Stat="crafting_products",Target=1 });
            if (!defs.Any(d => d.Id == "craft_batch")) defs.Add(new AchDef { Id="craft_batch",Name="Artesao",Desc="Complete dez lotes de fabricacao.",Stat="crafting_batches",Target=10 });
            if (!defs.Any(d => d.Id == "craft_sale")) defs.Add(new AchDef { Id="craft_sale",Name="Venda na feira",Desc="Venda produtos fabricados.",Stat="crafting_sales",Target=1 });
            if (!defs.Any(d => d.Id == "fish_first")) defs.Add(new AchDef { Id="fish_first",Name="Primeiro peixe",Desc="Fisgue e recolha um peixe.",Stat="fishing_catches",Target=1 });
            if (!defs.Any(d => d.Id == "fish_trip")) defs.Add(new AchDef { Id="fish_trip",Name="Pescaria concluida",Desc="Complete uma pescaria.",Stat="fishing_trips",Target=1 });
            if (!defs.Any(d => d.Id == "fish_sale")) defs.Add(new AchDef { Id="fish_sale",Name="Peixe vendido",Desc="Venda peixes na banca.",Stat="fishing_sales",Target=1 });
            if (!defs.Any(d => d.Id == "hunt_first")) defs.Add(new AchDef { Id="hunt_first",Name="Primeira coleta",Desc="Colete um animal durante a expedicao de caca.",Stat="hunting_harvests",Target=1 });
            if (!defs.Any(d => d.Id == "hunt_trip")) defs.Add(new AchDef { Id="hunt_trip",Name="Expedicao concluida",Desc="Conclua uma expedicao de caca.",Stat="hunting_trips",Target=1 });
            if (!defs.Any(d => d.Id == "hunt_sale")) defs.Add(new AchDef { Id="hunt_sale",Name="Venda da caca",Desc="Venda materiais de caca no acougue.",Stat="hunting_sales",Target=1 });
            if (!defs.Any(d => d.Id == "pilot_test")) defs.Add(new AchDef { Id="pilot_test",Name="Piloto qualificado",Desc="Conclua o voo de qualificacao ate Sandy Shores.",Stat="pilot_qualified",Target=1 });
            if (!defs.Any(d => d.Id == "pilot_first")) defs.Add(new AchDef { Id="pilot_first",Name="Passageiros em terra",Desc="Conclua uma entrega aerea de passageiros.",Stat="pilot_deliveries",Target=1 });
            if (!defs.Any(d => d.Id == "pilot_return")) defs.Add(new AchDef { Id="pilot_return",Name="De volta ao LSIA",Desc="Devolva uma aeronave apos um voo comercial.",Stat="pilot_returns",Target=1 });
            if (!defs.Any(d => d.Id == "prison_first")) defs.Add(new AchDef { Id="prison_first",Name="Custodia entregue",Desc="Conclua um transporte regular para Bolingbroke.",Stat="prison_transports",Target=1 });
            if (!defs.Any(d => d.Id == "prison_shift")) defs.Add(new AchDef { Id="prison_shift",Name="Onibus de volta",Desc="Devolva o onibus apos entregar a custodia.",Stat="prison_shifts",Target=1 });
            if (!defs.Any(d => d.Id == "tanker_first")) defs.Add(new AchDef { Id="tanker_first",Name="Combustivel entregue",Desc="Conclua uma descarga de combustivel no posto.",Stat="tanker_drops",Target=1 });
            if (!defs.Any(d => d.Id == "tanker_shift")) defs.Add(new AchDef { Id="tanker_shift",Name="De volta a LS GAS",Desc="Devolva o conjunto apos uma entrega de combustivel.",Stat="tanker_shifts",Target=1 });
            if (!defs.Any(d => d.Id == "supply_first")) defs.Add(new AchDef { Id="supply_first",Name="Reposicao iniciada",Desc="Conclua uma entrega de reposicao de loja.",Stat="supply_drops",Target=1 });
            if (!defs.Any(d => d.Id == "supply_shift")) defs.Add(new AchDef { Id="supply_shift",Name="Lojas abastecidas",Desc="Conclua um turno de reposicao com retorno do caminhao.",Stat="supply_shifts",Target=1 });
            string legacy = Path.Combine(Cfg.Dir, "Achievements.json");
            if (File.Exists(legacy))
            {
                try
                {
                    var entries = JObject.Parse(File.ReadAllText(legacy));
                    var stats = new Dictionary<string, string> { { "0", "started" }, { "1", "bank_used" }, { "3", "bank_million" }, { "4", "uber_trips" }, { "5", "pizza_drops" }, { "6", "postop_drops" }, { "7", "cars_bought" }, { "9", "garages_bought" }, { "12", "items_bought" }, { "13", "first_aid" }, { "15", "gas_filled" }, { "16", "storage_used" }, { "19", "belts_on" }, { "20", "radio_started" }, { "27", "hunting_harvests" }, { "34", "valet_returns" } };
                    foreach (var entry in entries.Properties())
                    {
                        if (!(entry.Value is JObject)) continue;
                        string stat; stats.TryGetValue(entry.Name, out stat);
                        defs.Add(new AchDef { Id = "psrp_" + entry.Name, Name = (string)entry.Value["Name"] ?? entry.Name, Desc = (string)entry.Value["Description"] ?? "", Stat = stat, Target = 1 });
                    }
                }
                catch (Exception ex) { Log.Error("legacy achievement definitions", ex); }
            }
            pool.Add(menu); Visuals.Register(menu, "achievements");
            Reload();
        }

        public void Reload()
        {
            d = Store.Load("Achievements.json", () => new AchData());
            if (d.Stats == null) d.Stats = new Dictionary<string, int>();
            if (d.Unlocked == null) d.Unlocked = new List<string>();
            menu.Visible = false;
        }

        int Get(string stat) { int v; return d.Stats.TryGetValue(stat, out v) ? v : 0; }

        public void Bump(string stat, int n = 1)
        {
            if (n <= 0) return;
            d.Stats[stat] = (int)System.Math.Min(int.MaxValue, (long)Get(stat) + n);
            foreach (AchDef a in defs.Where(x => x.Stat == stat && !d.Unlocked.Contains(x.Id) && Get(stat) >= x.Target))
            {
                d.Unlocked.Add(a.Id);
                Notify.Show("~y~Achievement:~s~ " + a.Name);
                Sfx.Play("achievement");
            }
            Progress?.Invoke(stat, n);
        }

        void Rebuild()
        {
            menu.Clear();
            menu.Name = d.Unlocked.Count + " / " + defs.Count + " unlocked";
            foreach (AchDef a in defs)
            {
                bool done = d.Unlocked.Contains(a.Id);
                var item = new NativeItem(a.Name, a.Desc)
                {
                    AltTitle = done ? "~g~Done" : string.IsNullOrEmpty(a.Stat) ? "PSRP: not implemented" : System.Math.Min(Get(a.Stat), a.Target) + "/" + a.Target
                };
                menu.Add(item);
            }
        }

        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode != Cfg.AchKey) return;
            if (menu.Visible) { menu.Visible = false; return; }
            if (pool.AreAnyVisible) return;
            Rebuild();
            menu.Visible = true;
        }

        public void Tick() { }
        public void Save() { Store.Write("Achievements.json", d); }
        public void Dispose() { }
    }
}
