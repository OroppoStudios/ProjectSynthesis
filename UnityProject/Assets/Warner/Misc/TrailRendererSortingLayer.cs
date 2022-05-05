using UnityEngine;
using System.Collections.Generic;

namespace Warner {

public class TrailRendererSortingLayer: MonoBehaviour 
	{
	#region MEMBER FIELDS

	public string sortingLayerName;
	public int sortingOrder;
	private TrailRenderer trail;
	private float originalTime;

	#endregion
	
	
	#region INIT STUFF
	
	void Awake ()
		{
		setRenderers(transform);
		for (int i=0;i<transform.childCount;i++)
			{
			setRenderers(transform.GetChild(i));
			}

		trail = GetComponent<TrailRenderer>();
		originalTime = trail.time;
		}
	
	
	void OnEnable()
		{
		//reset tthe trail renderer, this fixes the problem of using an object pool and trail continues last path
		Timing.run(resetTrail());
		}


	IEnumerator <float> resetTrail()
		{
		trail.time = -1f;		
		yield return 0;
		trail.time = originalTime;
		}
	
	void setRenderers(Transform obj)
		{
		TrailRenderer trailRenderer = obj.GetComponent<TrailRenderer>();
		if (trailRenderer)
			{
			// Set the sorting layer of the particle system.
			trailRenderer.sortingLayerName = sortingLayerName;
			trailRenderer.sortingOrder = sortingOrder;
			}
		}
		
	#endregion			
	}

}