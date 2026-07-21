using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Multiplayer.API;

namespace StkOutfitStandBody;

public class CompBodyTypeOverride : ThingComp
{
	public BodyTypeDef selectedBodyType = BodyTypeDefOf.Male;
	private static readonly HashSet<string> excludedDefs = ["Child", "Baby"];
	private static string GetBodyTypeLabel(BodyTypeDef def)
		=> def == null ? "" : (!def.label.NullOrEmpty() ? def.LabelCap : def.defName);

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		// Only show gizmo on the first selected stand with this comp
		if (!IsFirstSelectedStand())
			yield break;

		yield return new Command_Action
		{
			defaultLabel = "Body: " + GetBodyTypeLabel(selectedBodyType),
			defaultDesc = "Select a body type to use for rendering.",
			icon = selectedBodyType != null
				? ContentFinder<Texture2D>.Get(selectedBodyType.bodyNakedGraphicPath + "_south", false)
				: null,

			action = () =>
			{
				var options = new List<FloatMenuOption>();

				foreach (var def in DefDatabase<BodyTypeDef>.AllDefs.Where(d => !excludedDefs.Contains(d.defName)))
				{
					string label = GetBodyTypeLabel(def);

					options.Add(new FloatMenuOption(label, () =>
					{
						// Collect all selected stands with this comp
						var comps = Find.Selector.SelectedObjects
							.OfType<Building_OutfitStand>()
							.Select(b => b.GetComp<CompBodyTypeOverride>())
							.Where(c => c != null)
							.ToList();

						if (comps.Count > 0)
							SetBodyType(comps, def);
					}));

				}

				Find.WindowStack.Add(new FloatMenu(options));
			}

		};

	}

	[SyncMethod]
	public void SetBodyType(List<CompBodyTypeOverride> list, BodyTypeDef bodyDef)
	{
		foreach (var comp in list)
		{
			comp.selectedBodyType = bodyDef;
			((Building_OutfitStand)comp.parent).RecacheGraphics();
		}

	}

	private bool IsFirstSelectedStand()
	{
		foreach (var selected in Find.Selector.SelectedObjects)
			if (selected is Building_OutfitStand stand && stand.GetComp<CompBodyTypeOverride>() != null)
				return stand == parent; // true if this is the first

		return false;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Defs.Look(ref selectedBodyType, "selectedBodyType");

		//Looks redundant tbh, don't remember why I added this:
		//if (Scribe.mode == LoadSaveMode.PostLoadInit && selectedBodyType == null)
		//{
		//	selectedBodyType = DefDatabase<BodyTypeDef>.AllDefs
		//		.FirstOrDefault(d => !excludedDefs.Contains(d.defName));
		//	if (selectedBodyType == null)
		//		Log.Error("[StkOutfitStand] No valid BodyTypeDef found.");
		//}

	}

}

public class CompProperties_BodyTypeOverride : CompProperties
{
	public CompProperties_BodyTypeOverride()
	{
		compClass = typeof(CompBodyTypeOverride);
	}

}
