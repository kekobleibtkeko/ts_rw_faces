using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
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

        if (face.IsRegenerationNeeded())
            FaceRenderer.RegenerateFaces(face);

        if (face.CachedGraphic is not null)
        {
            var statue_color = pawn.Drawer.renderer.StatueColor;
            if (statue_color.HasValue)
            {
                FaceRenderer.MakeStatueColored(face, statue_color.Value);
            }
            //Log.Message($"aaaaa: {pawn}({pawn.thingIDNumber})  statue color: {statue_color}");
            __result = face.CachedGraphic;
        }
    }
}
