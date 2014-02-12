using System;
using System.Collections.Generic;

namespace Sketch
{
	internal class SubstrokeGuidComparer : IComparer<Substroke>
	{
		int IComparer<Substroke>.Compare(Substroke a, Substroke b)
		{
			return a.XmlAttrs.Id.ToString().CompareTo(b.XmlAttrs.Id.ToString());
		}
	}
}
