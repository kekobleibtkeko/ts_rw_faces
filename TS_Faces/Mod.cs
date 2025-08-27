using Verse;

namespace TS_Faces;

public class TSFacesMod : Mod
{
    public TSFacesMod(ModContentPack content) : base(content)
    {
        Log.Message("FACES LOADING >:3");
        foreach (var b in content.assetBundles.loadedAssetBundles)
        {
            var names = string.Join(", ", b.GetAllAssetNames());
            Log.Message($"loaded asset names in bundle '{b.name}': '{names}'");
        }
    }
}
