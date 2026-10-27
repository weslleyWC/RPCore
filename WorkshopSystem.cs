using System;
using System.Linq;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    // One reused menu. Highlighting never changes a vehicle and costs nothing.
    public sealed class WorkshopSystem
    {
        readonly ObjectPool pool;
        readonly NativeMenu menu = new NativeMenu("Oficina", "Personalizacao");
        readonly ShiftGate gate;
        Vehicle car;
        Ped actor;
        string profile;
        Func<bool> location;
        public WorkshopSystem(ObjectPool pool, ShiftGate gate)
        { this.pool=pool;this.gate=gate;pool.Add(menu);Visuals.Register(menu,"garage"); }
        bool Access => Cfg.EnableGarages && profile==Profile.Id && actor==Game.Player.Character && actor!=null && actor.Exists() && actor.IsAlive && car!=null && car.Exists() && !car.IsDead && actor.CurrentVehicle==car && car.Driver==actor && car.Speed<.5f && car.PassengerCount==0 && !Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,car) && !JobVehicles.Contains(car) && !Game.IsMissionActive && Game.Player.Wanted.WantedLevel==0 && (car.Model.IsCar || car.Model.IsBike && !car.Model.IsBicycle) && (gate==null || gate.Owner=="workshop") && location!=null && location();
        public void Open(Func<bool> nearby)
        {
            if(gate!=null && !gate.Acquire("workshop")){Notify.Show("Encerre a atividade atual antes da oficina.");return;}
            profile=Profile.Id;actor=Game.Player.Character;car=actor.CurrentVehicle;location=nearby;
            if(!Access){Close();Notify.Show("Pare como motorista, sem passageiros, procurado ou missao.");return;}
            Function.Call(Hash.SET_VEHICLE_MOD_KIT,car,0);pool.HideAll();Root();menu.Visible=true;
        }
        void Row(string title, Action action) {var row=new NativeItem(title,"Enter confirma a compra pelo banco. Sem alteracao ao apenas selecionar.");row.Activated+=(s,e)=>{if(!Access){Close();return;}try{action();}catch(Exception ex){Log.Error("workshop purchase",ex);Notify.Show("Compra nao concluida; confira o log.");}};menu.Add(row);}
        void Back(Action action) => Row("Voltar",action);
        int Get(Hash hash, params InputArgument[] args) => Function.Call<int>(hash,args);
        void IntOption(string name,int cost,Func<int> get,Action<int> set,int value,Action refresh)
        {Row(name+(get()==value?" [instalado]":" / $"+cost),()=>{int old=get();Purchase(cost,()=>get()==value,()=>set(value),()=>get()==value,()=>set(old));refresh();});}
        void Purchase(int price,Func<bool> same,Action apply,Func<bool> verify,Action undo)
        {
            SavedVehicle original=null;int index=-1;var transport=Wallet.Transport;
            var plate=Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,car);
            original=transport.FindIdentity(car.Model.Hash,plate);
            if(original!=null && (original.Stored || original.PendingRemoval || Wallet.VehicleInsurance.Pending?.VehicleId==original.Id)){Notify.Show("Veiculo possui operacao de garagem/seguro pendente.");return;}
            if(original!=null)index=transport.Vehicles.IndexOf(original);
            bool ok=Wallet.WorkshopBuy(price,()=>Access,same,()=>{
                apply();
                if(index>=0){var next=VehicleSnapshot.Capture(car);next.Id=original.Id;next.GarageId=original.GarageId;next.Extra=original.Extra;transport.Vehicles[index]=next;}
            },verify,()=>{if(index>=0)transport.Vehicles[index]=original;if(car!=null && car.Exists())undo();});
            Notify.Show(ok?"Personalizacao instalada e paga pelo banco.":"Sem compra: item atual, saldo insuficiente ou opcao indisponivel.");
        }
        void Root()
        {
            menu.Clear();menu.Name="Oficina / banco $"+Wallet.Bank;
            for(int i=0;i<50;i++)
            {
                int slot=i;int count=Get(Hash.GET_NUM_VEHICLE_MODS,car,slot);
                if(!WorkshopRules.Mod(slot,-1,count))continue;
                string label=Function.Call<string>(Hash.GET_MOD_SLOT_NAME,car,slot);
                Row(Label(label,"Pecas / slot "+slot)+" ("+count+")",()=>Mods(slot));
            }
            foreach(int i in new[]{18,20,22}){int slot=i;string name=slot==18?"Turbo":slot==20?"Fumaca dos pneus":"Xenon";
                Row(name+" / $"+(slot==18?500:200),()=>{bool old=Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON,car,slot);Purchase(slot==18?500:200,()=>false,()=>Function.Call(Hash.TOGGLE_VEHICLE_MOD,car,slot,!old),()=>Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON,car,slot)==!old,()=>Function.Call(Hash.TOGGLE_VEHICLE_MOD,car,slot,old));Root();});}
            Row("Pintura / interior / perolado",Paint);
            Row("Rodas: categoria",Wheels);
            Row("Vidros / placa / liveries",Details);
            Row("Neon / fumaca: cores e lados",Lights);
            Row("Extras",Extras);
            Row("Pneus: protecao / $200",()=>{bool old=Function.Call<bool>(Hash.GET_VEHICLE_TYRES_CAN_BURST,car);Purchase(200,()=>false,()=>Function.Call(Hash.SET_VEHICLE_TYRES_CAN_BURST,car,!old),()=>Function.Call<bool>(Hash.GET_VEHICLE_TYRES_CAN_BURST,car)==!old,()=>Function.Call(Hash.SET_VEHICLE_TYRES_CAN_BURST,car,old));Root();});
            Row("Fechar",Close);
        }
        static string Label(string key,string fallback)
        {if(string.IsNullOrWhiteSpace(key))return fallback;string text=Function.Call<string>(Hash.GET_FILENAME_FOR_AUDIO_CONVERSATION,key);return string.IsNullOrWhiteSpace(text)||text=="NULL"?fallback:text;}
        void Mods(int slot)
        {
            menu.Clear();int count=Get(Hash.GET_NUM_VEHICLE_MODS,car,slot);
            for(int i=-1;i<count && i<2048;i++){int value=i;
                var text=i<0?"Original":Label(Function.Call<string>(Hash.GET_MOD_TEXT_LABEL,car,slot,i),"Opcao "+(i+1));
                bool variation=Function.Call<bool>(Hash.GET_VEHICLE_MOD_VARIATION,car,slot);
                IntOption(text,WorkshopRules.Price(slot),()=>Get(Hash.GET_VEHICLE_MOD,car,slot),n=>Function.Call(Hash.SET_VEHICLE_MOD,car,slot,n,variation),value,()=>Mods(slot));}
            Back(Root);
        }
        void Wheels()
        {
            menu.Clear();foreach(int i in Enumerable.Range(0,13)){int type=i;
                Row("Categoria "+type+(Get(Hash.GET_VEHICLE_WHEEL_TYPE,car)==type?" [atual]":" / $700"),()=>{
                    int old=Get(Hash.GET_VEHICLE_WHEEL_TYPE,car),front=Get(Hash.GET_VEHICLE_MOD,car,23),rear=Get(Hash.GET_VEHICLE_MOD,car,24);
                    bool fv=Function.Call<bool>(Hash.GET_VEHICLE_MOD_VARIATION,car,23),rv=Function.Call<bool>(Hash.GET_VEHICLE_MOD_VARIATION,car,24);
                    Purchase(700,()=>old==type,()=>Function.Call(Hash.SET_VEHICLE_WHEEL_TYPE,car,type),()=>Get(Hash.GET_VEHICLE_WHEEL_TYPE,car)==type,()=>{Function.Call(Hash.SET_VEHICLE_WHEEL_TYPE,car,old);Function.Call(Hash.SET_VEHICLE_MOD,car,23,front,fv);Function.Call(Hash.SET_VEHICLE_MOD,car,24,rear,rv);});Wheels();});}Back(Root);
        }
        void Palette(string title,Func<int> get,Action<int> set,Action back)
        {menu.Clear();for(int i=0;i<=160;i++){int color=i;IntOption(title+" "+color,300,get,set,color,()=>Palette(title,get,set,back));}Back(back);}
        int[] Pair(Hash hash){var a=new OutputArgument();var b=new OutputArgument();Function.Call(hash,car,a,b);return new[]{a.GetResult<int>(),b.GetResult<int>()};}
        void Paint()
        {
            menu.Clear();
            foreach(bool secondary in new[]{false,true}){bool sec=secondary;Row(sec?"Secundaria":"Primaria",()=>Colors(sec));}
            foreach(bool wheel in new[]{false,true}){bool wh=wheel;Row(wh?"Cor das rodas":"Perolado",()=>Palette("Cor",()=>Pair(Hash.GET_VEHICLE_EXTRA_COLOURS)[wh?1:0],n=>{var p=Pair(Hash.GET_VEHICLE_EXTRA_COLOURS);Function.Call(Hash.SET_VEHICLE_EXTRA_COLOURS,car,wh?p[0]:n,wh?n:p[1]);},Paint));}
            foreach(bool dash in new[]{false,true}){bool d=dash;Row(d?"Painel":"Interior",()=>Palette("Cor",()=>{var o=new OutputArgument();Function.Call(d?Hash.GET_VEHICLE_EXTRA_COLOUR_5:Hash.GET_VEHICLE_EXTRA_COLOUR_6,car,o);return o.GetResult<int>();},n=>Function.Call(d?Hash.SET_VEHICLE_EXTRA_COLOUR_5:Hash.SET_VEHICLE_EXTRA_COLOUR_6,car,n),Paint));}Back(Root);
        }
        void Colors(bool secondary)
        {
            menu.Clear();for(int i=0;i<=160;i++){int value=i;Row("Cor "+i+" / $300",()=>{
                var old=VehicleSnapshot.Capture(car);int index=secondary?1:0;
                Action restore=()=>{Function.Call(Hash.SET_VEHICLE_COLOURS,car,old.PrimaryColor,old.SecondaryColor);RestoreCustom(old);};
                Purchase(300,()=>Pair(Hash.GET_VEHICLE_COLOURS)[index]==value && !Function.Call<bool>(secondary?Hash.GET_IS_VEHICLE_SECONDARY_COLOUR_CUSTOM:Hash.GET_IS_VEHICLE_PRIMARY_COLOUR_CUSTOM,car),()=>{
                    var colors=Pair(Hash.GET_VEHICLE_COLOURS);Function.Call(secondary?Hash.CLEAR_VEHICLE_CUSTOM_SECONDARY_COLOUR:Hash.CLEAR_VEHICLE_CUSTOM_PRIMARY_COLOUR,car);Function.Call(Hash.SET_VEHICLE_COLOURS,car,secondary?colors[0]:value,secondary?value:colors[1]);
                    var rgb=secondary?old.CustomPrimary:old.CustomSecondary;if(rgb!=null)Function.Call(secondary?Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR:Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR,car,rgb[0],rgb[1],rgb[2]);
                },()=>Pair(Hash.GET_VEHICLE_COLOURS)[index]==value,restore);Colors(secondary);});}Back(Paint);
        }
        void RestoreCustom(SavedVehicle s)
        {Function.Call(Hash.CLEAR_VEHICLE_CUSTOM_PRIMARY_COLOUR,car);Function.Call(Hash.CLEAR_VEHICLE_CUSTOM_SECONDARY_COLOUR,car);if(s.CustomPrimary!=null)Function.Call(Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR,car,s.CustomPrimary[0],s.CustomPrimary[1],s.CustomPrimary[2]);if(s.CustomSecondary!=null)Function.Call(Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR,car,s.CustomSecondary[0],s.CustomSecondary[1],s.CustomSecondary[2]);}
        void Details()
        {
            menu.Clear();for(int i=0;i<=6;i++){int n=i;IntOption("Vidros "+i,310,()=>Get(Hash.GET_VEHICLE_WINDOW_TINT,car),v=>Function.Call(Hash.SET_VEHICLE_WINDOW_TINT,car,v),n,Details);}
            for(int i=0;i<=5;i++){int n=i;IntOption("Estilo de placa "+i,200,()=>Get(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT_INDEX,car),v=>Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT_INDEX,car,v),n,Details);}
            int count=Get(Hash.GET_VEHICLE_LIVERY_COUNT,car);for(int i=0;i<count && i<2048;i++){int n=i;IntOption("Livery "+i,1000,()=>Get(Hash.GET_VEHICLE_LIVERY,car),v=>Function.Call(Hash.SET_VEHICLE_LIVERY,car,v),n,Details);}Back(Root);
        }
        void Lights()
        {
            menu.Clear();for(int i=0;i<4;i++){int side=i;Row("Neon lado "+i+" / $200",()=>{bool old=Function.Call<bool>(Hash.GET_VEHICLE_NEON_ENABLED,car,side);Purchase(200,()=>false,()=>Function.Call(Hash.SET_VEHICLE_NEON_ENABLED,car,side,!old),()=>Function.Call<bool>(Hash.GET_VEHICLE_NEON_ENABLED,car,side)==!old,()=>Function.Call(Hash.SET_VEHICLE_NEON_ENABLED,car,side,old));Lights();});}
            foreach(bool smoke in new[]{false,true}){bool s=smoke;Row(s?"Cor da fumaca":"Cor do neon",()=>Rgb(s));}Back(Root);
        }
        void Rgb(bool smoke)
        {
            menu.Clear();foreach(var rgb in new[]{new[]{255,255,255},new[]{255,0,0},new[]{0,255,0},new[]{0,0,255},new[]{255,255,0},new[]{255,0,255},new[]{0,255,255},new[]{255,128,0}}){var value=rgb;
                Row(string.Join(",",value)+" / $200",()=>{var old=VehicleSnapshot.Capture(car);var before=smoke?old.SmokeColor:old.NeonColor;Hash set=smoke?Hash.SET_VEHICLE_TYRE_SMOKE_COLOR:Hash.SET_VEHICLE_NEON_COLOUR;
                    Purchase(200,()=>before.SequenceEqual(value),()=>Function.Call(set,car,value[0],value[1],value[2]),()=>{var now=VehicleSnapshot.Capture(car);return (smoke?now.SmokeColor:now.NeonColor).SequenceEqual(value);},()=>Function.Call(set,car,before[0],before[1],before[2]));Rgb(smoke);});}Back(Lights);
        }
        void Extras()
        {
            menu.Clear();for(int i=0;i<=14;i++){int extra=i;if(!Function.Call<bool>(Hash.DOES_EXTRA_EXIST,car,extra))continue;Row("Extra "+i+" / $150",()=>{bool old=Function.Call<bool>(Hash.IS_VEHICLE_EXTRA_TURNED_ON,car,extra);Purchase(150,()=>false,()=>Function.Call(Hash.SET_VEHICLE_EXTRA,car,extra,old),()=>Function.Call<bool>(Hash.IS_VEHICLE_EXTRA_TURNED_ON,car,extra)==!old,()=>Function.Call(Hash.SET_VEHICLE_EXTRA,car,extra,!old));Extras();});}Back(Root);
        }
        public void Tick(){if(car!=null && (!menu.Visible || !Access))Close();}
        public void Close(){menu.Visible=false;car=null;actor=null;location=null;gate?.Release("workshop");}
    }
}
