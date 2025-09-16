using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LudeonTK;
using RimWorld;
using TS_Faces.Data;
using TS_Faces.Rendering;
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
public class SidedTRFaceParts(FacePartDef part, TSTransform4 transform) : IExposable
{
	public List<SidedDef<FacePartDef>> FaceParts = [new(part)];
	public SidedDeep<TSTransform4> Transform = new(transform);
	public void ExposeData()
	{
		Scribe_Collections.Look(ref FaceParts, "parts");
		Scribe_Deep.Look(ref Transform, "tr");
	}
}

public class ExtraFacePart(FacePartDef def, SlotDef? anchor) : IExposable
{
	public SlotDef Anchor = anchor ?? SlotDefOf.None;
	public FaceSide AnchorSide;

	public SidedDef<FacePartDef> SidedDef = new(def);
	public TSTransform4 Transform = new();

	public void ExposeData()
	{
		Scribe_Defs.Look(ref Anchor, "anchor");
		Scribe_Values.Look(ref AnchorSide, "anchside");
		Scribe_Deep.Look(ref SidedDef, "def");
		Scribe_Deep.Look(ref Transform, "tr");
	}
}

public readonly struct FacePartWithTransform
{
	public readonly FacePartDef Def;
	public readonly TSTransform4 Transform;

	public FacePartWithTransform(FacePartDef def, TSTransform4 transform) : this()
	{
		Def = def;
		Transform = transform;
	}
}

public class TSFacePersistentData() : IExposable
{
	public List<HeadDef> Heads = [HeadDefOf.AverageLong];
	public Dictionary<SlotDef, SidedTRFaceParts> PartsForSlots = [];
	public List<ExtraFacePart> ExtraParts = [];

	public Color? EyeLReplaceColor;
	public Color? EyeRReplaceColor;

	public Color? ScleraLReplaceColor;
	public Color? ScleraRReplaceColor;

	public TSFacePersistentData CreateCopy()
	{
		TSFacePersistentData copy = this.DirtyClone() ?? throw new Exception("unable to clone ts face data?");

		return copy;
	}

	public void ExposeData()
	{
		TSSaveUtility.LookDict(ref PartsForSlots, "mainparts");
		Scribe_Collections.Look(ref ExtraParts, "extraparts");
		Scribe_Values.Look(ref EyeLReplaceColor, "eyeCL");
		Scribe_Values.Look(ref EyeRReplaceColor, "eyeCR");
		Scribe_Values.Look(ref ScleraLReplaceColor, "sclCL");
		Scribe_Values.Look(ref ScleraRReplaceColor, "sclCR");
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

	public const float TSHeadBaseLayer = 30f;
	public Pawn Pawn => parent as Pawn ?? throw new Exception("Comp_TSFace attached to non-pawn");

	// Saved variables
	public TSFacePersistentData PersistentData = new();

	// Non-Saved variables
	public ReRenderState RenderState;
	public Graphic_Multi? CachedGraphic;
	public Color? OverriddenColor;

	public HeadDef GetActiveHeadDef()
	{
		if (TickCache<Comp_TSFace, HeadDef>.TryGetCached(this, out var def))
			return def;
		def = PersistentData.Heads.GetActiveFromFilters(Pawn) ?? HeadDefOf.AverageLong;
		TickCache<Comp_TSFace, HeadDef>.Cache(this, def);
		return def;
	}
	public FaceLayout GetActiveFaceLayout() => GetActiveHeadDef().faceLayout;
	public bool IsRegenerationNeeded() => RenderState == ReRenderState.Needed;
	public void RequestRegeneration() => RenderState = ReRenderState.Needed;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Color GetBaseEyeColor()
	{
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
	public Color GetScleraColor(FaceSide side) => side switch
	{
		FaceSide.Left => PersistentData.ScleraLReplaceColor ?? Color.white,
		FaceSide.Right => PersistentData.ScleraLReplaceColor ?? Color.white,
		FaceSide.None or _ => Color.white,
	};
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Color GetEyeColor(FaceSide side) => side switch
	{
		FaceSide.Left => PersistentData.EyeLReplaceColor ?? GetBaseEyeColor(),
		FaceSide.Right => PersistentData.EyeRReplaceColor ?? GetBaseEyeColor(),
		FaceSide.None or _ => Color.white,
	};

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

		TickCache<Comp_TSFace, PawnState>.Cache(this, state);
		return state;
	}

	public SidedTRFaceParts GenerateForSlot(SlotDef slot)
	{
		var part = slot.GetRandomPartFor(Pawn);
		if (part is null)
			TSFacesMod.Logger.Warning($"unable to generate part for slot '{slot}', pawn: {Pawn}", (Pawn, slot).GetHashCode());

		return new(part ?? FacePartDefOf.Empty, new());
	}

	public FacePartWithTransform? GetPartForSlot(SlotDef slot, FaceSide side)
	{
		var slot_parts = PersistentData.PartsForSlots.Ensure(slot, GenerateForSlot);
		var def = slot_parts.FaceParts
			.Select(x => x.ForSide(side))
			.GetActiveFromFilters(Pawn)
		;
		if (def is null)
			return null;
		return new(def, slot_parts.Transform.ForSide(side));
	}
	public bool TryGetPartForSlot(SlotDef slot, FaceSide side, out FacePartWithTransform part)
	{
		var found = GetPartForSlot(slot, side);
		part = found ?? new();
		return found.HasValue;
	}

	public override List<PawnRenderNode> CompRenderNodes()
	{
		return [new PawnRenderNode_TSFace(
			Pawn,
			this,
			Pawn.Drawer.renderer.renderTree
		)];
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
	}
}