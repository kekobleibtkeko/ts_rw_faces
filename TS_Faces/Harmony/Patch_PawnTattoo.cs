using HarmonyLib;
using Verse;

namespace TS_Faces.Harmony;

[HarmonyPatch(typeof(PawnRenderNode_Tattoo_Head), nameof(PawnRenderNode_Tattoo_Head.GraphicFor))]
public static class Patch_RemovePawnHeadTattoo
{
	public static void Postfix(Pawn pawn, ref Graphic __result)
	{
		__result = null!;
	}
}