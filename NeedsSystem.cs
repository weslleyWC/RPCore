using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.UI;

namespace RPCore
{
    public class NeedsSystem : ISystem
    {
        public string Name { get { return "needs"; } }

        NeedsData d;
        int last, nextHurt;
        bool wasDead;
        bool resting;
        public float Hunger { get { return d.Hunger; } }
        public float Thirst { get { return d.Thirst; } }
        public float Sleep { get { return d.Sleep; } }

        public NeedsSystem() { Reload(); }

        public void Reload()
        {
            d = Store.Load("Needs.json", () => new NeedsData());
            last = Game.GameTime;
            d.Hunger = Clamp(d.Hunger); d.Thirst = Clamp(d.Thirst);
            d.Sleep = Clamp(d.Sleep); resting = false;
            wasDead = false;
        }

        public void Feed(float hunger, float thirst)
        {
            d.Hunger = Clamp(d.Hunger + hunger);
            d.Thirst = Clamp(d.Thirst + thirst);
        }

        static float Clamp(float v) { return float.IsNaN(v) || float.IsInfinity(v) ? 100 : Math.Max(0f, Math.Min(100f, v)); }

        public void ToggleRest()
        {
            var p = Game.Player.Character;
            if (!resting && (p.IsInVehicle() || p.IsInCombat || p.IsDead || p.Speed > .3f || Game.IsMissionActive)) { Notify.Show("Pare a pe, fora de combate e de missao, para descansar."); return; }
            resting = !resting; Notify.Show(resting ? "Descansando. Mover-se interrompe o descanso." : "Descanso encerrado.");
        }

        public void Tick()
        {
            int now = Game.GameTime;
            // clamp dt so pause / loading screens don't dump the bars
            float dtMin = Math.Min(Math.Max(now - last, 0) / 60000f, 0.05f);
            last = now;
            if (Game.IsPaused || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)) return;

            Ped p = Game.Player.Character;
            if (!p.Exists()) return;

            if (p.IsDead) { wasDead = true; resting = false; return; }
            if (wasDead)
            {
                wasDead = false; // respawned at hospital, don't leave them starving
                d.Hunger = Math.Max(d.Hunger, 60f);
                d.Thirst = Math.Max(d.Thirst, 60f);
            }

            float mult = p.IsSprinting ? Cfg.SprintMult : (p.IsRunning ? 1.4f : 1f);
            if (resting && (p.Speed > .3f || p.IsInVehicle() || p.IsInCombat || Game.IsMissionActive)) resting = false;
            d.Sleep = Clamp(d.Sleep + (resting ? 60 : -.25f) * dtMin);
            d.Hunger = Clamp(d.Hunger - Cfg.HungerPerMin * mult * dtMin);
            d.Thirst = Clamp(d.Thirst - Cfg.ThirstPerMin * mult * dtMin);

            // starving/dehydrated drains health but never finishes you off
            if ((d.Hunger <= 0f || d.Thirst <= 0f) && unchecked(now - nextHurt) >= 0 && p.Health > Math.Min(p.MaxHealth, 101))
            {
                p.Health -= 1;
                nextHurt = now + 1500;
            }

            DrawHud();
        }

        void DrawHud()
        {
            if (Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE) || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) || Function.Call<bool>(Hash.IS_HUD_HIDDEN)) return;
            var p = Game.Player.Character;
            if (!Visuals.NeedsHud) return;
            float stamina = Function.Call<float>(Hash.GET_PLAYER_SPRINT_STAMINA_REMAINING, Game.Player.Handle);
            if (Visuals.TryNeeds(d.Hunger, d.Thirst, p.MaxHealth > 100 ? (p.Health - 100) * 100f / (p.MaxHealth - 100) : p.Health * 100f / Math.Max(1, p.MaxHealth), p.Armor, stamina, d.Sleep)) return;
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
        public void Dispose() { }
    }
}
