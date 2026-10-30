using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using LemonUI;
using LemonUI.Menus;
using Newtonsoft.Json;
namespace RPCore
{
    public sealed class StockMarketSystem : ISystem
    {
        public string Name=>"stockmarket";
        readonly ObjectPool pool;readonly NativeMenu menu=new NativeMenu("RPCore Exchange","BOLSA LOCAL");readonly PersonalServiceMenu ui;
        readonly Random random=new Random();readonly JsonFiles files=new JsonFiles();ExchangeClock clock=new ExchangeClock();
        MarketData market;Portfolio P=>Wallet.Exchange;int lastClock,sort;bool descending;string query="";Action refresh;
        string PathName=>Path.Combine(Cfg.Dir,"StockMarket",Profile.SafeId(Profile.Id)+".portfolio.json");
        bool Valid=>ExchangeRules.Valid(P) && ExchangeRules.Valid(market);
        public StockMarketSystem(ObjectPool pool){this.pool=pool;pool.Add(menu);Visuals.Register(menu,"stocks");ui=new PersonalServiceMenu(menu,e=>{Log.Error("exchange action",e);Notify.Show("Operacao nao concluida; confira saldos e RPCore.log.");ui.Close();});Reload();}
        static Portfolio Copy(Portfolio p)=>JsonConvert.DeserializeObject<Portfolio>(JsonConvert.SerializeObject(p));
        void Page(string key,Action parent,Action render){ui.Begin(key,parent,!key.Contains("confirm"));refresh=render;menu.Name="Carteira $"+Wallet.Cash+" / bolsa $"+(P?.Cash.ToString("0.00")??"--");}
        void Row(string title,string desc,Action action,bool enabled=true)=>ui.Add(title,desc,action,enabled);
        void Root()
        {
            Page("root",null,Root);
            if(!Valid){Row("Dados protegidos / somente leitura","Market ou carteira invalida. Nenhum saldo/campo foi corrigido automaticamente.",()=>{},false);Row("Fechar","Voltar ao jogo.",()=>ui.Close());menu.Visible=true;return;}
            var value=ExchangeRules.Value(P,market);
            Row("Patrimonio em acoes: "+(value?.ToString("$0.00")??"sem avaliacao completa"),"Caixa da bolsa $"+P.Cash.ToString("0.00")+". Simulacao local independente de BAWSAQ/Yahoo; cotacoes de jogo ativo.",()=>{},false);
            Row("Buscar ativo / "+(string.IsNullOrEmpty(query)?"sem filtro":query),"Busca por codigo, preserva todas as posicoes.",Search);
            Row("Todas as cotacoes","Abra um ativo para quantidade, compra/venda e favoritos.",()=>List("all"));
            Row("Minha carteira","Inclui ativos sem cotacao; nenhum registro e removido.",()=>List("held"));
            Row("Favoritos","Lista de acompanhamento do perfil.",()=>List("watch"));
            Row("Movimentar saldo","Deposito/saque entre carteira e caixa da bolsa. Conta bancaria permanece separada.",Funds);
            Row("Fechar","Voltar ao jogo.",()=>ui.Close());menu.Visible=true;
        }
        void List(string kind)
        {
            if(!Valid){Root();return;}Page("list:"+kind,Root,()=>List(kind));
            IEnumerable<string> symbols=kind=="held"?P.Holdings.Where(h=>h.Value>0).Select(h=>h.Key):kind=="watch"?P.Watchlist:market.Prices.Keys;
            var entries=ExchangeRules.List(P,market,symbols,query,sort,descending).ToArray();
            Row("Ordenar: "+(sort==0?"codigo":sort==1?"cotacao":"resultado")+(descending?" / decrescente":" / crescente"),"Ativos sem cotacao/custo ficam ao final.",()=>{sort=(sort+1)%3;List(kind);});
            Row("Inverter ordem","Muda somente a apresentacao.",()=>{descending=!descending;List(kind);});
            if(!string.IsNullOrEmpty(query))Row("Limpar busca: "+query,"Nao remove ativos.",()=>{query="";List(kind);});
            foreach(string entry in entries){string symbol=entry;market.Prices.TryGetValue(symbol,out decimal price);P.Holdings.TryGetValue(symbol,out int held);Row(symbol+" / "+(price>0?"$"+price.ToString("0.00"):"sem cotacao")+" / "+held+" acoes","Detalhes, ordens e favoritos.",()=>Symbol(symbol,kind));}
            if(entries.Length==0)Row("Nenhum ativo nesta lista","Dados anteriores preservados.",()=>{},false);
            Row("Voltar","Resumo da bolsa.",Root);menu.Visible=true;
        }
        void Search()
        {
            int actor=Game.Player.Character.Handle,model=Game.Player.Character.Model.Hash;string profile=Profile.Id;
            string text=Game.GetUserInput(WindowTitle.EnterMessage60,query,32);
            if(text==null || !menu.Visible || !LeisureContext.CanOpen || actor!=Game.Player.Character.Handle || model!=Game.Player.Character.Model.Hash || profile!=Profile.Id)return;
            query=text.Trim();List("all");
        }
        void Symbol(string symbol,string kind)
        {
            if(!Valid){Root();return;}Page("symbol:"+symbol,()=>List(kind),()=>Symbol(symbol,kind));
            market.Prices.TryGetValue(symbol,out decimal price);P.Holdings.TryGetValue(symbol,out int held);
            string avg=P.AvgCost.TryGetValue(symbol,out decimal average)?"$"+average.ToString("0.00"):"nao registrado";
            Row(symbol+" / "+(price>0?"$"+price.ToString("0.00"):"sem cotacao"),held+" acoes / custo medio "+avg+". Ordens exigem cotacao valida.",()=>{},false);
            if(price>0 && held>0 && P.AvgCost.ContainsKey(symbol)){
                string gain=ExchangeRules.Gain(P,market,symbol)?.ToString("$0.00")??"fora da faixa numerica";
                Row("Resultado nao realizado: "+gain,"Estimativa, sem pagamento automatico.",()=>{},false);
            }
            bool basisKnown=held==0 || P.AvgCost.ContainsKey(symbol);
            Row("Comprar / escolher quantidade",basisKnown?"Disponivel: "+ExchangeRules.Maximum(P,symbol,price,true):"Custo medio antigo nao registrado; compra adicional bloqueada para nao inventar custo.",()=>Order(symbol,true,false,kind),price>0 && basisKnown);
            Row("Comprar maximo","Prepara ordem completa e mostra custo antes de confirmar.",()=>Order(symbol,true,true,kind),price>0 && basisKnown);
            Row("Vender / escolher quantidade","Somente posicao propria; sem venda a descoberto.",()=>Order(symbol,false,false,kind),price>0 && held>0);
            Row("Vender tudo","Prepara venda da posicao disponivel.",()=>Order(symbol,false,true,kind),price>0 && held>0);
            Row(P.Watchlist.Contains(symbol)?"Remover favorito":"Adicionar favorito","Nao compra nem vende.",()=>{if(Wallet.ExchangeChange(p=>{if(p.Watchlist.Contains(symbol))p.Watchlist.RemoveAll(s=>s==symbol);else p.Watchlist.Add(symbol);return true;}))Export();Symbol(symbol,kind);});
            Row("Voltar","Lista de ativos.",()=>List(kind));menu.Visible=true;
        }
        void Order(string symbol,bool buy,bool all,string kind)
        {
            if(!Valid || !market.Prices.TryGetValue(symbol,out decimal price) || price<=0)return;
            var actor=Game.Player.Character;int model=actor.Model.Hash;string profile=Profile.Id;int amount=ExchangeRules.Maximum(P,symbol,price,buy);
            if(!all){string input=Game.GetUserInput(WindowTitle.EnterMessage60,"1",10);if(!menu.Visible || !LeisureContext.CanOpen || actor!=Game.Player.Character || model!=actor.Model.Hash || profile!=Profile.Id)return;if(!PhoneViewRules.Amount(input,out amount)){if(!string.IsNullOrWhiteSpace(input))Notify.Show("Quantidade inteira positiva.");return;}}
            if(amount<=0 || amount>ExchangeRules.Maximum(P,symbol,price,buy)){Notify.Show("Sem saldo/posicao/capacidade para esta quantidade.");return;}
            decimal total=price*amount;var expected=Copy(P);
            Page("confirm",()=>Symbol(symbol,kind),null);menu.Name=(buy?"Comprar ":"Vender ")+amount+" "+symbol+" / $"+total.ToString("0.00");
            Row("Cancelar","Nenhuma ordem executada.",()=>Symbol(symbol,kind));
            Row("Confirmar "+(buy?"compra":"venda"),"Preco unitario $"+price.ToString("0.00")+". Mudanca de cotacao ou carteira exige nova ordem.",()=>{
                if(!Valid || !market.Prices.TryGetValue(symbol,out decimal now) || now!=price || !ExchangeRules.Matches(P,expected)){Notify.Show("Cotacao/carteira mudou. Revise uma nova ordem.");Symbol(symbol,kind);return;}
                bool done=Wallet.ExchangeChange(p=>buy?p.Buy(symbol,amount,price):p.Sell(symbol,amount,price));
                if(done){Export();Notify.Show("Ordem salva: "+amount+" "+symbol+" / $"+total.ToString("0.00"));}else Notify.Show("Ordem recusada; dados preservados.");Symbol(symbol,kind);
            });menu.Visible=true;
        }
        void Funds()
        {
            if(!Valid){Root();return;}Page("funds",Root,Funds);
            Row("Depositar / escolher valor","Carteira -> caixa da bolsa; valor inteiro.",()=>MoveFunds(true,false));
            Row("Sacar / escolher valor","Bolsa -> carteira; centavos restantes permanecem na bolsa.",()=>MoveFunds(false,false));
            Row("Depositar todo disponivel","Sem valores fixos de $100.",()=>MoveFunds(true,true));
            Row("Sacar todo disponivel","Respeita capacidade da carteira; preserva centavos.",()=>MoveFunds(false,true));
            Row("Voltar","Resumo.",Root);menu.Visible=true;
        }
        void MoveFunds(bool deposit,bool all)
        {
            int held=Wallet.Cash;int actor=Game.Player.Character.Handle,model=Game.Player.Character.Model.Hash;string profile=Profile.Id;
            int amount=deposit?(int)Math.Min(held,decimal.MaxValue-P.Cash):(int)Math.Min(int.MaxValue-held,decimal.Floor(Math.Min(int.MaxValue,P.Cash)));
            if(!all){string input=Game.GetUserInput(WindowTitle.EnterMessage60,"",32);if(!menu.Visible || !LeisureContext.CanOpen || profile!=Profile.Id || actor!=Game.Player.Character.Handle || model!=Game.Player.Character.Model.Hash)return;if(!PhoneBankRules.Amount(input,out amount)){if(!string.IsNullOrWhiteSpace(input))Notify.Show("Valor inteiro positivo.");return;}}
            if(amount<=0){Notify.Show("Sem saldo/capacidade disponivel.");return;}var expected=Copy(P);held=Wallet.Cash;
            Page("funds-confirm",Funds,null);menu.Name=(deposit?"Depositar ":"Sacar ")+PhoneBankRules.Money(amount);
            Row("Cancelar","Nenhum saldo alterado.",Funds);
            Row("Confirmar movimentacao","Conta bancaria nao e alterada; usa somente carteira e bolsa.",()=>{if(Wallet.Cash!=held || !ExchangeRules.Matches(P,expected)){Notify.Show("Saldo mudou; revise.");Funds();return;}if(Wallet.ExchangeFunds(amount,deposit)){Export();Notify.Show("Movimentacao salva.");}else Notify.Show("Saldo/capacidade insuficiente.");Funds();});menu.Visible=true;
        }
        void Export(){try{files.Write(PathName,P);}catch(Exception e){Log.Error("exchange export after canonical commit",e);Notify.Show("Saldo salvo no Wallet; copia da carteira pendente. Nao repita a operacao.");}}
        public void Reload()
        {
            ui.Reset();
            try{market=LeisureContext.LoadProgress("Market.json",()=>new MarketData());}catch(Exception e){market=null;Log.Error("exchange quotes retained",e);}
            if(Wallet.Exchange==null)try{Wallet.Exchange=files.Read(PathName,()=>new Portfolio());}catch(Exception e){Log.Error("legacy portfolio retained",e);}
            clock=new ExchangeClock();lastClock=Game.GameTime;refresh=null;query="";sort=0;descending=false;
        }
        public void Tick()
        {
            ui.Tick(LeisureContext.CanOpen);int now=Game.GameTime,delta=NeedsRules.Delta(now,lastClock,LeisureContext.CanOpen);lastClock=now;
            if(Valid && clock.Advance(delta,LeisureContext.CanOpen)){
                foreach(string symbol in market.Prices.Keys.ToArray())market.Prices[symbol]=ExchangeRules.NextPrice(market.Prices[symbol],random.NextDouble()*.04-.02);
                if(menu.Visible)refresh?.Invoke();
            }
        }
        public void OnKey(KeyEventArgs e){if(e.KeyCode!=Cfg.StocksKey)return;if(menu.Visible)ui.Close();else if(!pool.AreAnyVisible && LeisureContext.CanOpen){Root();menu.Visible=true;}}
        public void Save(){if(!Valid)return;Wallet.Save();files.Write(PathName,P);Store.Write("Market.json",market);}
        public void Dispose(){ui.Reset();}
    }
}
