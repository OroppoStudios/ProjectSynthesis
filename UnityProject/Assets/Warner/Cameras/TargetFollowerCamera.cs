using UnityEngine;
using System;

namespace Warner
	{
	public class TargetFollowerCamera: MonoBehaviour
		{
		#region MEMBER FIELDS

		private Vector2 currentVelocity;
		private CameraController.TargetFollower targetFollower;

		#endregion



		#region INIT STUFF

		public void Awake()
			{
			enabled = false;
			}

		public void init()
			{
			targetFollower = CameraController.instance.targetFollower;

			transform.position = new Vector3(targetFollower.target.transform.position.x, 
				(targetFollower.target.transform.position.y-targetFollower.offsets.y)*
				targetFollower.followYPercentage, transform.position.z);

			enabled = true;
			}
		

		#endregion



		#region DESTROY STUFF

		private void OnDisable()
			{
			transform.localPosition = new Vector3(0, transform.localPosition.y, transform.localPosition.z);
			}

		#endregion



		#region FRAME STUFF

		private void Update()//we use update loop cause we want to ensure that the camera controller happens always after this with late update
			{	
			Vector2 toGoPosition = Vector2.zero;
			toGoPosition.x = targetFollower.target.transform.position.x-targetFollower.offsets.x;
			toGoPosition.y = (targetFollower.target.transform.position.y-targetFollower.offsets.y)*
				targetFollower.followYPercentage;

			Vector2 deltas = Vector2.zero;
			float length = (toGoPosition-transform.position.to2()).magnitude;
			deltas.x = (toGoPosition.x-transform.position.x) * 0.1f * length;
			deltas.y = (toGoPosition.y-transform.position.y) * 0.1f * length;
			//toGoPosition.x = Mathf.Lerp(transform.position.x, transform.position.x+delta, Time.deltaTime*50);						

			toGoPosition.x = Mathf.SmoothDamp(transform.position.x, transform.position.x+deltas.x, 
				ref currentVelocity.x, targetFollower.dampings.x*0.25f);							

			toGoPosition.y = Mathf.SmoothDamp(transform.position.y, transform.position.y+deltas.y, 
					ref currentVelocity.y, targetFollower.dampings.y*0.25f);							

			//toGoPosition.x = transform.position.x;

			if (!float.IsNaN(toGoPosition.x) && !float.IsNaN(toGoPosition.y))
				transform.position = toGoPosition;
			}
					
        #endregion
		}
	}