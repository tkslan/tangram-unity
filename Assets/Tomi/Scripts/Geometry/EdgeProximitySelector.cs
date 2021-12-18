using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tomi.Geometry
{
	public class EdgeProximitySelector
	{
		private const float DotThreshold = 1f;
		struct Proximity
		{
			public float Distance;
			public float Dot;
			public Plane Plane;
			public EdgeData Edge;
			public float Score;
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
					Dot = Mathf.Abs(Vector2.Dot(data.Dir, toEdge.Dir)),
					Edge = data,
					Plane = new Plane(data.PosA, data.PosB, toEdge.Center),
				};
				p.Score = p.Dot + (1 - p.Distance);
				proximities.Add(p);
			}

			var niceDot = new List<Proximity>();
			var avDist = proximities.Sum(s => s.Distance) / proximities.Count;
			niceDot.AddRange(proximities.FindAll(w=> w.Distance <= avDist && !w.Edge.Internal));
			
			if (niceDot.Count == 0)
			{
				if (threshold <= 0.2f)
					return new EdgeData();
				
				return CalculateProximity(toEdge, threshold - 0.15f);
			}

			var scoreOrder = niceDot.OrderByDescending(o => o.Score).ToArray();
			var bestEdge = scoreOrder.FirstOrDefault();
			return bestEdge.Edge;
		}
	}
}