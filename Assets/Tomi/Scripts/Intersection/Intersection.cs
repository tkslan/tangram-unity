using System;
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
		
		public Vector3 CalculateRoadDirection(SplineHandler road, int index)
		{
			if (index > 0 && index < road.Points.Count-1)
				return road.Points[index + 1] - road.Points[index];
			if (index == 0)
				return road.Points[1] - road.Points[0];
			
			return road.Points[index].normalized;
		}

		public void AdjustPoints()
		{
			AdjustMinorRoadLength();
			UpdatePointsOnRoad(MinorRoad, MinorRoadPointIndex);
			UpdatePointsOnRoad(MainRoad, MainRoadPointIndex);
		}

		private void AdjustMinorRoadLength()
		{
			var mDir = CalculateRoadDirection(MinorRoad, MinorRoadPointIndex).normalized * 0.5f;
			MinorRoad.Points[MinorRoadPointIndex] = Point + mDir;
		}
		
		private void UpdatePointsOnRoad(SplineHandler road, int mIndex)
		{
			var dir = CalculateRoadDirection(road, mIndex).normalized * 0.5f;

			var mDist2 = Point - dir;
			var mDist = Point + dir;
			//road.Points.Insert(mIndex,Point + dir);
			if (mIndex > 0 && mIndex + 1 < road.Points.Count)
			{
				if (Vector3.Distance(road.Points[mIndex + 1], road.Points[mIndex]) < mDist2.magnitude)
					road.Points[mIndex - 1] = mDist2;
				else
					road.Points[mIndex] = mDist2;

				if (Vector3.Distance(road.Points[mIndex - 1], road.Points[mIndex]) < mDist.magnitude)
					road.Points[mIndex + 1] = mDist;
				else
					road.Points[mIndex] = mDist;
			}
		}
		public void Adjust()
		{
			//var dir = CalculateMinorDirection()*0.5f;
			var foundVertex = new List<Vector3>();
			var mesh = MinorRoad.Builder.Mesh;
			
			var p = Point;
			p.y = 0;
			var verts = mesh.vertices;
			if (MinorRoadPointIndex == 0)
			{
				verts[1] = 
				verts[4] = Vector3.one;
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