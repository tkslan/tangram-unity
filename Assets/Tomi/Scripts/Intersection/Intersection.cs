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

			var mDistBefore = Point - dir;
			var mDistAfter = Point + dir;
			var originalPoint = road.Points[mIndex];
			
			if (mIndex > 0 && mIndex + 1 < road.Points.Count)
			{
				var isPreviousPointIsCloseEnough =
					Vector3.Distance(road.Points[mIndex - 1], originalPoint) < mDistBefore.magnitude;
				
				if (isPreviousPointIsCloseEnough)
					road.Points[mIndex - 1] = mDistBefore;
				else
					road.Points.Insert(mIndex, mDistBefore); 
				
				var isNextPointIsCloseEnough =
					Vector3.Distance(road.Points[mIndex + 1], originalPoint) < mDistAfter.magnitude;
				
				if (isNextPointIsCloseEnough)
					road.Points[mIndex + 1] = mDistAfter;
				else
					road.Points.Insert(mIndex, mDistAfter);
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
		public Mesh CombinedMesh;
		public void AddHandler(SplineHandler handler)
		{
			MinorRoads ??= new List<SplineHandler>();
			MinorRoads.Add(handler);
		}
	}
}