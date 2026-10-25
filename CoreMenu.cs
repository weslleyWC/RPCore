using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;
using Newtonsoft.Json.Linq;

namespace RPCore
{
    public class CoreMenu : ISystem
    {
        public string Name { get { return "coremenu"; } }
        readonly ObjectPool pool;
        readonly AchievementSystem ach;
        readonly Action save;
        readonly Action diagnose;
        readonly Action jobs;
        readonly Action housing;
        readonly Action barber;
        readonly Action characters;
        readonly Action insurance;
        readonly Action dmv;
        readonly NativeMenu menu = new NativeMenu("RPCore", "");
        public string RequestedProfile;
        bool dirty;
        public CoreMenu(ObjectPool pool, AchievementSystem ach, Action save, Action diagnose = null, Action jobs = null, Action housing = null, Action barber = null, Action characters = null, Action insurance = null, Action dmv = null) { this.dmv = dmv; this.insurance = insurance; this.characters = characters; this.barber = barber; this.housing = housing; this.jobs = jobs; this.pool = pool; this.ach = ach; this.save = save; this.diagnose = diagnose; pool.Add(menu); Visuals.Register(menu, "core"); }

        void Add(string title, string description, Action action)
        {
            var item = new NativeItem(title, description);
            item.Activated += (s, e) => { action(); dirty = true; }; menu.Add(item);
        }
        void Rebuild()
        {
            menu.Clear(); menu.Name = "0.53.0 / " + Profile.Id + " / cash $" + Wallet.Cash + " / bank $" + Wallet.Bank;
            if (dmv != null) Add("DMV / habilitacoes C, M1 e A", "GPS, teoria e pratica de carros/motos, com documentos separados por perfil.", () => { menu.Visible = false; dmv(); });
            if (insurance != null) Add("Seguro veicular / recuperacao", "Cadastro gratuito, GPS e recuperacao de destruido por franquia. Veiculos pessoais registrados nas garagens.", () => { menu.Visible = false; insurance(); });
            if (characters != null) Add("Personagens / criar e carregar", "Freemode masculino/feminino com perfil proprio; confirmacao antes de trocar o ped.", () => { menu.Visible = false; characters(); });
            if (barber != null) Add("Barbearias / cortes e olhos", "GPS, previa reversivel e confirmacao do atendimento.", () => { menu.Visible = false; barber(); });
            if (housing != null) Add("Moradia / Eclipse Towers", "Compra, GPS e acesso ao apartamento do perfil ativo.", () => { menu.Visible = false; housing(); });
            if (jobs != null) Add("Trabalhos / contratos", "Contratos de entregas e motoristas: GPS, contratar e cancelar.", () => { menu.Visible = false; jobs(); });
            if (diagnose != null) Add("Visual assets: diagnose", "Requests installed YTDs and checks every catalog texture's native dimensions. Writes VisualDiagnostics.json; does not verify rendered appearance.", diagnose);
            Add("Save now", "Writes the active profile; keeps previous versions as .bak.", () => { save(); Notify.Show("Save requested. Check RPCore.log for any write errors."); });
            Add("Bank: deposit $100", "RPCore bank balance is separate from other mods.", () => { if (Wallet.Deposit(100)) BankUsed(); else Notify.Show("Insufficient cash or balance limit."); });
            Add("Bank: withdraw $100", "Withdraw to the active character wallet.", () => { if (Wallet.Withdraw(100)) BankUsed(); else Notify.Show("Insufficient bank balance or cash limit."); });
            Add("Reload configuration", "Reads Settings.ini and Keys.ini. Feature switches take effect after script restart.", () => { Cfg.Load(); Notify.Show("Configuration reloaded."); });
            foreach (var directory in new[] { Path.Combine(Cfg.SaveDir, "Players"), Path.Combine(Cfg.Dir, "Legacy", "SaveData", "Players"), Path.Combine("scripts", "PSRP", "SaveData", "Players") }.Distinct())
            {
                if (!Directory.Exists(directory)) continue;
                foreach (string path in Directory.GetFiles(directory, "*.json"))
                {
                    string source = path;
                    string id = "PSRP_" + Profile.SafeId(Path.GetFileNameWithoutExtension(source));
                    Add("Import: " + Path.GetFileNameWithoutExtension(source), "Creates " + id + ". Imports hunger, hydration, cash, bank, gang, portfolio and unlocks. Does not change your ped model or original PSRP saves.", () => Import(source, id));
                }
            }
            if (Directory.Exists(Cfg.SaveDir))
                foreach (string directory in Directory.GetDirectories(Cfg.SaveDir).Where(d => File.Exists(Path.Combine(d, "Wallet.json"))))
                {
                    string id = Path.GetFileName(directory);
                    if (Profile.IsProtagonist(id) || CharacterRules.ValidId(id) && File.Exists(Path.Combine(directory, "Character.json"))) continue;
                    Add("Use profile: " + id, "For freemode peds only. Does not change appearance, position or vehicles.", () => {
                        if (Profile.IsProtagonist(Profile.Detect())) { Notify.Show("Switch to a freemode ped first."); return; }
                        RequestedProfile = id; menu.Visible = false;
                    });
                }
            dirty = false;
        }

        void BankUsed()
        {
            ach.Bump("bank_used"); if (Wallet.Bank >= 1000000) ach.Bump("bank_million");
            Wallet.Save(); ach.Save();
        }

        void Import(string source, string id)
        {
            try
            {
                string target = Path.Combine(Cfg.SaveDir, id);
                LegacyImport.ImportPlayer(source, target);
                // Supplementary data never overwrites an existing profile or any source file.
                string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(source)));
                var player = JObject.Parse(File.ReadAllText(source));
                string displayName = (string)player["PlayerName"];
                if (!string.IsNullOrEmpty(displayName) && displayName == Path.GetFileName(displayName) && displayName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0)
                {
                    string portfolio = Path.Combine(root, "StockMarket", displayName + ".portfolio.json");
                    string destination = Path.Combine(Cfg.Dir, "StockMarket", id + ".portfolio.json");
                    if (File.Exists(portfolio) && !File.Exists(destination))
                    {
                        JObject.Parse(File.ReadAllText(portfolio)); Directory.CreateDirectory(Path.GetDirectoryName(destination)); File.Copy(portfolio, destination);
                    }
                }
                string achievements = Path.Combine(root, "Achievements.json");
                if (File.Exists(achievements))
                {
                    var entries = JObject.Parse(File.ReadAllText(achievements));
                    var data = new AchData();
                    foreach (var entry in entries.Properties()) if (entry.Value is JObject && (bool?)entry.Value["Unlocked"] == true) data.Unlocked.Add("psrp_" + entry.Name);
                    new JsonFiles().Write(Path.Combine(target, "Achievements.json"), data);
                }
                Notify.Show("Imported " + id + ". Select Use profile while using a freemode ped.");
            }
            catch (Exception ex) { Log.Error("PSRP import", ex); Notify.Show("Import incomplete or profile already exists. Originals preserved; see RPCore.log."); }
        }
        public void Tick() { if (dirty && menu.Visible) Rebuild(); }
        public void OnKey(KeyEventArgs e) { if (e.KeyCode != Cfg.CoreKey) return; if (menu.Visible) menu.Visible = false; else if (!pool.AreAnyVisible) { Rebuild(); menu.Visible = true; } }
        public void Reload() { menu.Visible = false; }
        public void Save() { }
        public void Dispose() { menu.Visible = false; }
    }
}
