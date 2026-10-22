using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public sealed class DeliveryJobsMenu : ISystem
    {
        public string Name => "jobsmenu";
        readonly ObjectPool pool;
        readonly IWorkContract[] contracts;
        readonly NativeMenu menu = new NativeMenu("Trabalhos", "TRABALHOS / CONTRATOS");
        public DeliveryJobsMenu(ObjectPool pool, params IWorkContract[] contracts)
        {
            this.pool = pool; this.contracts = contracts; pool.Add(menu); Visuals.Register(menu,"core");
            foreach (var job in contracts)
            {
                var selected = job;
                Add(job.Label, "Contratar, GPS, progresso e encerramento do trabalho.", selected.OpenMenu);
            }
            Add("Gerenciar turno ativo", "Abra o trabalho ativo para cancelar ou segure End fora do menu.", () => {
                foreach (var job in contracts) if (job.Active) { job.OpenMenu(); return; }
                Notify.Show("Nenhum turno ativo.");
            });
        }
        void Add(string title, string description, System.Action action)
        {
            var row = new NativeItem(title,description); row.Activated += (s,e) => { menu.Visible=false; action(); }; menu.Add(row);
        }
        public void Open() { pool.HideAll(); menu.Visible = true; }
        public void Tick() { foreach (var job in contracts) job.PollCancel(); JobActors.Tick(); JobVehicles.Tick(); }
        public void OnKey(KeyEventArgs e) { }
        public void Reload() { menu.Visible = false; }
        public void Save() { }
        public void Dispose() { menu.Visible = false; JobActors.Dispose(); JobVehicles.Dispose(); }
    }
}
