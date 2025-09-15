using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace TS_Faces.Harmony
{
	public static class Patcher
	{
	    public static void Patch()
	    {
	        var harmony = new HarmonyLib.Harmony("tsuyao.faces");
	        List<PatchClassProcessor> patches = [
	            harmony.CreateClassProcessor(typeof(Patch_PawnHeadReplace)),
	        ];

	        foreach (var processor in patches)
	            processor.Patch();
	    }
	}
}
