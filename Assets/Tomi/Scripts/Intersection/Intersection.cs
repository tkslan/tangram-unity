using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.iOS;
using UnityEngine;
using UnityEngine.Assertions;

namespace Tomi
{
	[System.Serializable]
	public struct Intersection
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