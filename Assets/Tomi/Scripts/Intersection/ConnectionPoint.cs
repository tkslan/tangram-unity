using System;
using System.Collections.Generic;
using System.Linq;
using Tomi.Geometry;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace Tomi.Intersection
{
	public class ConnectionPoint
	{
		public Vector2 Point { get; }
		public int MainRoadPointIndex { get; }
		public int MinorRoadPointIndex { get; }
		public SplineHandler MainRoad { get; }
		public SplineHandler MinorRoad { get; }
		
		public ConnectionPoint(Vector3 point, int mainIndex, int minorIndex, SplineHandler mainRoad, SplineHandler minorRoad)
		{
			Point = point.ToVector2();
			MainRoadPointIndex = mainIndex;
			MinorRoadPointIndex = minorIndex;
			MainRoad = mainRoad;
			MinorRoad = minorRoad;
		}

		public ConnectionPoint(Vector3 point)
		{
			Point = point.ToVector2();
		}

		private Face PrepareConnection()
		{
			var newFace = MainRoad.Builder.EdgeGeometry.BevelAtPoint(Point, out var sideEdges) ??
			              MainRoad.Builder.FaceGeometry.MergeFacesAt(Point);
			
			if (sideEdges.Count == 2)
			{
				var dist = Vector2.Distance(sideEdges[0].Center, sideEdges[1].Center);
				if (dist < 0.5f)
				{
					//Debug.LogError($"Narrow at {MainRoad.Name}");
					//newFace = MainRoad.Builder.FaceGeometry.MergeFacesAt(Point);
				}
			}
			MainRoad.Builder.UpdatePbMesh();
			return newFace;
		}
		
		public void Connect()
		{
			var main = MainRoad.Builder;
			var minor = MinorRoad.Builder;
			var updated = false;
			var newFace = PrepareConnection();

			var point = Point;
			
			//Move faces backwards
			minor.FaceGeometry.MoveBackClosestFace(point);
			//Update point to first face
			if (minor.FaceGeometry.ClosestFace(point, out var closest))
				point = closest.Key;

			//Search for closest edge from face center point
			var edgeToSnap = main.EdgeGeometry.ReturnClosestEdgeOnMesh(point);
			
			var edgeData = minor.AdjustEndPosition(edgeToSnap, 1f);

			if (newFace != null)
			{
				var foundEdge = main.GetBestEdgeFromFace(newFace, edgeData);
				if (foundEdge.Valid)
				{
					//edgeToSnap = foundEdge;
					updated = true;
				}
			}

			if (!edgeToSnap.Valid || !edgeData.Valid)
			{
				Debug.LogError($"Error on [({main.PbMesh.name})->({minor.PbMesh.name})");
				return;
			}
			
			main.EdgeGeometry.ResizeEdge(edgeToSnap);
			Debug.Log($"Snap[({main.PbMesh.name}){edgeData.Center}]->({minor.PbMesh.name}){edgeToSnap.Center} U:{updated}");
			
			SnapVertices(main.PbMesh, edgeToSnap, minor.PbMesh, edgeData);
			//Combine(main.PbMesh, minor.PbMesh);
		}
		
		private void SnapVertices(ProBuilderMesh pbMeshMain, EdgeData edgeToSnap, ProBuilderMesh pbMeshMinor, EdgeData edgeData)
		{
			var mainVertA = pbMeshMain.VerticesInWorldSpace()[edgeToSnap.Edge.b];
			var mainVertB = pbMeshMain.VerticesInWorldSpace()[edgeToSnap.Edge.a];
			var vert = pbMeshMinor.GetVertices();
			var dot = Vector2.Dot(edgeToSnap.Dir, edgeData.Dir) > 0;
			
			vert[edgeData.Edge.a].position = dot ? mainVertB : mainVertA;
			vert[edgeData.Edge.b].position = dot ? mainVertA : mainVertB;
			pbMeshMinor.WeldVertices(pbMeshMinor.faces.SelectMany(s => s.indexes), 0.3f);
			pbMeshMinor.SetVertices(vert);
			pbMeshMinor.ToMesh(MeshTopology.Quads);
			pbMeshMinor.Refresh();
		}
		public void Combine(ProBuilderMesh pbMeshMain, ProBuilderMesh pbMeshMinor)
		{
			//Combine to single mesh
			var mesh = CombineMeshes.Combine(new[] {pbMeshMain, pbMeshMinor}, pbMeshMain);
			var first = mesh[0];
			//first.WeldVertices(mesh[0].faces.SelectMany(s => s.indexes), 0.2f);
			first.ToMesh(MeshTopology.Quads);
			first.Refresh();
			MinorRoad.Invalidate();
			GameObject.DestroyImmediate(MinorRoad.Builder.MeshObject);
		}
		
		private void Merge(ProBuilderMesh mesh, int[] indexes)
		{
			if (indexes.Length > 1)
			{
				int newIndex = mesh.MergeVertices(indexes);

				var success = newIndex > -1;

				if (success)
					mesh.SetSelectedVertices(new int[] { newIndex });
			}
		}
	
		public Vector3 CalculateRoadDirection(SplineHandler road, int index)
		{
			var dir = road.Points[0] - road.Points[index];
			
			if (index > 0 && index < road.Points.Count - 1)
				dir = road.Points[index + 1] - road.Points[index];

			if (index == 0)
				dir = road.Points[1] - road.Points[0];
			
			return dir.normalized;
		}
	}

	public static class Vector3Extension
	{
		public static Vector2 ToVector2(this Vector3 vector3)
		{
			return new Vector2(vector3.x, vector3.z);
		}
	}
}