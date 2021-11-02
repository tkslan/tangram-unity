using System;
using System.Collections.Generic;
using System.Linq;
using Tomi.Scripts.Intersection;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using Math = UnityEngine.ProBuilder.Math;

namespace Tomi
{
	public class SplinesController : MonoBehaviour
	{
		public void Initialize(List<SplineHandler> splineHandlers)
		{
			var mergedByName = MergeBySameName(splineHandlers);
		
			var stringCompare = new SimilarSplinesSearch();
			var result = stringCompare.FindAllWithSingleDistanceInName(mergedByName);
			var mergedBySimilarName = MergeBySimilarName(result, mergedByName.Count);
			
			MergeBySimilarPoints(mergedBySimilarName, out var mergedBySamePoint);
			
			var intersections = FindConnectedSplines(mergedBySamePoint);
			
			//AdjustConnectionPoints(intersections);
		
			Build(mergedBySamePoint, true);
			
			/*foreach (var intersection in intersections.SelectMany(s=>s.Value))
			{
				SpawnDebugIntersection(intersection);
			}
			CombineMeshes(intersections);
			*/
		
			//;
			foreach (var intersection in intersections)
			{
				ConnectMainRoadIntersections(intersection);
			}
		}

		private void ConnectMainRoadIntersections(KeyValuePair<SplineHandler, List<Intersection>> road)
		{
			foreach (var intersection in road.Value)
			{
				var point = intersection.ConnectionPoint;
				
				if (point.MinorRoad.IsValid && point.MainRoad.IsValid)
				{
					point.BevelMainRoadConnection();
					point.UpdateRoadConnectionsMesh();
				}
			}
		}

		private List<GameObject> processed = new List<GameObject>();
		
		
		private void CombineMeshes(Dictionary<SplineHandler,List<Intersection>> intersections)
		{
			var intersectionsAll = intersections.SelectMany(s => s.Value).ToList();
			for (int i = 0; i < intersectionsAll.Count; i++)
			{
				var intersection = intersectionsAll[i];

				if (intersection.CombinedMesh != null)
				{
					Debug.Log("Intersection combined : "+ intersection.ConnectionPoint.MainRoadName);
					continue;
				}

				var combine = new CombineInstance[intersection.MinorRoads.Count+1];
				var j = 0;
				for (j = 0; j < intersection.MinorRoads.Count; j++)
				{
					var minor = intersection.MinorRoads[j];
					combine[j].mesh = minor.Builder.Mesh;
					combine[j].transform = Matrix4x4.zero;
					minor.Builder.GameObject.SetActive(false);
				}

				combine[j].mesh = intersection.ConnectionPoint.MainRoad.Builder.Mesh;
				combine[j].transform = intersection.ConnectionPoint.MainRoad.Matrix;
				
				intersection.ConnectionPoint.MainRoad.Builder.UpdateMesh(new Mesh());
				intersection.ConnectionPoint.MainRoad.Builder.Mesh.CombineMeshes(combine, true, false);
				intersection.CombinedMesh = intersection.ConnectionPoint.MainRoad.Builder.Mesh;
				for (j = 0; j < intersection.MinorRoads.Count; j++)
				{
					var minor = intersection.MinorRoads[j];
					Destroy(minor.Builder.GameObject);
				}
			}
		}
		private Dictionary<SplineHandler, List<Intersection>> FindConnectedSplines(List<SplineHandler> splines)
		{
			var connected = new Dictionary<SplineHandler, List<Intersection>>();
			
			foreach (var mainRoad in splines)
			{
				foreach (var minorRoad in splines)
				{
					if (mainRoad != minorRoad && mainRoad.IsConnectedWith(minorRoad, out var point))
					{
						var intersection = new Intersection();
						if (!connected.ContainsKey(mainRoad))
						{
							intersection.ConnectionPoint = point;
							intersection.AddHandler(minorRoad);
							connected.Add(mainRoad, new List<Intersection>(){intersection});
						}
						else
						{
							intersection.ConnectionPoint = point;
							intersection.AddHandler(minorRoad);
							connected[mainRoad].Add(intersection);
						}
					}
				}
			}
			return connected;
		}

		public void SpawnDebugIntersection(Intersection i)
		{
			var mainRoad = i.ConnectionPoint.MainRoad;
			var point = i.ConnectionPoint.Point;
			point.y = 0;
			var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
			p.transform.SetParent(mainRoad.Builder.GameObject.transform,false);
			p.transform.localPosition = point;
			
			p.transform.rotation = Quaternion.LookRotation(i.ConnectionPoint.CalculateRoadDirection(i.ConnectionPoint.MinorRoad, i.ConnectionPoint.MinorRoadPointIndex));
			var scale = Vector3.one * mainRoad.PolylineOptions.Width;
			p.transform.localScale = scale +( Vector3.one * Mathf.Sqrt(scale.magnitude) / Mathf.PI);
			var ih = p.AddComponent<IntersectionHandler>();
			ih.Initialize(i);
		}
		
		private List<SplineHandler> MergeBySimilarName(Dictionary<SplineHandler, List<SplineHandler>> relatedHandlers, int countBefore)
		{
			var merged = new List<SplineHandler>();
			var sumOfAll = 0;
			
			foreach (var byName in relatedHandlers)
			{
					//Not found any child objects ,add parent to merged
					if (byName.Value.Count == 0)
					{
						merged.Add(byName.Key);
						sumOfAll++;
						continue;
					}
					
					var sum = new List<SplineHandler>() {byName.Key};
					sum.AddRange(byName.Value);
					sumOfAll += sum.Count;
					MergeBySimilarPoints(sum, out var joinedSplines);
					merged.AddRange(joinedSplines);
				
			} 
			Debug.Assert(sumOfAll == countBefore);
			return merged;
		}
		private List<SplineHandler> MergeBySameName(List<SplineHandler> splineHandlers)
		{
			var merged = new List<SplineHandler>(splineHandlers);
			var resultCount = 0;
			
			do
			{
				resultCount = 0;
				var groupByNames = merged.GroupBy(g => g.Name).ToList();
				merged.Clear();
				foreach (var byName in groupByNames)
				{
					resultCount += MergeBySimilarPoints(byName.ToList(), out var joinedSplines);
					merged.AddRange(joinedSplines);
				}
				
			} while (resultCount > 0);

			Debug.Log("Raw count: "+merged.Count);
			var validOnly= merged.FindAll(f => f.IsValid).OrderBy(o => o.Name).ToList();
			Debug.Log("Valid count: "+merged.Count + "/" + splineHandlers.Count);
			return validOnly;
		}

		private int MergeBySimilarPoints(List<SplineHandler> groupedHandler, out List<SplineHandler> joinedSplines)
		{
			var resultCount = 0;
		   joinedSplines = new List<SplineHandler>(groupedHandler);
			//TODO: Wee need do/while here 
			do
			{
				resultCount = 0;
				groupedHandler = new List<SplineHandler>(joinedSplines);
				joinedSplines.Clear();
				//Search in names ground and try to merge 
				foreach (var splineHandler in groupedHandler)
				{
					if (!splineHandler.IsValid) continue;

					var result = groupedHandler.FindAll(f => f.CanJoinWith(splineHandler));
					resultCount += result.Count;

					foreach (var handler in result)
					{
						if (splineHandler.Join(handler))
						{
							//Debug.Log($"Merged {splineHandler.Name} -> {handler.Name}");
						}
						else
						{
							Debug.Log("Cant merge this right now");
						}
					}

					joinedSplines.Add(splineHandler);

				}
			} while (resultCount > 0);


			Debug.Log($"Reduced: {groupedHandler.Count}-> {joinedSplines.Count} [{resultCount}]");
			return resultCount;
		}

		private void Build(List<SplineHandler> splineHandlers, bool withSplines =false)
		{
			foreach (var handler in splineHandlers)
			{
				handler.Build(transform, withSplines);
			}
		}
	}
}