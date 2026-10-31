using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;

namespace RPCore
{
    public class VisualSystem : ISystem
    {
        public string Name { get { return "visuals"; } }
        readonly SeatbeltSystem belt;
        readonly Func<bool> cruise;
        readonly Func<Vehicle, FuelTank> fuel;
        int diagnosticEnd, nextDiagnostic;
        bool warned;
        public VisualSystem(SeatbeltSystem belt, Func<bool> cruise, Func<Vehicle, FuelTank> fuel)
        {
            this.belt = belt; this.cruise = cruise; this.fuel = fuel; Visuals.Running = true; LoadRoads();
        }
        VisualDiagnosticReport report;
        System.Collections.Generic.Queue<TextureProbe> pending;
        public void Diagnose()
        {
            report = new VisualDiagnosticReport(); pending = null;
            diagnosticEnd = Game.GameTime+6000; nextDiagnostic = Game.GameTime;
            Notify.Show("Checking installed YTD dictionaries and texture dimensions. Result in about 10 seconds.");
        }
        void Diagnostics()
        {
            if(report==null) return;
            if(pending==null)
            {
                if(unchecked(Game.GameTime-nextDiagnostic)>=0)
                {
                    nextDiagnostic=Game.GameTime+1000;
                    foreach(var dictionary in Visuals.Catalog.Dictionaries.Keys) Visuals.RequestForDiagnostic(dictionary);
                }
                if(unchecked(Game.GameTime-diagnosticEnd)<0) return;
                pending = new System.Collections.Generic.Queue<TextureProbe>();
                foreach(var dictionary in Visuals.Catalog.Dictionaries)
                {
                    bool loaded=false;
                    try { loaded=Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED,dictionary.Key); }
                    catch(Exception ex) { Log.Error("visual dictionary probe: "+dictionary.Key,ex); }
                    report.Dictionaries[dictionary.Key]=loaded;
                    Log.Write("VISUAL dict "+dictionary.Key+": "+(loaded ? "loaded" : "not loaded after explicit streaming request"));
                    foreach(var texture in dictionary.Value) pending.Enqueue(new TextureProbe { Dictionary=dictionary.Key,Texture=texture.Name,ExpectedWidth=texture.Width,ExpectedHeight=texture.Height,DictionaryLoaded=loaded });
                }
            }
            // Spread native queries across frames; do not block the game for a full catalog sweep.
            for(int i=0;i<16 && pending.Count>0;i++)
            {
                var probe=pending.Dequeue();
                if(probe.DictionaryLoaded)
                {
                    try { var size=Function.Call<GTA.Math.Vector3>(Hash.GET_TEXTURE_RESOLUTION,probe.Dictionary,probe.Texture); probe.Width=size.X; probe.Height=size.Y; }
                    catch(Exception ex) { probe.Error=ex.Message; }
                }
                report.Textures.Add(probe);
                if(probe.Status!="dimensions_match") Log.Write("VISUAL texture "+probe.Dictionary+"/"+probe.Texture+": "+probe.Status+"; expected "+probe.ExpectedWidth+"x"+probe.ExpectedHeight+", returned "+probe.Width+"x"+probe.Height);
            }
            if(pending.Count>0) return;
            foreach(string name in Visuals.Catalog.Models)
            {
                bool valid=false;
                try { var model=new Model(name);valid=model.IsValid && model.IsInCdImage; }
                catch(Exception ex) { Log.Error("visual model probe: "+name,ex); }
                report.Models[name]=valid;
            }
            report.Enabled=Visuals.Enabled;report.Active=Visuals.Active;report.NeedsHud=Visuals.NeedsHud;report.WalletHud=Visuals.WalletHud;
            report.VehicleHud=Visuals.VehicleHud;report.MenuBanners=Visuals.MenuBanners;report.PhoneEnabled=Visuals.PhoneEnabled;
            report.AspectRatio=GTA.UI.Screen.AspectRatio;
            int matched=report.Textures.Count(t=>t.Status=="dimensions_match"),loadedCount=report.Dictionaries.Count(d=>d.Value);
            string path=System.IO.Path.Combine(Cfg.Dir,"VisualDiagnostics.json");
            try { new JsonFiles().Write(path,report);Notify.Show("YTD: "+loadedCount+"/"+report.Dictionaries.Count+" loaded; "+matched+"/"+report.Textures.Count+" dimensions match. See RPCore/VisualDiagnostics.json."); }
            catch(Exception ex) { Log.Error("visual diagnostic report write",ex);Notify.Show("Could not write visual report. See RPCore.log."); }
            Log.Write("VISUAL summary "+loadedCount+" dictionaries; "+matched+"/"+report.Textures.Count+" matching resolutions. Names/dimensions do not prove rendered appearance. Active="+report.Active);
            report=null;pending=null;
        }
        public void Tick()
        {
            Diagnostics(); Visuals.TickMenus();
            if (Visuals.Active && !warned && Visuals.RequestTimedOut("PSRP") && !Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, "PSRP"))
            {
                warned = true; Log.Write("VISUAL PSRP still not loaded 10s after an explicit request. Check mounted archive/overrides and run the native texture diagnostic; installation is not inferred from this check.");
                Notify.Show("PSRP did not stream after a request. F10 > Visual assets: diagnose.");
            }
            if (!Visuals.Active || Game.IsPaused || GameplayContext.SceneBlocking || GameplayContext.HudHidden) return;
            var p = Game.Player.Character; if (p.IsDead) return;
            if (Visuals.WalletHud && Visuals.Has("PSRP", "MoneyIcon"))
            {
                float s = Visuals.Scale, x = ReferenceLayout.StatusX(Visuals.Width,s,Visuals.InsetX), y = Visuals.InsetY+Visuals.NeedsOffsetY*s;
                // MoneyUI/BankUI contain their own round icon and black pill in a padded 384px texture.
                Visuals.Sprite("PSRP", "MoneyUI", x +30*s, y-2*s,130*s,130*s,Color.White);
                Visuals.Sprite("PSRP", "BankUI", x +120*s, y-2*s,130*s,130*s,Color.White);
                Visuals.Label(HudMath.CompactCash(Wallet.Cash),x+88*s,y+52*s,.3f*s,Color.White,GTA.UI.Font.ChaletComprimeCologne);
                Visuals.Label(HudMath.CompactMoney(Wallet.Bank),x+178*s,y+52*s,.3f*s,Color.White,GTA.UI.Font.ChaletComprimeCologne);
            }
            if (!Visuals.VehicleReady || !p.IsInVehicle()) return;
            var v = p.CurrentVehicle;
            if (v == null || v.Driver != p || !(v.Model.IsCar || v.Model.IsBike || v.Model.IsQuadBike) || v.Model.IsBicycle) return;
            DrawVehicle(v);
        }
        void DrawVehicle(Vehicle v)
        {
            float s = Visuals.Scale, center = Visuals.Width / 2 - 6*s, y = 1080-Visuals.InsetY-175*s;
            float speed = ReferenceLayout.Speed(v.Speed,InterfaceOptions.MetricSpeed);
            string gear = v.GetSpeedVector(true).Y < -.5f ? "R" : v.CurrentGear > 0 ? v.CurrentGear.ToString() : "N";
            Visuals.Label(speed.ToString("0"),center,y,.9f*s,Color.White,GTA.UI.Font.Pricedown,GTA.UI.Alignment.Center);
            Visuals.Label(gear,center+22*s,y,.2f*s,Color.White,GTA.UI.Font.Pricedown);
            Visuals.Label(InterfaceOptions.MetricSpeed ? "kph" : "mph",center+22*s,y+30*s,.2f*s,Color.White,GTA.UI.Font.Pricedown);
            float rpm = Math.Max(0,Math.Min(1,v.CurrentRPM));
            Visuals.Rect(center-130*s,y+53*s,260*s,12*s,Color.FromArgb(200,0,0,0));
            Visuals.Rect(center-130*s,y+53*s,260*s*rpm,10*s,rpm>.88f ? Color.Red : rpm>.61f ? Color.Orange : Color.LimeGreen);
            Color off = Color.FromArgb(175,200,200,200);
            Visuals.Sprite("PSRP","FlatTireIcon",center-129*s,y+82*s,45*s,45*s,new[] {0,1,2,3,4,5,45,47}.Any(i=>Function.Call<bool>(Hash.IS_VEHICLE_TYRE_BURST,v,i,false)) ? Color.Red : off);
            Visuals.Sprite("PSRP","CarSeatbeltIcon",center-82*s,y+77*s,50*s,50*s,belt.IsBelted ? Color.Lime : off);
            Visuals.Sprite("PSRP","CarCruiseIcon",center+31*s,y+77*s,50*s,50*s,cruise() ? Color.Lime : off);
            Visuals.Sprite("PSRP","CarHeadLightIcon",center+81*s,y+81*s,50*s,50*s,v.AreLightsOn ? Color.White : off);
            if (v.EngineHealth<300) Visuals.Sprite("PSRP","CarOilIcon",center+64*s,y+60*s,30*s,40*s,Color.Red);
            FuelTank tank = fuel(v);
            if (tank!=null && tank.Capacity>0)
            {
                double f = Math.Max(0,Math.Min(1,tank.Liters/tank.Capacity));
                Visuals.Sprite("PSRP","CarFuelIcon",center-18*s,y+68*s,40*s,40*s,Color.White);
                Visuals.Rect(center-29*s,y+110*s,60*s,10*s,Color.FromArgb(200,0,0,0));
                Visuals.Rect(center-29*s,y+110*s,60*s*(float)f,10*s,f<.2 ? Color.Red : Color.Orange);
                Visuals.Label((InterfaceOptions.MetricSpeed ? "km: " : "mi: ")+(tank.DistanceKm/(InterfaceOptions.MetricSpeed ? 1 : 1.609344)).ToString("0.0"),center,y+123*s,.22f*s,Color.White,GTA.UI.Font.Pricedown,GTA.UI.Alignment.Center);
            }
            if ((Game.GameTime/500)%2==0)
            {
                if(v.IsLeftIndicatorLightOn) Visuals.Sprite("PSRP","LBlinker",center-170*s,y+80*s,30*s,30*s,Color.Lime);
                if(v.IsRightIndicatorLightOn) Visuals.Sprite("PSRP","Rblinker",center+144*s,y+80*s,30*s,30*s,Color.Lime);
            }
            if (InterfaceOptions.LocationHud) DrawLocation(v.Position);
        }
        int nextLocation;
        string street = "", zone = "";
        System.Collections.Generic.Dictionary<string,int> speedLimits = new System.Collections.Generic.Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        void LoadRoads()
        {
            try
            {
                var loaded = new JsonFiles().Read(System.IO.Path.Combine(Cfg.Dir,"Visuals","SpeedLimits.json"),()=>new System.Collections.Generic.Dictionary<string,int>());
                speedLimits = new System.Collections.Generic.Dictionary<string,int>(loaded,StringComparer.OrdinalIgnoreCase);
            }
            catch(Exception ex) { Log.Error("visual road rules",ex); }
        }
        void DrawLocation(GTA.Math.Vector3 position)
        {
            if(unchecked(Game.GameTime-nextLocation)>=0)
            {
                nextLocation=Game.GameTime+500; street=World.GetStreetName(position); zone=World.GetZoneLocalizedName(position);
            }
            float s=Visuals.Scale,x=Visuals.InsetX+345*s,y=1080-Visuals.InsetY-84*s;
            Visuals.Label(zone,x,y,.3f*s,Color.White,GTA.UI.Font.ChaletComprimeCologne);
            Visuals.Label(street,x,y+31*s,.3f*s,Color.White,GTA.UI.Font.ChaletComprimeCologne);
            Visuals.Label(GTA.Chrono.GameClock.Hour.ToString("00")+":"+GTA.Chrono.GameClock.Minute.ToString("00"),x+187*s,y,.3f*s,Color.White,GTA.UI.Font.ChaletComprimeCologne,GTA.UI.Alignment.Right);
            Visuals.Label(GTA.Chrono.GameClock.Day+"/"+GTA.Chrono.GameClock.Month+"/"+GTA.Chrono.GameClock.Year,x+187*s,y+31*s,.3f*s,Color.White,GTA.UI.Font.ChaletComprimeCologne,GTA.UI.Alignment.Right);
            int limit;
            if(street!=null && speedLimits.TryGetValue(street,out limit) && new[] {40,50,60,70}.Contains(limit))
                Visuals.Sprite("PSRP",limit.ToString(),Visuals.InsetX+250*s,1080-Visuals.InsetY-210*s,45*s,55*s,Color.White);
        }
        public void OnKey(KeyEventArgs e) { }
        public void Reload() { LoadRoads(); }
        public void Save() { }
        public void Dispose() { Visuals.Dispose(); }
    }
}
