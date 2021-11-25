using Tomi.Intersection;
using UnityEngine;

namespace Tomi.Scripts.Intersection
{
	public class IntersectionHandler: MonoBehaviour
	{
		[SerializeField] private IntersectionData _intersection;
		public void Initialize(IntersectionData intersection)
		{
			_intersection = intersection;
		}
	}
}