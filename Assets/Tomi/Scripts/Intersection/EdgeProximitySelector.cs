using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tomi
{
	public class EdgeProximitySelector
	{
		struct Proximity
		{
			public float Distance;
			public float Dot;
			public Plane Plane;
			public EdgeData Edge;
		}

		private readonly List<EdgeData> _datas;
		private readonly float _dotThreshold = 0.65f;
		public EdgeProximitySelector(List<EdgeData> edges)
		{
			_datas = new List<EdgeData>(edges);
		}

		public EdgeData CalculateProximity(EdgeData toEdge)
		{
			var proximities = new List<Proximity>();

			foreach (var data in _datas)
			{
				var p = new Proximity()
				{
					Distance = Vector3.Distance(data.Center, toEdge.Center),
					Dot = Mathf.Abs(Vector3.Dot(data.Dir, toEdge.Dir)),
					Edge = data,
					Plane = new Plane(data.PosA, data.PosB, toEdge.Center),
				};

				proximities.Add(p);
			}

			var niceDot = new List<Proximity>();
			niceDot.AddRange(proximities.FindAll(f => f.Dot > _dotThreshold));
			if (niceDot.Count == 0)
			{
				throw new Exception("No points found");
				niceDot.Add(proximities.OrderByDescending(f => f.Dot).FirstOrDefault());
			}

			var bestEdge = niceDot.OrderBy(o => o.Distance).FirstOrDefault();
			
			return bestEdge.Edge;
		}
	}
}