using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LudeonTK;
using RimWorld;
using TS_Faces.Data;
using TS_Faces.Rendering;
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

public class TRFacePart : IExposable
{
    public static TRFacePart Empty = new(){ PartDef = FacePartDefOf.Empty, Transform = new() };
    public FacePartDef PartDef = FacePartDefOf.Empty;
    public TSTransform4 Transform = new();

    public TRFacePart() { }
    public TRFacePart(FacePartDef def)
    {
        PartDef = def;
    }

    public TRFacePart CreateCopy()
    {
        TRFacePart copy = new()
        {
            PartDef = PartDef,
            Transform = Transform.CreateCopy()
        };
        return copy;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref PartDef, "part");
        Scribe_Deep.Look(ref Transform, "tr");
    }
}

public class TSFacePersistentData() : IExposable
{
    public List<HeadDef> Heads = [HeadDefOf.AverageLong];
    public TRFacePart Mouth = new(FacePartDefOf.DebugMouth);
    public TRFacePart Nose = new();
    public TRFacePart EyeL = new(FacePartDefOf.DebugEye);
    public TRFacePart? EyeR;
    public TRFacePart IrisL = new(FacePartDefOf.DebugIris);
    public TRFacePart? IrisR;
    public TRFacePart HighlightL = new();
    public TRFacePart? HighlightR;
    public TRFacePart BrowL = new();
    public TRFacePart? BrowR;
    public TRFacePart EarL = new();
    public TRFacePart? EarR;

    public Color? EyeLReplaceColor;
    public Color? EyeRReplaceColor;

    public Color? ScleraLReplaceColor;
    public Color? ScleraRReplaceColor;

    public TSFacePersistentData CreateCopy()
    {
        TSFacePersistentData copy = this.DirtyClone() ?? throw new Exception("unable to clone ts face data?");
        copy.Heads = [..Heads];
        copy.Mouth = Mouth.CreateCopy();
        copy.Nose = Nose.CreateCopy();
        copy.EyeL = EyeL.CreateCopy();
        copy.EyeR = EyeR?.CreateCopy();
        copy.IrisL = IrisL.CreateCopy();
        copy.IrisR = IrisR?.CreateCopy();
        copy.HighlightL = HighlightL.CreateCopy();
        copy.HighlightR = HighlightR?.CreateCopy();
        copy.BrowL = BrowL.CreateCopy();
        copy.BrowR = BrowR?.CreateCopy();
        copy.EarL = EarL.CreateCopy();
        copy.EarR = EarR?.CreateCopy();
        return copy;
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref Heads, "head");
        Scribe_Deep.Look(ref Mouth, "mouth");
        Scribe_Deep.Look(ref Nose, "Nose");
        Scribe_Deep.Look(ref EyeL, "eyeL");
        Scribe_Deep.Look(ref EyeR, "eyeR");
        Scribe_Deep.Look(ref IrisL, "irisL");
        Scribe_Deep.Look(ref IrisR, "irisR");
        Scribe_Deep.Look(ref HighlightL, "hlL");
        Scribe_Deep.Look(ref HighlightR, "hlR");
        Scribe_Deep.Look(ref BrowL, "browL");
        Scribe_Deep.Look(ref BrowR, "browR");
        Scribe_Deep.Look(ref EarL, "earL");
        Scribe_Deep.Look(ref EarR, "earR");
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
    public Pawn Pawn => parent as Pawn ?? throw new NullReferenceException("Comp_TSFace attached to non-pawn");

    public TSFacePersistentData PersistentData = new();

    public ReRenderState RenderState;
    public Graphic_Multi? CachedGraphic;

    public HeadDef GetActiveHeadDef() => PersistentData.Heads.GetFilteredDef(Pawn);
    public FaceLayout GetActiveFaceLayout() => GetActiveHeadDef().faceLayout;
    public bool IsRegenerationNeeded() => RenderState == ReRenderState.Needed;
    public void RequestRegeneration() => RenderState = ReRenderState.Needed;

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
        return Color.white;
    }
    public Color GetScleraColor(FaceSide side) => side switch
    {
        FaceSide.Left => PersistentData.ScleraLReplaceColor ?? Color.white,
        FaceSide.Right => PersistentData.ScleraLReplaceColor ?? Color.white,
        FaceSide.None or _ => Color.white,
    };
    public Color GetEyeColor(FaceSide side) => side switch
    {
        FaceSide.Left => PersistentData.EyeLReplaceColor ?? GetBaseEyeColor(),
        FaceSide.Right => PersistentData.EyeRReplaceColor ?? GetBaseEyeColor(),
        FaceSide.None or _=> Color.white,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PawnState GetPawnState()
    {
        var pawn = Pawn;
        if (pawn.Awake())
        {
            return PawnState.Normal;
        }
        else if (pawn.Corpse?.IsDessicated() == true)
        {
            return PawnState.Dessicated;
        }
        else if (pawn.Dead)
        {
            return PawnState.Dead;
        }
        else
        {
            return PawnState.Sleeping;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TRFacePart GetPartForSlot(FaceSlot slot) => slot switch
    {
        FaceSlot.EyeL => PersistentData.EyeL,
        FaceSlot.EyeR => PersistentData.EyeR ?? PersistentData.EyeL,
        FaceSlot.Nose => PersistentData.Nose,
        FaceSlot.Mouth => PersistentData.Mouth,
        FaceSlot.BrowL => PersistentData.BrowL,
        FaceSlot.BrowR => PersistentData.BrowR ?? PersistentData.BrowL,
        FaceSlot.EarL => PersistentData.EarL,
        FaceSlot.EarR => PersistentData.EarR ?? PersistentData.EarL,
        FaceSlot.IrisL => PersistentData.IrisL,
        FaceSlot.IrisR => PersistentData.IrisR ?? PersistentData.IrisL,
        FaceSlot.HighlightL => PersistentData.HighlightL,
        FaceSlot.HighlightR => PersistentData.HighlightR ?? PersistentData.HighlightL,
        FaceSlot.None or _ => TRFacePart.Empty,
    };

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

    public override void Notify_WearerDied()
    {
        base.Notify_WearerDied();
        RequestRegeneration();
    }
}