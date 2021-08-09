using System.Collections.Generic;
using System.Linq;
using Mapzen.Unity;
using Mapzen.VectorData;
using UnityEditor;
using UnityEngine;

namespace Tomi
{
	public class SplineMeshBuilder
	{
		private readonly SplineHandler _splineHandler;

		private const string UvGridMaterialGUID = "7f88baed2a0f94bb3a42f7bc45b5fcf8";
		private const string AsphaltGUID = "4c0619faa9f87524989ff9fa25868a75";
		public SplineMeshBuilder(SplineHandler splineHandler)
		{
			_splineHandler = splineHandler;
		}

		private Material LoadRoadMaterial()
		{
			var path = AssetDatabase.GUIDToAssetPath(AsphaltGUID);
			var material = AssetDatabase.LoadAssetAtPath<Material>(path);
			return material;
		}

		public Mesh Build(Transform parent)
		{
			var pointsData = _splineHandler.Points;
			var meshData = new MeshData();

			var v2List = new List<Vector2>();
			foreach (var vector3 in pointsData)
			{
				v2List.Add(new Vector2(vector3.x,vector3.z));
			}
			
			var builder = new PolylineBuilder(meshData, _splineHandler.PolylineOptions, Matrix4x4.identity, _splineHandler.Name);
			builder.OnBeginLineString();

			for (var index = 0; index < pointsData.Count; index++)
			{
				builder.OnPoint(new Point(pointsData[index].x, pointsData[index].z));
			}
	
			builder.OnEndLineString();

			var gameObject = new GameObject(_splineHandler.Name);
			gameObject.transform.parent = parent.transform;

			var mesh = new Mesh();
			GenerateMesh(gameObject, meshData, ref mesh);
			return mesh;
		}

		private void GenerateMesh(GameObject gameObject, MeshData meshData, ref Mesh mesh)
		{
			var meshBucket = meshData.Meshes[0];
			
			mesh.SetVertices(meshBucket.Vertices);
			mesh.SetUVs(0, meshBucket.UVs);
			mesh.subMeshCount = meshBucket.Submeshes.Count;
			for (int s = 0; s < meshBucket.Submeshes.Count; s++)
			{
				mesh.SetTriangles(meshBucket.Submeshes[s].Indices, s);
			}

			mesh.RecalculateNormals();
			
			var materials = meshBucket.Submeshes.Select(s => s.Material).ToArray();
			
			var meshFilterComponent = gameObject.AddComponent<MeshFilter>();
			var meshRendererComponent = gameObject.AddComponent<MeshRenderer>();
			meshRendererComponent.materials = materials;
			meshFilterComponent.mesh = mesh;
		}
		
	}
}