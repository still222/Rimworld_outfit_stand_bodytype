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
		Log.Message("[StkOutfitStandBody] Harmony patches applied.");

		if (MP.enabled)
			MP.RegisterSyncMethod(typeof(CompBodyTypeOverride), nameof(CompBodyTypeOverride.SetBodyType));
	}
}
