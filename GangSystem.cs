using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public partial class GangDefinition
    {
        [Newtonsoft.Json.JsonIgnore] public Vector3 Position=>new Vector3(HQX,HQY,HQZ);
    }

    public class GangSystem : ISystem
    {
        public string Name { get { return "gangs"; } }
        readonly ObjectPool pool;
        readonly NativeMenu menu = new NativeMenu("Gangs", "");
        readonly List<GangDefinition> gangs;
        readonly List<Blip> blips = new List<Blip>();
        readonly AchievementSystem ach;
        GangState state;
        readonly GangLoyalty loyalty;readonly GangStrength strength;readonly GangDriving driving;readonly GangTerritories turf;readonly GangTreasury treasury;
        bool InitiationPending=>loyalty.HasPending || strength.HasPending || driving.HasPending || turf.HasPending;
        void SaveState(GangState next){Store.Write("GangState.json",next);state=next;dirty=true;}
        bool dirty;NativeItem presenceStatus;
        readonly GangPresence presence=new GangPresence();
        readonly JsonFiles definitions=new JsonFiles();
        string CreatedPath=>System.IO.Path.Combine(Cfg.Dir,"Gangs","RPCoreCreated.json");

        public GangSystem(ObjectPool pool, AchievementSystem ach):this(pool,ach,new ShiftGate()) {}
        public GangSystem(ObjectPool pool, AchievementSystem ach,ShiftGate gate)
        {
            this.pool = pool; this.ach = ach; pool.Add(menu); Visuals.Register(menu, "gangs");
            gangs = Store.LoadDef("Gangs/CustomGangs.json", () => new List<GangDefinition>()).Where(g => g != null && !string.IsNullOrWhiteSpace(g.Name)).GroupBy(g => g.Name).Select(g => g.First()).ToList();
            gangs.AddRange(Store.LoadDef("Gangs/RPCoreCreated.json", () => new List<GangDefinition>()).Where(g => g != null && !string.IsNullOrWhiteSpace(g.Name) && !gangs.Any(old => old.Name == g.Name)));
            foreach (var gang in gangs)
            {
                var b = MapBlips.Create(gang.Position, "RPCore Gang HQ: " + gang.Name, BlipSprite.GangHighlight, BlipColor.Purple, .75f);
                b.Color = gang.GangColor == "Purple" ? BlipColor.Purple : gang.GangColor == "Green" ? BlipColor.Green : BlipColor.Yellow;
                blips.Add(b);
            }
            ach.Progress += Progress; Reload();var shared=gate??new ShiftGate();loyalty=new GangLoyalty(shared,pool,presence,()=>state,SaveState);strength=new GangStrength(shared,pool,presence,()=>state,SaveState);driving=new GangDriving(shared,pool,presence,()=>state,SaveState);turf=new GangTerritories(shared,pool,()=>state,SaveState,()=>gangs);treasury=new GangTreasury(shared,pool,()=>state,()=>gangs);
        }

        public void Create(string name)
        {
            if (gangs.Any(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase))) { Notify.Show("Ja existe uma gangue com este nome."); return; }
            string path = System.IO.Path.Combine(Cfg.Dir, "Gangs", "RPCoreCreated.json");
            var created = definitions.Read(path, () => new List<GangDefinition>());
            if (created.Count >= 50) { Notify.Show("Limite de 50 gangues criadas."); return; }
            var p = Game.Player.Character.Position;
            var gang = new GangDefinition { Name = name, Description = "Gangue criada no RPCore", HQX = p.X, HQY = p.Y, HQZ = p.Z, GangColor = "Purple" };
            created.Add(gang); definitions.Write(path, created); gangs.Add(gang);
            var b = MapBlips.Create(gang.Position, "RPCore Gang HQ: " + name, BlipSprite.GangHighlight, BlipColor.Purple, .75f); blips.Add(b); dirty = true;
            Notify.Show("Gangue cadastrada. F4: HQ e afiliacao.");
        }

        void Progress(string stat, int amount)
        {
            if (stat != "pizza_drops" || string.IsNullOrEmpty(state.Gang) || !gangs.Any(g => g.Name == state.Gang)) return;
            try{var next=CharacterRules.Copy(state);if(GangProgressionRules.Grant(next,amount))SaveState(next);}catch(Exception ex){Log.Error("gang pizza reputation",ex);Notify.Show("Entrega preservada; reputacao extra nao gravada. Confira o log.");}
        }

        void Rebuild()
        {
            menu.Clear(); menu.Name = (state.Gang ?? "Unaffiliated") + " / rep " + state.Reputation;
            int rank=GangProgressionRules.Rank(state);var rankRow=new NativeItem("Patente: "+GangProgressionRules.Name(rank)+" / "+rank,"Promocoes: 250 / 750 / 1500 pontos. Captura +50, defesa +25, perda de defesa -20. Patente nao cai por perda de reputacao na mesma gangue.");menu.Add(rankRow);
            var funds=new NativeItem("Tesouraria / pagamentos","Saldo financiado explicitamente pelo banco; participacao por patente apos intervalo do jogo.");funds.Activated+=(s,e)=>treasury.Open();menu.Add(funds);
            var territories=new NativeItem("Territorios / mapa e disputas","15 territorios, donos por perfil, GPS e capturas explicitas. Afiliacao necessaria para disputar.");territories.Activated+=(s,e)=>turf.Open();menu.Add(territories);
            var enabled=new NativeItem("Presenca nas HQs: "+(state.PresenceEnabled?"LIGADA":"DESLIGADA"),"Lideres/membros nos postos configurados. Opcional por perfil; no maximo 12 NPCs com limites configuraveis.");
            enabled.Activated+=(s,e)=>{try{var copy=CharacterRules.Copy(state);copy.PresenceEnabled=!copy.PresenceEnabled;Store.Write("GangState.json",copy);state=copy;presence.Clear();dirty=true;}catch(Exception ex){Log.Error("gang presence toggle",ex);Notify.Show("Preferencia nao salva; estado anterior conservado.");}};menu.Add(enabled);
            presenceStatus=new NativeItem("NPCs ativos: "+presence.Count,"Ativacao proxima da HQ; modelo, piso e espaco precisam estar disponiveis. Sem guerras/fiscalizacao nesta etapa.");menu.Add(presenceStatus);
            if(state.Loyalty?.Pending!=null){
                var p=state.Loyalty.Pending;
                AddLoyaltyAction((loyalty.Active?"Suspender":"Retomar")+" iniciacao: "+p.Gang+" / "+p.Collected+"/2", "Retome a pe junto ao lider com presenca ligada. Sem taxa; conserva pacotes confirmados.",()=>{if(loyalty.Active){loyalty.Suspend();return true;}return loyalty.Resume();});
                AddLoyaltyAction("Cancelar iniciacao pendente", "Descarta pacotes desta tentativa, sem mudar filiacao ou reputacao.",()=>loyalty.Cancel());
            }
            if(state.Strength?.Pending!=null){var p=state.Strength.Pending;
                AddStrengthAction((strength.Active?"Suspender":"Retomar")+" prova de forca: "+p.Gang+" / "+(p.Report?"aprovada":p.Defeated?.Count+"/3"),"Junto ao lider, sem filiacao. Luta parcial reinicia os tres; aprovacao completa fica salva para confirmar.",()=>{if(strength.Active){strength.Suspend();return true;}return strength.Resume();});
                AddStrengthAction("Cancelar prova de forca pendente","Sem taxa; descarta apenas esta tentativa.",()=>strength.Cancel());
            }
            if(state.Driving?.Pending!=null){var p=state.Driving.Pending;
                AddDrivingAction((driving.Active?"Suspender":"Retomar")+" iniciacao de direcao: "+p.Gang+" / "+p.Stage,"Junto ao lider, sem filiacao. Retomada parcial reinicia embarque/alvo/fuga/retorno; aprovacao completa fica emitivel.",()=>{if(driving.Active){driving.Suspend();return true;}return driving.Resume();});
                AddDrivingAction("Cancelar iniciacao de direcao pendente","Sem taxa; descarta somente esta tentativa.",()=>driving.Cancel());
            }
            AddCrewAction("Recrutar membro mais proximo", "A pe, a ate 3m de um membro da sua gangue. Presenca ligada; equipe de ate 3. Lideres e NPCs alheios nao podem ser recrutados.",()=>presence.RecruitNearest(state.Gang));
            AddCrewAction("Equipe: seguir", "Segue a pe e embarca como passageiro quando voce dirige um carro parado com lugar livre. Nao usa veiculos reservados de trabalho.",()=>presence.Order(false));
            AddCrewAction("Equipe: aguardar aqui", "Pare os veiculos. Desembarca e aguarda no local; use seguir para continuar.",()=>presence.Order(true));
            AddCrewAction("Dispensar equipe", "Retorno a pe aos postos quando perto da HQ; fora dela os NPCs proprios sao liberados. Sem teleporte ou taxa.",()=>{presence.DismissCrew();return true;});
            AddPostAction("Definir posto do lider aqui",true,false);
            AddPostAction("Adicionar posto de membro aqui",false,false);
            AddPostAction("Remover ultimo posto de membro",false,true);
            foreach (var g in gangs)
            {
                var gang = g;
                var route = new NativeItem("GPS: " + gang.Name, (gang.Description ?? "")+" / postos disponiveis: "+presence.Posts(gang));
                route.Activated += (s, e) => { Function.Call(Hash.SET_NEW_WAYPOINT, gang.HQX, gang.HQY); menu.Visible = false; };
                menu.Add(route);
                AddDrivingAction("Iniciacao por direcao: "+gang.Name,"Junto ao lider e sem filiacao. Voce dirige um carro; integrante elimina alvo rival, fuga de 150m e retorno juntos. Sem alterar seu carro ou armas.",()=>driving.Begin(gang,gangs));
                AddStrengthAction("Iniciacao por forca: "+gang.Name,"Junto ao lider, sem filiacao, desarmado e com vida acima de 100. Tres desafiantes, apenas punhos. Nao restaura sua vida ou armadura.",()=>strength.Begin(gang));
                AddLoyaltyAction("Iniciacao por lealdade: "+gang.Name,"Sem filiacao, a pe a ate 3m do lider vivo e com presenca ligada. Busque dois pacotes e retorne. Nao substitui Join legado.",()=>loyalty.Begin(gang));
                var join = new NativeItem("Join " + gang.Name, "Visit the HQ on foot. Complete pizza deliveries to earn reputation in RPCore.");
                join.Activated += (s, e) => {
                    var p = Game.Player.Character;
                    if (p.IsDead || p.IsInVehicle() || p.Position.DistanceTo(gang.Position) > 5) { Notify.Show("Visit the gang HQ on foot to join."); return; }
                    if(InitiationPending){Notify.Show("Cancele/conclua a iniciacao antes de usar Join.");return;}
                    if (state.Gang == gang.Name) return;
                    var next=CharacterRules.Copy(state);next.Gang=gang.Name;next.Reputation=0;next.Rank=1;SaveState(next);
                    Notify.Show("Joined " + gang.Name);
                };
                menu.Add(join);
            }
            var leave = new NativeItem("Leave current gang");
            leave.Activated += (s, e) => { var next=CharacterRules.Copy(state);next.Gang=null;next.Reputation=0;next.Rank=1;SaveState(next); }; menu.Add(leave);
            dirty = false;
        }

        void AddDrivingAction(string label,string description,Func<bool> action)
        {var item=new NativeItem(label,description);item.Activated+=(s,e)=>{try{if(!action())Notify.Show("Confira lider, filiacao, modelos rivais, pendencia e atividade atual.");else{dirty=true;menu.Visible=false;}}catch(Exception ex){driving.Suspend();Log.Error("gang driving menu",ex);Notify.Show("Iniciacao suspensa; dados confirmados conservados.");}};menu.Add(item);}

        void AddStrengthAction(string label,string description,Func<bool> action)
        {var item=new NativeItem(label,description);item.Activated+=(s,e)=>{try{if(!action())Notify.Show("Confira lider, vida, punhos, filiacao, pendencia e atividade atual.");else{dirty=true;menu.Visible=false;}}catch(Exception ex){strength.Suspend();Log.Error("gang strength menu",ex);Notify.Show("Prova suspensa; dados confirmados conservados.");}};menu.Add(item);}

        void AddLoyaltyAction(string label,string description,Func<bool> action)
        {var item=new NativeItem(label,description);item.Activated+=(s,e)=>{try{if(!action())Notify.Show("Confira filiacao, lider proximo, presenca, tentativa e atividade atual.");else{dirty=true;menu.Visible=false;}}catch(Exception ex){loyalty.Suspend();Log.Error("gang loyalty menu",ex);Notify.Show("Acao nao confirmada; dados persistidos preservados.");}};menu.Add(item);}

        void AddCrewAction(string label,string description,Func<bool> action)
        {var item=new NativeItem(label,description);item.Activated+=(s,e)=>{try{if(!action())Notify.Show("Confira filiacao, distancia, vagas, seguranca e veiculos parados.");else{menu.Visible=false;Notify.Show("Equipe: "+presence.CrewCount+"/3. Ordem confirmada.");}}catch(Exception ex){Log.Error("gang crew order",ex);Notify.Show("Ordem nao concluida; confira a equipe no F4.");}};menu.Add(item);}

        void AddPostAction(string label,bool leader,bool remove)
        {
            var item=new NativeItem(label,"Somente sua gangue criada no RPCoreCreated.json. A pe a ate 70m da HQ, sem combate/procurado/missao. Postos a pelo menos 2m; limite 20. Modelos existentes preservados.");
            item.Activated+=(s,e)=>{try{
                if(InitiationPending){Notify.Show("Conclua/cancele a iniciacao antes de editar postos.");return;}
                var p=Game.Player.Character;
                if(p.IsDead || p.IsInVehicle() || p.IsInCombat || Game.IsMissionActive || Game.Player.Wanted.WantedLevel>0 || InterfaceOptions.ModalOpen || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)){Notify.Show("Configure a pe, em seguranca.");return;}
                var source=definitions.Read(CreatedPath,()=>new List<GangDefinition>());
                var own=source.Find(g=>g!=null && string.Equals(g.Name,state.Gang,StringComparison.OrdinalIgnoreCase));
                var point=new GangSpot{X=p.Position.X,Y=p.Position.Y,Z=p.Position.Z,Heading=p.Heading};
                if(own==null || !GangPresenceRules.AtHQ(own,point)){Notify.Show("Filiacao a uma gangue criada por voce e proximidade da HQ necessarias.");return;}
                var next=GangPresenceRules.Edit(source,state.Gang,point,leader,remove);
                if(next==null){Notify.Show("Posto muito proximo, limite atingido ou nada para remover.");return;}
                definitions.Write(CreatedPath,next);var replacement=next.Find(g=>g!=null && string.Equals(g.Name,state.Gang,StringComparison.OrdinalIgnoreCase));
                int index=gangs.FindIndex(g=>string.Equals(g.Name,state.Gang,StringComparison.OrdinalIgnoreCase));if(index>=0)gangs[index]=replacement;
                presence.Clear();dirty=true;Notify.Show("Postos salvos. Afaste-se alguns metros para liberar o spawn.");
            }catch(Exception ex){Log.Error("gang post edit",ex);Notify.Show("Postos nao alterados; confira arquivo e backup.");}};menu.Add(item);
        }

        public void Tick()
        {
            try{presence.Tick(gangs,state.PresenceEnabled,state.Gang);}catch(Exception ex){presence.Clear();Log.Error("gang presence",ex);}
            try{loyalty.Tick();}catch(Exception ex){loyalty.Suspend();Log.Error("gang loyalty tick",ex);Notify.Show("Iniciacao suspensa; progresso confirmado preservado.");}
            try{strength.Tick();}catch(Exception ex){strength.Suspend();Log.Error("gang strength tick",ex);Notify.Show("Prova suspensa; retome no lider. Dados confirmados conservados.");}
            try{driving.Tick();}catch(Exception ex){driving.Suspend();Log.Error("gang driving tick",ex);Notify.Show("Iniciacao de direcao suspensa; retome junto ao lider.");}
            try{turf.Tick();}catch(Exception ex){turf.Suspend();Log.Error("gang turf tick",ex);Notify.Show("Disputa suspensa; donos/progresso gravados preservados.");}
            if (menu.Visible && dirty) Rebuild();
            if(menu.Visible && presenceStatus!=null)presenceStatus.Title="NPCs ativos: "+presence.Count+" / equipe "+presence.CrewCount+"/3";
            var p = Game.Player.Character;
            if (p.IsDead) return;
            foreach (var g in gangs.Where(g => p.Position.DistanceTo(g.Position) < 35))
                World.DrawMarker(MarkerType.Cylinder, g.Position - new Vector3(0, 0, 1), Vector3.Zero, Vector3.Zero, new Vector3(2, 2, 1), Color.FromArgb(90, 160, 80, 220));
        }
        public void OnKey(KeyEventArgs e)
        {
            if(e.KeyCode==Keys.End && turf.Active){turf.Suspend();dirty=true;Notify.Show("Disputa suspensa. Retome na area pelo menu Territorios.");return;}
            if(e.KeyCode==Keys.End && driving.Active){driving.Suspend();dirty=true;Notify.Show("Iniciacao suspensa. Retomada parcial reinicia a missao de direcao.");return;}
            if(e.KeyCode==Keys.End && strength.Active){strength.Suspend();dirty=true;Notify.Show("Prova suspensa. Retomada parcial reinicia os tres desafiantes.");return;}
            if(e.KeyCode==Keys.End && loyalty.Active){loyalty.Suspend();dirty=true;Notify.Show("Iniciacao suspensa. Retome junto ao lider pelo F4.");return;}
            if(e.KeyCode==Keys.E){try{turf.Interact();driving.Interact();strength.Interact();loyalty.Interact();}catch(Exception ex){turf.Suspend();driving.Suspend();strength.Suspend();loyalty.Suspend();Log.Error("gang initiation interaction",ex);Notify.Show("Entrega nao confirmada; retome pelo F4.");}return;}
            if (e.KeyCode != Cfg.GangsKey) return;
            if (menu.Visible) menu.Visible = false;
            else if (!pool.AreAnyVisible) { Rebuild(); menu.Visible = true; }
        }
        public void Reload() { treasury?.Close();turf?.Reload();driving?.Suspend();strength?.Suspend();loyalty?.Suspend();presence.Clear();dirty=true;state = Store.Load("GangState.json", () => new GangState()); state.Reputation = Math.Max(0, state.Reputation); menu.Visible = false; }
        public void Save() { Store.Write("GangState.json", state); }
        public void Dispose() { treasury.Close();turf.Dispose();driving.Dispose();strength.Dispose();loyalty.Dispose();presence.Dispose();ach.Progress -= Progress; menu.Visible = false; foreach (var b in blips) if (b.Exists()) b.Delete(); }
    }
}
