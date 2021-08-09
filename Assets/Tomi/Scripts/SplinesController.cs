﻿using System.Collections.Generic;
using System.Linq;
using Tomi.Scripts.Intersection;
using UnityEngine;

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
			Build(mergedBySamePoint);
			
			var intersections = FindConnectedSplines(mergedBySamePoint);
			foreach (var intersection in intersections)
			{
				for (var index = 0; index < intersection.Value.Count; index++)
				{
					var i = intersection.Value[index];
					i.ConnectionPoint.Adjust();
				}
			}
			
			foreach (var intersection in intersections.SelectMany(s=>s.Value))
			{
				SpawnDebugIntersection(intersection);
			}
			
			Debug.Log($"Summary {splineHandlers.Count } -> {mergedBySamePoint.Count}");
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
			
			p.transform.rotation = Quaternion.LookRotation(i.ConnectionPoint.CalculateDirection());
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

		private void Build(List<SplineHandler> splineHandlers)
		{
			foreach (var handler in splineHandlers)
			{
				handler.Build(transform, false);
			}
		}
	}
}