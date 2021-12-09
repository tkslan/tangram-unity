using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Tomi.Geometry
{
	public abstract class Geometry
	{
		protected readonly ProBuilderMesh PbMesh;
		public readonly List<Vector3> Points;
		public Geometry(ProBuilderMesh mesh, List<Vector3> points)
		{
			PbMesh = mesh;
			Points = points;
		}

		public abstract void Refresh();
	}
}