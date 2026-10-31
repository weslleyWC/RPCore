using System;
namespace RPCore
{
    // Preview is a temporary presentation, never a purchase or garage record.
    public sealed class DealershipPreviewTicket
    {
        readonly int actor,model,started;readonly long address;readonly string profile;
        public DealershipPreviewTicket(int actor,int model,long address,string profile,int started)
        {this.actor=actor;this.model=model;this.address=address;this.profile=profile;this.started=started;}
        public bool Current(int actor,int model,long address,string profile,int now,bool displayed,double movementSquared)
        {
            uint elapsed=unchecked((uint)(now-started));
            return this.actor!=0 && this.actor==actor && this.model==model && this.address!=0 && this.address==address && this.profile!=null && this.profile==profile
                && !double.IsNaN(movementSquared) && movementSquared>=0 && movementSquared<=25 && elapsed<(displayed?120000u:10000u);
        }
        public static bool Fits(float width,float length,float height)=>Finite(width) && Finite(length) && Finite(height) && width>0 && width<=3 && length>0 && length<=7 && height>0 && height<=3;
        static bool Finite(float n)=>!float.IsNaN(n) && !float.IsInfinity(n);
    }
}
