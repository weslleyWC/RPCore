using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA.UI;
using GTA;
using GTA.Native;
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
        readonly Action dmv,traffic,studio,halloween,editor,passenger,world;
        NativeMenu target;readonly NativeMenu services,bank,profiles,tools,imports,legacyProfiles,confirmation;
        NativeMenu[] All=>new[]{menu,services,bank,profiles,tools,imports,legacyProfiles,confirmation};
        readonly NativeMenu menu = new NativeMenu("RPCore", "");
        public string RequestedProfile;
        bool dirty;
        public CoreMenu(ObjectPool pool, AchievementSystem ach, Action save, Action diagnose = null, Action jobs = null, Action housing = null, Action barber = null, Action characters = null, Action insurance = null, Action dmv = null, Action traffic = null, Action studio = null, Action halloween = null, Action editor = null, Action passenger = null, Action world = null) { this.passenger=passenger;this.world=world; this.editor=editor; this.halloween=halloween; this.studio=studio; this.traffic=traffic; this.dmv = dmv; this.insurance = insurance; this.characters = characters; this.barber = barber; this.housing = housing; this.jobs = jobs; this.pool = pool; this.ach = ach; this.save = save; this.diagnose = diagnose; pool.Add(menu); Visuals.Register(menu, "core");services=Group("Servicos e trabalho");bank=Group("Banco");profiles=Group("Personagens e perfis");tools=Group("Avancado / manutencao");imports=Group("Importar saves PSRP");legacyProfiles=Group("Perfis legados");confirmation=Group("Confirmar operacao"); }
        NativeMenu Group(string name){var child=new NativeMenu("RPCore",name);pool.Add(child);Visuals.Register(child,name=="Banco"?"stocks":"core");return child;}

        void Add(string title, string description, Action action)
        {
            var item = new NativeItem(title, description);
            item.Activated += (s, e) => {if(!CanAct())return;try{action();}catch(Exception ex){Log.Error("core menu: "+title,ex);Notify.Show("Operacao nao concluida. Confira RPCore.log.");}finally{dirty=true;}}; (target??menu).Add(item);
        }
        void Rebuild()
        {
            var selected=All.Select(m=>m.Items.Count==0?0:m.SelectedIndex).ToArray();
            menu.Clear();services.Clear();bank.Clear();profiles.Clear();tools.Clear();imports.Clear();legacyProfiles.Clear();menu.Name="RPCore";bank.Name="Banco / saldo $"+Wallet.Bank;
            menu.AddSubMenu(services);menu.AddSubMenu(bank);menu.AddSubMenu(profiles);menu.AddSubMenu(tools);
            target=services;
            if(passenger!=null)Add("Transporte / taxi e limousine","Contratar motorista como passageiro.",()=>{pool.HideAll();passenger();});
            target=tools;
            if(world!=null)Add("Mundo / clima e horario","Controles opcionais de clima e relogio.",()=>{pool.HideAll();world();});
            target=tools;
            if(editor!=null) Add("Map Builder / objetos e locais","Editor persistente por perfil, props proprios, icones e pontos de servico.",()=>{pool.HideAll();editor();});
            target=services;
            if(halloween!=null) Add("Halloween / The Last Ride","Romero, corrida ao cemiterio, perseguidores fantasmas e pulso defensivo.",()=>{pool.HideAll();halloween();});
            target=services;
            if(studio!=null) Add("Contratos / assalto ao estudio","Cinco preparacoes e final de The Hollywood Act.",()=>{pool.HideAll();studio();});
            target=services;
            if(traffic!=null) Add("Policia / transito e multas","Fiscalizacao opcional, patrulha observadora, autuacoes e pagamento bancario.",()=>{pool.HideAll();traffic();});
            target=services;
            if (dmv != null) Add("DMV / habilitacoes C, M1 e A", "GPS, teoria e pratica de carros/motos, com documentos separados por perfil.", () => { pool.HideAll(); dmv(); });
            if (insurance != null) Add("Seguro veicular / recuperacao", "Cadastro gratuito, GPS e recuperacao de destruido por franquia. Veiculos pessoais registrados nas garagens.", () => { pool.HideAll(); insurance(); });
            target=profiles;
            if (characters != null) Add("Personagens / criar e carregar", "Freemode masculino/feminino com perfil proprio; confirmacao antes de trocar o ped.", () => { pool.HideAll(); characters(); });
            target=services;
            if (barber != null) Add("Barbearias / cortes e olhos", "GPS, previa reversivel e confirmacao do atendimento.", () => { pool.HideAll(); barber(); });
            if (housing != null) Add("Moradia / Eclipse Towers", "Compra, GPS e acesso ao apartamento do perfil ativo.", () => { pool.HideAll(); housing(); });
            if (jobs != null) Add("Trabalhos / contratos", "Contratos de entregas e motoristas: GPS, contratar e cancelar.", () => { pool.HideAll(); jobs(); });
            target=tools;
            if (diagnose != null) Add("Diagnostico dos recursos visuais", "Consulta as texturas instaladas e registra dimensoes; nao substitui o teste visual no GTA.", diagnose);
            target=tools;
            Add("Salvar agora", "Grava o personagem atual e conserva a versao anterior em .bak.", () => { save(); Notify.Show("Salvamento solicitado. Confira RPCore.log se houver erro."); });
            target=bank;
            Add("Depositar valor...","Escolha o valor em dinheiro que deseja transferir ao banco.",()=>Transfer(true,false));
            Add("Sacar valor...","Escolha quanto retirar para a carteira do personagem.",()=>Transfer(false,false));
            Add("Depositar o maximo","Limitado pelo dinheiro disponivel e espaco no saldo bancario.",()=>Transfer(true,true));
            Add("Sacar o maximo","Limitado pelo saldo e espaco na carteira.",()=>Transfer(false,true));
            Add("Depositar $100","Atalho de valor fixo preservado.",()=>TransferAmount(true,100));
            Add("Sacar $100","Atalho de valor fixo preservado.",()=>TransferAmount(false,100));
            target=tools;
            Add("Recarregar configuracao", "Le Settings.ini e Keys.ini. Mudancas nos recursos exigem reiniciar os plugins.", () => { Cfg.Load(); Notify.Show("Configuracao recarregada."); });
            profiles.AddSubMenu(imports);profiles.AddSubMenu(legacyProfiles);target=imports;
            var imported=new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var directory in new[] { Path.Combine(Cfg.SaveDir, "Players"), Path.Combine(Cfg.Dir, "Legacy", "SaveData", "Players"), Path.Combine("scripts", "PSRP", "SaveData", "Players") }.Distinct())
            {
                if (!Directory.Exists(directory)) continue;
                foreach (string path in Directory.GetFiles(directory, "*.json"))
                {
                    string source = path;
                    string id = "PSRP_" + Profile.SafeId(Path.GetFileNameWithoutExtension(source));
                    if(!imported.Add(id) || Directory.Exists(Path.Combine(Cfg.SaveDir,id)))continue;
                    Add("Importar: " + Path.GetFileNameWithoutExtension(source), "Cria " + id + ". Importa dados do save PSRP sem mudar seu ped nem substituir os originais.", () => Confirm("Importar "+id,"Cria um perfil separado; nunca substitui um perfil existente.",()=>Import(source,id)));
                }
            }
            target=legacyProfiles;
            if (Directory.Exists(Cfg.SaveDir))
                foreach (string directory in Directory.GetDirectories(Cfg.SaveDir).Where(d => File.Exists(Path.Combine(d, "Wallet.json"))))
                {
                    string id = Path.GetFileName(directory);
                    if (Profile.IsProtagonist(id) || CharacterRules.ValidId(id) && File.Exists(Path.Combine(directory, "Character.json"))) continue;
                    Add("Usar perfil: " + id, "Somente ped freemode; preserva aparencia, posicao e veiculos.", () => {
                        if (!Freemode()) { Notify.Show("Troque para um personagem freemode primeiro."); return; }
                        Confirm("Usar perfil: "+id,"Somente dados legados no ped freemode atual; nao altera o modelo.",()=>{if(!Freemode())return;RequestedProfile=id;pool.HideAll();});
                    });
                }
            if(imports.Items.Count==0)imports.Add(new NativeItem("Nenhum save novo disponivel"){Enabled=false});
            if(legacyProfiles.Items.Count==0)legacyProfiles.Add(new NativeItem("Nenhum perfil legado disponivel"){Enabled=false});
            for(int i=0;i<All.Length;i++)if(All[i].Items.Count>0)All[i].SelectedIndex=Math.Max(0,Math.Min(selected[i],All[i].Items.Count-1));
            dirty = false;
        }

        void BankUsed()
        {
            ach.Bump("bank_used"); if (Wallet.Bank >= 1000000) ach.Bump("bank_million");
            ach.Save();bank.Name="Banco / saldo $"+Wallet.Bank;
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
                Notify.Show("Importado " + id + ". Selecione Usar perfil com um ped freemode.");
            }
            catch (Exception ex) { Log.Error("PSRP import", ex); Notify.Show("Importacao incompleta ou perfil existente. Originais preservados; confira RPCore.log."); }
        }
        bool Freemode(){int hash=Game.Player.Character.Model.Hash;return hash==CharacterRules.ModelHash("mp_m_freemode_01") || hash==CharacterRules.ModelHash("mp_f_freemode_01");}
        bool CanAct(){var p=Game.Player.Character;return p!=null && p.Exists() && !p.IsDead && !Game.IsPaused && !Game.IsMissionActive && !GameplayContext.SceneBlocking && Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id;}
        public void OpenBank(){if(!CanAct())return;pool.HideAll();Rebuild();bank.Visible=true;}
        void Confirm(string title,string description,Action action)
        {
            var back=All.FirstOrDefault(m=>m.Visible);string profile=Profile.Id;int actor=Game.Player.Character.Handle;pool.HideAll();confirmation.Clear();confirmation.Name=title;target=confirmation;
            Add("Cancelar",description,()=>{confirmation.Visible=false;if(back!=null)back.Visible=true;});
            Add("Confirmar",description,()=>{if(profile!=Profile.Id || actor!=Game.Player.Character.Handle)return;action();confirmation.Visible=false;if(back!=null)back.Visible=true;});confirmation.SelectedIndex=0;confirmation.Visible=true;
        }
        void Transfer(bool deposit,bool maximum)
        {
            int amount=PhoneViewRules.BankMaximum(Wallet.Cash,Wallet.Bank,deposit);
            if(!maximum){string profile=Profile.Id;int actor=Game.Player.Character.Handle;string input=Game.GetUserInput(WindowTitle.EnterMessage60,"100",10);if(!CanAct() || profile!=Profile.Id || actor!=Game.Player.Character.Handle)return;if(!PhoneViewRules.Amount(input,out amount)){if(!string.IsNullOrWhiteSpace(input))Notify.Show("Informe um valor inteiro positivo.");return;}}
            TransferAmount(deposit,amount);
        }
        void TransferAmount(bool deposit,int amount)
        {
            if(!CanAct() || amount<=0 || !(deposit?Wallet.DepositSaved(amount):Wallet.WithdrawSaved(amount))){Notify.Show("Saldo insuficiente ou limite de saldo atingido.");return;}
            Notify.Show((deposit?"Depositado: $":"Sacado: $")+amount);try{BankUsed();}catch(Exception ex){Log.Error("bank achievement",ex);}
        }
        public void Tick(){if(All.Any(m=>m.Visible) && !CanAct()){Reload();return;}if(dirty && All.Any(m=>m.Visible) && !confirmation.Visible)Rebuild();if(bank.Visible)bank.Name="Banco $"+Wallet.Bank+" / carteira $"+Wallet.Cash;}
        public void OnKey(KeyEventArgs e) {if(e.KeyCode!=Cfg.CoreKey)return;if(All.Any(m=>m.Visible))Reload();else if(!pool.AreAnyVisible && CanAct()){Rebuild();menu.Visible=true;}}
        public void Reload() {foreach(var m in All)m.Visible=false;RequestedProfile=null;}
        public void Save() { }
        public void Dispose() { Reload(); }
    }
}
