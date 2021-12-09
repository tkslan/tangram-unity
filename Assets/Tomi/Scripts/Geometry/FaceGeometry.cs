using System.Collections.Generic;
using System.Linq;
using Tomi.Intersection;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace Tomi.Geometry
{
	public class FaceGeometry : Geometry
	{
		public FaceGeometry(ProBuilderMesh mesh, List<Vector3> points) : base(mesh, points) { }
		public override void Refresh() { }
		public bool FindClosestFacesToPoint(Vector2 point, out List<KeyValuePair<Face, Vector2>> orderedFaces)
		{
			orderedFaces = new List<KeyValuePair<Face, Vector2>>();
			var orderedList = new Dictionary<Face, Vector2>();
			//Don't use on to small objects
			if (PbMesh.faceCount < 2)
				return false;

			foreach (var face in PbMesh.faces)
			{
				var edgesCenter = face.edges.Select(edge => Math.Average(PbMesh.positions, new[] {edge.a, edge.b}))
					.ToList();
				//calculate average position of face
				var pos = Math.Average(edgesCenter);
				orderedList.Add(face, pos.ToVector2());
			}

			if (orderedList.Count == 0)
				return false;

			orderedFaces.AddRange(orderedList.OrderBy(o => Vector2.Distance(o.Value, point)));
			return true;
		}

		private int RemoveToTightFaces(ProBuilderMesh pbMesh, Vector2 startPoint, float distance = 0.5f)
		{
			if (!FindClosestFacesToPoint(startPoint, out var facesToCheck))
				return -1;

			var count = 0;
			var prevFace = facesToCheck[0].Key;
			foreach (var pair in facesToCheck)
			{
				var nextFace = pair.Key;
				if (nextFace == prevFace) continue;
			
				if (Vector2.Distance(pair.Value, startPoint) < distance)
				{
					pbMesh.DeleteFace(prevFace);
					pbMesh.ToMesh();
					pbMesh.Refresh();
					count++;
					Debug.LogWarning($"Removed face:{pair.Key}");
				}

				//set next point
				startPoint = pair.Value;
				prevFace = nextFace;
			}

			pbMesh.WeldVertices(pbMesh.faces.SelectMany(s => s.indexes), distance);
			pbMesh.ToMesh(MeshTopology.Quads);
			pbMesh.Refresh();
			return count;
		}
		
		public void MergeFacesAt(Vector2 point)
		{
			FindClosestFacesToPoint(point, out var faces);
			var first = faces[0];
			var second = faces[1];
			var newFace = MergeElements.Merge(PbMesh, new[] {first.Key, second.Key});
		}
	}
}