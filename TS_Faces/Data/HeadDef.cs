using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TS_Faces.Data;

[DefOf]    
public static class HeadDefOf
{
    public static HeadDef AverageLong = default!;
    
    static HeadDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(HeadDefOf));
    }
}

public class HeadDef : Def, IPartFilter
{
    [NoTranslate]
    public string graphicPath = "";

    public FaceLayout faceLayout = new();
    public string? shader;
    public PartColor color = PartColor.Skin;

    public float commonality = 0.1f;
    public List<PartFilterEntry> filters = [];

    IEnumerable<PartFilterEntry> IPartFilter.FilterEntries => filters;
    float IPartFilter.Commonality => commonality;
}
