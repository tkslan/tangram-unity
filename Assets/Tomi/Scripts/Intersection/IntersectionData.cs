using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.iOS;
using UnityEngine;
using UnityEngine.Assertions;

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