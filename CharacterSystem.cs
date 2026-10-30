using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class CharacterSystem : ISystem
    {
        public string Name=>"characters";
        readonly ObjectPool pool;readonly ShiftGate gate;readonly Action<string,int> transition;readonly CharacterFiles files;readonly JsonFiles json=new JsonFiles();readonly NativeMenu menu;
        public Func<CharacterBody,CharacterBody> SnapshotFilter;
        CharacterCard current;Operation operation;bool switching,lease;
        readonly PersonalServiceMenu ui;readonly CharacterEntryGate entry=new CharacterEntryGate();int entryClock;
        CharacterCatalogue Catalogue()=>CharacterCatalogue.Read(Cfg.SaveDir,id=>files.Read(id),(id,ex)=>Log.Error("character catalogue "+id,ex));
        string newName="Novo personagem",newModel="mp_m_freemode_01";
        sealed class Operation
        {
            public string Profile,OriginProfile;public Ped Origin,Actor;public Model Model;public Vector3 Position;public float Heading;public int Started;
            public CharacterCard Card;public CharacterReturn Return;public CharacterBody Body;public GroomingLook Look;public FaceHeritage Heritage;public bool Swapped;
        }
        string ReturnPath=>Path.Combine(Cfg.Dir,"CharacterReturn.json");
        bool Safe=>Game.Player.CanControlCharacter && Profile.Detect()==Profile.Id && GTA.UI.Screen.IsFadedIn && !Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE) && !Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Function.Call<bool>(Hash.IS_GAMEPLAY_CAM_RENDERING) && Game.Player.Wanted.WantedLevel==0 && Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsFalling && Game.Player.Character.Speed<.5f;
        public CharacterSystem(ObjectPool pool,ShiftGate gate,Action<string,int> transition)
        {this.pool=pool;this.gate=gate;this.transition=transition;files=new CharacterFiles(Cfg.SaveDir);menu=new NativeMenu("Personagens RPCore","CRIAR / CARREGAR / VOLTAR");pool.Add(menu);Visuals.Register(menu,"core");ui=new PersonalServiceMenu(menu,ex=>{End();ui.Close();Log.Error("character action",ex);Notify.Show("Operacao nao concluida. Seus personagens foram preservados; confira o log.");});entryClock=Game.GameTime;Reload();}
        void Add(string title,string description,Action action)
        {ui.Add(title,description,action);}
        public void OpenMenu(){entry.Consume();ShowList(Catalogue());}
        void ShowList(CharacterCatalogue catalogue)
        {
            End();pool.HideAll();ui.Begin("list");menu.Name="Escolha seu personagem";
            foreach(var saved in catalogue.Characters){var card=saved;
                string status=card.ApplyPending?"Retomar: ":card.Id==Profile.Id?"Ativo: ":"Carregar: ";
                Add(status+card.Name+" / "+(card.Model=="mp_m_freemode_01"?"Masculino":"Feminino"),"Carrega seu perfil, roupas e equipamento salvos. Dinheiro e inventario pertencem a este personagem.",()=>Confirm(card));
            }
            Add("Criar novo personagem","Novo perfil independente; nao copia dinheiro ou progresso de outro personagem.",CreateMenu);
            foreach(string id in catalogue.ProtectedIds)Add("Personagem protegido / "+id,"Registro indisponivel. O arquivo original foi conservado; confira Character.json e seu backup.",()=>Notify.Show("Personagem preservado; confira arquivo/backup e RPCore.log."));
            if(catalogue.ScanFailed)Add("Lista indisponivel","Nao foi possivel ler todos os personagens. Seus arquivos nao foram substituidos.",()=>{});
            if(File.Exists(ReturnPath))try{var original=ReadReturn();if(CharacterRules.ProtagonistHash(Profile.Id)==0 || original.ApplyPending)Add(original.ApplyPending?"Retomar volta ao protagonista":"Voltar ao protagonista de origem","Restaura o personagem de historia registrado antes da troca.",ConfirmReturn);}
            catch(Exception ex){Log.Error("character return list",ex);Add("Backup do protagonista protegido","Arquivo invalido; confira o backup. Original preservado.",()=>Notify.Show("Confira CharacterReturn.json e seu backup."));}
            if(current!=null && CharacterRules.SameBodyOwner(current,Profile.Id,Game.Player.Character.Model.Hash) && !current.ApplyPending)Add("Salvar roupas / equipamento atuais","Registra o personagem atual sem copiar dados de outro perfil.",()=>{Save();Notify.Show("Personagem salvo.");});
            Add("Continuar com personagem atual","Fecha a selecao e continua jogando. F10 permite reabrir depois.",()=>ui.Close());menu.Visible=true;
        }
        void PollEntry()
        {
            if(entry.Consumed)return;
            if(!InterfaceOptions.CharacterSelectionOnStart){entry.Consume();return;}
            int now=Game.GameTime,elapsed=unchecked(now-entryClock);entryClock=now;
            bool ready=operation==null && !menu.Visible && !pool.AreAnyVisible && gate.Owner==null && Safe;
            if(!entry.Advance(elapsed,ready))return;
            var catalogue=Catalogue();
            if(catalogue.OpenCreation){End();pool.HideAll();CreateMenu();}
            else ShowList(catalogue);
        }
        void CreateMenu()
        {
            entry.Consume();ui.Begin("create",OpenMenu);menu.Name="Crie seu personagem RPCore";
            Add("Nome: "+newName,"Ate 40 caracteres, sem quebras de linha. Nome nao e usado como caminho de arquivo.",()=>{string profile=Profile.Id;int actor=Game.Player.Character.Handle,model=Game.Player.Character.Model.Hash;string text=Game.GetUserInput(WindowTitle.EnterMessage60,newName,40)?.Trim();if(!menu.Visible || !Safe || profile!=Profile.Id || actor!=Game.Player.Character.Handle || model!=Game.Player.Character.Model.Hash)return;if(text==null)return;if(CharacterRules.ValidName(text))newName=text;else Notify.Show("Nome invalido; selecao mantida.");CreateMenu();});
            Add("Modelo: "+(newModel=="mp_m_freemode_01"?"Masculino":"Feminino"),"Freemode do GTA instalado. Alternar aqui ainda nao troca o ped.",()=>{newModel=newModel=="mp_m_freemode_01"?"mp_f_freemode_01":"mp_m_freemode_01";CreateMenu();});
            Add("Criar perfil e abrir confirmacao","Cria uma pasta nova com saldo zero. Para jogar, confirme Carregar na proxima tela.",()=>{
                if(!CharacterRules.ValidName(newName))return;
                if(Directory.Exists(Cfg.SaveDir) && Directory.GetDirectories(Cfg.SaveDir).Count(d=>CharacterRules.ValidId(Path.GetFileName(d)))>=64){Notify.Show("Limite de 64 personagens cadastrados.");return;}
                var parents=GroomingSystem.NativeParents();if(parents.Length==0){Notify.Show("Origens freemode indisponiveis. Nada criado.");return;}
                int second=parents.Contains(21)?21:parents[Math.Min(1,parents.Length-1)];
                var card=new CharacterCard{Id="RP_"+Guid.NewGuid().ToString("N").Substring(0,16),Name=newName,Model=newModel,BaseFace=new FaceHeritage{ShapeFirst=parents[0],SkinFirst=parents[0],ShapeSecond=second,SkinSecond=second}};
                files.Create(card);Confirm(card);
            });
            Add("Continuar com personagem atual","Nenhum perfil e criado enquanto voce nao confirmar a criacao.",()=>ui.Close());
            Add("Voltar a lista","Seus personagens salvos.",OpenMenu);menu.Visible=true;
        }
        void Confirm(CharacterCard supplied)
        {
            var card=files.Read(supplied.Id);ui.Begin("confirm:"+card.Id,OpenMenu,false);menu.Name="Carregar "+card.Name+"?";
            Add("Cancelar / voltar","Mantem o personagem atual.",OpenMenu);
            Add("Confirmar e carregar","Salva o perfil atual, carrega modelo e dados escolhidos no local atual. Fique parado a pe, sem missao/procurado/atividade.",()=>Begin(card,null));menu.Visible=true;
        }
        CharacterReturn ReadReturn()
        {var value=json.Read<CharacterReturn>(ReturnPath,()=>null);if(value==null || !value.Valid){json.Protect(ReturnPath);throw new InvalidDataException("Invalid protagonist return snapshot.");}return value;}
        void ConfirmReturn()
        {
            var original=ReadReturn();ui.Begin("return",OpenMenu,false);menu.Name="Voltar para "+original.Profile+"?";
            Add("Cancelar / voltar","Continua no personagem atual.",OpenMenu);
            Add("Confirmar volta","Salva o personagem RPCore e restaura o protagonista registrado, mantendo local e seus saldos.",()=>Begin(null,original));menu.Visible=true;
        }
        void LoadingMenu(string title)
        {
            ui.Begin("loading");menu.Name=title;
            Add("Cancelar carregamento","Interrompe a operacao; se o modelo ja mudou, o registro fica pendente para retomar.",()=>{End();ui.Close();});menu.Visible=true;
        }
        void Begin(CharacterCard supplied,CharacterReturn original)
        {
            if(!Safe || operation!=null){Notify.Show("Pare a pe, sem missao/procurado/combate/outra interface.");return;}
            int origin=Game.Player.Character.Model.Hash;
            if(CharacterRules.ProtagonistHash(Profile.Id)!=origin && CharacterRules.ModelHash("mp_m_freemode_01")!=origin && CharacterRules.ModelHash("mp_f_freemode_01")!=origin){Notify.Show("Use um protagonista ou freemode compativel antes da troca.");return;}
            if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return;}lease=true;
            Save();var card=supplied==null?null:files.Read(supplied.Id);
            if(card!=null && card.Id==Profile.Id && !card.ApplyPending && Game.Player.Character.Model.Hash==CharacterRules.ModelHash(card.Model)){End();ui.Close();Notify.Show("Este personagem ja esta ativo. Salve ou personalize na barbearia.");return;}
            GroomingLook look=null;FaceHeritage heritage=null;
            if(card!=null){
                var wallet=json.Read<WalletData>(Path.Combine(Cfg.SaveDir,card.Id,"Wallet.json"),()=>new WalletData());
                if(wallet.Barber!=null && !wallet.Barber.Valid)throw new InvalidDataException("Saved barber data invalid.");
                if(wallet.Grooming!=null && !wallet.Grooming.Valid)throw new InvalidDataException("Saved grooming data invalid.");
                look=wallet.Grooming?.Looks.Find(l=>l.Model==CharacterRules.ModelHash(card.Model));heritage=look?.Heritage??card.BaseFace;
                if(!FaceRules.Compatible(heritage,GroomingSystem.NativeParents()))throw new InvalidDataException("Saved head blend resources unavailable.");
            }
            if(original!=null && !original.Valid)throw new InvalidDataException("Invalid protagonist snapshot.");
            int hash=card==null?original.Body.Model:CharacterRules.ModelHash(card.Model);var model=new Model(hash);
            if(!model.IsInCdImage || !model.IsPed)throw new InvalidOperationException("Character model unavailable.");
            operation=new Operation{Profile=card?.Id??original.Profile,Card=card,Return=original,Body=card?.Body??original?.Body,Look=look,Heritage=heritage,Origin=Game.Player.Character,OriginProfile=Profile.Id,Model=model,Position=Game.Player.Character.Position,Heading=Game.Player.Character.Heading,Started=Game.GameTime};
            Function.Call(Hash.REQUEST_MODEL,hash);LoadingMenu("Carregando personagem / ate 10s");
        }
        void Swap(Operation op)
        {
            SaveBody(true);
            if(op.Card!=null && CharacterRules.ProtagonistHash(Profile.Id)!=0){
                if(File.Exists(ReturnPath) && ReadReturn().ApplyPending)throw new InvalidOperationException("Finish pending protagonist return first.");
                var backup=new CharacterReturn{Profile=Profile.Id,Body=CharacterBodyRuntime.Capture(Game.Player.Character)};
                json.Write(ReturnPath,backup);
            }
            if(op.Card!=null){var next=CharacterRules.Copy(op.Card);next.ApplyPending=true;files.Write(next);op.Card=next;}
            if(op.Return!=null){var backup=CharacterRules.Copy(op.Return);backup.ApplyPending=true;json.Write(ReturnPath,backup);op.Return=backup;}
            switching=true;try{transition(op.Profile,op.Model.Hash);}finally{switching=false;}
            op.Swapped=true;op.Actor=Game.Player.Character;op.Actor.Position=op.Position;op.Actor.Heading=op.Heading;current=op.Card;
            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION,op.Actor);
            if(op.Body!=null)CharacterBodyRuntime.Apply(op.Actor,op.Body);
            else{Function.Call(Hash.REMOVE_ALL_PED_WEAPONS,op.Actor,true);op.Actor.MaxHealth=200;op.Actor.Health=200;op.Actor.Armor=0;}
            if(op.Card==null){Complete(op);return;}
            var h=op.Heritage;Function.Call(Hash.SET_PED_HEAD_BLEND_DATA,op.Actor,h.ShapeFirst,h.ShapeSecond,0,h.SkinFirst,h.SkinSecond,0,h.ShapeMix/100f,h.SkinMix/100f,0f,false);
            op.Started=Game.GameTime;LoadingMenu("Preparando rosto / ate 10s");
        }
        void Complete(Operation op)
        {
            if(op.Card!=null){
                for(int i=0;i<20;i++)Function.Call(Hash.SET_PED_MICRO_MORPH,op.Actor,i,0f);
                for(int i=0;i<=12;i++)Function.Call(Hash.SET_PED_HEAD_OVERLAY,op.Actor,i,255,0f);
                Function.Call(Hash.SET_PED_HAIR_TINT,op.Actor,0,0);Function.Call(Hash.SET_HEAD_BLEND_EYE_COLOR,op.Actor,0);
                if(op.Look!=null)GroomingSystem.ApplySurface(op.Look);
                var wallet=json.Read<WalletData>(Path.Combine(Cfg.SaveDir,op.Card.Id,"Wallet.json"),()=>new WalletData());var barber=wallet.Barber?.Looks.Find(l=>l.Model==op.Model.Hash);
                if(barber!=null && barber.HasEye)Function.Call(Hash.SET_HEAD_BLEND_EYE_COLOR,op.Actor,barber.Eye);
                var next=CharacterRules.Copy(op.Card);next.Body=CharacterRules.MergeBody(next.Body,CharacterBodyRuntime.Capture(op.Actor));next.ApplyPending=false;files.Write(next);current=next;
            }
            if(op.Return!=null){var backup=CharacterRules.Copy(op.Return);backup.ApplyPending=false;json.Write(ReturnPath,backup);}
            End();ui.Close();Notify.Show("Personagem carregado / perfil "+Profile.Id+". F10: roupas, moradia e barbearias.");
        }
        public void Tick()
        {
            ui.Tick(menu.Visible && Safe);
            try{PollEntry();}catch(Exception ex){entry.Consume();ui.Close();Log.Error("character entry",ex);}
            var op=operation;if(op==null)return;
            try{
                bool same=op.Swapped?op.Actor!=null && op.Actor.Exists() && op.Actor==Game.Player.Character && op.Actor.Model.Hash==op.Model.Hash && Profile.Id==op.Profile:op.Origin!=null && op.Origin.Exists() && op.Origin==Game.Player.Character && Profile.Id==op.OriginProfile;
                if(!same || !Safe || !menu.Visible || unchecked((uint)(Game.GameTime-op.Started))>=10000 || !op.Swapped && op.Origin.Position.DistanceTo(op.Position)>2){End();ui.Close();Notify.Show("Carregamento interrompido. Perfil preservado; confira a lista de personagens para retomar.");return;}
                if(!op.Swapped){if(op.Model.IsLoaded)Swap(op);return;}
                if(Function.Call<bool>(Hash.HAS_PED_HEAD_BLEND_FINISHED,op.Actor) && unchecked((uint)(Game.GameTime-op.Started))>0)Complete(op);
            }
            catch(Exception ex){End();ui.Close();Log.Error("character load",ex);Notify.Show("Carregamento incompleto. Snapshot preservado; retome pela lista de personagens.");}
        }
        void End(){var op=operation;operation=null;if(op!=null)op.Model.MarkAsNoLongerNeeded();if(lease)gate.Release(Name);lease=false;}
        public void Save()=>SaveBody(false);
        void SaveBody(bool beforeSwap)
        {
            if(!PersonalServiceRules.CanSnapshot(gate.Owner))return;
            if(current==null || !CharacterRules.CanCapture(current,Profile.Id,Game.Player.Character.Model.Hash,!Game.Player.Character.IsDead,!beforeSwap && operation!=null))return;
            var next=CharacterRules.Copy(current);var snapshot=CharacterBodyRuntime.Capture(Game.Player.Character);
            if(SnapshotFilter!=null)snapshot=SnapshotFilter(snapshot);
            next.Body=CharacterRules.MergeBody(next.Body,snapshot);files.Write(next);current=next;
        }
        public void Reload()
        {
            if(switching)return;End();ui.Reset();current=null;
            if(CharacterRules.ValidId(Profile.Id))try{current=files.Read(Profile.Id);}catch(Exception ex){Log.Error("character profile",ex);}
        }
        public void OnKey(KeyEventArgs e){}
        public void Dispose(){entry.Consume();End();ui.Reset();}
    }
}
