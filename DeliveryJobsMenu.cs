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
        readonly PizzaJob pizza;
        readonly PostOpJob post;
        readonly NativeMenu menu = new NativeMenu("Trabalhos", "ENTREGAS / CONTRATOS");
        public DeliveryJobsMenu(ObjectPool pool, PizzaJob pizza, PostOpJob post)
        {
            this.pool = pool; this.pizza = pizza; this.post = post; pool.Add(menu); Visuals.Register(menu,"core");
            Add("Pizza", "Contrato na pizzaria ou turno rapido com " + Cfg.JobKey + ".", pizza.OpenMenu);
            Add("Post OP", "Van, caixas, entregas e retorno. Pagamento no banco.", post.OpenMenu);
            Add("Cancelar turno ativo", "Mantem entregas ja pagas. Abra o contrato ativo para cancelar, ou segure End fora do menu.", () => { if (pizza.Active) pizza.OpenMenu(); else if (post.Active) post.OpenMenu(); else Notify.Show("Nenhum turno ativo."); });
        }
        void Add(string title, string description, System.Action action)
        {
            var row = new NativeItem(title,description); row.Activated += (s,e) => { menu.Visible=false; action(); }; menu.Add(row);
        }
        public void Open() { pool.HideAll(); menu.Visible = true; }
        public void Tick() { pizza.PollCancel(); post.PollCancel(); JobVehicles.Tick(); }
        public void OnKey(KeyEventArgs e) { }
        public void Reload() { menu.Visible = false; }
        public void Save() { }
        public void Dispose() { menu.Visible = false; JobVehicles.Dispose(); }
    }
}
