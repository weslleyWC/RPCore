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
        CharacterCard current;Operation operation;bool switching,lease;
        string newName="Novo personagem",newModel="mp_m_freemode_01";
        sealed class Operation
        {
            public string Profile,OriginProfile;public Ped Origin,Actor;public Model Model;public Vector3 Position;public float Heading;public int Started;
            public CharacterCard Card;public CharacterReturn Return;public CharacterBody Body;public GroomingLook Look;public FaceHeritage Heritage;public bool Swapped;
        }
        string ReturnPath=>Path.Combine(Cfg.Dir,"CharacterReturn.json");
        bool Safe=>!Game.IsPaused && !Game.IsMissionActive && !InterfaceOptions.ModalOpen && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Function.Call<bool>(Hash.IS_GAMEPLAY_CAM_RENDERING) && Game.Player.Wanted.WantedLevel==0 && Game.Player.Character.IsHuman && !Game.Player.Character.IsDead && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsFalling && Game.Player.Character.Speed<.5f;
        public CharacterSystem(ObjectPool pool,ShiftGate gate,Action<string,int> transition)
        {this.pool=pool;this.gate=gate;this.transition=transition;files=new CharacterFiles(Cfg.SaveDir);menu=new NativeMenu("Personagens RPCore","CRIAR / CARREGAR / VOLTAR");pool.Add(menu);Visuals.Register(menu,"core");Reload();}
        void Add(string title,string description,Action action)
        {var item=new NativeItem(title,description);item.Activated+=(s,e)=>{try{action();}catch(Exception ex){End();menu.Visible=false;Log.Error("character action",ex);Notify.Show("Operacao nao concluida. Arquivos anteriores preservados; confira personagens/pendencias e log.");}};menu.Add(item);}
        public void OpenMenu()
        {
            End();pool.HideAll();menu.Clear();menu.Name="Personagens / perfil "+Profile.Id;
            Add("Criar novo personagem","Nome e freemode masculino/feminino. Perfil novo sem copiar dinheiro/progresso; selecao nao troca seu ped.",CreateMenu);
            if(Directory.Exists(Cfg.SaveDir))foreach(var dir in Directory.GetDirectories(Cfg.SaveDir).Where(d=>CharacterRules.ValidId(Path.GetFileName(d))).OrderBy(d=>d).Take(64)){
                string id=Path.GetFileName(dir);
                try{var card=files.Read(id);Add((card.ApplyPending?"Pendente: ":"Carregar: ")+card.Name+" / "+(card.Model=="mp_m_freemode_01"?"M":"F"),id+". Abre confirmacao. Carrega modelo, roupas/equipamento salvos e aparencia confirmada no RPCore.",()=>Confirm(card));}
                catch(Exception ex){Log.Error("character list "+id,ex);Add("Perfil protegido: "+id,"Character.json invalido/ausente. Nenhum save sera substituido.",()=>Notify.Show("Confira Character.json e seu backup. Perfil preservado."));}
            }
            if(File.Exists(ReturnPath))try{var original=ReadReturn();if(CharacterRules.ProtagonistHash(Profile.Id)==0 || original.ApplyPending)Add(original.ApplyPending?"Retomar volta ao protagonista":"Voltar ao protagonista de origem","Abre confirmacao; restaura modelo, roupas/equipamentos registrados antes de sair. Mantem local e dinheiro do modo historia.",ConfirmReturn);}
            catch(Exception ex){Log.Error("character return list",ex);Add("Backup do protagonista protegido","Arquivo invalido; confira CharacterReturn.json e seu backup. Original preservado.",()=>Notify.Show("Backup de retorno protegido; confira o log."));}
            if(current!=null && CharacterRules.SameBodyOwner(current,Profile.Id,Game.Player.Character.Model.Hash) && !current.ApplyPending)Add("Salvar roupas / equipamento atuais","Rosto vem dos grupos confirmados no RPCore. Cores/overlays de outros scripts nao sao capturados por offsets.",()=>{Save();Notify.Show("Corpo/equipamento registrados no personagem.");});
            Add("Fechar","Retornar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        void CreateMenu()
        {
            menu.Clear();menu.Name="Novo perfil / sem mudar personagem";
            Add("Nome: "+newName,"Ate 40 caracteres, sem quebras de linha. Nome nao e usado como caminho de arquivo.",()=>{string text=Game.GetUserInput(WindowTitle.EnterMessage60,newName,40)?.Trim();if(CharacterRules.ValidName(text))newName=text;else Notify.Show("Nome invalido; selecao mantida.");CreateMenu();});
            Add("Modelo: "+(newModel=="mp_m_freemode_01"?"Masculino":"Feminino"),"Freemode do GTA instalado. Alternar aqui ainda nao troca o ped.",()=>{newModel=newModel=="mp_m_freemode_01"?"mp_f_freemode_01":"mp_m_freemode_01";CreateMenu();});
            Add("Criar perfil e abrir confirmacao","Cria uma pasta nova com saldo zero. Para jogar, confirme Carregar na proxima tela.",()=>{
                if(!CharacterRules.ValidName(newName))return;
                if(Directory.Exists(Cfg.SaveDir) && Directory.GetDirectories(Cfg.SaveDir).Count(d=>CharacterRules.ValidId(Path.GetFileName(d)))>=64){Notify.Show("Limite de 64 personagens cadastrados.");return;}
                var parents=GroomingSystem.NativeParents();if(parents.Length==0){Notify.Show("Origens freemode indisponiveis. Nada criado.");return;}
                int second=parents.Contains(21)?21:parents[Math.Min(1,parents.Length-1)];
                var card=new CharacterCard{Id="RP_"+Guid.NewGuid().ToString("N").Substring(0,16),Name=newName,Model=newModel,BaseFace=new FaceHeritage{ShapeFirst=parents[0],SkinFirst=parents[0],ShapeSecond=second,SkinSecond=second}};
                files.Create(card);Confirm(card);
            });
            Add("Voltar","Nenhuma troca de ped.",OpenMenu);menu.Visible=true;
        }
        void Confirm(CharacterCard supplied)
        {
            var card=files.Read(supplied.Id);menu.Clear();menu.Name="Carregar "+card.Name+"?";
            Add("Confirmar troca de personagem","Salva perfil atual antes da troca. Restaura o personagem escolhido no local atual, sem copiar dinheiro/itens de outro perfil. Requer ficar parado a pe, fora de missao/procurado/atividade.",()=>Begin(card,null));
            Add("Cancelar","Perfil criado fica na lista; nao altera ped, dinheiro ou perfil ativo.",OpenMenu);menu.Visible=true;
        }
        CharacterReturn ReadReturn()
        {var value=json.Read<CharacterReturn>(ReturnPath,()=>null);if(value==null || !value.Valid){json.Protect(ReturnPath);throw new InvalidDataException("Invalid protagonist return snapshot.");}return value;}
        void ConfirmReturn()
        {
            var original=ReadReturn();menu.Clear();menu.Name="Voltar para "+original.Profile+"?";
            Add("Confirmar volta","Salva personagem RPCore antes de restaurar roupas/equipamento do protagonista de origem. Mantem local atual; nao modifica saldo do modo historia.",()=>Begin(null,original));
            Add("Cancelar","Continua no personagem atual.",OpenMenu);menu.Visible=true;
        }
        void Begin(CharacterCard supplied,CharacterReturn original)
        {
            if(!Safe || operation!=null){Notify.Show("Pare a pe, sem missao/procurado/combate/outra interface.");return;}
            if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return;}lease=true;
            Save();var card=supplied==null?null:files.Read(supplied.Id);
            if(card!=null && card.Id==Profile.Id && !card.ApplyPending && Game.Player.Character.Model.Hash==CharacterRules.ModelHash(card.Model)){End();Notify.Show("Este personagem ja esta ativo. Salve ou personalize na barbearia.");return;}
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
            Function.Call(Hash.REQUEST_MODEL,hash);menu.Clear();menu.Name="Carregando modelo / ate 10s";
            Add("Cancelar carregamento","Antes da troca, mantem personagem atual; depois, pode deixar aplicacao pendente para retomar.",()=>{End();menu.Visible=false;});menu.SelectedIndex=0;menu.Visible=true;
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
            op.Started=Game.GameTime;menu.Name="Preparando rosto / ate 10s";
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
            End();menu.Visible=false;Notify.Show("Personagem carregado / perfil "+Profile.Id+". F10: roupas, moradia e barbearias.");
        }
        public void Tick()
        {
            var op=operation;if(op==null)return;
            try{
                bool same=op.Swapped?op.Actor!=null && op.Actor.Exists() && op.Actor==Game.Player.Character && op.Actor.Model.Hash==op.Model.Hash && Profile.Id==op.Profile:op.Origin!=null && op.Origin.Exists() && op.Origin==Game.Player.Character && Profile.Id==op.OriginProfile;
                if(!same || !Safe || !menu.Visible || unchecked((uint)(Game.GameTime-op.Started))>=10000 || !op.Swapped && op.Origin.Position.DistanceTo(op.Position)>2){End();menu.Visible=false;Notify.Show("Carregamento interrompido. Perfil preservado; confira a lista de personagens para retomar.");return;}
                if(!op.Swapped){if(op.Model.IsLoaded)Swap(op);return;}
                if(Function.Call<bool>(Hash.HAS_PED_HEAD_BLEND_FINISHED,op.Actor) && unchecked((uint)(Game.GameTime-op.Started))>0)Complete(op);
            }
            catch(Exception ex){End();menu.Visible=false;Log.Error("character load",ex);Notify.Show("Carregamento incompleto. Snapshot preservado; retome pela lista de personagens.");}
        }
        void End(){var op=operation;operation=null;if(op!=null)op.Model.MarkAsNoLongerNeeded();if(lease)gate.Release(Name);lease=false;}
        public void Save()=>SaveBody(false);
        void SaveBody(bool beforeSwap)
        {
            if(current==null || !CharacterRules.CanCapture(current,Profile.Id,Game.Player.Character.Model.Hash,!Game.Player.Character.IsDead,!beforeSwap && operation!=null))return;
            var next=CharacterRules.Copy(current);next.Body=CharacterRules.MergeBody(next.Body,CharacterBodyRuntime.Capture(Game.Player.Character));files.Write(next);current=next;
        }
        public void Reload()
        {
            if(switching)return;End();menu.Visible=false;current=null;
            if(CharacterRules.ValidId(Profile.Id))try{current=files.Read(Profile.Id);}catch(Exception ex){Log.Error("character profile",ex);}
        }
        public void OnKey(KeyEventArgs e){}
        public void Dispose(){End();menu.Visible=false;}
    }
}
