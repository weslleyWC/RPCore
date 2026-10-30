using System;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Native;
namespace RPCore
{
    public sealed partial class PhoneSystem
    {
        float PhoneScale=>Math.Min(Visuals.Scale,Math.Min((Visuals.Width-2*Visuals.InsetX)/258,(1080-2*Visuals.InsetY)/459));
        PointF Origin(float scale)=>new PointF(Visuals.Width-Visuals.InsetX-258*scale,1080-Visuals.InsetY-459*scale);
        void MouseTick()
        {
            GTA.UI.Hud.ShowCursorThisFrame();foreach(var c in new[]{GTA.Control.LookLeftRight,GTA.Control.LookUpDown,GTA.Control.CursorAccept,GTA.Control.CursorCancel,GTA.Control.CursorScrollUp,GTA.Control.CursorScrollDown})Game.DisableControlThisFrame(c);
            float scale=PhoneScale;var origin=Origin(scale);bool held=Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED,0,(int)GTA.Control.CursorAccept);
            if(!InventoryMouseLayout.Point(Function.Call<float>(Hash.GET_CONTROL_NORMAL,0,(int)GTA.Control.CursorX),Function.Call<float>(Hash.GET_CONTROL_NORMAL,0,(int)GTA.Control.CursorY),Visuals.Width,origin.X,origin.Y,scale,out var point)){mouseReady=false;pressed=-1;return;}
            if(!mouseReady){mouseReady=true;mouseHeld=held;pressed=-1;return;}
            bool down=held && !mouseHeld,up=!held && mouseHeld;mouseHeld=held;
            if(Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED,0,(int)GTA.Control.CursorCancel)){pressed=-1;Back();return;}
            int hit=page=="Home"?PhoneBankRules.HomeHit(point):PhoneViewRules.RowHit(point,PhoneViewRules.First(row),Rows().Length,RowTop);
            if(down){pressed=hit;if(hit>=0){if(page=="Home")selection=hit;else row=hit;}}
            if(up && pressed>=0){int start=pressed;pressed=-1;if(start==hit)Activate();}
            if(new RectangleF(11,9,220,424).Contains(point))
            {
                if(Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED,0,(int)GTA.Control.CursorScrollDown))Navigate(0,1);
                if(Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED,0,(int)GTA.Control.CursorScrollUp))Navigate(0,-1);
            }
        }
        string Time()=>GTA.Chrono.GameClock.Hour.ToString("00")+":"+GTA.Chrono.GameClock.Minute.ToString("00");
        void Draw()
        {
            float s=PhoneScale;var origin=Origin(s);float x=origin.X,y=origin.Y,fullX=x-1662*s,fullY=y-621*s;
            Action<string,float,float,float,Color,GTA.UI.Alignment,float> text=(t,px,py,scale,c,a,w)=>Visuals.Label(t,x+px*s,y+py*s,scale*s,c,GTA.UI.Font.ChaletLondon,a,w*s);
            if(!Visuals.Sprite("PSRP_PHONE","PhoneTexture",fullX,fullY,1920*s,1080*s,Color.White))Visuals.Rect(x,y,242*s,441*s,Color.FromArgb(245,18,18,20));
            Visuals.Sprite("PSRP_PHONE","PhoneWallpaper"+data.Wallpaper,fullX,fullY,1920*s,1080*s,Color.White);
            if(page=="Home")
            {
                text(Time(),123,23,.47f,Color.White,GTA.UI.Alignment.Center,0);
                text(GTA.Chrono.GameClock.DayOfWeek.ToString().ToUpperInvariant(),123,54,.25f,Color.White,GTA.UI.Alignment.Center,0);
                Visuals.Rect(x+23*s,y+85*s,196*s,49*s,selection==PhoneBankRules.App?Color.FromArgb(157,43,230):Color.FromArgb(130,10,209));
                Visuals.Rect(x+29*s,y+91*s,37*s,37*s,Color.FromArgb(167,80,229));
                text("rp",47,94,.38f,Color.White,GTA.UI.Alignment.Center,0);
                text("RPCore Bank",75,91,.28f,Color.White,GTA.UI.Alignment.Left,140);
                text("Sua conta no celular",75,113,.17f,Color.White,GTA.UI.Alignment.Left,140);
                for(int i=0;i<ReferenceLayout.PhoneIcons.Length;i++)
                {
                    float ix=30.5f+i%3*65,iy=154+i/3*65;
                    if(i==selection)Visuals.Sprite("PSRP_PHONE","HoverIcon",x+(ix-2)*s,y+(iy-2)*s,52*s,52*s,Color.FromArgb(100,255,255,255));
                    if(!Visuals.Sprite("PSRP_PHONE",ReferenceLayout.PhoneIcons[i],x+ix*s,y+iy*s,48*s,48*s,Color.White))text(ReferenceLayout.PhoneLabels[i],ix+24,iy+12,.20f,Color.White,GTA.UI.Alignment.Center,58);
                    int unread=i==1 || i==2?PhoneEdits.History(data,i==1).Count(index=>!data.History[index].Read):0;
                    if(unread>0){Visuals.Sprite("PSRP_PHONE","UnreadMarker",x+(ix+37)*s,y+(iy-9)*s,20*s,20*s,Color.White);text(Math.Min(99,unread).ToString(),ix+47,iy-10,.22f,Color.White,GTA.UI.Alignment.Center,0);}
                }
                return;
            }
            if(PhoneBankRules.IsPage(page)){DrawBank(x,y,s,text);return;}
            string art=page=="NoteView"?"NotesOverlay":page=="Fitness"?"FitnessOverlay":page=="Twitch"?"TwitchOverlay":"PhoneOptionsBG";
            if(!Visuals.Sprite("PSRP_PHONE",art,fullX,fullY,1920*s,1080*s,Color.White))Visuals.Rect(x+11*s,y+9*s,220*s,424*s,Color.FromArgb(245,18,21,25));
            string title=page=="HistoryView"?detailReturn:page;
            text(title,122,26,.38f,Color.White,GTA.UI.Alignment.Center,200);
            var rows=Rows();row=Math.Max(0,Math.Min(row,Math.Max(0,rows.Length-1)));int first=PhoneViewRules.First(row);
            for(int i=first;i<Math.Min(rows.Length,first+7);i++)
            {
                float top=RowTop+(i-first)*39;
                if(i==row)Visuals.Rect(x+17*s,y+top*s,208*s,35*s,Color.FromArgb(155,60,90,120));
                string label=rows[i]??"";text(label.Length>27?label.Substring(0,24)+"...":label,23,top+4,.26f,Color.White,GTA.UI.Alignment.Left,196);
            }
            if(page=="Messages" || page=="Notifications")
            {if(rows.Length==0)text("Nenhuma atividade local.",23,95,.27f,Color.White,GTA.UI.Alignment.Left,196);text("ENTER: ler   "+(rows.Length==0?"":(row+1)+"/"+rows.Length),23,374,.22f,Color.Gray,GTA.UI.Alignment.Left,196);}
            else if(page=="HistoryView" || page=="NoteView")
            {
                text(detail.Count==0?"":detail[Math.Min(detailPage,detail.Count-1)],23,88,.26f,Color.White,GTA.UI.Alignment.Left,196);
                text("Pagina "+(detailPage+1)+"/"+detail.Count+" / setas: ler",23,374,.2f,Color.Gray,GTA.UI.Alignment.Left,196);
            }
            else if(page=="Stopwatch" || page=="Timer")text(PhoneClockState.Format(page=="Timer"?clock.Remaining:clock.Elapsed),122,118,.48f,Color.White,GTA.UI.Alignment.Center,200);
            else if(page=="Clock")text(Time(),122,235,.55f,Color.White,GTA.UI.Alignment.Center,0);
            else if(page=="Calculator"){text(result,23,100,.3f,Color.White,GTA.UI.Alignment.Left,196);text("ENTER: ex.125*4",23,302,.25f,Color.Gray,GTA.UI.Alignment.Left,196);}
            else if(page=="Fitness")text("Saude: "+HudMath.Percent(NeedsRules.Health(Game.Player.Character.Health,Game.Player.Character.MaxHealth))+"%~n~Colete: "+Game.Player.Character.Armor+"~n~Fome: "+HudMath.Percent(needs.Hunger)+"~n~Sede: "+HudMath.Percent(needs.Thirst)+"~n~Sono: "+HudMath.Percent(needs.Sleep)+"~n~Folego: "+HudMath.Percent(needs.Stamina),23,105,.29f,Color.White,GTA.UI.Alignment.Left,196);
            else if(page=="DeleteNote")text("Excluir esta nota?~n~"+(noteExpected==null?"":noteExpected.Length>100?noteExpected.Substring(0,97)+"...":noteExpected),23,205,.25f,Color.White,GTA.UI.Alignment.Left,196);
            else if(page=="Notes")text("ENTER: abrir / criar   DELETE: excluir...",23,374,.2f,Color.Gray,GTA.UI.Alignment.Left,196);
            text("Voltar: Backspace / direito   "+Visuals.PhoneKey+": fechar",23,401,.2f,Color.LightGray,GTA.UI.Alignment.Left,200);
        }
    }
}
