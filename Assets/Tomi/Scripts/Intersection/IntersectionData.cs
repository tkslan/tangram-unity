using System.Collections.Generic;
using UnityEngine;

namespace Tomi.Intersection
{
	[System.Serializable]
	public struct IntersectionData
	{
		public ConnectionPoint ConnectionPoint;
		public List<SplineHandler> MinorRoads;
		public Mesh CombinedMesh;
		public void AddHandler(SplineHandler handler)
		{
			MinorRoads ??= new List<SplineHandler>();
			MinorRoads.Add(handler);
		}
	}
}