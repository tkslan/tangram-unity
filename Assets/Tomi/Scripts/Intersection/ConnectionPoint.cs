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
		
		private List<WingedEdge> _wingedEdges;
		private EdgeData _bevelPointEdgeData;
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
		
		public void UpdateRoadConnectionsMesh()
		{
			var main = MainRoad.Builder;
			var minor = MinorRoad.Builder;
			
			var newFace = main.EdgeGeometry.BevelAtPoint(Point);
			if (newFace != null)
			{
				//main.FaceGeometry.MergeFacesAt(Point);
				MainRoad.Builder.UpdatePbMesh();
			}
			var edgeToSnap = main.EdgeGeometry.ReturnClosestEdgeOnMesh(Point);
			var edgeData = MinorRoad.Builder.AdjustEndPosition(edgeToSnap);
			
			if (!edgeData.Valid)
				return;
			
			var angleSelected = 0f;
			var faceCenter = Vector2.zero;
			
			if (main.FaceGeometry.FindClosestFacesToPoint(edgeData.Center, out var orderedFaces))
			{
				var firstFace = orderedFaces.FirstOrDefault();
				faceCenter = firstFace.Value;
				var face = firstFace.Key;

				var faceWingedEdgesData = main.EdgeGeometry.GetFaceWingedEdges(face);
				if (faceWingedEdgesData.Count > 0)
				{
					var proxy = new EdgeProximitySelector(faceWingedEdgesData);
					edgeToSnap = proxy.CalculateProximity(edgeData);
				}
				else
				{
					Debug.Log("Dot problem on :" + MainRoad.Name);
				}
			}
			
			
			//mainEdgeService.ResizeEdge(edgeToSnap);
			SnapVerticles(main.PbMesh, edgeToSnap, minor.PbMesh, edgeData);
			Debug.Log($"Edge to snap [{edgeToSnap},{edgeData.Center}]:{edgeToSnap.Center} | DotSelected:{angleSelected}| Face:{faceCenter}");
		
		
			//Combine(pbMeshMain, pbMeshMinor);
		}
		
		private void SnapVerticles(ProBuilderMesh pbMeshMain, EdgeData edgeToSnap, ProBuilderMesh pbMeshMinor, EdgeData edgeData)
		{
			var mainVertA = pbMeshMain.VerticesInWorldSpace()[edgeToSnap.Edge.b];
			var mainVertB = pbMeshMain.VerticesInWorldSpace()[edgeToSnap.Edge.a];
			var vert = pbMeshMinor.GetVertices();
			vert[edgeData.Edge.a].position = mainVertA;
			vert[edgeData.Edge.b].position = mainVertB;
			pbMeshMinor.SetVertices(vert);
			pbMeshMinor.ToMesh();
			pbMeshMinor.Refresh();
		}
		private void Combine(ProBuilderMesh pbMeshMain, ProBuilderMesh pbMeshMinor)
		{
			//Combine to single mesh
			var mesh = CombineMeshes.Combine(new[] {pbMeshMain, pbMeshMinor}, pbMeshMain);
			var first = mesh[0];
			first.WeldVertices(mesh[0].faces.SelectMany(s => s.indexes), 0.2f);
			first.ToMesh(MeshTopology.Quads);
			first.Refresh();

			GameObject.DestroyImmediate(MinorRoad.Builder.MeshObject);
		}
		
		float SignedAngleBetween(Vector3 a, Vector3 b, Vector3 n){
			// angle in [0,180]
			float angle = Vector3.Angle(a,b);
			float sign = Mathf.Sign(Vector3.Dot(n,Vector3.Cross(a,b)));

			// angle in [-179,180]
			float signed_angle = angle * sign;

			// angle in [0,360] (not used but included here for completeness)
			//float angle360 =  (signed_angle + 180) % 360;

			return signed_angle;
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

	
		private float GetDot(EdgeData lhs, EdgeData rhs)
		{
			var dot = Vector3.Dot(lhs.Dir, rhs.Dir);
			Debug.Log($"DOT [{MainRoad.Name}]->[{MinorRoad.Name}]={dot}");
			return dot;
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