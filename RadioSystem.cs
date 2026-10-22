using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using GTA;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public class RadioStation { public string Name, Url; }
    public class RadioSystem : ISystem
    {
        public string Name { get { return "radio"; } }
        readonly ObjectPool pool;
        readonly NativeMenu menu = new NativeMenu("Custom Radio", "MP3 streams / Windows Media Player");
        readonly AchievementSystem ach;
        dynamic player;
        bool waiting;
        int started;
        public RadioSystem(ObjectPool pool, AchievementSystem ach) { this.pool = pool; this.ach = ach; pool.Add(menu); Reload(); }

        public static List<RadioStation> ReadStations(string path)
        {
            var stations = new List<RadioStation>();
            if (!File.Exists(path)) return stations;
            var doc = new XmlDocument { XmlResolver = null };
            using (var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null })) doc.Load(reader);
            foreach (XmlNode node in doc.SelectNodes("/RadioSettings/Stations/Station"))
            {
                Uri uri;
                if (Uri.TryCreate(node.InnerText.Trim(), UriKind.Absolute, out uri) && (uri.Scheme == "http" || uri.Scheme == "https")) stations.Add(new RadioStation { Name = node.Attributes?["name"]?.Value ?? uri.Host, Url = uri.AbsoluteUri });
            }
            return stations;
        }
        void Play(RadioStation station)
        {
            try
            {
                Stop();
                var type = Type.GetTypeFromProgID("WMPlayer.OCX");
                if (type == null) throw new NotSupportedException("Windows Media Player COM is unavailable.");
                player = Activator.CreateInstance(type); player.settings.volume = 50;
                player.URL = station.Url; player.controls.play(); waiting = true; started = Game.GameTime;
                Notify.Show("Connecting: " + station.Name + ". Use a direct MP3 stream URL.");
            }
            catch (Exception ex) { Log.Error("radio", ex); Stop(); Notify.Show("Radio unavailable. Check Windows Media Player and RPCore.log."); }
        }
        void Stop()
        {
            waiting = false;
            if (player == null) return;
            object instance = player;
            try { player.controls.stop(); player.close(); }
            finally { player = null; if (Marshal.IsComObject(instance)) Marshal.FinalReleaseComObject(instance); }
        }
        public void Reload()
        {
            Stop(); menu.Visible = false; menu.Clear();
            var stop = new NativeItem("Stop radio"); stop.Activated += (s, e) => Stop(); menu.Add(stop);
            try
            {
                foreach (var station in ReadStations(Path.Combine(Cfg.Dir, "CustomRadioStream.xml")))
                {
                    var entry = new NativeItem(station.Name, station.Url); entry.Activated += (s, e) => Play(station); menu.Add(entry);
                }
            }
            catch (Exception ex) { Log.Error("radio XML", ex); }
        }
        public void Tick()
        {
            if (player == null || !waiting) return;
            if ((int)player.playState == 3) { waiting = false; ach.Bump("radio_started"); }
            else if ((int)player.error.errorCount > 0 || unchecked(Game.GameTime - started) > 30000)
            { Stop(); Notify.Show("Stream failed. The supplied PSRP URL may be a webpage; configure a direct MP3 stream."); }
        }
        public void OnKey(KeyEventArgs e) { if (e.KeyCode != Cfg.RadioKey) return; if (menu.Visible) menu.Visible = false; else if (!pool.AreAnyVisible) menu.Visible = true; }
        public void Save() { }
        public void Dispose() { menu.Visible = false; Stop(); }
    }
}
