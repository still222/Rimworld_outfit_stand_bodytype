using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace StkOutfitStandBody;

[StaticConstructorOnStartup]
public static class Startup
{
	static Startup()
	{
		var harmony = new Harmony("stk.outfitstandbodytype");
		harmony.PatchAll();

		if (MP.enabled)
			MP.RegisterAll();

	}

}

