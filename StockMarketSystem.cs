using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;

namespace RPCore
{
    public class MarketData : ExtensibleData
    {
        public Dictionary<string, decimal> Prices = new Dictionary<string, decimal> { { "PIZ", 25m }, { "LTD", 50m }, { "AUTO", 100m } };
    }

    public class StockMarketSystem : ISystem
    {
        public string Name { get { return "stockmarket"; } }
        readonly ObjectPool pool;
        readonly NativeMenu menu = new NativeMenu("RPCore Exchange", "Local simulation / not BAWSAQ");
        readonly Random random = new Random();
        readonly JsonFiles files = new JsonFiles();
        MarketData market;
        Portfolio portfolio;
        int nextPrice;
        bool dirty;
        string PathName { get { return Path.Combine(Cfg.Dir, "StockMarket", Profile.SafeId(Profile.Id) + ".portfolio.json"); } }

        public StockMarketSystem(ObjectPool pool) { this.pool = pool; pool.Add(menu); Reload(); }
        public void Reload()
        {
            menu.Visible = false;
            market = Store.Load("Market.json", () => new MarketData());
            if (market.Prices == null) market.Prices = new MarketData().Prices;
            try { portfolio = Wallet.Exchange ?? files.Read(PathName, () => new Portfolio()); }
            catch (Exception ex) { Log.Error("portfolio load", ex); throw; }
            if (portfolio.Holdings == null) portfolio.Holdings = new Dictionary<string, int>();
            if (portfolio.AvgCost == null) portfolio.AvgCost = new Dictionary<string, decimal>();
            if (portfolio.Watchlist == null) portfolio.Watchlist = new List<string>();
            if (portfolio.Cash < 0 || portfolio.Holdings.Any(h => h.Value < 0) || portfolio.AvgCost.Any(h => h.Value < 0))
            {
                files.Protect(PathName);
                throw new InvalidDataException("Invalid negative portfolio balance: original protected.");
            }
            Wallet.Exchange = portfolio;
            nextPrice = Game.GameTime + 60000;
        }

        void Rebuild()
        {
            int selected = menu.SelectedIndex;
            menu.Clear(); menu.Name = "Cash $" + Wallet.Cash + " / exchange $" + portfolio.Cash.ToString("0.00");
            Action<string, Action> add = (title, action) => {
                var row = new NativeItem(title);
                row.Activated += (s, e) => { action(); Save(); Wallet.Save(); dirty = true; };
                menu.Add(row);
            };
            add("Deposit $100", () => { if (Wallet.Debit(100)) portfolio.Cash += 100; });
            add("Withdraw $100", () => {
                if (portfolio.Cash >= 100 && Wallet.Cash <= int.MaxValue - 100) { Wallet.Credit(100); portfolio.Cash -= 100; }
            });
            foreach (var quote in market.Prices.Where(q => q.Value > 0).ToList())
            {
                string symbol = quote.Key; decimal price = quote.Value;
                int held; portfolio.Holdings.TryGetValue(symbol, out held);
                add("Buy " + symbol + " $" + price.ToString("0.00"), () => { if (!portfolio.Buy(symbol, 1, market.Prices[symbol])) Notify.Show("Not enough exchange funds."); });
                add("Sell " + symbol + " (" + held + " held)", () => { if (!portfolio.Sell(symbol, 1, market.Prices[symbol])) Notify.Show("No shares to sell."); });
                add((portfolio.Watchlist.Contains(symbol) ? "Unwatch " : "Watch ") + symbol, () => {
                    if (portfolio.Watchlist.Contains(symbol)) portfolio.Watchlist.Remove(symbol); else portfolio.Watchlist.Add(symbol);
                });
            }
            foreach (var held in portfolio.Holdings.Where(h => !market.Prices.ContainsKey(h.Key))) menu.Add(new NativeItem(held.Key + " x" + held.Value, "No quote available. Holding preserved.") { Enabled = false });
            menu.SelectedIndex = Math.Max(0, Math.Min(selected, menu.Items.Count - 1)); dirty = false;
        }

        public void Tick()
        {
            if (unchecked(Game.GameTime - nextPrice) >= 0)
            {
                nextPrice = Game.GameTime + 60000;
                foreach (var symbol in market.Prices.Keys.ToList()) market.Prices[symbol] = Math.Round(Math.Max(1, Math.Min(1000000, market.Prices[symbol] * (1m + (decimal)(random.NextDouble() * .04 - .02)))), 2);
                dirty = true;
            }
            if (dirty && menu.Visible) Rebuild();
        }
        public void OnKey(KeyEventArgs e)
        {
            if (e.KeyCode != Cfg.StocksKey) return;
            if (menu.Visible) menu.Visible = false;
            else if (!pool.AreAnyVisible) { Rebuild(); menu.Visible = true; }
        }
        public void Save()
        {
            // The wallet is the canonical checkpoint: MP cash, bank and exchange funds commit in one file.
            Wallet.Save(); files.Write(PathName, portfolio); Store.Write("Market.json", market);
        }
        public void Dispose() { menu.Visible = false; }
    }
}
