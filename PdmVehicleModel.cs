using System.Globalization;
using GTA;
namespace RPCore
{
    internal static class PdmVehicleModel
    {
        public static Model Resolve(int hash)=>new Model(hash);
        public static Model Resolve(string name)
        {
            if(name!=null && name.Length==9 && name[0]=='#' && uint.TryParse(name.Substring(1),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out uint hash))return new Model(unchecked((int)hash));
            return new Model(name);
        }
    }
}
