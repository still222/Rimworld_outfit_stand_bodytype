using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Multiplayer.API;

namespace StkOutfitStandBody;

public class CompBodyTypeOverride : ThingComp
{
	// Cache reflection once
	private static readonly MethodInfo recacheGraphicsMethod =
		typeof(Building_OutfitStand).GetMethod("RecacheGraphics", BindingFlags.Instance | BindingFlags.NonPublic);

	private static string GetBodyTypeLabel(BodyTypeDef def)
		=> def == null ? "" : (!def.label.NullOrEmpty() ? def.LabelCap : def.defName);

	// Skip these body defs
	private static readonly HashSet<string> excludedDefs = ["Child", "Baby"];

	// Default body
	public BodyTypeDef selectedBodyType = BodyTypeDefOf.Male;

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
	
	[SyncMethod(SyncContext.None)]
	public void SetBodyType(List<CompBodyTypeOverride> list, BodyTypeDef def)
	{
		foreach (var comp in list)
		{
			comp.selectedBodyType = def;
			recacheGraphicsMethod?.Invoke(comp.parent, null);
		}
	}

	private bool IsFirstSelectedStand()
	{
		foreach (var selected in Find.Selector.SelectedObjects)
		{
			if (selected is Building_OutfitStand stand && stand.GetComp<CompBodyTypeOverride>() != null)
				return stand == parent; // true if this is the first
		}
		return false;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Defs.Look(ref selectedBodyType, "selectedBodyType");

		if (Scribe.mode == LoadSaveMode.PostLoadInit && selectedBodyType == null)
		{
			selectedBodyType = DefDatabase<BodyTypeDef>.AllDefs
				.FirstOrDefault(d => !excludedDefs.Contains(d.defName));

			if (selectedBodyType == null)
			{
				Log.Error("[StkOutfitStand] No valid BodyTypeDef found. Gizmo will not work.");
			}
		}
	}
}

public class CompProperties_BodyTypeOverride : CompProperties
{
	public CompProperties_BodyTypeOverride()
	{
		compClass = typeof(CompBodyTypeOverride);
	}
}