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
    public static HeadDef Default = default!;
    
    static HeadDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(HeadDefOf));
    }
}

public class HeadDef : Def, IGeneFiltered
{
    [NoTranslate]
    public string graphicPath = "";

    public Gender gender = Gender.None;

    public FloatRange? beautyRange;

    public FaceLayout faceLayout = new();

    public List<GeneDef> validGenes = [];
    public List<GeneDef> neededGenes = [];
    public List<GeneDef> disallowedGenes = [];

    public bool forGhoul = false;

    public IEnumerable<GeneDef> ValidGenes => validGenes;
    public IEnumerable<GeneDef> NeededGenes => neededGenes;
    public IEnumerable<GeneDef> DisallowedGenes => disallowedGenes;
}
