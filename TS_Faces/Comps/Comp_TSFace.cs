using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LudeonTK;
using RimWorld;
using TS_Faces.Data;
using TS_Faces.RenderNodes;
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
    public static TRFacePart Sclera = new() { PartDef = FacePartDefOf.Sclera, Transform = new() };
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
    public List<HeadDef> Heads = [HeadDefOf.Default];
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
    public TRFacePart? ScleraL;
    public TRFacePart? ScleraR;

    public Color? EyeLReplaceColor;
    public Color? EyeRReplaceColor;

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
        copy.ScleraL = ScleraL?.CreateCopy();
        copy.ScleraR = ScleraR?.CreateCopy();
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
        Scribe_Deep.Look(ref ScleraL, "sclL");
        Scribe_Deep.Look(ref ScleraR, "sclR");
        Scribe_Values.Look(ref EyeLReplaceColor, "eyeCL");
        Scribe_Values.Look(ref EyeRReplaceColor, "eyeCR");
    }
}

public class Comp_TSFace : ThingComp
{
    public const float TSHeadBaseLayer = 40f;
    private Pawn Pawn => parent as Pawn ?? throw new NullReferenceException("Comp_TSFace attached to non-pawn");

    public TSFacePersistentData PersistentData = new();

    public HeadDef GetActiveHeadDef() => PersistentData.Heads.GetFilteredDef(Pawn);
    public FaceLayout GetActiveFaceLayout() => GetActiveHeadDef().faceLayout;

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
    public Color GetScleraColor(Side side) => side switch
    {
        Side.Left => Color.white,
        Side.Right => Color.white,
        Side.None or _ => Color.white,
    };
    public Color GetEyeColor(Side side) => side switch
    {
        Side.Left => PersistentData.EyeLReplaceColor ?? GetBaseEyeColor(),
        Side.Right => PersistentData.EyeRReplaceColor ?? GetBaseEyeColor(),
        Side.None or _=> Color.white,
    };

    public PawnState GetPawnState()
    {
        var pawn = Pawn;
        if (pawn.Awake())
        {
            return PawnState.Normal;
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
        FaceSlot.ScleraL => PersistentData.ScleraL ?? TRFacePart.Sclera,
        FaceSlot.ScleraR => PersistentData.ScleraR ?? PersistentData.ScleraL ?? TRFacePart.Sclera,
        FaceSlot.None or _ => TRFacePart.Empty,
    };

    public override List<PawnRenderNode> CompRenderNodes()
    {
        var tree = Pawn.Drawer.renderer.renderTree;
        var root_node = new PawnRenderNode_TSFace(Pawn, this, tree);
        var sub_nodes = root_node.GetSubNodes();
        return [root_node, ..sub_nodes];
    }

    public override void PostExposeData()
    {
        Scribe_Deep.Look(ref PersistentData, "tsfacedata");
    }
}
