using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.UI;

namespace RPCore
{
    public partial class NeedsSystem : ISystem
    {
        public string Name { get { return "needs"; } }
        public event Action<string,string> Alert;

        NeedsData d;
        int last, hurtClock, saveClock;
        bool lowFood,lowWater,lowSleep,wasSprinting;
        readonly ShiftGate gate;
        bool wasDead;
        bool resting;
        public float Hunger { get { return d.Hunger; } }
        public float Thirst { get { return d.Thirst; } }
        public float Sleep { get { return d.Sleep; } }

        public NeedsSystem(ShiftGate gate=null,Func<bool> menusOpen=null) { this.gate=gate;this.menusOpen=menusOpen;Reload(); }
        public NeedsConsumption PrepareFood(string id,float hunger,float thirst,float energy=0)=>NeedsRules.Food(d,id,hunger,thirst,energy);
        public bool Consumed(string id)=>d.LastConsumptionId==id;
        public bool ApplyConsumption(NeedsConsumption receipt)=>NeedsRules.Apply(ref d,receipt,next=>Store.Write("Needs.json",next));

        public void Reload()
        {
            ResetSleep();
            d = Store.Load("Needs.json", () => new NeedsData());
            last = Game.GameTime;
            d.Hunger = Clamp(d.Hunger); d.Thirst = Clamp(d.Thirst);
            d.Sleep = Clamp(d.Sleep);d.Stamina=Clamp(d.Stamina);d.StaminaCooldown=Math.Max(0,Math.Min(5000,d.StaminaCooldown));wasSprinting=false; resting = false;
            wasDead = false;hurtClock=saveClock=0;lowFood=lowWater=lowSleep=false;
        }

        public void Feed(float hunger, float thirst)
        {
            d.Hunger = Clamp(d.Hunger + hunger);
            d.Thirst = Clamp(d.Thirst + thirst);
        }

        static float Clamp(float v) => NeedsRules.Clamp(v);

        public void ToggleRest()
        {
            var p = Game.Player.Character;
            if(sleeping){Notify.Show("Voce esta recuperando o sono. Aguarde acordar.");return;}
            if (!resting && (p.IsInVehicle() || p.IsInCombat || p.IsDead || p.Speed > .3f || Game.IsMissionActive)) { Notify.Show("Pare a pe, fora de combate e de missao, para descansar."); return; }
            resting = !resting; Notify.Show(resting ? "Descansando. Mover-se interrompe o descanso." : "Descanso encerrado.");
        }

        public void Tick()
        {
            int now = Game.GameTime;
            int delta=NeedsRules.Delta(now,last,!Game.IsPaused && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE));last=now;
            NativeBedTick(now);
            if(Game.IsPaused || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE))return;

            Ped p = Game.Player.Character;
            if (p==null || !p.Exists()) return;

            if(sleeping){SleepTick(p,delta);DrawHud();return;}
            if (p.IsDead) { wasDead = true; resting = false; return; }
            if (wasDead)
            {
                wasDead = false; // respawned at hospital, don't leave them starving
                d.Hunger = Math.Max(d.Hunger, 60f);
                d.Thirst = Math.Max(d.Thirst, 60f);d.Sleep=Math.Max(d.Sleep,60f);d.Stamina=100;d.StaminaCooldown=0;if(d.SleepRecovery?.Valid==true)d.SleepRecovery=null;
            }

            float mult = p.IsSprinting ? Cfg.SprintMult : (p.IsRunning ? 1.4f : 1f);
            if (resting && (p.Speed > .3f || p.IsInVehicle() || p.IsInCombat || Game.IsMissionActive)) resting = false;
            float food=Math.Abs(Cfg.HungerPerMin-1.1f)<.0001f?1:Cfg.HungerPerMin;
            float water=Math.Abs(Cfg.ThirstPerMin-1.6f)<.0001f?1:Cfg.ThirstPerMin;
            NeedsRules.Advance(d,delta,food,water,mult,resting);
            bool sprinting=p.IsSprinting && !p.IsSwimmingUnderWater;NeedsRules.Stamina(d,delta,sprinting,wasSprinting);wasSprinting=sprinting;
            if(d.StaminaCooldown>0 && !Game.IsMissionActive && !p.IsInVehicle())Game.DisableControlThisFrame(GTA.Control.Sprint);
            Warn(d.Hunger,ref lowFood,"Fome baixa. Coma algo no inventario.");
            Warn(d.Thirst,ref lowWater,"Sede baixa. Beba algo no inventario.");
            Warn(d.Sleep,ref lowSleep,"Energia baixa. Pare em seguranca para descansar.");
            bool depleted=d.Hunger<=0 || d.Thirst<=0;
            hurtClock=depleted?Math.Min(1500,hurtClock+delta):0;
            if(depleted && !Game.IsMissionActive)
            {
                if(hurtClock>=1500){int floor=Math.Min(p.MaxHealth,101);if(p.Health>floor)p.Health=Math.Max(floor,p.Health-1);hurtClock=0;}
                if(p.IsOnFoot && !p.IsInVehicle())Game.DisableControlThisFrame(GTA.Control.Sprint);

            }
            if(d.Sleep<=0 || d.SleepRecovery!=null)TrySleep(p);
            saveClock+=delta;if(saveClock>=5000){saveClock=0;try{Save();}catch(Exception ex){Log.Error("needs checkpoint",ex);}}

            DrawHud();
        }

        void Warn(float value,ref bool warned,string message){if(value>20)warned=false;if(value<=10 && !warned){warned=true;Notify.Show(message);try{Alert?.Invoke("Fitness",message);}catch(Exception ex){Log.Error("needs alert history",ex);}}}

        void DrawHud()
        {
            if (Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE) || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) || Function.Call<bool>(Hash.IS_HUD_HIDDEN)) return;
            var p = Game.Player.Character;
            if (!Visuals.NeedsHud) return;
            float stamina = p.IsSwimmingUnderWater?Clamp(Game.Player.RemainingUnderwaterTime*4.1f):d.Stamina;
            if (Visuals.TryNeeds(d.Hunger, d.Thirst, NeedsRules.Health(p.Health,p.MaxHealth), p.Armor, stamina, d.Sleep)) return;
            Bar(Cfg.HudX, Cfg.HudY, "FOOD", d.Hunger);
            Bar(Cfg.HudX, Cfg.HudY + 22f, "WATER", d.Thirst);
        }

        static void Bar(float x, float y, string label, float value)
        {
            float f = value / 100f;
            Color c = f > 0.5f ? Color.FromArgb(200, 120, 200, 90)
                    : f > 0.2f ? Color.FromArgb(200, 230, 180, 50)
                               : Color.FromArgb(220, 220, 60, 50);
            new TextElement(label, new PointF(x, y - 3f), 0.3f, Color.White).Draw();
            new ContainerElement(new PointF(x + 52f, y), new SizeF(110f, 12f), Color.FromArgb(140, 0, 0, 0)).Draw();
            if (f > 0f) new ContainerElement(new PointF(x + 52f, y), new SizeF(110f * f, 12f), c).Draw();
        }

        public void OnKey(KeyEventArgs e) { }
        public void Save() { Store.Write("Needs.json", d); }
        public void Dispose() { StopSleeping();bedSleep.Reset(); }
    }
}
