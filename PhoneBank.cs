using System;
using System.Drawing;
using GTA;

namespace RPCore
{
    public sealed partial class PhoneSystem
    {
        PhoneBankOperation bankOperation;
        PhoneBankOperation bankReceipt;
        bool bankHeld;
        public Action BankTransferred;
        void OpenBank(){bankOperation=null;bankReceipt=null;bankHeld=true;OpenPage("Bank");}
        void BeginBank(bool deposit,bool all)
        {
            bankOperation=null;
            int amount=PhoneViewRules.BankMaximum(Wallet.Cash,Wallet.Bank,deposit);
            if(amount<=0){Notify.Show((deposit?Wallet.Cash:Wallet.Bank)<=0?"RPCore Bank: sem saldo disponivel para esta operacao.":"RPCore Bank: conta ou carteira no limite numerico do GTA.");return;}
            if(!all)
            {
                string input=Input("",32);if(input==null || string.IsNullOrWhiteSpace(input))return;
                if(!PhoneBankRules.Amount(input,out amount)){Notify.Show("RPCore Bank: digite um valor inteiro positivo, como 10000 ou 10.000.");return;}
            }
            if(!Visible || !Safe() || !PhoneBankRules.IsPage(page))return;
            var actor=Game.Player.Character;
            bankOperation=PhoneBankOperation.Create(Profile.Id,actor.Handle,actor.Model.Hash,Wallet.Cash,Wallet.Bank,amount,deposit);
            if(bankOperation==null){Notify.Show("RPCore Bank: valor acima do saldo disponivel ou da capacidade da conta/carteira.");return;}
            OpenPage("BankConfirm");
        }
        void BankAction()
        {
            if(page=="Bank")
            {
                BeginBank(row==0 || row==2,row>=2);return;
            }
            if(page=="BankReceipt"){OpenBank();return;}
            if(page!="BankConfirm")return;
            if(row==0){OpenBank();return;}
            // Detach the quote before doing IO: a repeated Enter/click cannot reuse it.
            var operation=bankOperation;bankOperation=null;
            if(operation==null){OpenBank();return;}
            PhoneBankResult outcome;
            try
            {
                var actor=Game.Player.Character;
                outcome=operation.Commit(Profile.Id,actor.Handle,actor.Model.Hash,Wallet.Cash,Wallet.Bank,(amount,deposit)=>deposit?Wallet.DepositSaved(amount):Wallet.WithdrawSaved(amount));
            }
            catch(Exception ex)
            {
                Log.Error("RPCore Bank transfer",ex);OpenBank();Notify.Show("RPCore Bank: gravacao nao concluida. Confira os saldos; operacao nao sera repetida automaticamente.");return;
            }
            if(outcome!=PhoneBankResult.Completed){OpenBank();Notify.Show("RPCore Bank: saldo ou contexto mudou. Revise e confirme uma nova operacao.");return;}
            bankReceipt=operation;OpenPage("BankReceipt");
            try{BankTransferred?.Invoke();}catch(Exception ex){Log.Error("phone bank achievement",ex);}
        }
        void DrawBank(float x,float y,float s,Action<string,float,float,float,Color,GTA.UI.Alignment,float> text)
        {
            var purple=Color.FromArgb(130,10,209);var ink=Color.FromArgb(37,24,49);var grey=Color.FromArgb(108,94,119);
            Action<float,float,float,float,Color> rect=(px,py,w,h,c)=>Visuals.Rect(x+px*s,y+py*s,w*s,h*s,c);
            Action<string,float,float,float,Color> label=(value,px,py,size,color)=>text(value,px,py,size,color,GTA.UI.Alignment.Left,196);
            rect(11,9,220,424,Color.FromArgb(251,248,255));rect(11,9,220,94,purple);
            label("RPCore Bank",23,23,.38f,Color.White);
            label("Seu dinheiro, no seu ritmo.",23,64,.21f,Color.White);
            var operation=page=="BankReceipt"?bankReceipt:bankOperation;
            if(page=="Bank")
            {
                label("Saldo em conta",23,116,.25f,grey);label(PhoneBankRules.Money(Wallet.Bank),23,143,.43f,ink);
                label("Na carteira: "+PhoneBankRules.Money(Wallet.Cash),23,184,.23f,grey);
            }
            else if(operation!=null)
            {
                bool receipt=page=="BankReceipt";
                label(receipt?(operation.Deposit?"Deposito realizado":"Saque realizado"):(operation.Deposit?"Confirmar deposito":"Confirmar saque"),23,115,.28f,ink);
                label(PhoneBankRules.Money(operation.Amount),23,147,.43f,purple);
                label(receipt?"Movimentacao salva na sua conta.":"Confira antes de continuar.",23,187,.22f,grey);
                label((receipt?"Conta: ":"Conta apos: ")+PhoneBankRules.Money(receipt?Wallet.Bank:operation.BankAfter),23,222,.23f,ink);
                label((receipt?"Carteira: ":"Carteira apos: ")+PhoneBankRules.Money(receipt?Wallet.Cash:operation.CashAfter),23,251,.23f,ink);
            }
            var rows=Rows();row=Math.Max(0,Math.Min(row,rows.Length-1));
            for(int i=0;i<rows.Length;i++)
            {
                float top=RowTop+i*39;bool selected=i==row;
                rect(17,top,208,35,selected?purple:Color.FromArgb(236,226,247));
                label(rows[i],25,top+5,.26f,selected?Color.White:purple);
            }
            label("Voltar: Backspace / direito",23,398,.20f,grey);
            label(Visuals.PhoneKey+": fechar celular",23,415,.17f,grey);
        }
    }
}
