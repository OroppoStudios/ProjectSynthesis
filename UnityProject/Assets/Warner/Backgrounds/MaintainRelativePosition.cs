using UnityEngine;

namespace Warner 
	{
	public class MaintainRelativePosition : MonoBehaviour 
		{
		#region MEMBER FIELDS

		public bool smooth;
		public bool maintainX = true;
		public bool maintainY = true;

		private Vector2 m_CurrentVelocity;
		private Vector3 initialDif;


		#endregion



		#region INIT STUFF


		private void Start()
			{
			CameraController.instance.onMove += onCameraMove;
			initialDif = CameraController.instance.cam.transform.position - transform.position;
			}

		#endregion



		#region DESTROY STUFF

		private void OnDestroy()
			{
			CameraController.instance.onMove -= onCameraMove;
			}

		#endregion



		#region EVENTS HANDLER STUFF


		private void onCameraMove()
			{
			if (!CameraController.instance.ready)
				return;

			Vector3 targetPos = CameraController.instance.cam.transform.position - initialDif;

			if (!maintainX)
				targetPos.x = transform.position.x;

			if (!maintainY)
				targetPos.y = transform.position.y;

			targetPos.z = transform.position.z;

			Vector2 newPos;
			if (smooth)
				newPos = Vector2.SmoothDamp(transform.position, targetPos, ref m_CurrentVelocity, 1,-1,Time.deltaTime);
				else
				newPos = targetPos;

			transform.position = newPos;
			}

		#endregion
		}
	}