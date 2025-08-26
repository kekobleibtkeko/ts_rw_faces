using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TS_Faces.Comps;
using TS_Faces.Rendering;
using Verse;

namespace TS_Faces.Patches;

[HarmonyPatch(typeof(PawnRenderNode_Head), nameof(PawnRenderNode_Head.GraphicFor))]
public static class Patch_PawnHeadReplace
{
    public static void Postfix(Pawn pawn, ref Graphic __result)
    {
        if (__result is null
            || !pawn.TryGetComp(out Comp_TSFace face))
            return;

        if (face.CachedGraphic is not null)
            __result = face.CachedGraphic;
    }
}
