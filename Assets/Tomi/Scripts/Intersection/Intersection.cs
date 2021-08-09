using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Tomi
{
	[System.Serializable]
	public struct ConnectionPoint
	{
		public Vector3 Point;
		
		public int MainRoadPointIndex;
		public int MinorRoadPointIndex;
		public SplineHandler MainRoad;
		public SplineHandler MinorRoad;
		public string MainRoadName;
		public string MinorRoadName;
		public ConnectionPoint(Vector3 point, int mainIndex, int minorIndex, SplineHandler mainRoad, SplineHandler minorRoad)
		{
			Point = point;
			MainRoadPointIndex = mainIndex;
			MinorRoadPointIndex = minorIndex;
			MainRoad = mainRoad;
			MinorRoad = minorRoad;
			MainRoadName = mainRoad.Name;
			MinorRoadName = minorRoad.Name;
		}

		public Vector3 CalculateDirection()
		{
			//Calculate direction 
			return MinorRoadPointIndex == 0
				? MinorRoad.Points[1] - MinorRoad.Points[0]
				: MinorRoad.Points[MinorRoadPointIndex - 1] - MinorRoad.Points[MinorRoadPointIndex];
		}
		
		public void Adjust()
		{
		
			var dir = CalculateDirection()*0.5f;
			var foundVertex = new List<Vector3>();
			var mesh = MinorRoad.Builder.Mesh;

			var p = Point;
			p.y = 0;
			var verts = mesh.vertices;
			if (MinorRoadPointIndex == 0)
			{
				verts[1] = 
				verts[4] = Point;
			}
			
			mesh.SetVertices(verts);
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
		}
	}
	
	[System.Serializable]
	public struct Intersection
	{
		public ConnectionPoint ConnectionPoint;
		public List<SplineHandler> MinorRoads;

		public void AddHandler(SplineHandler handler)
		{
			MinorRoads ??= new List<SplineHandler>();
			MinorRoads.Add(handler);
		}
	}
}