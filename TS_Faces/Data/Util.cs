using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TS_Faces.Comps;
using UnityEngine;
using Verse;
using static Verse.PawnRenderNodeProperties;

namespace TS_Faces.Data;

public interface IGeneFiltered
{
    IEnumerable<GeneDef> ValidGenes { get; }
    IEnumerable<GeneDef> NeededGenes { get; }
    IEnumerable<GeneDef> DisallowedGenes { get; }
}
public static class GeneFilteredExtensions
{
    public const int NEEDED_WEIGHT = 3;
    public const int VALID_WEIGHT = 2;
    public static T GetFilteredDef<T>(this IEnumerable<T> list, Pawn pawn, Predicate<T>? customPredicate = null)
        where T : IGeneFiltered
    {
        T? curdef = default;
        float curfit = -1f;
        foreach (var def in list)
        {
            if (def.DisallowedGenes.Any(pawn.genes.HasActiveGene) || (customPredicate?.Invoke(def) == false))
                continue;

            if (def.NeededGenes.Any())
            {
                if (curfit < (def.NeededGenes.Count() * NEEDED_WEIGHT)
                    && def.NeededGenes.All(pawn.genes.HasActiveGene))
                {
                    curdef = def;
                    curfit = def.NeededGenes.Count() * NEEDED_WEIGHT;
                }
            }
            else if (def.ValidGenes.Any())
            {
                var valid = def.ValidGenes.Count(pawn.genes.HasActiveGene) * VALID_WEIGHT;
                if (curfit < valid)
                {
                    curdef = def;
                    curfit = valid;
                }
            }
            else if (curfit < 0f && def.NeededGenes.EnumerableNullOrEmpty())
            {
                curdef = def;
                curfit = 0f;
            }
        }

        return curdef ?? list.First();
    }
}

public enum PartColor
{
    None,
    Eye,
    Sclera,
    Skin,
    Hair,
}

public enum PawnState
{
    Normal,
    Sleeping,
    Dead,
    Dessicated
}

public enum FaceSide
{
    None,
    Left,
    Right,
}

public enum FaceSlot
{
    None,
    EyeL,
    EyeR,
    Nose,
    Mouth,
    BrowL,
    BrowR,
    EarL,
    EarR,
    IrisL,
    IrisR,
    HighlightL,
    HighlightR,
    ScleraL,
    ScleraR,
}

public static class FaceSlotExtensions
{
    public const float EPSILON = 0.0004f;
    public const float EPSILON_2 = EPSILON * 2;
    public const float EPSILON_3 = EPSILON * 3;
    public const float EPSILON_4 = EPSILON * 4;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static FaceSide Mirror(this FaceSide side) => side switch
	{
		FaceSide.Left => FaceSide.Right,
		FaceSide.Right => FaceSide.Left,
		FaceSide.None or _ => FaceSide.None,
	};

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FaceSlot Mirror(this FaceSlot slot) => slot switch
    {
        FaceSlot.EyeL => FaceSlot.EyeR,
        FaceSlot.EyeR => FaceSlot.EyeL,
        FaceSlot.Nose => FaceSlot.Nose,
        FaceSlot.Mouth => FaceSlot.Mouth,
        FaceSlot.BrowL => FaceSlot.BrowR,
        FaceSlot.BrowR => FaceSlot.BrowL,
        FaceSlot.EarL => FaceSlot.EarR,
        FaceSlot.EarR => FaceSlot.EarL,
        FaceSlot.IrisL => FaceSlot.IrisR,
        FaceSlot.IrisR => FaceSlot.IrisL,
        FaceSlot.HighlightL => FaceSlot.HighlightR,
        FaceSlot.HighlightR => FaceSlot.HighlightL,
        FaceSlot.ScleraL => FaceSlot.ScleraR,
        FaceSlot.ScleraR => FaceSlot.ScleraL,
        FaceSlot.None or _ => FaceSlot.None,
    };

    [Obsolete]
    public static float ToLayerOffset(this FaceSlot slot) => slot switch
    {
        FaceSlot.EyeL or FaceSlot.EyeR => EPSILON_4,
        FaceSlot.Nose => EPSILON,
        FaceSlot.Mouth => EPSILON,
        FaceSlot.BrowL or FaceSlot.BrowR => EPSILON,
        FaceSlot.EarL or FaceSlot.EarR => EPSILON,
        FaceSlot.IrisL or FaceSlot.IrisR => EPSILON_2,
        FaceSlot.HighlightL or FaceSlot.HighlightR => EPSILON_3,
        FaceSlot.ScleraL or FaceSlot.ScleraR => EPSILON,
        FaceSlot.None or _ => 0f,
    };

	[Obsolete]
    public static FaceSide ToSide(this FaceSlot slot) => slot switch
	{
		FaceSlot.EyeL => FaceSide.Left,
		FaceSlot.EyeR => FaceSide.Right,
		FaceSlot.BrowL => FaceSide.Left,
		FaceSlot.BrowR => FaceSide.Right,
		FaceSlot.EarL => FaceSide.Left,
		FaceSlot.EarR => FaceSide.Right,
		FaceSlot.IrisL => FaceSide.Left,
		FaceSlot.IrisR => FaceSide.Right,
		FaceSlot.HighlightL => FaceSide.Left,
		FaceSlot.HighlightR => FaceSide.Right,
		FaceSlot.ScleraL => FaceSide.Left,
		FaceSlot.ScleraR => FaceSide.Right,
		FaceSlot.None
					or FaceSlot.Nose
					or FaceSlot.Mouth
					or _
					=> FaceSide.None,
	};

    [Obsolete]
    public static FaceSlot OnSide(this FaceSlot slot, FaceSide side) => slot switch
    {
        FaceSlot.EyeL or FaceSlot.EyeR => side == FaceSide.Left ? FaceSlot.EyeL : FaceSlot.EyeR,
        FaceSlot.BrowL or FaceSlot.BrowR => side == FaceSide.Left ? FaceSlot.BrowL : FaceSlot.BrowR,
        FaceSlot.EarL or FaceSlot.EarR => side == FaceSide.Left ? FaceSlot.EarL : FaceSlot.EarR,
        FaceSlot.IrisL or FaceSlot.IrisR => side == FaceSide.Left ? FaceSlot.IrisL : FaceSlot.IrisR,
        FaceSlot.HighlightL or FaceSlot.HighlightR => side == FaceSide.Left ? FaceSlot.HighlightL : FaceSlot.HighlightR,
        FaceSlot.ScleraL or FaceSlot.ScleraR => side == FaceSide.Left ? FaceSlot.ScleraL : FaceSlot.ScleraR,
        FaceSlot.Nose => FaceSlot.Nose,
        FaceSlot.Mouth => FaceSlot.Mouth,
        FaceSlot.None or _ => FaceSlot.None,
    };
}
