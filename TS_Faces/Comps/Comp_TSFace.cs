using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using TS_Faces.Data;
using TS_Faces.Mod;
using TS_Faces.Rendering;
using TS_Faces.Util;
using TS_Lib.Save;
using TS_Lib.Transforms;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace TS_Faces.Comps;

public class CompProperties_TSFace : CompProperties
{
	public CompProperties_TSFace()
	{
		compClass = typeof(Comp_TSFace);
	}
}
public class SidedTRFaceParts(FacePartDef part, TSTransform4 transform) : IExposable, ICreateCopy<SidedTRFaceParts>
{
	public List<SidedDef<FacePartDef>> FaceParts = [new(part)];
	public SidedTransform Transform = new(transform);

	public SidedTRFaceParts CreateCopy()
	{
		var copy = this.DirtyClone() ?? throw new Exception("unable to clone sided parts?");
		copy.FaceParts = [.. FaceParts.Select(x => x.CreateCopy<FacePartDef, SidedDef<FacePartDef>>())];
		copy.Transform = Transform.CreateCopy<TSTransform4, SidedTransform>();
		return copy;
	}

	public void ExposeData()
	{
		Scribe_Collections.Look(ref FaceParts, "parts");
		Scribe_Deep.Look(ref Transform, "tr");
	}
}

public class ExtraFacePart : IExposable, ICreateCopy<ExtraFacePart>
{
	public const int CACHE_SECONDS = 20;
	public const int CACHE_TICKS = TSUtil.Ticks.TICKS_PER_SECOND * CACHE_SECONDS;
	public ExtraPartDef Def = default!;
	public SlotDef? Anchor;

	public SidedTransform SidedTransform = new(new());
	private FaceSide? AnchorSide;

	public bool IsMirrored
		=> GetAnchorSide() != FaceSide.None
		&& (Def.mirror || GetAnchor() != SlotDefOf.None);

	[Obsolete("don't use directly, only for de/serialization", true)]
	public ExtraFacePart() { }
	public ExtraFacePart(ExtraPartDef def)
	{
		Def = def;
		SidedTransform.ApplyTransform(
			def.transforms.Collapse(Verse.Rand.Int).ToTransform(),
			def.mirror
		);
		TSFacesMod.Logger.Verbose($"Created extra part for '{Def}', transform: '{SidedTransform}'");
	}

	public void ExposeData()
	{
		Scribe_Defs.Look(ref Def, "def");
		Scribe_Defs.Look(ref Anchor, "anchor");
		Scribe_Values.Look(ref AnchorSide, "side");
		Scribe_Deep.Look(ref SidedTransform, "tr");
	}

	private void NotifyChange()
	{
		TickCache<ExtraFacePart, TSTransform4>.ResetCache(this);
	}

	public SlotDef GetAnchor() => Anchor ?? Def.anchor;
	public FaceSide GetAnchorSide() => AnchorSide ?? Def.side;

	public void ChangeAnchorSide(FaceSide side)
	{
		AnchorSide = side;
		NotifyChange();
	}

	public void ChangeAnchor(SlotDef anchor)
	{
		Anchor = anchor;
		NotifyChange();
	}

	public TSTransform4 GetTransformWithAnchor(Comp_TSFace face, FaceSide side)
	{
		var cache_key = (this, side);
		// TODO: invalidate cache when face changes active head
		if (TickCache<(ExtraFacePart, FaceSide), TSTransform4>.TryGetCached(cache_key, out var cached))
			return cached;

		TSTransform4 tr = SidedTransform.ForSide(side).CreateCopy();
		var anchor = GetAnchor();
		if (anchor != SlotDefOf.None)
		{
			Rot4.AllRotations.Do(rot =>
			{
				var face_layout = face.GetActiveFaceLayout();
				var part = face_layout.ForRot(rot).Parts.FirstOrDefault(layout => layout.side == side && layout.slot == anchor);
				var side_tr = tr.ForRot(rot);
				if (part is null)
				{
					// TSFacesMod.Logger.Warning($"unable to find anchor for extra part {Def} for {face.Pawn}, slot={anchor}, side={side}");
					// make part functionally invisible if anchor is not found
					side_tr.Scale = Vector2.zero;
					return;
				}

				side_tr.Offset += part.pos.ToUpFacingVec3();
				side_tr.RotationOffset += part.rotation;
			});
		}
		
		TSFacesMod.Logger.Verbose($"Extra part for '{Def}' final transform: '{tr}'");
		TickCache<(ExtraFacePart, FaceSide), TSTransform4>.Cache(cache_key, tr, CACHE_TICKS);
		return tr;
	}

	public FacePartWithTransform GetForSide(Comp_TSFace face, FaceSide side)
	{
		return new(
			Def.def,
			GetTransformWithAnchor(face, side)
		);
	}

	public IEnumerable<FacePartWithTransform> GetFor(Comp_TSFace face)
	{
		var side = GetAnchorSide();
		yield return GetForSide(face, side);
		if (IsMirrored)
			yield return GetForSide(face, side.Mirror());
	}

	public ExtraFacePart CreateCopy()
	{
		var copy = this.DirtyClone() ?? throw new Exception("unable to clone extra face part?");
		copy.SidedTransform = SidedTransform.CreateCopy<TSTransform4, SidedTransform>();
		return copy;
	}
}

public struct FacePartWithTransform
{
	public readonly FacePartDef Def;
	public readonly TSTransform4 Transform;

	public FaceSide Side;
	public SlotDef? Slot;

	public FacePartWithTransform(FacePartDef def, TSTransform4 transform) : this()
	{
		Def = def;
		Transform = transform;
	}
}

public class TSFacePersistentData() : IExposable
{
	public List<HeadDef> Heads = [];
	public Dictionary<SlotDef, SidedTRFaceParts> PartsForSlots = [];
	public List<ExtraFacePart> ExtraParts = [];
	public Dictionary<FacePartModifierDef, int> AppliedModifiers = [];

	public SidedValue<Color?> EyeReplaceColors = new(null);
	public SidedValue<Color?> ScleraReplaceColors = new(null);

	public TSFacePersistentData CreateCopy()
	{
		TSFacePersistentData copy = this.DirtyClone() ?? throw new Exception("unable to clone ts face data?");
		copy.Heads = [.. Heads];
		copy.PartsForSlots = PartsForSlots.ToDictionary(kv => kv.Key, kv => kv.Value.CreateCopy());
		copy.ExtraParts = [.. ExtraParts.Select(x => x.CreateCopy())];
		copy.EyeReplaceColors = EyeReplaceColors.CreateCopy();
		copy.ScleraReplaceColors = ScleraReplaceColors.CreateCopy();
		return copy;
	}

	public void ExposeData()
	{
		Scribe_Collections.Look(ref Heads, "heads");
		TSSaveUtility.LookDict(ref PartsForSlots, "mainparts");
		Scribe_Collections.Look(ref ExtraParts, "extraparts");
		TSSaveUtility.LookDict(ref AppliedModifiers, "mods");
		Scribe_Deep.Look(ref EyeReplaceColors!, "eyeclr");
		Scribe_Deep.Look(ref ScleraReplaceColors!, "sclclr");
	}
}

public class Comp_TSFace : ThingComp
{
	public enum ReRenderState
	{
		Needed,
		InProgress,
		UpToDate,
	}

	public const float TSHeadBaseLayer = 50f;
	public const int MAX_PART_MODS = 3;
	public const float PART_RAND_BREAK_CHANCE = 0.3f;

	public Pawn Pawn => parent as Pawn ?? throw new Exception("Comp_TSFace attached to non-pawn");

	// Saved variables
	public TSFacePersistentData PersistentData = new();

	// Non-Saved variables
	public ReRenderState RenderState;
	public Graphic_Multi? CachedGraphic;
	public Color? OverriddenColor;

	public PawnState? PreviousState;
	public int CheckTimer;

	public void GenerateExtraParts(StringBuilder? reasons = null)
	{
		foreach (var detail in TSUtil.GetEnumValues<ExtraPartDef.PartDetail>())
		{
			reasons?.AppendLine($"Trying to collect extra part for detail level {detail} for {Pawn}");
			var def = FacesUtil.RandomExtraParts.Ensure(detail).GetRandomFilterableFor(Pawn, reasons);
			if (def is null || def.skip)
			{
				reasons?.AppendReason("no part found");
				continue;
			}

			reasons?.AppendLine($"found def {def}, adding...");
			var part = new ExtraFacePart(def);
			PersistentData.ExtraParts.Add(part);
		}
	}

	public HeadDef? GenerateFittingHeadDef(StringBuilder? reasons = null)
	{
		return FacesUtil.Heads.GetRandomFilterableFor(Pawn, reasons);
	}

	public HeadDef GetActiveHeadDef()
	{
		if (TickCache<Comp_TSFace, HeadDef>.TryGetCached(this, out var def))
			return def;
		if (!PersistentData.Heads.Any())
		{
			PersistentData.Heads.Add(GenerateFittingHeadDef() ?? HeadDefOf.AverageLong);
			// assume that pawn has not generated extra parts yet in this case
			var parts_reasons = new StringBuilder();
			GenerateExtraParts(parts_reasons);

			// TSFacesMod.Logger.Verbose(parts_reasons.ToString());
		}
		def = PersistentData.Heads.GetActiveFromFilters(Pawn) ?? HeadDefOf.AverageLong;
		TickCache<Comp_TSFace, HeadDef>.Cache(this, def);
		return def;
	}
	public FaceLayoutDef GetActiveFaceLayout() => GetActiveHeadDef().faceLayout;
	public bool IsRegenerationNeeded() => RenderState == ReRenderState.Needed;
	public void RequestRegeneration()
	{
		Pawn.Drawer.renderer.SetAllGraphicsDirty();
		RenderState = ReRenderState.Needed;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Color GetEyeColorInner(FaceSide side)
	{
		foreach (var hediff in Pawn.health.hediffSet.hediffs)
		{
			if (side != FaceSide.None && !hediff.part.def.label.ContainsLowerInvariant(side))
				continue;
			GeneEyeColor? color = hediff.def.GetModExtension<GeneEyeColor>();
			if (color is null)
				continue;
			return color.eyeColor;
		}
		foreach (var gene in Pawn.genes.GenesListForReading)
		{
			if (!gene.Active)
				continue;
			GeneEyeColor? color = gene.def.GetModExtension<GeneEyeColor>();
			if (color is null)
				continue;
			return color.eyeColor;
		}
		return Color.gray;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Color? GetScleraColorReplacement(FaceSide side)
	{
		foreach (var hediff in Pawn.health?.hediffSet?.hediffs ?? [])
		{
			if (side != FaceSide.None && hediff.part?.def?.label?.ContainsLowerInvariant(side) == false)
				continue;
			GeneEyeColor? color = hediff.def.GetModExtension<GeneEyeColor>();
			if (color is null || !color.scleraColor.HasValue)
				continue;
			return color.scleraColor.Value;
		}
		foreach (var gene in Pawn.genes.GenesListForReading)
		{
			if (!gene.Active)
				continue;
			GeneEyeColor? color = gene.def.GetModExtension<GeneEyeColor>();
			if (color is null)
				continue;
			return color.eyeColor;
		}
		return null;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Color GetScleraColor(FaceSide side) => side switch
	{
		FaceSide.Right or FaceSide.Left
			=> PersistentData.ScleraReplaceColors.ForSide(side)
			?? GetScleraColorReplacement(side)
			?? Color.white,
		FaceSide.None or _ => Color.white,
	};
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Color GetEyeColor(FaceSide side) => side switch
	{
		FaceSide.Right or FaceSide.Left
			=> PersistentData.EyeReplaceColors.ForSide(side)
			?? GetEyeColorInner(side),
		FaceSide.None or _ => Color.white,
	};
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PawnState GetPawnState()
	{
		if (TickCache<Comp_TSFace, PawnState>.TryGetCached(this, out var state))
			return state;

		var pawn = Pawn;
		if (pawn.Awake())
		{
			state = PawnState.Normal;
		}
		else if (pawn.Corpse?.IsDessicated() == true)
		{
			state = PawnState.Dessicated;
		}
		else if (pawn.Dead)
		{
			state = PawnState.Dead;
		}
		else
		{
			state = PawnState.Sleeping;
		}

		return TickCache<Comp_TSFace, PawnState>.Cache(this, state);
	}

	public void GenerateModifiersForSlot(SlotDef slot, SidedTRFaceParts sided, StringBuilder? reasons = null)
	{
		for (int i = 0; i < MAX_PART_MODS; i++)
		{
			var seed = Verse.Rand.Int;
			if (TryGetRandomModifierForSlot(slot, out var mod, reasons, PersistentData.AppliedModifiers.Keys)
				&& PersistentData.AppliedModifiers.TryAdd(mod, seed))
			{
				// TSFacesMod.Logger.Verbose($"applying part transforms...");
				mod.ApplyTo(sided, seed);
			}
			else
			{
				// TSFacesMod.Logger.Warning($"error getting part/applying transforms");
				break;
			}

			if (Verse.Rand.Value < PART_RAND_BREAK_CHANCE)
			{
				break;
			}
		}
	}

	public SidedTRFaceParts GeneratePartForSlot(SlotDef slot)
	{
		var reasons = new StringBuilder();
		var part = slot.GetRandomPartFor(Pawn, reasons);
		var tr = new TSTransform4();
		if (part is null)
		{
			TSFacesMod.Logger.Warning($"unable to generate part for slot '{slot}', pawn: {Pawn}, reasons:\n{reasons}", (Pawn, slot).GetHashCode());
			return new(FacePartDefOf.Empty, new());
		}

		var sided = new SidedTRFaceParts(part, tr);
		GenerateModifiersForSlot(slot, sided, reasons);
		// TSFacesMod.Logger.Verbose($"modifier res: {reasons}");
		return sided;
	}

	public bool TryGetRandomModifierForSlot(SlotDef slot, [NotNullWhen(true)] out FacePartModifierDef? def, StringBuilder? reasons = null, IEnumerable<FacePartModifierDef>? except = null)
	{
		// TSFacesMod.Logger.Verbose($"trying to get random mod for slot {slot} for {Pawn}\nforbidden={(string.Join(", ", except ?? []))}");
		def = FacesUtil.PartModifiers
			.Ensure(slot)
			.Except(except ?? [])
			.GetRandomFilterableFor(Pawn, reasons)
		;
		// if (def is not null)
		// {
		// 	TSFacesMod.Logger.Verbose($"got part {def}");
		// }
		return def is not null;
	}

	public FacePartWithTransform? GetPartForSlot(SlotDef slot, FaceSide side, bool rec = false)
	{
		if (!TryGetSidedPartForSlot(slot, out var slot_parts))
		{
			TSFacesMod.Logger.Warning($"unable to get sided parts for slot {slot} for {Pawn}");
			return default;
		}
		// get active part from selected for slot
		var def = slot_parts.FaceParts
			.Select(x => x.ForSide(side))
			.GetActiveFromFilters(Pawn)
		;
		if (def is null)
		{
			var random_new = GeneratePartForSlot(slot);
			if (PersistentData.PartsForSlots[slot].FaceParts.Any(x => x.Main == random_new.FaceParts.FirstOrDefault()?.Main))
			{
				TSFacesMod.Logger.Warning($"unable to generate new fitting part for slot '{slot}', pawn: {Pawn}", (Pawn, slot, random_new.FaceParts.FirstOrDefault().Main).GetHashCode());
				return null;
			}
			slot_parts.FaceParts.Add(random_new.FaceParts.First());
			if (!rec)
				return GetPartForSlot(slot, side, true);
			TSFacesMod.Logger.Warning($"unable to attain a part after a recursion for {Pawn}");
			return null;
		}
		return new(def, slot_parts.Transform.ForSide(side))
		{
			Slot = slot,
			Side = side,
		};
	}
	public bool TryGetPartForSlot(SlotDef slot, FaceSide side, out FacePartWithTransform part)
	{
		var found = GetPartForSlot(slot, side);
		part = found ?? new();
		return found.HasValue;
	}
	public bool TryGetSidedPartForSlot(SlotDef slot, [NotNullWhen(true)] out SidedTRFaceParts? parts)
	{
		// TODO: cache
		// get list of parts for slot or generate a needed part
		parts = PersistentData.PartsForSlots.Ensure(slot, GeneratePartForSlot);
		return parts is not null;
	}

	public override List<PawnRenderNode> CompRenderNodes()
	{
		return [new PawnRenderNode_TSFace(
			Pawn,
			this,
			Pawn.Drawer.renderer.renderTree
		)];
	}

	public void ResetAll()
	{
		PersistentData = new();
		RequestRegeneration();
	}

	public override void PostExposeData()
	{
		Scribe_Deep.Look(ref PersistentData, "tsfacedata");
	}

	public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
	{
		base.Notify_Killed(prevMap, dinfo);
		RequestRegeneration();
	}

	public override void CompTickInterval(int delta)
	{

		if (Pawn.Map != Find.CurrentMap)
			return;
		if ((CheckTimer += delta) < FacesSettings.Instance.CompUpdateInterval)
		{
			return;
		}
		else
		{
			CheckTimer = 0;
		}

		var state = GetPawnState();
		if (PreviousState.HasValue && PreviousState.Value != state)
		{
			// Log.Message($"regenerating face for {Pawn}");
			RequestRegeneration();
		}
		PreviousState = state;
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		var gizmos = base.CompGetGizmosExtra();
		if (TSUtil.ShowDebugGizmos)
		{
			gizmos = gizmos.Append(new Command_Action() {
				defaultLabel = "reset face",
				action = ResetAll,
			});
		}
		return gizmos;
	}
}