using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public abstract class DrivingJobSystem : ISystem, IWorkContract
    {
        public abstract string Name { get; }
        public abstract string Label { get; }
        public bool Active => Shift != null;
        protected readonly ObjectPool Pool;
        protected readonly Random Random = new Random();
        protected readonly NativeMenu Menu;
        protected DrivingShift Shift;
        readonly ShiftGate gate;
        readonly AchievementSystem achievements;
        Blip route;
        protected Vector3 Target;
        int cancelSince = -1, paymentRetry;
        protected bool InputAllowed => !InterfaceOptions.ModalOpen && !Pool.AreAnyVisible && !Game.IsPaused;
        protected DrivingJobSystem(ObjectPool pool, AchievementSystem achievements, ShiftGate gate)
        {
            Pool = pool; this.achievements = achievements; this.gate = gate;
            Menu = new NativeMenu(Label,"TRABALHO / MOTORISTA"); Pool.Add(Menu); Visuals.Register(Menu,Name);
        }
        protected static Vector3 Position(JobPoint p) => new Vector3(p.X,p.Y,p.Z);
        protected static JobPoint Point(Vector3 p) => new JobPoint { Name="position", X=p.X,Y=p.Y,Z=p.Z };
        protected void Add(string title, string description, Action action)
        {
            var row = new NativeItem(title,description);
            row.Activated += (s,e) => { try { action(); } catch(Exception ex) { Log.Error(Name+" action",ex); Notify.Show("Acao nao concluida. Veja RPCore.log."); } };
            Menu.Add(row);
        }
        public void OpenMenu()
        {
            Pool.HideAll(); Menu.Clear(); var progress = Wallet.JobProgress(Name);
            Menu.Name = Label+" / "+progress.Deliveries+" servicos / $"+progress.Earned;
            BuildMenu();
            if (Active)
            {
                Add("GPS: objetivo atual","Marca o objetivo deste trabalho.",()=>{ Gps();Menu.Visible=false; });
                Add("Encerrar turno","Mantem servicos pagos; cancela o atendimento atual sem recompensa.",()=>End("Turno encerrado."));
            }
            Menu.Visible=true;
        }
        protected abstract void BuildMenu();
        protected abstract void StopOwned();
        protected abstract void WorkTick();
        protected virtual void IdleTick() { }
        protected bool BeginShift(int count)
        {
            var p = Game.Player.Character;
            if (Active || p.IsDead || !p.IsHuman || p.IsRagdoll || p.IsInCombat || Game.IsMissionActive || Game.IsPaused || Game.Player.Wanted.WantedLevel > 0) { Notify.Show("Inicie sem procurado, combate ou missao.");return false; }
            if (!gate.Acquire(Name)) { Notify.Show("Encerre o trabalho de "+gate.Owner+" antes de iniciar outro.");return false; }
            Shift=new DrivingShift(count);paymentRetry=0;cancelSince=-1;Menu.Visible=false;return true;
        }
        protected bool Award(int step, int amount, bool complete, int rating)
        {
            if (unchecked(Game.GameTime-paymentRetry)<0) return false;
            bool credited;
            try
            {
                if (!Wallet.PayDelivery(Name,Shift.Id,step,amount,true,true,complete,out credited,rating))
                { paymentRetry=Game.GameTime+3000;Notify.Show("Limite de saldo/progresso. Pagamento pendente.");return false; }
            }
            catch(Exception ex) { paymentRetry=Game.GameTime+5000;Log.Error(Name+" payment",ex);Notify.Show("Falha ao salvar. Pagamento nao confirmado; tente novamente.");return false; }
            if (credited)
            {
                Notify.Show("~g~+$"+amount+" no banco~s~ / "+Label+" / "+rating+" estrelas");
                try { Sfx.Play("delivered");achievements.Bump(Name=="valet"?"valet_returns":"uber_trips");achievements.Bump("cash_earned",amount);if(complete)achievements.Bump(Name+"_shifts");achievements.Save(); }
                catch(Exception ex) { Log.Error(Name+" presentation/achievements after payment",ex); }
            }
            return true;
        }
        protected void Route(Vector3 point, string name, BlipSprite sprite = BlipSprite.PersonalVehicleCar)
        {
            ClearRoute();Target=point;route=World.CreateBlip(point);route.Name="RPCore "+Label+": "+name;route.Sprite=sprite;route.Color=BlipColor.Yellow;route.Scale=.8f;route.ShowRoute=true;
        }
        protected void ClearRoute() { if(route!=null && route.Exists())route.Delete();route=null;Target=Vector3.Zero; }
        public void Gps() { if(Target!=Vector3.Zero) Function.Call(Hash.SET_NEW_WAYPOINT,Target.X,Target.Y);else Notify.Show("Aguardando objetivo."); }
        protected void Status(string text)
        {
            string message=Label+" "+Shift.Completed+"/"+Shift.Total+"  "+text;
            if(!Visuals.TryJobHeader(Name,message,Color.White))new TextElement(message,new PointF(GTA.UI.Screen.ScaledWidth/2,20),.36f,Color.White,GTA.UI.Font.ChaletLondon,Alignment.Center).Draw();
        }
        protected static void Marker(Vector3 point, float size=1.5f)
        {
            if(Game.Player.Character.Position.DistanceTo(point)<65)World.DrawMarker(MarkerType.Cylinder,point-new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(size,size,1),Color.FromArgb(110,80,160,255));
        }
        public void Tick()
        {
            if(!Active){IdleTick();return;}
            if(Game.Player.Character.IsDead || Game.IsMissionActive){End("Turno encerrado por morte ou inicio de missao.");return;}
            if(Game.IsPaused)return;
            WorkTick();
        }
        public void PollCancel()
        {
            if(!Active || !InputAllowed || !Game.IsKeyPressed(Keys.End)){cancelSince=-1;return;}
            if(cancelSince<0)cancelSince=Game.GameTime;
            if(unchecked(Game.GameTime-cancelSince)>=1000)End("Turno cancelado. Servicos ja pagos foram mantidos.");
        }
        protected void End(string message)
        {
            Shift?.Cancel();
            try { StopOwned();ClearRoute(); }
            finally { Shift=null;gate.Release(Name);Menu.Visible=false; }
            Notify.Show(message);
        }
        public void OnKey(KeyEventArgs e) { }
        public void Reload() { if(Active)End("Turno encerrado na troca de perfil.");Menu.Visible=false; }
        public void Save() { }
        public virtual void Dispose() { try { StopOwned();ClearRoute(); } finally { Shift=null;gate.Release(Name);Menu.Visible=false; } }
    }
}
