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
			var mergedByName = MergeByName(splineHandlers);
			Build(mergedByName);
		}

		private List<SplineHandler> MergeByName(List<SplineHandler> splineHandlers)
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
					var list = byName.ToList();
					//Search in names ground and try to merge 
					foreach (var splineHandler in list)
					{
						if (!splineHandler.IsValid) continue;

						var result = list.FindAll(f => f.CanJoinWith(splineHandler));
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
						
						merged.Add(splineHandler);
					}
				}
				
			} while (resultCount > 0);

			Debug.Log("Raw count: "+merged.Count);
			var validOnly= merged.FindAll(f => f.IsValid).OrderBy(o => o.Name).ToList();
			Debug.Log("Valid count: "+merged.Count + "/" + splineHandlers.Count);
			return validOnly;
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