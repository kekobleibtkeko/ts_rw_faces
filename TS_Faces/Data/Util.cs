using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

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
    public static T GetFilteredDef<T>(this IEnumerable<T> list, Pawn pawn)
        where T : IGeneFiltered
    {
        T? curdef = default;
        float curfit = -1f;
        foreach (var def in list)
        {
            if (def.DisallowedGenes.Any(pawn.genes.HasActiveGene))
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

public enum PawnState
{
    Normal,
    Sleeping,
    Dead
}

public enum Side
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
    const float EPSILON = 0.05f;
    const float EPSILON_2 = EPSILON * 2;
    const float EPSILON_3 = EPSILON * 3;
    const float EPSILON_4 = EPSILON * 4;
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

    public static Side ToSide(this FaceSlot slot) => slot switch
    {
        FaceSlot.EyeL => Side.Left,
        FaceSlot.EyeR => Side.Right,
        FaceSlot.BrowL => Side.Left,
        FaceSlot.BrowR => Side.Right,
        FaceSlot.EarL => Side.Left,
        FaceSlot.EarR => Side.Right,
        FaceSlot.IrisL => Side.Left,
        FaceSlot.IrisR => Side.Right,
        FaceSlot.HighlightL => Side.Left,
        FaceSlot.HighlightR => Side.Right,
        FaceSlot.ScleraL => Side.Left,
        FaceSlot.ScleraR => Side.Right,
        FaceSlot.None
                    or FaceSlot.Nose
                    or FaceSlot.Mouth
                    or _
                    => Side.None,
    };

    public static FaceSlot OnSide(this FaceSlot slot, Side side) => slot switch
    {
        FaceSlot.EyeL or FaceSlot.EyeR => side == Side.Left ? FaceSlot.EyeL : FaceSlot.EyeR,
        FaceSlot.BrowL or FaceSlot.BrowR => side == Side.Left ? FaceSlot.BrowL : FaceSlot.BrowR,
        FaceSlot.EarL or FaceSlot.EarR => side == Side.Left ? FaceSlot.EarL : FaceSlot.EarR,
        FaceSlot.IrisL or FaceSlot.IrisR => side == Side.Left ? FaceSlot.IrisL : FaceSlot.IrisR,
        FaceSlot.HighlightL or FaceSlot.HighlightR => side == Side.Left ? FaceSlot.HighlightL : FaceSlot.HighlightR,
        FaceSlot.ScleraL or FaceSlot.ScleraR => side == Side.Left ? FaceSlot.ScleraL : FaceSlot.ScleraR,
        FaceSlot.Nose => FaceSlot.Nose,
        FaceSlot.Mouth => FaceSlot.Mouth,
        FaceSlot.None or _ => FaceSlot.None,
    };
}
