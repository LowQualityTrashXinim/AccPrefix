To have your prefix effect to not be broken, do the following

go into any ModSystem class or even Mod class

write <br/>
public override void PostAddRecipes() {<br/>
	if (ModLoader.TryGetMod("PrefixImproved", out Mod PrefixImproved)) {<br/>
		PrefixImproved.Call("AddValueToModdedPrefix", /*Your mod prefix name*/, /*Prefix value*/);
	}<br/>
}

Note : Value 1 mean your prefix have point of 1, this allow prefix with value of 2,3 to merge with your prefix but not 4
