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

public class HeadDef : Def, IPawnFilterable
{
	[NoTranslate]
	public string graphicPath = "";

	public FaceLayout faceLayout = new();
	public string? shader;
	public PartColor color = PartColor.Skin;

	public int commonality = 10;
	public List<PawnFilterEntry> filters = [];

	IEnumerable<PawnFilterEntry> IPawnFilterable.FilterEntries => filters;
	float IPawnFilterable.Commonality => commonality;

	public override void ResolveReferences()
	{
		base.ResolveReferences();
		faceLayout.ResolveReferences();
	}
}
