using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace StkOutfitStandBody;

[HarmonyPatch(typeof(Building_OutfitStand), "get_BodyTypeDefForRendering")]
public static class Patch_OutfitStand_BodyType
{
	public static bool Prefix(Building_OutfitStand __instance, ref BodyTypeDef __result)
	{
		var comp = __instance.GetComp<CompBodyTypeOverride>();
		if (comp == null || comp.selectedBodyType == null)
		{
			Log.Warning("[StkOutfitStand] No 'selectedBodyType' found from comp, defaulting to Male.");
			return true;
		}

		__result = comp.selectedBodyType;
		return false;
	}
}

// Patch for a child outfit stand, just a simple replacer
[HarmonyPatch(typeof(Building_OutfitStand), "InitGraphics")]
public static class Patch_InitGraphics_ReplaceBodyTexture
{
	static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var code = new List<CodeInstruction>(instructions);
		var targetString = "Things/Building/OutfitStand/OutfitStand_BodyChild";
		var replacementString = "Things/Pawn/Humanlike/Bodies/Naked_Child";

		for (int i = 0; i < code.Count; i++)
		{
			if (code[i].opcode == OpCodes.Ldstr && code[i].operand is string str && str == targetString)
			{
				code[i].operand = replacementString;
			}
		}

		return code;
	}
}

// Patch for size of the displayed apparel
[HarmonyPatch(typeof(Building_OutfitStand), "DrawAt")]
public static class Patch_DrawAt_ApparelMeshSize
{
	static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var code = new List<CodeInstruction>(instructions);

		for (int i = 0; i < code.Count; i++)
		{
			var instr = code[i];

			// Look for ldc.r4 1.5f (float constant)
			if (instr.opcode == OpCodes.Ldc_R4 && (float)instr.operand == 1.5f)
			{
				// Also check Patch_Building_OutfitStand_DrawAt.DrawSize
				instr.operand = 1.25f;
			}
		}

		return code;
	}
}

// "Main" patch, to replace body
[HarmonyPatch(typeof(Building_OutfitStand), "DrawAt")]
public static class Patch_Building_OutfitStand_DrawAt
{
	private static readonly Vector2 DrawSize = new(1.25f, 1.25f);
	private static Shader Shader => ShaderDatabase.Cutout;

	static void Prefix(Building_OutfitStand __instance, ref Graphic_Multi __state)
	{
		var comp = __instance.GetComp<CompBodyTypeOverride>();

		if (comp?.selectedBodyType != null)
		{
			string path = comp.selectedBodyType.bodyNakedGraphicPath;
			__state = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(
				path, Shader, DrawSize, Color.white
			);
		}
	}

	static void Postfix(Building_OutfitStand __instance, Vector3 drawLoc, bool flip, Graphic_Multi __state)
	{
		if (__state != null)
		{
			var rot = flip ? __instance.Rotation.Opposite : __instance.Rotation;
			__state
				.GetColoredVersion(__state.Shader, __instance.DrawColor, __instance.DrawColorTwo)
				.Draw(drawLoc.WithYOffset(0.05f), rot, __instance);
		}
	}
}