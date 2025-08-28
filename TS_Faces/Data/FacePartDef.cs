using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TS_Faces.Data;

[DefOf]
public static class FacePartDefOf
{
    public static FacePartDef Empty = default!;

    public static FacePartDef DebugEye = default!;
    public static FacePartDef DebugIris = default!;
    public static FacePartDef DebugMouth = default!;

    static FacePartDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(FacePartDefOf));
    }
}

public class FacePartDef : Def, IGeneFiltered
{
    public enum SlotHint
    {
        None,
        Eye,
        Nose,
        Mouth,
        Brow,
        Ear,
        Iris,
        Highlight,
    }

    public SlotHint slotHint = SlotHint.None;
    public Gender gender = Gender.None;
    public FloatRange? beautyRange;

    public string? shader;
    public PartColor color = PartColor.None;
    public Color? customColor;
    public string graphicPath = "";
    public bool floating = false;

    public string? graphicPathSleep;
    public bool hideSleep = false;

    public string? graphicPathDead;
    public bool hideDead = false;

    public bool noMirror = false;
    public Vector2 drawSize = Vector2.one;

    public float commonality = 0.1f;

    public List<GeneDef> validGenes = [];
    public List<GeneDef> neededGenes = [];
    public List<GeneDef> disallowedGenes = [];

    public IEnumerable<GeneDef> ValidGenes => validGenes;
    public IEnumerable<GeneDef> NeededGenes => neededGenes;
    public IEnumerable<GeneDef> DisallowedGenes => disallowedGenes;
}
