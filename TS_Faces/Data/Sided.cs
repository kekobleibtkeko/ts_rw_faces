using System;
using System.ComponentModel;
using RimWorld;
using TS_Faces.Util;
using TS_Lib.Transforms;
using TS_Lib.Util;
using Verse;

namespace TS_Faces.Data;

public enum SidedSide
{
	Main,
	Other,
}

public abstract class Sided<T>(T main) : IExposable
{
	public T Main = main;
	public T? Other;

	public virtual T ForSide(SidedSide side) => side switch
	{
		SidedSide.Other => Other ?? Main,
		SidedSide.Main or _ => Main,
	};
	public virtual T ForSide(FaceSide side) => ForSide(side switch
	{
		FaceSide.Right => SidedSide.Other,
		FaceSide.None or FaceSide.Left or _ => SidedSide.Main,
	});

	public abstract void ExposeData();
}

public class SidedDef<T>(T main) : Sided<T>(main)
	where
		T : Def, new()
{
	public override void ExposeData() => this.SaveDef();
}

public class SidedDeep<T>(T main) : Sided<T>(main)
	where
		T: IExposable
{
	public override void ExposeData() => this.SaveDeep();
}

public class SidedMirror<T>(T main) : SidedDeep<T>(main)
	where
		T: IExposable, IMirrorable<T>
{
	public override T ForSide(SidedSide side) => side switch
	{
		SidedSide.Other => Other ?? Main.Mirror(),
		SidedSide.Main or _ => Main,
	};
}

public class SidedValue<T>(T main) : Sided<T>(main), ICreateCopy<SidedValue<T>>
{
	public SidedValue<T> CreateCopy() => this.CreateCopyValue<T, SidedValue<T>>();

	public override void ExposeData() => this.SaveValue();
}

public static class SidedExtensions
{
	public static void SaveDeep<T>(this Sided<T> obj)
		where
			T : IExposable
	{
		Scribe_Deep.Look(ref obj.Main!, "main");
		Scribe_Deep.Look(ref obj.Other, "other");
	}
	public static void SaveDef<T>(this Sided<T> obj)
		where
			T : Def, new()
	{
		Scribe_Defs.Look(ref obj.Main!, "main");
		Scribe_Defs.Look(ref obj.Other, "other");
	}

	public static void SaveValue<T>(this Sided<T> sided)
	{
		Scribe_Values.Look(ref sided.Main!, "main");
		Scribe_Values.Look(ref sided.Other, "main");
	}

	public static TSide CreateCopy<T, TSide>(this TSide sided)
		where
			T : ICreateCopy<T>
		where
			TSide : Sided<T>
	{
		var copy = sided.DirtyClone() ?? throw new System.Exception("unable to clone sided?");
		if (sided.Main is not null)
			copy.Main = sided.Main.CreateCopy();
		if (sided.Other is not null)
			copy.Other = sided.Other.CreateCopy();
		return copy;
	}

	public static TSide CreateCopyValue<T, TSide>(this TSide sided)
		where
			TSide : Sided<T>
	{
		var copy = sided.DirtyClone() ?? throw new System.Exception("unable to clone sided?");
		copy.Main = sided.Main;
		copy.Other = sided.Other;
		return copy;
	}
}