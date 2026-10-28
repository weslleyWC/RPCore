using System;
using System.Globalization;
using System.Linq;
namespace RPCore
{
    public enum VehicleAction
    {
        Engine,Cruise,LeftSignal,RightSignal,Hazards,Hood,Trunk,
        DoorFrontLeft,DoorFrontRight,DoorRearLeft,DoorRearRight,
        WindowFrontLeft,WindowFrontRight,WindowRearLeft,WindowRearRight,
        WindowsUp,WindowsDown,ExteriorLights,InteriorLights,Neons,OpenMenu
    }
    public static class VehicleUiRules
    {
        public static int Door(VehicleAction action)=>action==VehicleAction.Hood?4:action==VehicleAction.Trunk?5:action>=VehicleAction.DoorFrontLeft && action<=VehicleAction.DoorRearRight?action-VehicleAction.DoorFrontLeft:-1;
        public static int Window(VehicleAction action)=>action>=VehicleAction.WindowFrontLeft && action<=VehicleAction.WindowRearRight?action-VehicleAction.WindowFrontLeft:-1;
        public static bool BodySafe(bool car,float speed)=>car && !float.IsNaN(speed) && !float.IsInfinity(speed) && speed>=0 && speed<=.5f;
        public static bool SameOwner(int actor,int currentActor,int vehicle,int currentVehicle,string profile,string currentProfile,bool driver)=>actor!=0 && actor==currentActor && vehicle!=0 && vehicle==currentVehicle && profile!=null && profile==currentProfile && driver;
        public static bool Rgb(string text,out int[] rgb)
        {
            rgb=null;if(string.IsNullOrWhiteSpace(text))return false;var parts=text.Split(new[]{',',';',' '},StringSplitOptions.RemoveEmptyEntries);if(parts.Length!=3)return false;var result=new int[3];
            for(int i=0;i<3;i++)if(!int.TryParse(parts[i],NumberStyles.None,CultureInfo.InvariantCulture,out result[i]) || result[i]<0 || result[i]>255)return false;rgb=result;return true;
        }
        public static int RememberNeons(TransportData data,string key,int observed,Action save)
        {
            if(data==null || string.IsNullOrWhiteSpace(key) || observed<0 || observed>15 || save==null || data.NeonEquipment!=null && (data.NeonEquipment.Count>2048 || data.NeonEquipment.Any(p=>string.IsNullOrWhiteSpace(p.Key) || p.Value<0 || p.Value>15)))return -1;
            int known=0;data.NeonEquipment?.TryGetValue(key,out known);int mask=known|observed;if(mask==known)return known;
            if(data.NeonEquipment!=null && data.NeonEquipment.Count>=2048 && !data.NeonEquipment.ContainsKey(key))return -1;
            var old=data.NeonEquipment;var next=old==null?new System.Collections.Generic.Dictionary<string,int>():new System.Collections.Generic.Dictionary<string,int>(old);next[key]=mask;
            try{data.NeonEquipment=next;save();return mask;}catch{data.NeonEquipment=old;throw;}
        }
        public static bool Performance(int slot)=>slot==11 || slot==12 || slot==13 || slot==15 || slot==16;
        public static bool Removed(bool exists,int expectedModel,string expectedKey,int actualModel,string actualKey)=>!exists || expectedModel!=actualModel || expectedKey!=actualKey;
        public static int Resolve(TransportData data,string id,string garage)=>data?.Vehicles==null || id==null?-1:data.Vehicles.FindIndex(v=>v!=null && v.Id==id && v.GarageId==garage);
    }
}
