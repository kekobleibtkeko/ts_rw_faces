using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace TS_Faces.Data;

public class FaceLayoutPart
{
    public FaceSlot slot;
    public Vector3 pos;
    public float rotation;

    public FaceLayoutPart WithSlot(FaceSlot slot)
    {
        var copy = this.DirtyClone() ?? throw new Exception("unable to clone FaceLayoutPart?");
        copy.slot = slot;
        return copy;
    }

    public static FaceLayoutPart Mirrored(FaceLayoutPart part)
    {
        return new FaceLayoutPart
        {
            slot = part.slot.Mirror(),
            pos = part.pos.ScaledBy(new Vector3(-1, 1, 1)),
            rotation = 360 - part.rotation
        };
    }
}

public class FaceLayout
{
    public List<FaceLayoutPart> east = [];
    public List<FaceLayoutPart> south = [];
    public List<FaceLayoutPart>? north;
    public List<FaceLayoutPart>? west;

    public List<FaceLayoutPart> MirrorSide(IEnumerable<FaceLayoutPart> parts)
    {
        return [..parts.Select(FaceLayoutPart.Mirrored)];
    }

    public IEnumerable<FaceLayoutPart> ForRot(Rot4 rot) => rot.AsInt switch
    {
        0 => north ?? Enumerable.Empty<FaceLayoutPart>(),
        1 => east,
        2 => south,
        3 => (west ??= MirrorSide(east)),
        _ => south,
    };
}
