using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace TS_Faces.Patches;

[StaticConstructorOnStartup]
public static class Patcher
{
    static Patcher()
    {
        var harmony = new Harmony("tsuyao.faces");
        List<PatchClassProcessor> patches = [
            harmony.CreateClassProcessor(typeof(Patch_PawnHeadReplace)),
        ];

        foreach (var processor in patches)
            processor.Patch();
    }
}
