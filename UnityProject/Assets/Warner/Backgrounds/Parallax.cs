using UnityEngine;
using System;
using System.Collections.Generic;

namespace Warner
	{
	public class Parallax: MonoBehaviour 
		{
		#region MEMBER FIELDS

		public List<LayerData> backgrounds;

		[Serializable]
		public struct LayerData
			{
			public bool enabled;
			public Transform transform;
            public float position;
			}

		public static Parallax instance;
		private Vector3 previousCamPos;

		#endregion



		#region INIT STUFF

		private void Awake()
			{
			instance = this;
			}

		private void OnEnable()
            {
            if (CameraController.instance==null)
                return;

            CameraController.instance.onMove += onCameraMove;
            CameraController.instance.onCameraReady += onCameraReady;
            LayerData layerData;

            for (int i = 0; i<backgrounds.Count; i++)
                if (!backgrounds[i].transform.gameObject.activeSelf)
                    {
                    layerData = backgrounds[i];
                    layerData.enabled = false;
                    backgrounds[i] = layerData;
                    }
			}

		#endregion



		#region DESTROY STUFF

		private void OnDisable()
			{
			CameraController.instance.onMove -= onCameraMove;
			CameraController.instance.onCameraReady -= onCameraReady;
			}

		#endregion



		#region EVENT HANDLERS STUFF


		private void onCameraReady()
			{
			previousCamPos = CameraController.instance.cam.transform.position;
			}


		private void onCameraMove()
			{
			if (!CameraController.instance.ready)
				return;

			for (int i=0; i<backgrounds.Count; i++) 
				{
				if (!backgrounds[i].enabled)
					continue;

				//the parallax is the opposite of the camera movement because the previous frame multiplied by the scale
				Vector3 parallax = (previousCamPos-CameraController.instance.cam.transform.position)*
					(backgrounds[i].position*-0.1f);

				//create a target position which is the background's current position with its target position
				Vector2 backgroundTargetPos = new Vector3(backgrounds[i].transform.position.x+
					parallax.x, backgrounds[i].transform.position.y+parallax.y);

				backgrounds[i].transform.position = backgroundTargetPos;
				}

			//set the previousCamPos to the camera's position at the end of the frame
			previousCamPos = CameraController.instance.cam.transform.position;
			}

		#endregion



        #region MISC


        public void addLayer(Transform layerTransform, float position)
            {
            bool found = false;
            for (int i = 0; i<backgrounds.Count; i++)
                if (backgrounds[i].transform==layerTransform)
                    {
                    found = true;
                    break;
                    }

            if (found)
                return;

            LayerData layerData = new LayerData();
            layerData.enabled = layerTransform.gameObject.activeSelf;
            layerData.transform = layerTransform;
            layerData.position = position;

            backgrounds.Add(layerData);
            }

        #endregion
		}	

	}