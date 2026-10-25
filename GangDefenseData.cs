using Newtonsoft.Json;
namespace RPCore
{
    public sealed class GangDefenseOptions : ExtensibleData
    {
        public int Waves=2,PerWave=3;
        [JsonIgnore] public bool Valid=>Waves>=1 && Waves<=2 && PerWave>=1 && PerWave<=3 && Waves*PerWave<=6;
    }
    public static class GangDefenseRules
    {
        public static bool NextWave(int spawned,int defeated,int total,int waveSize)=>waveSize>=1 && waveSize<=3 && spawned>=waveSize && total>=1 && total<=6 && spawned<total && spawned%waveSize==0 && defeated==spawned;
    }
}
