using System;

namespace EaWXParse
{
    public class SharedFunctions
    {
        //This does work, but accessing the functions is shockingly awkward and it means the final product needs a dll...
        //make functions public
        ///EaWXParse.SharedFunctions yyy = new EaWXParse.SharedFunctions();
        //string wut = yyy.UpTwoFolder("Path");
        public string UpTwoFolder(string Path)
        {
            return Path.Substring(0, Path.LastIndexOf("\\"));
        }
    }

    public struct hardpoint
    {
        public string name;
        public bool targetable;
    }

    public struct Projectile
    {
        string Name;
        string DamageType;
        string DamageAmount;
    }
}
