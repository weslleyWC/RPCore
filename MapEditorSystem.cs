using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
namespace RPCore
{
    public sealed class MapEditorSystem : ISystem
    {
        public string Name=>"map_editor";const string Decor="RPCore_MapObject";
        sealed class Live{public Prop Prop;public Model Model;public int Requested,Hash;public bool Applied;}
        readonly ObjectPool pool;readonly ShiftGate gate;readonly Func<bool> gangFree;readonly Action<JobPoint> workshop;readonly JsonFiles files=new JsonFiles();readonly EditorOptions options;readonly NativeMenu menu;
        readonly Dictionary<string,Live> live=new Dictionary<string,Live>();readonly List<Blip> blips=new List<Blip>();readonly List<Tuple<Prop,int>> retired=new List<Tuple<Prop,int>>();readonly HashSet<string> errors=new HashSet<string>();
        MapEditorData data;EditorMap loaded,draft;EditorObject selected;EditorPose beforeMove;Ped actor;string profile,path,signature,beforeMap;long expected;int token,last,errorAt;bool protectedFile;float step=1;
        public bool Editing=>draft!=null;public bool Manipulating{get;private set;}
        public IEnumerable<EditorPoint> ServicePoints=>!Editing && !protectedFile && profile==Profile.Id && loaded!=null?loaded.Points.Where(p=>p.Kind!=EditorPointKind.Marker):Enumerable.Empty<EditorPoint>();
        static Vector3 Pos(EditorPose p)=>new Vector3(p.X,p.Y,p.Z);
        static EditorPose Pose(Vector3 p,float heading=0)=>new EditorPose{X=p.X,Y=p.Y,Z=p.Z,RZ=heading};
        bool Identity=>profile==Profile.Id && actor!=null && actor.Exists() && actor==Game.Player.Character;
        bool Calm=>Game.Player.Character.IsHuman && Game.Player.Character.IsAlive && !Game.Player.Character.IsInCombat && !Game.Player.Character.IsRagdoll && !Game.IsPaused && !Game.IsMissionActive && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE) && Game.Player.Wanted.WantedLevel==0;
        bool Own(Prop p)=>p!=null && p.Exists() && Function.Call<bool>(Hash.DECOR_EXIST_ON,p,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,p,Decor)==token;
        static string Digest(string file){if(!File.Exists(file))return null;using(var h=SHA256.Create())return Convert.ToBase64String(h.ComputeHash(File.ReadAllBytes(file)));}
        public MapEditorSystem(ObjectPool pool,ShiftGate gate,Func<bool> gangFree=null,Action<JobPoint> workshop=null)
        {
            this.pool=pool;this.gate=gate;this.gangFree=gangFree??(()=>true);this.workshop=workshop;options=Store.LoadDef("Editor/Defaults.json",EditorOptions.Defaults);if(options?.Valid!=true){options=EditorOptions.Defaults();Log.Write("Invalid editor defaults retained; built-in defaults used.");}
            menu=new NativeMenu("Map Builder","OBJETOS / ICONES / SERVICOS");pool.Add(menu);Visuals.Register(menu,"core");LoadProfile();
        }
        void LoadProfile()
        {
            profile=Profile.Id;path=Path.Combine(Cfg.SaveDir,Profile.SafeId(profile),"MapEditor.json");protectedFile=false;data=null;
            try{data=files.Read(path,()=>new MapEditorData());signature=Digest(path);if(data?.Valid!=true)throw new InvalidDataException("Mapa invalido/versao desconhecida.");}
            catch(Exception ex){protectedFile=true;files.Protect(path);Log.Error("editor load",ex);}
            if(!protectedFile && data.AutoLoad)LoadMap(data.Maps.First(m=>m.Id==data.Selected));
        }
        void Persist(MapEditorData next)
        {
            if(profile!=Profile.Id || protectedFile || next?.Valid!=true)throw new InvalidOperationException("Perfil/arquivo de mapas protegido.");if(Digest(path)!=signature)throw new IOException("MapEditor.json mudou fora do jogo. Exporte rascunho e recarregue antes de gravar.");files.Write(path,next);signature=Digest(path);
        }
        void Add(string title,string description,Action action,bool enabled=true){var row=new NativeItem(title,description){Enabled=enabled};string owner=Profile.Id;row.Activated+=(s,e)=>{if(owner!=Profile.Id)return;try{action();}catch(Exception ex){Log.Error("editor menu",ex);Notify.Show("Acao nao concluida: "+ex.Message+" Rascunho e mapa salvo conservados.");}};menu.Add(row);}
        string Text(string value,int max=60)=>Game.GetUserInput(WindowTitle.EnterMessage60,value??"",max)?.Trim();
        public void OpenMenu()
        {
            pool.HideAll();menu.Clear();if(protectedFile){Add("Mapas protegidos","JSON invalido/versao desconhecida. Corrija MapEditor.json ou restaure .bak; nenhum mapa e sobrescrito.",()=>{});menu.Visible=true;return;}
            if(Editing){EditMenu();return;}
            Add("Novo mapa","Cria rascunho por perfil. Objetos e icones so ficam persistentes ao Salvar. Sem modificar props/locais do GTA ou de outros mods.",()=>Begin(null));
            Add("Auto-carregar selecionado: "+(data.AutoLoad?"SIM":"NAO"),"Opcional. Ao recarregar/trocar perfil, ativa apenas o mapa selecionado salvo. Props carregam por proximidade. Comeca desligado.",()=>{if(data.Selected==null){Notify.Show("Salve/selecione um mapa antes.");return;}EditorTransactions.Apply(ref data,b=>{b.AutoLoad=!b.AutoLoad;return true;},Persist);OpenMenu();});
            Add("Descarregar mapa ativo","Remove somente props/icones criados por este editor; conserva todos os JSON salvos.",()=>{Unload();OpenMenu();});
            foreach(var map in data.Maps){var m=map;Add((loaded?.Id==m.Id?"ATIVO / ":"")+m.Name,m.Objects.Count+" objetos / "+m.Points.Count+" icones e pontos. Abrir opcoes.",()=>MapMenu(m.Id));}
            Add("Importar mapa JSON","Lista arquivos em RPCore/Maps/Exports. Importa copia com novos IDs; nao executa scripts, nao altera arquivo fonte.",ImportMenu);
            Add("Recarregar arquivos do perfil","Descarta apenas estado de carregamento. Grave/exporte rascunho antes. Arquivo corrompido continua protegido durante esta sessao.",()=>{Unload();LoadProfile();OpenMenu();});
            Add("Fechar","Voltar ao jogo.",()=>menu.Visible=false);menu.Visible=true;
        }
        void MapMenu(string id)
        {
            var m=data.Maps.FirstOrDefault(v=>v.Id==id);if(m==null)return;pool.HideAll();menu.Clear();Add(m.Name,m.Objects.Count+" objetos, "+m.Points.Count+" pontos.",()=>{});
            Add("Carregar / selecionar","Ativa o mapa e seus servicos. Sem escrever configuracoes originais de lojas/postos/garagens.",()=>{if(!Calm || gate.Owner!=null || InterfaceOptions.ModalOpen){Notify.Show("Carregue fora de missao/atividade/procurado.");return;}if(EditorTransactions.Apply(ref data,b=>{b.Selected=id;return true;},Persist)){LoadMap(data.Maps.First(v=>v.Id==id));OpenMenu();}});
            Add("Editar mapa", "Reserva atividade; cria copia de trabalho. Preview sem colisao. Cancelar conserva versao salva.",()=>Begin(id));
            Add("Exportar JSON", "Informe nome simples: letras/numeros/_/-. Arquivo em RPCore/Maps/Exports. Nome existente sera substituido com backup .bak.",()=>Export(m));
            Add("Excluir mapa salvo...","Abre confirmacao; remove apenas este mapa da biblioteca e seus recursos proprios.",()=>{menu.Clear();Add("CONFIRMAR exclusao: "+m.Name,"Arquivo possui backup atomico; demais mapas e arquivos originais mantidos.",()=>{if(EditorTransactions.Delete(ref data,id,m.Revision,Persist)){if(loaded?.Id==id)Unload();OpenMenu();}});Add("Voltar","Conservar mapa.",()=>MapMenu(id));});
            Add("Voltar","Biblioteca de mapas.",OpenMenu);menu.Visible=true;
        }
        void Begin(string mapId)
        {
            if(Editing || !Calm || !Game.Player.Character.IsOnFoot || InterfaceOptions.ModalOpen || gate.Owner!=null || !Wallet.GangWarEconomyFree || !gangFree() || !gate.Acquire(Name)){Notify.Show("Edite a pe, fora de atividade/missao/procurado e pendencias.");return;}
            try{var old=mapId==null?null:data.Maps.FirstOrDefault(m=>m.Id==mapId);if(mapId!=null && old==null)throw new InvalidOperationException("Mapa inexistente.");if(old==null && data.Maps.Count>=options.MaxMaps)throw new InvalidOperationException("Limite de mapas atingido.");string name=old?.Name??Text("Meu mapa");if(!EditorRules.Label(name)){gate.Release(Name);return;}beforeMap=loaded?.Id;draft=old==null?new EditorMap{Id=Guid.NewGuid().ToString("N"),Name=name}:EditorRules.Copy(old);expected=old?.Revision??-1;actor=Game.Player.Character;UnloadScene();token=new Random().Next(1,int.MaxValue);errors.Clear();RefreshBlips(draft);last=Game.GameTime;EditMenu();}
            catch{gate.Release(Name);draft=null;actor=null;throw;}
        }
        void EditMenu()
        {
            pool.HideAll();menu.Clear();Add("Rascunho: "+draft.Name,"Preview sem colisao. Salvar grava objetos, transformacoes, icones/pontos e metadados por perfil.",()=>{});
            Add("Adicionar objeto / modelo...","Modelos de props base ou DLC instalado; nao aceita ped/veiculo ou estruturas gigantes. Surge3m a frente, sem colisao.",ModelMenu);
            Add("Adicionar icone / servico aqui","Salva a posicao atual. Loja usa inventario/precos existentes; posto usa combustivel existente; oficina usa reparo/lavagem e personalizacao.",NewPointMenu);
            foreach(var row in draft.Objects){var o=row;Add("Objeto: "+o.Model,"X="+o.Pose.X.ToString("0.0")+" Y="+o.Pose.Y.ToString("0.0")+" Z="+o.Pose.Z.ToString("0.0"),()=>ObjectMenu(o.Id));}
            foreach(var row in draft.Points){var p=row;Add("Ponto: "+p.Name+" / "+p.Kind,"Editar nome, icone, cor, alcance e local.",()=>PointMenu(p.Id));}
            Add("Renomear mapa","Mantem objetos e pontos.",()=>{string n=Text(draft.Name);if(EditorRules.Label(n))draft.Name=n;EditMenu();});
            Add("Salvar mapa e sair","Grava atomicamente com .bak, preserva outros mapas. Colisoes marcadas so ativam quando area estiver livre.",()=>{if(!EditorTransactions.Put(ref data,draft,expected,options,Persist))throw new InvalidOperationException("Limite/versao do mapa mudou; exporte rascunho e recarregue.");string saved=draft.Id;FinishEdit(false);LoadMap(data.Maps.First(m=>m.Id==saved));OpenMenu();});
            Add("Exportar rascunho","Permite recuperar rascunho sem sobrescrever arquivo do perfil.",()=>Export(draft));
            Add("Cancelar rascunho e sair","Descarta alteracoes NAO salvas, restaura mapa que estava carregado.",()=>{FinishEdit(true);OpenMenu();});
            Add("Fechar menu / continuar editando","F11 > Editor reabre o menu. END cancela rascunho. Nenhum objeto ambiente e selecionado.",()=>menu.Visible=false);menu.Visible=true;
        }
        void ModelMenu()
        {
            menu.Clear();foreach(string name in options.Models){string model=name;Add(model,"Adicionar prop proprio.",()=>AddObject(model));}Add("Digitar modelo...","Nome do modelo de prop, incluindo DLC ja instalado.",()=>AddObject(Text("prop_roadcone02a")));Add("Voltar","Rascunho.",EditMenu);
        }
        void AddObject(string model)
        {
            if(!EditorRules.ModelNameSafe(model) || draft.Objects.Count>=options.MaxObjects)throw new InvalidOperationException("Modelo/limite invalido.");var m=new Model(model);if(!m.IsValid || !m.IsInCdImage || m.IsPed || m.IsVehicle)throw new InvalidOperationException("Prop inexistente; confirme DLC instalado.");var o=new EditorObject{Id=Guid.NewGuid().ToString("N"),Model=model,Pose=Pose(actor.Position+actor.ForwardVector*3-new Vector3(0,0,1),actor.Heading)};draft.Objects.Add(o);ObjectMenu(o.Id);
        }
        void ObjectMenu(string id)
        {
            selected=draft.Objects.FirstOrDefault(o=>o.Id==id);if(selected==null)return;menu.Clear();Add(selected.Model,errors.Contains(selected.Id)?"Modelo indisponivel/fora dos limites. JSON preservado; confira DLC e recarregue o mapa.":"Transformacao de prop proprio; alteracoes ficam no rascunho.",()=>{});
            Add("Manipular no mundo","Numpad4/6:X;2/8:Y;PgUp/PgDn:Z;Q/E:rumo. Enter confirma, ESC/Backspace desfaz este movimento, F11 confirma e volta; END cancela todo rascunho.",()=>{if(actor.Position.DistanceTo(Pos(selected.Pose))>60){Notify.Show("Aproxime-se do objeto ou traga-o para frente.");return;}beforeMove=EditorRules.Copy(selected.Pose);Manipulating=true;last=Game.GameTime;pool.HideAll();});
            Add("Passo: "+step.ToString("0.0"),"Unidades por segundo na manipulacao. Alterna0,1/1/5.",()=>{step=step==.1f?1:step==1?5:.1f;ObjectMenu(id);});
            Add("Trazer para frente do jogador","Reposiciona apenas este prop do rascunho.",()=>{selected.Pose=Pose(actor.Position+actor.ForwardVector*3-new Vector3(0,0,1),actor.Heading);ObjectMenu(id);});
            foreach(string axis in new[]{"X","Y","Z","RX","RY","RZ"}){string a=axis;Add("Definir "+a,"Informe numero decimal; posicao GTA ou rotacao em graus.",()=>{string raw=Text("0");if(!float.TryParse(raw,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float v))throw new InvalidOperationException("Use ponto decimal.");var p=EditorRules.Copy(selected.Pose);typeof(EditorPose).GetField(a).SetValue(p,v);if(!p.Valid)throw new InvalidOperationException("Coordenada/rotacao fora dos limites.");selected.Pose=p;ObjectMenu(id);});}
            Add("Ajustar ao chao","Usa apenas o prop proprio carregado.",()=>{if(live.TryGetValue(id,out var l) && Own(l.Prop) && Function.Call<bool>(Hash.PLACE_OBJECT_ON_GROUND_PROPERLY,l.Prop)){var p=l.Prop.Position;selected.Pose.X=p.X;selected.Pose.Y=p.Y;selected.Pose.Z=p.Z;}else Notify.Show("Aguarde carregar o objeto e aproxime-se.");ObjectMenu(id);});
            Add("Colisao apos salvar: "+(selected.Collision?"SIM":"NAO"),"Preview nunca colide. Carregamento com colisao aguarda area livre de pessoas/veiculos.",()=>{selected.Collision=!selected.Collision;ObjectMenu(id);});
            Add("Duplicar","Copia prop/rotacao/metadados com novoID e deslocamento1m.",()=>{if(draft.Objects.Count>=options.MaxObjects)return;var o=EditorRules.Copy(selected);o.Id=Guid.NewGuid().ToString("N");if(!EditorRules.Translate(o.Pose,1,0,0))return;draft.Objects.Add(o);ObjectMenu(o.Id);});
            Add("Excluir objeto do rascunho","Nao exclui objetos do GTA/arquivos de modelos. Salve para tornar a remocao persistente.",()=>{draft.Objects.Remove(selected);selected=null;EditMenu();});Add("Voltar","Rascunho.",EditMenu);
        }
        void NewPointMenu()
        {
            menu.Clear();foreach(EditorPointKind kind in Enum.GetValues(typeof(EditorPointKind))){var k=kind;Add(k==EditorPointKind.Workshop?"Oficina: servicos e personalizacao":k.ToString(),"Criar ponto na sua posicao atual. Mesmos precos/sistemas RPCore existentes; nao cria propriedade/renda gratuita.",()=>{string name=Text(k.ToString());var p=new EditorPoint{Id=Guid.NewGuid().ToString("N"),Name=name,Kind=k,Pose=Pose(actor.Position,actor.Heading),Sprite=(int)(k==EditorPointKind.Grocery?BlipSprite.Store:k==EditorPointKind.Fuel?BlipSprite.JerryCan:k==EditorPointKind.Workshop?BlipSprite.Garage:BlipSprite.Standard)};if(!EditorRules.CanAddPoint(draft,p,options.MaxPoints))throw new InvalidOperationException("Ponto invalido/duplicado no raio5m ou limite atingido.");draft.Points.Add(p);RefreshBlips(draft);PointMenu(p.Id);});}Add("Voltar","Rascunho.",EditMenu);
        }
        void PointMenu(string id)
        {
            var p=draft.Points.FirstOrDefault(v=>v.Id==id);if(p==null)return;menu.Clear();Add(p.Name+" / "+p.Kind,"Servicos ativos somente depois de salvar/sair. Oficina oferece reparo/lavagem e personalizacao do veiculo.",()=>{});
            Add("Renomear","Nome do icone.",()=>{string n=Text(p.Name);if(EditorRules.Label(n))p.Name=n;RefreshBlips(draft);PointMenu(id);});
            Add("Mover para minha posicao","Mantem tipo e icone.",()=>{p.Pose=Pose(actor.Position,actor.Heading);RefreshBlips(draft);PointMenu(id);});
            var sprites=new[]{BlipSprite.Standard,BlipSprite.Safehouse,BlipSprite.Garage,BlipSprite.Warehouse,BlipSprite.Store,BlipSprite.JerryCan};Add("Alternar icone","Padrao, casa, garagem, armazem, loja, posto.",()=>{int i=Array.FindIndex(sprites,s=>(int)s==p.Sprite);p.Sprite=(int)sprites[(i+1)%sprites.Length];RefreshBlips(draft);PointMenu(id);});
            var colors=new[]{BlipColor.Blue,BlipColor.White,BlipColor.Red,BlipColor.Orange,BlipColor.Yellow,BlipColor.Green,BlipColor.Pink};Add("Alternar cor","Azul, branco, vermelho, laranja, amarelo, verde, rosa.",()=>{int i=Array.FindIndex(colors,c=>(int)c==p.Color);p.Color=(int)colors[(i+1)%colors.Length];RefreshBlips(draft);PointMenu(id);});
            Add("Somente perto: "+p.ShortRange,"Alterna alcance do minimapa.",()=>{p.ShortRange=!p.ShortRange;RefreshBlips(draft);PointMenu(id);});Add("Mostrar na legenda: "+p.Legend,"Alterna legenda.",()=>{p.Legend=!p.Legend;RefreshBlips(draft);PointMenu(id);});
            Add("Excluir ponto","Remocao somente do rascunho; confirme salvando mapa.",()=>{draft.Points.Remove(p);RefreshBlips(draft);EditMenu();});Add("Voltar","Rascunho.",EditMenu);
        }
        string ExportFolder=>Path.Combine(Cfg.Dir,"Maps","Exports");
        void Export(EditorMap map){string name=Text("mapa_"+map.Id.Substring(0,Math.Min(8,map.Id.Length)),40);if(!EditorRules.FileName(name))throw new InvalidOperationException("Use letras/numeros/_/- no nome do arquivo.");files.Write(Path.Combine(ExportFolder,name+".json"),map);Notify.Show("Mapa exportado em RPCore/Maps/Exports/"+name+".json");}
        void ImportMenu()
        {
            menu.Clear();var all=Directory.Exists(ExportFolder)?Directory.EnumerateFiles(ExportFolder,"*.json").OrderBy(v=>v).Take(24).ToArray():new string[0];
            foreach(string file in all){string f=file;Add(Path.GetFileName(f),"Importar como copia na biblioteca deste perfil. Modelos so criam props apos carregar/editar mapa.",()=>{if(new FileInfo(f).Length>1024*1024)throw new InvalidOperationException("Arquivo excede1MB.");var source=files.Read<EditorMap>(f,()=>null);var copy=EditorRules.ImportCopy(source);if(copy==null || !EditorTransactions.Put(ref data,copy,-1,options,Persist))throw new InvalidOperationException("Formato/limites invalidos ou biblioteca cheia.");OpenMenu();});}
            if(all.Length==0)Add("Nenhum JSON exportado","Use Exportar em um mapa/rascunho primeiro.",()=>{});Add("Voltar","Biblioteca.",OpenMenu);menu.Visible=true;
        }
        void LoadMap(EditorMap map){UnloadScene();loaded=EditorRules.Copy(map);token=new Random().Next(1,int.MaxValue);errors.Clear();RefreshBlips(loaded);}
        void RefreshBlips(EditorMap map){foreach(var b in blips)if(b!=null && b.Exists())b.Delete();blips.Clear();foreach(var p in map.Points){var b=World.CreateBlip(Pos(p.Pose));if(b==null || !b.Exists()){Log.Write("Editor: blip indisponivel, ponto preservado.");continue;}b.Name=p.Name;b.Sprite=(BlipSprite)p.Sprite;b.Color=(BlipColor)p.Color;b.IsShortRange=p.ShortRange;Function.Call(Hash.SET_BLIP_HIDDEN_ON_LEGEND,b,!p.Legend);blips.Add(b);}}
        void Remove(Live l){if(Own(l.Prop)){if(Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,l.Prop))retired.Add(Tuple.Create(l.Prop,token));else l.Prop.Delete();}if(l.Model.Hash!=0)l.Model.MarkAsNoLongerNeeded();}
        void UnloadScene(){foreach(var l in live.Values)Remove(l);live.Clear();foreach(var b in blips)if(b!=null && b.Exists())b.Delete();blips.Clear();}
        void Unload(){UnloadScene();loaded=null;}
        bool FreeCollision(Vector3 p,float radius)=>!World.GetNearbyPeds(p,radius+1).Any(v=>v.Exists()) && !World.GetNearbyVehicles(p,radius+2).Any(v=>v.Exists());
        void RenderMap(EditorMap map)
        {
            foreach(string id in live.Keys.Where(id=>!map.Objects.Any(o=>o.Id==id)).ToArray()){Remove(live[id]);live.Remove(id);}
            foreach(var o in map.Objects){float distance=Game.Player.Character.Position.DistanceTo(Pos(o.Pose));if(distance>options.UnloadRadius){if(live.TryGetValue(o.Id,out var old)){Remove(old);live.Remove(o.Id);}continue;}if(distance>options.LoadRadius && !live.ContainsKey(o.Id) || errors.Contains(o.Id))continue;
                if(!live.TryGetValue(o.Id,out var l)){var m=new Model(o.Model);if(!m.IsValid || !m.IsInCdImage || m.IsPed || m.IsVehicle){errors.Add(o.Id);Log.Write("Editor unavailable prop retained: "+o.Model);continue;}m.Request();l=new Live{Model=m,Requested=Game.GameTime,Hash=m.Hash};live.Add(o.Id,l);}
                if(l.Prop==null){if(!l.Model.IsLoaded){if(unchecked(Game.GameTime-l.Requested)>10000){errors.Add(o.Id);Remove(l);live.Remove(o.Id);}continue;}var min=new OutputArgument();var max=new OutputArgument();Function.Call(Hash.GET_MODEL_DIMENSIONS,l.Hash,min,max);float radius=Math.Max(min.GetResult<Vector3>().Length(),max.GetResult<Vector3>().Length());if(!GangPresenceRules.Finite(radius) || radius>20 || radius<.01f){errors.Add(o.Id);Remove(l);live.Remove(o.Id);continue;}if(!Editing && o.Collision && !FreeCollision(Pos(o.Pose),radius))continue;
                    l.Prop=World.CreateProp(l.Model,Pos(o.Pose),false,false);if(l.Prop==null || !l.Prop.Exists())throw new InvalidOperationException("Prop nao foi criado: "+o.Model);if(!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE,Decor,3))Function.Call(Hash.DECOR_REGISTER,Decor,3);if(!Function.Call<bool>(Hash.DECOR_SET_INT,l.Prop,Decor,token)){l.Prop.MarkAsNoLongerNeeded();l.Prop=null;throw new InvalidOperationException("Marcacao de prop recusada.");}l.Prop.IsPersistent=true;l.Prop.IsCollisionEnabled=false;l.Prop.IsPositionFrozen=true;
                }
                if(!Own(l.Prop) || l.Prop.Model.Hash!=l.Hash)continue;if(Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,l.Prop))continue;if(!Editing && l.Applied)continue;l.Prop.Position=Pos(o.Pose);l.Prop.Rotation=new Vector3(o.Pose.RX,o.Pose.RY,o.Pose.RZ);l.Prop.IsPositionFrozen=true;l.Prop.IsCollisionEnabled=!Editing && o.Collision;l.Applied=true;
            }
        }
        void FinishEdit(bool restore)
        {
            string previous=beforeMap;Manipulating=false;draft=null;selected=null;beforeMove=null;actor=null;beforeMap=null;Unload();gate.Release(Name);if(restore && profile==Profile.Id && previous!=null){var old=data.Maps.FirstOrDefault(m=>m.Id==previous);if(old!=null)LoadMap(old);}
        }
        public void Tick()
        {
            for(int i=retired.Count-1;i>=0;i--){var r=retired[i];var p=r.Item1;if(p==null || !p.Exists() || !Function.Call<bool>(Hash.DECOR_EXIST_ON,p,Decor) || Function.Call<int>(Hash.DECOR_GET_INT,p,Decor)!=r.Item2){retired.RemoveAt(i);continue;}if(!Function.Call<bool>(Hash.IS_ENTITY_ATTACHED,p)){p.Delete();retired.RemoveAt(i);}}
            try{
                if(Editing && (!Identity || !Calm || gate.Owner!=Name || !Wallet.GangWarEconomyFree || !gangFree())){if(Game.IsPaused)return;FinishEdit(true);menu.Visible=false;Notify.Show("Edicao cancelada por perfil/ator/missao/procurado; mapa salvo conservado.");return;}
                if(Game.IsPaused || Game.IsMissionActive || Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE))return;
                if(Manipulating){int dt=Math.Max(0,Math.Min(100,unchecked(Game.GameTime-last)));last=Game.GameTime;foreach(var control in new[]{GTA.Control.Attack,GTA.Control.Aim,GTA.Control.MoveLeftRight,GTA.Control.MoveUpDown,GTA.Control.Jump,GTA.Control.Phone,GTA.Control.SelectWeapon,GTA.Control.FrontendPause,GTA.Control.FrontendPauseAlternate})Game.DisableControlThisFrame(control);
                    float d=step*dt/1000f;EditorRules.Translate(selected.Pose,(Game.IsKeyPressed(Keys.NumPad6)?d:0)-(Game.IsKeyPressed(Keys.NumPad4)?d:0),(Game.IsKeyPressed(Keys.NumPad8)?d:0)-(Game.IsKeyPressed(Keys.NumPad2)?d:0),(Game.IsKeyPressed(Keys.PageUp)?d:0)-(Game.IsKeyPressed(Keys.PageDown)?d:0),0,0,(Game.IsKeyPressed(Keys.E)?d*4:0)-(Game.IsKeyPressed(Keys.Q)?d*4:0));World.DrawMarker(MarkerType.Chevron1,Pos(selected.Pose)+new Vector3(0,0,1),Vector3.Zero,Vector3.Zero,new Vector3(.3f,.3f,.3f),Color.Lime);GTA.UI.Screen.ShowSubtitle("EDITOR: Num4/6 X | Num2/8 Y | PgUp/Dn Z | Q/E rumo | Enter confirma | ESC desfaz | END cancela",100);
                }
                if(Editing)RenderMap(draft);else if(loaded!=null)RenderMap(loaded);
                if(!Editing && Calm && gate.Owner==null && !pool.AreAnyVisible && !InterfaceOptions.ModalOpen && workshop!=null){var p=Game.Player.Character;var v=p.CurrentVehicle;if(v!=null && v.Driver==p && v.Speed<.5f && !JobVehicles.Contains(v)){var point=ServicePoints.FirstOrDefault(s=>s.Kind==EditorPointKind.Workshop && p.Position.DistanceTo(Pos(s.Pose))<=4.5f);if(point!=null){GTA.UI.Screen.ShowHelpTextThisFrame("Pressione ~INPUT_CONTEXT~ para oficina e personalizacao.");if(Game.IsControlJustPressed(GTA.Control.Context))workshop(new JobPoint{Name=point.Name,X=point.Pose.X,Y=point.Pose.Y,Z=point.Pose.Z});}}}
            }catch(Exception ex){if(Editing)Manipulating=false;if(unchecked(Game.GameTime-errorAt)>=0){errorAt=Game.GameTime+5000;Log.Error("map editor",ex);Notify.Show("Editor: confira log/modelo. Mapa salvo conservado.");}}
        }
        public void OnKey(KeyEventArgs e)
        {
            if(!Editing)return;if(e.KeyCode==Keys.End){FinishEdit(true);menu.Visible=false;return;}if(!Manipulating)return;
            if(e.KeyCode==Keys.Escape || e.KeyCode==Keys.Back){selected.Pose=beforeMove;Manipulating=false;ObjectMenu(selected.Id);menu.Visible=true;}else if(e.KeyCode==Keys.Enter || e.KeyCode==Keys.F11){Manipulating=false;ObjectMenu(selected.Id);menu.Visible=true;}
        }
        public void Save(){} // Drafts require the explicit save action; no autosave over user edits.
        public void Reload(){menu.Visible=false;if(Editing)FinishEdit(false);Unload();LoadProfile();}
        public void Dispose(){menu.Visible=false;if(Editing)FinishEdit(false);Unload();foreach(var r in retired)if(r.Item1!=null && r.Item1.Exists() && Function.Call<bool>(Hash.DECOR_EXIST_ON,r.Item1,Decor) && Function.Call<int>(Hash.DECOR_GET_INT,r.Item1,Decor)==r.Item2)r.Item1.MarkAsNoLongerNeeded();retired.Clear();}
    }
}
