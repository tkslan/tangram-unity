using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tomi
{
	public class SplinesController : MonoBehaviour
	{
		private List<SplineHandler> _rejected;
		public void Initialize(List<SplineHandler> splineHandlers)
		{
			_rejected = new List<SplineHandler>();
			var mergedByName = MergeBySameName(splineHandlers);
		
			//Build(mergedByName); return;
			var stringCompare = new SimilarSplinesSearch();
			var result = stringCompare.FindAllWithSingleDistanceInName(mergedByName);
			var mergedBySimilarName = MergeBySimilarName(result, mergedByName.Count);
			Build(mergedBySimilarName);
		}

		private List<SplineHandler> MergeBySimilarName(Dictionary<SplineHandler, List<SplineHandler>> relatedHandlers, int countBefore)
		{
			var merged = new List<SplineHandler>();
			var resultCount = 0;
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
					resultCount += JoinSimilarSplinesGroup(sum, out var joinedSplines);
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
					resultCount += JoinSimilarSplinesGroup(byName.ToList(), out var joinedSplines);
					merged.AddRange(joinedSplines);
				}
				
			} while (resultCount > 0);

			Debug.Log("Raw count: "+merged.Count);
			var validOnly= merged.FindAll(f => f.IsValid).OrderBy(o => o.Name).ToList();
			Debug.Log("Valid count: "+merged.Count + "/" + splineHandlers.Count);
			return validOnly;
		}

		private int JoinSimilarSplinesGroup(List<SplineHandler> groupedHandler, out List<SplineHandler> joinedSplines)
		{
			var resultCount = 0;
			joinedSplines = new List<SplineHandler>();
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
						Debug.Log($"Merged {splineHandler.Name} -> {handler.Name}");
					}
					else
					{
						Debug.Log("Cant merge this right now");
					}
				}
						
				joinedSplines.Add(splineHandler);
			}

			Debug.Log($"Reduced: {groupedHandler.Count}-> {joinedSplines.Count} [{resultCount}]");
			return resultCount;
		}

		private void Build(List<SplineHandler> splineHandlers)
		{
			foreach (var handler in splineHandlers)
			{
				handler.Build(transform);
			}
		}
	}
}