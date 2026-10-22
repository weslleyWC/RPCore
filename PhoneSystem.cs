using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;

namespace RPCore
{
    public class PhoneSystem : ISystem
    {
        public string Name { get { return "phone"; } }
        readonly ObjectPool pool;
        readonly Action<Keys> dispatch;
        readonly PropPresentation props;
        readonly InventorySystem inventory;
        readonly NeedsSystem needs;
        readonly Action jobs;
        readonly string[] shortcuts = { "Wallet / Bank", "Inventory", "Garages", "Stocks", "Radio", "Gangs", "Pizza shift", "Settings", "Trabalhos / Post OP" };
        PhoneData data;
        int selection, row, stopwatchStart = -1;
        string page = "Home", query = "", result = "0";
        public bool Visible { get; private set; }
        public PhoneSystem(ObjectPool pool, Action<Keys> dispatch, PropPresentation props, InventorySystem inventory, NeedsSystem needs, Action jobs = null)
        { this.jobs = jobs; this.pool = pool; this.dispatch = dispatch; this.props = props; this.inventory = inventory; this.needs = needs; Reload(); inventory.Used += ItemUsed; }
        void ItemUsed(ItemDef item)
        {
            // Presentation/history must never abort consumption or corrupt the inventory transaction.
            try { data.History.Insert(0, new PhoneEntry { Title = "Inventory", Body = "Used " + item.Name }); if (data.History.Count > 64) data.History.RemoveRange(64, data.History.Count - 64); Save(); }
            catch (Exception ex) { Log.Error("phone local history", ex); }
        }
        void Close()
        {
            Visible = false; props?.ClosePhone(); page = "Home";
            Game.DisableControlThisFrame(GTA.Control.FrontendPause); Game.DisableControlThisFrame(GTA.Control.FrontendPauseAlternate);
        }
        void OpenPage(string name) { page = name; row = 0; }
        int[] Matches() { return Enumerable.Range(0, shortcuts.Length).Where(i => shortcuts[i].IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToArray(); }
        void Shortcut(int i)
        {
            if (i == 8) { Close(); jobs?.Invoke(); return; }
            if ((i == 2 && !Cfg.EnableGarages) || (i == 4 && !Cfg.EnableRadio) || (i == 5 && !Cfg.EnableGangs)) { Notify.Show("System disabled in configuration."); return; }
            Keys key = i == 0 ? Cfg.CoreKey : i == 1 ? Cfg.InventoryKey : i == 2 ? Cfg.GarageKey : i == 3 ? Cfg.StocksKey : i == 4 ? Cfg.RadioKey : i == 5 ? Cfg.GangsKey : i == 6 ? Cfg.JobKey : InterfaceOptions.SettingsKey;
            Close(); dispatch(key);
        }
        void Activate()
        {
            if (page == "Home")
            {
                OpenPage(ReferenceLayout.PhoneLabels[selection]);
                if (page == "Notifications" || page == "Messages") { foreach (var h in data.History) h.Read = true; Save(); }
                if (page == "Radio") Shortcut(4);
                return;
            }
            if (page == "Notes")
            {
                string text = Game.GetUserInput(WindowTitle.EnterMessage60, row == 0 ? "" : data.Notes[Math.Min(row-1,data.Notes.Count-1)], 120);
                if (!string.IsNullOrWhiteSpace(text)) { if (row == 0) { if (data.Notes.Count < 30) data.Notes.Insert(0,text); } else data.Notes[row-1] = text; Save(); }
            }
            else if (page == "Calculator")
            {
                string expression = Game.GetUserInput(WindowTitle.EnterMessage60, "", 60);
                string answer; PhoneMath.Calculate(expression,out answer); result = expression + "~n~=" + answer;
            }
            else if (page == "Clock") { stopwatchStart = stopwatchStart < 0 ? Game.GameTime : -1; }
            else if (page == "Settings")
            {
                if (row == 0) { data.Wallpaper = data.Wallpaper % 10 + 1; Save(); }
                else Shortcut(row-1);
            }
            else if (page == "Search")
            {
                var matches = Matches();
                if (row == 0) { query = Game.GetUserInput(WindowTitle.EnterMessage60, query, 32); row = 0; }
                else if (row-1 < matches.Length) Shortcut(matches[row-1]);
            }
            else if (page == "Contacts") { Shortcut(new[] { 0,2,6,5,8 }[row]); }
        }
        int RowCount()
        {
            if (page == "Notes") return data.Notes.Count + 1;
            if (page == "Settings") return shortcuts.Length + 1;
            if (page == "Search") return Matches().Length + 1;
            if (page == "Contacts") return 5;
            return 1;
        }
        void Navigate(int dx, int dy)
        { if (page == "Home") selection = HudMath.MoveGrid(selection,dx,dy,3,ReferenceLayout.PhoneIcons.Length); else row = Math.Max(0,Math.Min(RowCount()-1,row+dy)); }
        void Back() { if (page == "Home") Close(); else OpenPage("Home"); }
        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode == Visuals.PhoneKey)
            {
                if (Visible) { Close(); return; }
                var p = Game.Player.Character;
                if (!Visuals.Enabled || !Visuals.PhoneEnabled || pool.AreAnyVisible || inventory.GridVisible || p.IsDead || p.IsInVehicle() || Game.IsMissionActive) return;
                Visible = true; page = "Home"; props?.ShowPhone(); return;
            }
            if (!Visible) return;
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Back) Back();
            else if (e.KeyCode == Keys.Left) Navigate(-1,0);
            else if (e.KeyCode == Keys.Right) Navigate(1,0);
            else if (e.KeyCode == Keys.Up) Navigate(0,-1);
            else if (e.KeyCode == Keys.Down) Navigate(0,1);
            else if (e.KeyCode == Keys.Enter) Activate();
            else if (e.KeyCode == Keys.Delete && page == "Notes" && row > 0 && row <= data.Notes.Count) { data.Notes.RemoveAt(row-1); row = Math.Max(0,row-1); Save(); }
        }
        public void Tick()
        {
            if (!Visible) return;
            var p = Game.Player.Character;
            if (!Visuals.Enabled || !Visuals.PhoneEnabled || p.IsDead || p.IsInVehicle() || Game.IsMissionActive || pool.AreAnyVisible || inventory.GridVisible || Game.IsPaused || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE)) { Close(); return; }
            foreach (var control in new[] { GTA.Control.Attack, GTA.Control.Aim, GTA.Control.Phone, GTA.Control.SelectWeapon, GTA.Control.FrontendPause, GTA.Control.FrontendPauseAlternate }) Game.DisableControlThisFrame(control);
            if (!Function.Call<bool>(Hash.IS_USING_KEYBOARD_AND_MOUSE,0))
            {
                if (Game.IsControlJustPressed(GTA.Control.FrontendLeft)) Navigate(-1,0);
                if (Game.IsControlJustPressed(GTA.Control.FrontendRight)) Navigate(1,0);
                if (Game.IsControlJustPressed(GTA.Control.FrontendUp)) Navigate(0,-1);
                if (Game.IsControlJustPressed(GTA.Control.FrontendDown)) Navigate(0,1);
                if (Game.IsControlJustPressed(GTA.Control.FrontendAccept)) { Activate(); return; }
                if (Game.IsControlJustPressed(GTA.Control.FrontendCancel)) { Back(); return; }
            }
            Draw();
        }
        string Time()
        {
            int hour = GTA.Chrono.GameClock.Hour;
            return ((hour+11)%12+1) + ":" + GTA.Chrono.GameClock.Minute.ToString("00") + (hour >= 12 ? " PM" : " AM");
        }
        void Draw()
        {
            float s = Visuals.Scale, x = Visuals.Width-Visuals.InsetX-258*s, y = 1080-Visuals.InsetY-459*s;
            float fullX = x-1662*s, fullY = y-621*s;
            Action<string,float,float,float,Color,GTA.UI.Alignment,float> text = (t,px,py,scale,color,align,wrap) => Visuals.Label(t,x+px*s,y+py*s,scale*s,color,GTA.UI.Font.ChaletLondon,align,wrap*s);
            if (!Visuals.Sprite("PSRP_PHONE","PhoneTexture",fullX,fullY,1920*s,1080*s,Color.White)) Visuals.Rect(x,y,242*s,441*s,Color.FromArgb(245,18,18,20));
            Visuals.Sprite("PSRP_PHONE","PhoneWallpaper"+data.Wallpaper,fullX,fullY,1920*s,1080*s,Color.White);
            if (page == "Home")
            {
                text(Time(),123,54,.65f,Color.White,GTA.UI.Alignment.Center,0);
                text(GTA.Chrono.GameClock.DayOfWeek.ToString().ToUpperInvariant(),123,99,.35f,Color.White,GTA.UI.Alignment.Center,0);
                for (int i=0;i<ReferenceLayout.PhoneIcons.Length;i++)
                {
                    float ix = 30.5f+i%3*65, iy = 154+i/3*65;
                    if (i == selection) Visuals.Sprite("PSRP_PHONE","HoverIcon",x+(ix-2)*s,y+(iy-2)*s,52*s,52*s,Color.FromArgb(100,255,255,255));
                    if (!Visuals.Sprite("PSRP_PHONE",ReferenceLayout.PhoneIcons[i],x+ix*s,y+iy*s,48*s,48*s,Color.White))
                        text(ReferenceLayout.PhoneLabels[i],ix+24,iy+12,.20f,Color.White,GTA.UI.Alignment.Center,58);
                    int unread = data.History.Count(h => !h.Read);
                    if (i == 2 && unread > 0)
                    {
                        Visuals.Sprite("PSRP_PHONE","UnreadMarker",x+(ix+37)*s,y+(iy-9)*s,20*s,20*s,Color.White);
                        text(Math.Min(99,unread).ToString(),ix+47,iy-10,.22f,Color.White,GTA.UI.Alignment.Center,0);
                    }
                }
                return;
            }
            // Use the existing screen art, without a second opaque rectangle over the wallpaper on the home screen.
            Visuals.Rect(x+11*s,y+9*s,220*s,424*s,Color.FromArgb(245,18,21,25));
            text(page,122,26,.4f,Color.White,GTA.UI.Alignment.Center,200);
            Action<string,int> line = (label,index) => {
                int first = Math.Max(0,row-5), display = index-first; if (display<0 || display>=7) return;
                if (index==row) Visuals.Rect(x+17*s,y+(78+display*39)*s,208*s,35*s,Color.FromArgb(155,60,90,120));
                text(label.Length>27 ? label.Substring(0,24)+"..." : label,23,82+display*39,.26f,Color.White,GTA.UI.Alignment.Left,196);
            };
            if (page == "Notes") { line("+ New note",0); for(int i=0;i<data.Notes.Count;i++) line(data.Notes[i],i+1); text("Enter: edit | Delete: remove",23,374,.2f,Color.LightGray,GTA.UI.Alignment.Left,200); }
            else if (page == "Settings") { line("Wallpaper: "+data.Wallpaper,0); for(int i=0;i<shortcuts.Length;i++) line(shortcuts[i],i+1); }
            else if (page == "Search") { line("Search: "+query,0); var found=Matches(); for(int i=0;i<found.Length;i++) line(shortcuts[found[i]],i+1); }
            else if (page == "Contacts")
            {
                line("Fleeca / Bank",0);line("Mechanic / Garages",1);line("Pizza This / Work",2);line("Gang HQ",3);line("Trabalhos / Post OP",4);
                text("RPCore service shortcuts.~n~Voice calls are not integrated.",23,283,.23f,Color.LightGray,GTA.UI.Alignment.Left,196);
            }
            else if (page == "Notifications" || page == "Messages")
            {
                string history = string.Join("~n~~n~",data.History.Take(5).Select(h => h.Title+": "+h.Body));
                text(history.Length==0 ? "No local activity yet." : history,23,82,.26f,Color.White,GTA.UI.Alignment.Left,196);
                text("Local RPCore history; no SMS service.",23,374,.2f,Color.LightGray,GTA.UI.Alignment.Left,196);
            }
            else if (page == "Clock")
            {
                text(Time(),122,114,.55f,Color.White,GTA.UI.Alignment.Center,0);
                text(stopwatchStart<0 ? "Enter: start stopwatch" : TimeSpan.FromMilliseconds(Math.Max(0,Game.GameTime-stopwatchStart)).ToString(@"mm\:ss")+"~n~Enter: reset",122,207,.35f,Color.White,GTA.UI.Alignment.Center,196);
            }
            else if (page == "Calculator") { text(result,23,105,.35f,Color.White,GTA.UI.Alignment.Left,196);text("Enter: type e.g. 125*4",23,302,.27f,Color.LightGray,GTA.UI.Alignment.Left,196); }
            else if (page == "Fitness") { text("Health: "+pHealth()+"~n~Armor: "+Game.Player.Character.Armor+"~n~Hunger: "+HudMath.Percent(needs.Hunger)+"~n~Hydration: "+HudMath.Percent(needs.Thirst)+"~n~Sleep: "+HudMath.Percent(needs.Sleep),23,105,.3f,Color.White,GTA.UI.Alignment.Left,196); }
            else if (page == "Uber") text("Uber passenger jobs are not implemented in RPCore yet. Use Settings for garages and existing jobs.",23,105,.29f,Color.White,GTA.UI.Alignment.Left,196);
            text("Backspace: home | F1: close",23,401,.21f,Color.LightGray,GTA.UI.Alignment.Left,200);
        }
        int pHealth() { return Game.Player.Character.Health; }
        public void Reload()
        {
            Close(); data = Store.Load("Phone.json",() => new PhoneData());
            if (data.Notes == null) data.Notes = new System.Collections.Generic.List<string>();
            if (data.History == null) data.History = new System.Collections.Generic.List<PhoneEntry>();
            data.Notes = data.Notes.Where(n => n != null).Take(30).ToList(); data.History = data.History.Where(h => h != null).Take(64).ToList();
            data.Wallpaper = Math.Max(1,Math.Min(10,data.Wallpaper)); row = 0; stopwatchStart = -1;
        }
        public void Save() { Store.Write("Phone.json",data); }
        public void Dispose() { inventory.Used -= ItemUsed; Close(); }
    }
}
