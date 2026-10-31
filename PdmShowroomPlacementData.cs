using System;
namespace RPCore
{
    public static class PdmShowroomPlacement
    {
        public static bool Valid(float x,float y,float z,float heading)=>Finite(x) && Finite(y) && Finite(z) && Finite(heading) && Math.Abs(x)<=3 && Math.Abs(y)<=3 && Math.Abs(z)<=1 && Math.Abs(heading)<=180;
        static bool Finite(float value)=>!float.IsNaN(value) && !float.IsInfinity(value);
        public static JobPoint Point(JobPoint original,PdmShowroomEntry entry)
        {
            if(original?.Valid!=true || entry==null || !Valid(entry.OffsetX,entry.OffsetY,entry.OffsetZ,entry.OffsetHeading))throw new ArgumentException("Invalid showroom placement.");
            return new JobPoint{Name=original.Name,X=original.X+entry.OffsetX,Y=original.Y+entry.OffsetY,Z=original.Z,Heading=original.Heading+entry.OffsetHeading};
        }
        public static bool Save(WalletData wallet,string stockId,float x,float y,float z,float heading,string receipt,Action save,out bool changed)
        {changed=false;if(!Valid(x,y,z,heading))return false;return PdmTransactions.Apply(wallet,receipt,0,d=>{if(!PdmEnterpriseRules.Idle(d))return false;var e=d.Showroom.Find(v=>v.StockId==stockId);if(e==null)return false;e.OffsetX=x;e.OffsetY=y;e.OffsetZ=z;e.OffsetHeading=heading;return true;},save,out changed);}
    }
}
