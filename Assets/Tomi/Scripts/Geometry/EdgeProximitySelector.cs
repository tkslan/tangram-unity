using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tomi.Geometry
{
	public class EdgeProximitySelector
	{
		private const float DotThreshold = 0.50f;
		struct Proximity
		{
			public float Distance;
			public float Dot;
			public Plane Plane;
			public EdgeData Edge;
		}

		private readonly List<EdgeData> _data;
		public EdgeProximitySelector(List<EdgeData> edges)
		{
			_data = new List<EdgeData>(edges);
		}

		public EdgeData CalculateProximity(EdgeData toEdge, float threshold = DotThreshold)
		{
			var proximities = new List<Proximity>();

			foreach (var data in _data)
			{
				var p = new Proximity()
				{
					Distance = Vector2.Distance(data.Center, toEdge.Center),
					Dot = Vector2.Dot(data.Dir, toEdge.Dir),
					Edge = data,
					Plane = new Plane(data.PosA, data.PosB, toEdge.Center),
				};

				proximities.Add(p);
			}

			var niceDot = new List<Proximity>();
			niceDot.AddRange(proximities.FindAll(f => Mathf.Abs(f.Dot) > threshold));
			
			if (niceDot.Count == 0)
			{
				return CalculateProximity(toEdge, threshold - 0.05f);
			}

			var bestEdge = niceDot.OrderBy(o => o.Distance).FirstOrDefault();
			
			return bestEdge.Edge;
		}
	}
}