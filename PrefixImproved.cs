using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace PrefixImproved
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class PrefixImproved : Mod
	{
        public override object Call(params object[] args)
        {
            if (PrefixModSystem.instance == null)
            {
                Logger.Error("Call was called before PrefixModSystem class loaded. Make sure to only use Call after Mod.PostSetupContent.");
                return "Failure";
            }
            return PrefixModSystem.instance.Call(args);
        }
    }
}
