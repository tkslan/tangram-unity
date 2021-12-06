using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Tomi.Geometry
{
	public class Geometry
	{
		protected readonly ProBuilderMesh PbMesh;
		protected readonly List<Vector3> Points;
		private readonly Dictionary<Vector2, UnityEngine.ProBuilder.Face> _beveledPoints;

		public Geometry(ProBuilderMesh mesh, List<Vector3> points)
		{
			PbMesh = mesh;
			Points = points;
		}
	}
}