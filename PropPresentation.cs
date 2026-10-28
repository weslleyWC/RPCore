using System;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;

namespace RPCore
{
    // Phone presentation. Consumables have a single presentation owner in InventorySystem.
    public class PropPresentation : ISystem
    {
        public string Name { get { return "visualprops"; } }
        Prop prop;
        Ped owner;
        Model model;
        PropBinding binding;
        string dictionary, clip;
        bool waiting, phone, animationStarted;
        int deadline, end;

        // Consumables are presented exclusively by InventorySystem. Subscribing to Used
        // here used to spawn a second prop and a competing animation for every consumption.
        public PropPresentation(InventorySystem inventory) { }
        public void ShowPhone()
        {
            PropBinding phoneBinding;
            if (Visuals.Bindings.ItemProps == null || !Visuals.Bindings.ItemProps.TryGetValue("_phone", out phoneBinding)) phoneBinding = new PropBinding { Model = "phonesmart", Fallback = "prop_npc_phone_02", X = .01f, Y = .01f, Z = 0, RX = 0, RY = 0, RZ = 180 };
            if (Visuals.Active && Visuals.UseProps) Start(phoneBinding, true);
        }
        public void ClosePhone() { if (phone) Stop(); }
        void Start(PropBinding request, bool isPhone)
        {
            Stop();
            var p = Game.Player.Character;
            if (p == null || !p.Exists() || !p.IsHuman || p.IsDead || p.IsInVehicle() || Game.IsMissionActive || p.IsRagdoll || p.IsInCombat) return;
            // Do not replace an armed player's secondary weapon animation with a food/phone pose.
            if (p.Weapons.Current.Hash != WeaponHash.Unarmed) return;
            model = new Model(request.Model ?? "");
            if (!model.IsValid || !model.IsInCdImage) model = new Model(request.Fallback ?? "");
            if (!model.IsValid || !model.IsInCdImage) { Log.Write("VISUAL prop unavailable: " + request.Model + " (consumption unaffected)"); return; }
            binding = request; owner = p; phone = isPhone;
            dictionary = isPhone ? "cellphone@" : request.Drink ? "mp_player_intdrink" : "mp_player_inteat@burger";
            clip = isPhone ? "cellphone_text_read_base" : request.Drink ? "loop_bottle" : "mp_player_int_eat_burger";
            model.Request(); Function.Call(Hash.REQUEST_ANIM_DICT, dictionary);
            waiting = true; deadline = Game.GameTime + 2500; end = Game.GameTime + (isPhone ? 600000 : 4000);
        }
        public void Tick()
        {
            if (owner == null) return;
            if (!Visuals.Active || !Visuals.UseProps || !owner.Exists() || owner != Game.Player.Character || owner.IsDead || owner.IsInVehicle() || owner.IsRagdoll || owner.IsInCombat || Game.IsMissionActive || owner.Weapons.Current.Hash != WeaponHash.Unarmed || unchecked(Game.GameTime - end) >= 0) { Stop(); return; }
            if (!waiting) return;
            if (unchecked(Game.GameTime - deadline) >= 0) { Log.Write("VISUAL prop/animation request timed out: " + model.Hash); Stop(); return; }
            if (!model.IsLoaded || !Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, dictionary)) return;
            prop = World.CreateProp(model, owner.Position, false, false);
            if (prop == null || !prop.Exists()) { Stop(); return; }
            prop.IsCollisionEnabled = false;
            prop.AttachTo(owner.Bones[(Bone)57005], new Vector3(binding.X, binding.Y, binding.Z), new Vector3(binding.RX, binding.RY, binding.RZ)); // SKEL_R_Hand
            Function.Call(Hash.TASK_PLAY_ANIM, owner, dictionary, clip, 4f, -4f, phone ? -1 : 3000, 49, 0f, false, false, false); animationStarted = true;
            model.MarkAsNoLongerNeeded(); waiting = false;
        }
        void Stop()
        {
            if (prop != null && prop.Exists()) prop.Delete();
            prop = null;
            if (animationStarted && owner != null && owner.Exists() && dictionary != null && clip != null) Function.Call(Hash.STOP_ANIM_TASK, owner, dictionary, clip, 2f);
            if (waiting) model.MarkAsNoLongerNeeded();
            owner = null; waiting = false; phone = false; animationStarted = false; dictionary = clip = null;
        }
        public void OnKey(KeyEventArgs e) { }
        public void Reload() { Stop(); }
        public void Save() { }
        public void Dispose() { Stop(); }
    }
}
