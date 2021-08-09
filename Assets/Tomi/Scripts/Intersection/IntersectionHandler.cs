using UnityEngine;

namespace Tomi.Scripts.Intersection
{
	public class IntersectionHandler: MonoBehaviour
	{
		[SerializeField] private Tomi.Intersection _intersection;
		public void Initialize(Tomi.Intersection intersection)
		{
			_intersection = intersection;
		}
	}
}