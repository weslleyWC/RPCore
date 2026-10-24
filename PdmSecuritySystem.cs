using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class PdmSecuritySystem : ISystem
    {
        sealed class Guard
        {
            public string EmployeeId; public Ped Ped, Target; public Blip Blip; public Vector3 Post;
            public bool Paid, Returning; public int EngagedAt, Cooldown, NextWalk;
        }
        public string Name => "pdmsecurity";
        readonly ObjectPool pool; readonly NativeMenu menu; readonly PdmSecurityOptions options; readonly Vector3 office;
        readonly Func<IEnumerable<Ped>> crew; readonly Action syncPayroll, staffMenu, back;
        readonly Dictionary<string, Guard> guards = new Dictionary<string, Guard>();
        readonly Dictionary<string, int> respawn = new Dictionary<string, int>();
        int nextScan, nextSpawn; string profile;
        bool WorldAllowed => Wallet.Pdm.Valid && Wallet.Pdm.Owned && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && !Game.Player.Character.IsDead && Game.Player.Character.IsHuman && Game.Player.Wanted.WantedLevel == 0 && Game.Player.Character.Position.DistanceTo(office) < 180;
        static bool Alive(Ped p) => p != null && p.Exists() && !p.IsDead;
        static Vector3 Position(JobPoint p) => new Vector3(p.X, p.Y, p.Z);
        public PdmSecuritySystem(ObjectPool pool, PdmSecurityOptions options, Vector3 office, Func<IEnumerable<Ped>> crew, Action syncPayroll, Action staffMenu, Action back)
        {
            this.pool = pool; this.options = options; this.office = office; this.crew = crew; this.syncPayroll = syncPayroll; this.staffMenu = staffMenu; this.back = back;
            menu = new NativeMenu("PDM / seguranca", "POSTOS / DEFESA LOCAL"); pool.Add(menu); Visuals.Register(menu, "pdm"); profile = Profile.Id;
        }
        void Add(string title, string description, Action action)
        { var item = new NativeItem(title, description); item.Activated += (s, e) => { try { action(); } catch (Exception ex) { Log.Error("pdm security menu", ex); Notify.Show("Seguranca indisponivel; contratacoes e salarios preservados."); } }; menu.Add(item); }
        public void OpenMenu()
        {
            syncPayroll(); pool.HideAll(); menu.Clear();
            if (!Wallet.Pdm.Valid) { Notify.Show("Dados PDM invalidos; original preservado."); return; }
            foreach (var post in options.Posts)
            {
                var row = post; var employee = Wallet.Pdm.Staff.Employees.Find(e => e.Role == row.Offer.Id); Guard guard; guards.TryGetValue(row.Offer.Id, out guard);
                string state = employee == null ? "nao contratado" : !PdmSecurityPolicy.Paid(Wallet.Pdm, row.Offer.Id) ? "suspenso / salario ou acerto pendente" : guard == null ? "aguardando chegada perto da PDM" : guard.Target != null ? "defesa local" : "no posto / retornando";
                Add(row.Offer.Name + " / " + state, "GPS do posto. Salario devido: $" + (employee?.Owed ?? 0) + ". Defesa apenas contra NPCs atacando voce/equipe na area local.", () => { Function.Call(Hash.SET_NEW_WAYPOINT, row.Point.X, row.Point.Y); menu.Visible = false; });
            }
            Add("Contratar / pagar / demitir", "Equipe e salarios da empresa; nao usa banco pessoal automaticamente.", staffMenu);
            Add("Retornar guardas aos postos", "Interrompe a defesa e ordena retorno a pe, sem teleporte. Aguarda 10s antes de nova resposta.", () => { foreach (var g in guards.Values) Return(g); menu.Visible = false; });
            Add(options.EnableDefense ? "Defesa local: ativa" : "Defesa local: desativada na configuracao", "Raio de " + options.DefenseRadius + "m por posto. Suspende com procurado/missao, divida ou morte. Nao substitui policia nem cancela estrelas.", () => { });
            Add("Voltar", "Empresa PDM.", back); menu.Visible = true;
        }
        Vector3 SpawnPoint(JobPoint point)
        {
            var original = Position(point);
            foreach (var p in new[] { original, original + new Vector3(2, 0, 0), original - new Vector3(2, 0, 0) })
            { var safe = JobActors.Sidewalk(p); if (safe != Vector3.Zero && safe.DistanceTo(original) <= 6 && JobActors.Free(safe, 2)) return safe; }
            return Vector3.Zero;
        }
        void Spawn(PdmGuardPost post, PdmEmployee employee)
        {
            int now = Game.GameTime, due; if (respawn.TryGetValue(post.Offer.Id, out due) && unchecked(now - due) < 0) return;
            var model = new Model(post.Model); Ped ped = null; Blip blip = null;
            try
            {
                if (!model.IsValid || !model.IsPed || !model.IsInCdImage || !model.Request(1500)) return;
                var point = SpawnPoint(post.Point); if (point == Vector3.Zero) return;
                ped = World.CreatePed(model, point, post.Point.Heading); if (!Alive(ped)) return;
                ped.IsPersistent = true; ped.BlockPermanentEvents = true;
                Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD, ped, false);
                blip = ped.AddBlip(); blip.Name = "RPCore: " + employee.Name; blip.Color = BlipColor.White;
                var guard = new Guard { EmployeeId = employee.Id, Ped = ped, Blip = blip, Post = point };
                guards.Add(post.Offer.Id, guard); ped = null; blip = null;
                SetPaid(guard, PdmSecurityPolicy.Paid(Wallet.Pdm, post.Offer.Id), true);
            }
            finally { if (blip != null && blip.Exists()) blip.Delete(); if (ped != null && ped.Exists()) ped.Delete(); model.MarkAsNoLongerNeeded(); }
        }
        void SetPaid(Guard g, bool paid, bool initial = false)
        {
            if (paid == g.Paid && !initial) return;
            g.Paid = paid; g.Target = null; g.Ped.Task.ClearAll(); g.Ped.Weapons.RemoveAll();
            if (paid && options.EnableDefense) g.Ped.Weapons.Give(WeaponHash.CarbineRifle, 120, true, true);
            g.Blip.Color = paid ? BlipColor.Green : BlipColor.White; Return(g);
        }
        void Return(Guard g)
        { if (!Alive(g.Ped)) return; g.Target = null; g.Returning = true; g.Cooldown = Game.GameTime + 10000; JobActors.Walk(g.Ped, g.Post); g.NextWalk = Game.GameTime + 5000; }
        bool Threat(Guard g, Ped candidate, Ped[] protectedPeds)
        {
            if (candidate == null || !candidate.Exists()) return false;
            bool attacking = protectedPeds.Any(p => Alive(p) && Function.Call<bool>(Hash.IS_PED_IN_COMBAT, candidate, p));
            return PdmSecurityPolicy.CanDefend(options.EnableDefense, g.Paid, WorldAllowed,
                candidate.IsHuman, !candidate.IsDead, candidate == Game.Player.Character || Function.Call<bool>(Hash.IS_PED_A_PLAYER, candidate),
                candidate.PedType == PedType.Cop || candidate.PedType == PedType.Swat || candidate.PedType == PedType.Army, Function.Call<bool>(Hash.IS_ENTITY_A_MISSION_ENTITY, candidate),
                protectedPeds.Any(p => p == candidate), attacking, Function.Call<bool>(Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, g.Ped, candidate, 17),
                candidate.Position.DistanceTo(g.Post), g.Ped.Position.DistanceTo(g.Post), options.DefenseRadius);
        }
        void UpdateGuard(Guard g, Ped[] protectedPeds)
        {
            int now = Game.GameTime;
            if (g.Target != null && (unchecked(now - g.EngagedAt) >= options.EngagementSeconds * 1000 || !Threat(g, g.Target, protectedPeds))) Return(g);
            if (g.Target != null) return;
            if (g.Returning)
            {
                if (g.Ped.Position.DistanceTo(g.Post) <= 1.5f) { g.Ped.Task.StandStill(-1); g.Returning = false; }
                else if (unchecked(now - g.NextWalk) >= 0) { JobActors.Walk(g.Ped, g.Post); g.NextWalk = now + 5000; }
            }
            if (!g.Paid || !options.EnableDefense || g.Returning || unchecked(now - g.Cooldown) < 0) return;
            var target = World.GetNearbyPeds(g.Post, options.DefenseRadius).Where(p => Threat(g, p, protectedPeds)).OrderBy(p => p.Position.DistanceTo(g.Ped.Position)).FirstOrDefault();
            if (target == null) return;
            g.Target = target; g.EngagedAt = now; Function.Call(Hash.TASK_COMBAT_PED, g.Ped, target, 0, 16);
        }
        static void Delete(Guard g) { if (g.Blip != null && g.Blip.Exists()) g.Blip.Delete(); if (g.Ped != null && g.Ped.Exists()) g.Ped.Delete(); }
        void Clear() { foreach (var g in guards.Values) Delete(g); guards.Clear(); }
        public void Tick()
        {
            if (Game.IsPaused) return;
            if (profile != Profile.Id || !WorldAllowed || !Wallet.Pdm.Staff.Employees.Any(e => PdmSecurityPolicy.IsGuard(e.Role))) { Clear(); return; }
            int now = Game.GameTime; if (unchecked(now - nextScan) < 0) return; nextScan = now + 500;
            try
            {
                // Flush before duty decisions so a wage deadline cannot grant a free extra response.
                syncPayroll();
                foreach (var post in options.Posts)
                {
                    Guard g; guards.TryGetValue(post.Offer.Id, out g); var e = Wallet.Pdm.Staff.Employees.Find(x => x.Role == post.Offer.Id);
                    if (g != null && (e == null || e.Id != g.EmployeeId || !Alive(g.Ped)))
                    {
                        if (!Alive(g.Ped)) respawn[post.Offer.Id] = now + options.RespawnSeconds * 1000;
                        Delete(g); guards.Remove(post.Offer.Id); g = null;
                    }
                    if (g == null && e != null && unchecked(now - nextSpawn) >= 0) Spawn(post, e);
                    else if (g != null) SetPaid(g, PdmSecurityPolicy.Paid(Wallet.Pdm, post.Offer.Id));
                }
                if (unchecked(now - nextSpawn) >= 0) nextSpawn = now + 5000;
                var protectedPeds = crew().Concat(guards.Values.Select(g => g.Ped)).Concat(new[] { Game.Player.Character }).Where(Alive).ToArray();
                foreach (var g in guards.Values) UpdateGuard(g, protectedPeds);
            }
            catch (Exception ex) { Clear(); nextSpawn = Game.GameTime + 5000; Log.Error("pdm security duty", ex); }
        }
        public void Save() { }
        public void Reload() { Clear(); respawn.Clear(); profile = Profile.Id; nextScan = nextSpawn = Game.GameTime; menu.Visible = false; }
        public void OnKey(KeyEventArgs e) { }
        public void Dispose() { Clear(); menu.Visible = false; }
    }
}
