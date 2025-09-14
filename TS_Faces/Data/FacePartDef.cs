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
    public static FacePartDef DebugNose = default!;

    static FacePartDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(FacePartDefOf));
    }
}

public class FacePartDef : Def, IPartFiltered
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

    public SlotDef slot = SlotDefOf.Eye;

    public string? shader;
    public PartColor color = PartColor.None;
    public Color? customColor;
    public bool floating = false;
    public string graphicPath = "";
    public string? graphicPathMissing;

    public string? graphicPathSleep;
    public bool hideSleep = false;

    public string? graphicPathDead;
    public bool hideDead = false;

    public bool noMirror = false;
    public Vector2 drawSize = Vector2.one;
    public Vector2 offset = Vector2.zero;

    public float commonality = 0.1f;
    public List<PartFilterEntry> filters = [];

    IEnumerable<PartFilterEntry> IPartFiltered.FilterEntries => filters;
    float IPartFiltered.Commonality => commonality;
}
