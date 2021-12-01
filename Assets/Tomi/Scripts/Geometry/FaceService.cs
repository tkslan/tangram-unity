using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace Tomi.Geometry
{
	public class FaceService : GeometryService
	{
		public FaceService(ProBuilderMesh mesh, List<Vector3> points) : base(mesh, points)
		{
		}

		public bool FindClosestFacesToPoint(Vector3 point, out List<KeyValuePair<Face, Vector3>> orderedFaces)
		{
			orderedFaces = new List<KeyValuePair<Face, Vector3>>();
			var orderedList = new Dictionary<Face, Vector3>();
			//Don't use on to small objects
			if (PbMesh.faceCount < 2)
				return false;

			foreach (var face in PbMesh.faces)
			{
				var edgesCenter = face.edges.Select(edge => Math.Average(PbMesh.positions, new[] {edge.a, edge.b}))
					.ToList();
				//calculate average position of face
				var pos = Math.Average(edgesCenter);
				orderedList.Add(face, pos);
			}

			if (orderedList.Count == 0)
				return false;

			orderedFaces.AddRange(orderedList.OrderBy(o => Vector3.Distance(o.Value, point)));
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
				//Use 2d vector, Y pos is different
				var v2d = new Vector2(pair.Value.x, pair.Value.z);
				if (Vector2.Distance(v2d, startPoint) < distance)
				{
					pbMesh.DeleteFace(prevFace);
					pbMesh.ToMesh();
					pbMesh.Refresh();
					count++;
					Debug.LogWarning($"Removed face:{pair.Key}");
				}

				//set next point
				startPoint = v2d;
				prevFace = nextFace;
			}

			pbMesh.WeldVertices(pbMesh.faces.SelectMany(s => s.indexes), distance);
			pbMesh.ToMesh(MeshTopology.Quads);
			pbMesh.Refresh();
			return count;
		}
	}
}