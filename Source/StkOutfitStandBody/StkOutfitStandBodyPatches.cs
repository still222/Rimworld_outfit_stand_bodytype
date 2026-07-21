using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace StkOutfitStandBody;

[HarmonyPatch(typeof(Building_OutfitStand), nameof(Building_OutfitStand.BodyTypeDefForRendering), MethodType.Getter)]
public static class Patch_OutfitStand_BodyType
{
	[HarmonyPostfix]
	public static void Postfix(Building_OutfitStand __instance, ref BodyTypeDef __result)
	{
		var comp = __instance.GetComp<CompBodyTypeOverride>();

		if (comp != null && comp.selectedBodyType != null)
			__result = comp.selectedBodyType;
	}

}

// Patch for a child outfit stand
[HarmonyPatch(typeof(Building_OutfitStand), nameof(Building_OutfitStand.InitGraphics))]
public static class Patch_InitGraphics_ReplaceBodyTexture
{
	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var code = new List<CodeInstruction>(instructions);
		string targetString = "Things/Building/OutfitStand/OutfitStand_BodyChild";
		string replacementString = "Things/Pawn/Humanlike/Bodies/Naked_Child";

		foreach (var instr in code)
			if (instr.opcode == OpCodes.Ldstr && instr.operand is string str && str == targetString)
			{
				instr.operand = replacementString;
				break;
			}

		return code;
	}

}

[HarmonyPatch(typeof(Building_OutfitStand), nameof(Building_OutfitStand.DrawAt))]
public static class Patch_Building_OutfitStand_DrawAt
{
	static readonly MethodInfo getBodyGraphic =
		AccessTools.Method(typeof(Patch_Building_OutfitStand_DrawAt), nameof(GetBodyGraphic));
	static readonly FieldInfo bodyGraphic =
		AccessTools.Field(typeof(Building_OutfitStand), nameof(Building_OutfitStand.bodyGraphic));
	static readonly MethodInfo getMeshSetForSize =
		AccessTools.Method(
			typeof(MeshPool),
			nameof(MeshPool.GetMeshSetForSize),
			[typeof(float), typeof(float)]
		);
	private static readonly float drawMod = 1.25f;	// Originaly 1.5f
	private static Vector2 drawSize = new(drawMod, drawMod);
	private static Shader Shader => ShaderDatabase.Cutout;

	[HarmonyTranspiler]
	static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var code = new List<CodeInstruction>(instructions);

		//Log.Message(string.Join("\n", code.Select((x, i) => $"{i}: {x}")));

		int changes = 0;
		//Graphic_Multi obj = (num ? bodyGraphicChild : bodyGraphic); => GetBodyGraphic
		//Mesh mesh = MeshPool.GetMeshSetForSize(1.5f, 1.5f).MeshAt(rot); => drawMod
		for (int i = 0; i < code.Count; i++)
		{
			if (!(code[i].Calls(getMeshSetForSize) || code[i].LoadsField(bodyGraphic)))
				continue;

			if (code[i].LoadsField(bodyGraphic))
			{
				code[i] = new CodeInstruction(OpCodes.Ldarg_0).WithLabels(code[i].labels);;
				code.Insert(i + 1, new CodeInstruction(OpCodes.Call, getBodyGraphic));
			}

			else
			{
				code[i - 2].operand = drawMod;
				code[i - 1].operand = drawMod;
			}

			if (++changes == 2)
				break;
		}

		return code;
	}

	static Graphic_Multi GetBodyGraphic(Building_OutfitStand stand)
	{
		return (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(
			stand.BodyTypeDefForRendering.bodyNakedGraphicPath,
			Shader,
			drawSize,
			Color.white);
	}

}
