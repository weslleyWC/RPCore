using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class BarberSystem : ISystem
    {
        public string Name=>"barber";
        public Action OpenDetails;
        public bool CanUseService=>AtShop;
        readonly ObjectPool pool;readonly ShiftGate gate;readonly NativeMenu menu;readonly BarberOptions options;readonly List<Blip> blips=new List<Blip>();
        Ped actor;BarberLook original,draft;string profile,receipt;Vector3 shop;float heading;
        static Vector3 Pos(JobPoint p)=>new Vector3(p.X,p.Y,p.Z);
        static bool Freemode(int model)=>model==new Model("mp_m_freemode_01").Hash || model==new Model("mp_f_freemode_01").Hash;
        bool Safe=>!Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !Game.Player.Character.IsDead && Game.Player.Character.IsHuman && !Game.Player.Character.IsInVehicle() && !Game.Player.Character.IsRagdoll && !Game.Player.Character.IsFalling && !Game.Player.Character.IsInCombat && Game.Player.Wanted.WantedLevel==0;
        JobPoint Nearest=>options.Shops.OrderBy(p=>Game.Player.Character.Position.DistanceTo(Pos(p))).First();
        public BarberSystem(ObjectPool pool,ShiftGate gate)
        {
            this.pool=pool;this.gate=gate;var defaults=BarberDefaults.Load();options=Store.LoadDef("Jobs/Barber.json",()=>defaults);
            if(options==null || !options.Valid){Log.Write("Invalid Barber.json; embedded defaults used, original retained.");options=defaults;}
            menu=new NativeMenu("Barbearias RPCore","CORTES / TEXTURAS / OLHOS");pool.Add(menu);Visuals.Register(menu,"core");
            foreach(var p in options.Shops)blips.Add(MapBlips.Create(Pos(p),"RPCore: "+p.Name,BlipSprite.Barber,BlipColor.Blue,.7f));
        }
        void Add(string title,string description,Action action)
        {var row=new NativeItem(title,description);row.Activated+=(s,e)=>{try{action();}catch(Exception ex){try{Cancel();}catch(Exception restore){Log.Error("barber restore",restore);}Log.Error("barber action",ex);Notify.Show("Atendimento nao confirmado. Confira o log.");}};menu.Add(row);}
        static BarberLook Capture(Ped p)
        {
            var look=new BarberLook{Model=p.Model.Hash,Hair=Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION,p,2),Texture=Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION,p,2),Palette=Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION,p,2)};
            if(Freemode(look.Model)){int eye=Function.Call<int>(Hash.GET_HEAD_BLEND_EYE_COLOR,p);if(eye>=0 && eye<=31){look.HasEye=true;look.Eye=eye;}}
            if(!look.Valid)throw new InvalidOperationException("Unsupported barber snapshot.");return look;
        }
        static bool Compatible(Ped p,BarberLook look)
        {
            if(!look.Valid || p.Model.Hash!=look.Model || look.HasEye && (!Freemode(look.Model) || !Capture(p).HasEye))return false;
            int draws=Function.Call<int>(Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS,p,2);
            if(look.Hair>=draws && !(draws==0 && look.Hair==0))return false;
            int textures=Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,p,2,look.Hair);
            return look.Texture<textures || textures==0 && look.Texture==0;
        }
        static void Write(Ped p,BarberLook look)
        {
            if(p==null || !p.Exists() || p!=Game.Player.Character || p.Model.Hash!=look.Model)throw new InvalidOperationException("Barber owner changed.");
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION,p,2,look.Hair,look.Texture,look.Palette);
            if(look.HasEye)Function.Call(Hash.SET_HEAD_BLEND_EYE_COLOR,p,look.Eye);
            var actual=Capture(p);
            if(actual.Model!=look.Model || actual.Hair!=look.Hair || actual.Texture!=look.Texture || actual.Palette!=look.Palette || look.HasEye && (!actual.HasEye || actual.Eye!=look.Eye))throw new InvalidOperationException("Barber variation did not apply.");
        }
        static bool Apply(Ped p,BarberLook look)
        {
            if(!Compatible(p,look)){Notify.Show("Visual incompativel com este ped/recursos. Nada alterado.");return false;}
            var before=Capture(p);try{Write(p,look);return true;}catch{Write(p,before);throw;}
        }
        public void OpenMenu()
        {
            Cancel();pool.HideAll();menu.Clear();menu.Name="Barbearias / banco $"+Wallet.Bank;
            if(!Wallet.Barber.Valid){Notify.Show("Dados de barbearia invalidos; original preservado.");return;}
            if(OpenDetails!=null)Add("Aparencia / rosto e acabamento","Selecao sem alterar personagem; aplicacao apenas apos confirmar. Servico separado.",OpenDetails);
            Add("Iniciar previa / confirmar por $"+options.Price,"Perto de uma barbearia, a pe e sem atividade. Cobranca unica pelo banco RPCore apenas ao confirmar mudancas.",Begin);
            if(Wallet.Barber.Looks.Any(l=>l.Model==Game.Player.Character.Model.Hash))Add("Reaplicar ultimo visual confirmado","Na barbearia, mesmo modelo. Sem nova cobranca nem aplicacao automatica no load.",RestoreSaved);
            foreach(var p in options.Shops){var point=p;Add("GPS: "+point.Name,"Use o menu F10 da barbearia ao chegar. A tecla E nativa do GTA permanece disponivel.",()=>{Function.Call(Hash.SET_NEW_WAYPOINT,point.X,point.Y);menu.Visible=false;});}
            Add("Servicos "+Wallet.Barber.Services+" / pagos $"+Wallet.Barber.Spent,"Cortes/texturas; olhos nos freemode. Rosto, tintura, barba e pele no menu Aparencia.",()=>{});
            Add("Fechar","Retornar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        bool AtShop=>Safe && Game.Player.Character.Position.DistanceTo(Pos(Nearest))<=2.5f && !InterfaceOptions.ModalOpen && Function.Call<bool>(Hash.IS_GAMEPLAY_CAM_RENDERING);
        void Begin()
        {
            if(!AtShop || !Wallet.Barber.Valid){Notify.Show("Va a pe a barbearia, sem procurado/missao/outra camera.");return;}
            if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return;}
            actor=Game.Player.Character;profile=Profile.Id;heading=actor.Heading;shop=Pos(Nearest);receipt=Guid.NewGuid().ToString("N");original=Capture(actor);draft=JsonConvert.DeserializeObject<BarberLook>(JsonConvert.SerializeObject(original));BuildEditor();
        }
        bool EditingAllowed=>original!=null && actor!=null && actor.Exists() && actor==Game.Player.Character && actor.Model.Hash==original.Model && profile==Profile.Id && AtShop && actor.Position.DistanceTo(shop)<=3;
        void BuildEditor()
        {
            menu.Clear();menu.Name="Previa / corte "+draft.Hair+" / textura "+draft.Texture;
            Add("Corte anterior","Mantem as tintas atuais; muda apenas a variacao do cabelo.",()=>Change(false,-1));Add("Proximo corte","Variacoes disponiveis neste ped.",()=>Change(false,1));
            Add("Textura anterior","Texturas do corte selecionado.",()=>Change(true,-1));Add("Proxima textura","Texturas do corte selecionado.",()=>Change(true,1));
            if(original.HasEye){Add("Olhos: "+draft.Eye+" / anterior","Apenas freemode com leitura valida da cor atual.",()=>Eye(-1));Add("Proxima cor de olhos","Previa reversivel.",()=>Eye(1));}
            Add("Girar para a esquerda","Ajusta orientacao para conferir o visual na camera normal.",()=>Turn(-30));Add("Girar para a direita","Orientacao original restaurada ao sair.",()=>Turn(30));
            Add("Confirmar e pagar $"+options.Price,"Somente se houve alteracao; grava visual do modelo e debita banco juntos. Sem repetir ao recarregar.",Confirm);
            Add("Cancelar e restaurar","Fechar menu/afastar/abrir celular tambem cancela sem cobrar.",()=>{Cancel();OpenMenu();});menu.Visible=true;
        }
        void Change(bool texture,int delta)
        {
            if(!EditingAllowed){Cancel();return;}
            var next=JsonConvert.DeserializeObject<BarberLook>(JsonConvert.SerializeObject(draft));int count=texture?Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,actor,2,next.Hair):Function.Call<int>(Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS,actor,2);
            if(count<=0){Notify.Show("Sem variacoes disponiveis neste ped.");return;}
            if(texture)next.Texture=WardrobeRules.Cycle(next.Texture,delta,count,false);
            else{next.Hair=WardrobeRules.Cycle(next.Hair,delta,count,false);next.Texture=0;next.Palette=0;}
            if(Apply(actor,next))draft=next;BuildEditor();
        }
        void Eye(int delta)
        {if(!EditingAllowed || !original.HasEye){Cancel();return;}var next=JsonConvert.DeserializeObject<BarberLook>(JsonConvert.SerializeObject(draft));next.Eye=WardrobeRules.Cycle(next.Eye,delta,32,false);if(Apply(actor,next))draft=next;BuildEditor();}
        void Turn(float delta){if(EditingAllowed)actor.Heading+=delta;else Cancel();}
        void Confirm()
        {
            if(!EditingAllowed){Cancel();return;}
            if(original.Same(draft)){Notify.Show("Sem alteracao; nada cobrado.");return;}
            if(!Capture(actor).Same(draft)){Notify.Show("Outro sistema alterou o visual. Previa cancelada sem cobrar.");Cancel();return;}
            if(!Wallet.BarberConfirm(original,draft,options.Price,receipt,out _)){Notify.Show("Confira saldo do banco/limites/dados. Pode cancelar sem cobrar.");return;}
            Finish();Notify.Show("Atendimento confirmado: $"+options.Price+" do banco RPCore.");OpenMenu();
        }
        void RestoreSaved()
        {
            if(!AtShop || !Wallet.Barber.Valid){Notify.Show("Reaplique na barbearia.");return;}
            var look=Wallet.Barber.Looks.Find(l=>l.Model==Game.Player.Character.Model.Hash);if(look==null)return;
            if(!gate.Acquire(Name)){Notify.Show("Encerre a atividade de "+gate.Owner+".");return;}
            try{if(Apply(Game.Player.Character,look))Notify.Show("Visual confirmado reaplicado, sem nova cobranca.");}finally{gate.Release(Name);}
        }
        void Finish()
        {try{if(actor!=null && actor.Exists() && actor==Game.Player.Character && original!=null && actor.Model.Hash==original.Model)actor.Heading=heading;}finally{original=null;draft=null;actor=null;profile=null;receipt=null;gate.Release(Name);}}
        void Cancel()
        {try{if(original!=null && actor!=null && actor.Exists() && actor==Game.Player.Character && actor.Model.Hash==original.Model)Write(actor,original);}finally{Finish();}}
        public void Tick()
        {
            try
            {
                if(original!=null && (!EditingAllowed || !menu.Visible)){Cancel();menu.Visible=false;}
                if(!Safe || InterfaceOptions.ModalOpen || pool.AreAnyVisible)return;var p=Nearest;var pos=Pos(p);if(Game.Player.Character.Position.DistanceTo(pos)>15)return;
                World.DrawMarker(MarkerType.Cylinder,pos-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(1,1,.2f),Color.FromArgb(110,100,180,220));
                if(Game.Player.Character.Position.DistanceTo(pos)<=2.5f)GTA.UI.Screen.ShowHelpTextThisFrame("Barbearia RPCore: F10 > Barbearias / cortes e olhos");
            }
            catch(Exception ex){try{Cancel();}finally{menu.Visible=false;}Log.Error("barber tick",ex);}
        }
        public void OnKey(KeyEventArgs e){}
        public void Save(){}
        public void Reload(){try{Cancel();}finally{menu.Visible=false;}}
        public void Dispose(){try{Reload();}finally{foreach(var b in blips)if(b.Exists())b.Delete();blips.Clear();}}
    }
}
