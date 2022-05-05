using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.PostProcessing;

namespace Warner
	{
	public class CameraController: MonoBehaviour
		{
		#region MEMBER FIELDS

        [Range (-100, 20)] public float zoomLevel;
		public bool handheldEffect;
		public float handHeldDamping = 2.5f;
		public float handHeldDistance = 0.25f;
		public Camera cam;
		public Vector2 nativeResolution = new Vector2(1920,1080);
		public float pixelsPerUnit = 100;
		public TargetFollower targetFollower;

		[NonSerialized] public bool follow;
		[NonSerialized] public Rect worldBoundaries;
		[NonSerialized] public Vector2 worldSize;
		[NonSerialized] public bool worldBoundsReady;
		[NonSerialized] public bool zoomingOut;
		[NonSerialized] public bool zoomingIn;
		[NonSerialized] public bool zooming;
		[NonSerialized] public PostProcessingBehaviour postProcessing;

		public delegate void EventsHandler();
		public event EventsHandler onMove;
		public event EventsHandler onCameraReady;

		[Serializable] public class ShakeData
			{
			[Range (0, 10)] public int cycles = 4;
			[Range (0f, 1f)] public float duration = 0.2f;
			[Range (0f, 2f)] public float xOffset = 1f;
			[Range (0f, 2f)] public float yOffset = 1f;
			}

		[Serializable]
		public class TargetFollower
			{
			public Vector2 dampings = new Vector2(0.1f, 0.5f);
			public Vector2 offsets;
			[Range (0,1)] 
			public float followYPercentage = 0.15f;
			[NonSerialized] 
			public CharacterMovements target;
			[NonSerialized] 
			public TargetFollowerCamera instance;
			}

		public static CameraController instance;

		private Vector2 shakeOffset;
		private Vector2 handHeldOffset;
		private Vector2 handHeldTarget;
		private Vector2 handHeldVelocity;
		private Vector2 lastCameraPosition;
		private IEnumerator <float> shakeRoutine;
		private float _unitsPerPixel;
		private bool _ready;
		private Vector2 originalTargetOffsets;
		private float defaultSize;
		private Dictionary<string, PostProcessingProfile> postProcessingProfiles = new Dictionary<string, PostProcessingProfile>();

        const float zoomStep = 0.1f;
		const string zoomTweenId = "CameraZoomTween";

		#endregion



		#region INIT

		private void Awake()
			{
			instance = this;

			GameObject targetFollowerGameObject = new GameObject("TargetFollower");
			targetFollowerGameObject.transform.parent = transform;
			targetFollower.instance = targetFollowerGameObject.AddComponent<TargetFollowerCamera>();
			originalTargetOffsets = targetFollower.offsets;

			float scale = Screen.height/nativeResolution.y;
            defaultSize = ((Screen.height/2f)/(pixelsPerUnit*scale)) + (zoomStep*zoomLevel*-1);
            cam.orthographicSize = defaultSize;

			loadPostProcessingProfiles();

			enabled = false;
			}


		public void init(CharacterMovements target)
			{		
			targetFollower.target = target;
			targetFollower.instance.init();

			cam.transform.position = targetFollower.instance.transform.position;
			updateWorldSize();
																	
			follow = true;
			enabled = true;
			_ready = true;

			if (onCameraReady!=null)
				onCameraReady();
			}


		public bool ready
			{
			get
				{
				return _ready;
				}
			}

		#endregion



		#region MISC

		public float unitsPerPixel
			{
			get
				{
				if (_unitsPerPixel==0)	
					_unitsPerPixel = cam.ScreenToWorldPoint(new Vector2(1,0)).x-
						cam.ScreenToWorldPoint(new Vector2(0,0)).x;

				return _unitsPerPixel;
				}
			}


		public float unitsToPixels(float unityUnits)
			{
			return (Screen.height/(cam.orthographicSize*2f))*unityUnits;
			}


		public float pixelsToUnits(int pixels)
			{
			return pixels*unitsPerPixel;
			}


		public float roundToNearestPixel(float unityUnits)
			{
			float pixelx = unitsToPixels(unityUnits);
			pixelx = Mathf.Round(pixelx);
			float adjustedUnityUnits = pixelx/(Screen.height/(cam.orthographicSize*2f));
			return adjustedUnityUnits;
			}

		#endregion



		#region DESTROY STUFF

		private void OnDisable()
			{
			targetFollower.instance.enabled = false;
			worldBoundsReady = false;
			follow = false;
			_ready = false;

			if (cam!=null)
				cam.transform.localPosition = Vector2.zero;
			}

		#endregion



		#region FRAME UPDATE

		private void Update()
            {
			updateWorldSize();
			updateWorldBoundaries();
			handleHandHeldEffect();
			}


		private void LateUpdate()
			{
			Vector2 targetPosition = 
				(follow ? targetFollower.instance.transform.position.to2() : Vector2.zero)+
				handHeldOffset+shakeOffset;

			cam.transform.position = targetPosition;

			if (cam.transform.position.to2()!=lastCameraPosition && onMove!=null)
				onMove();

			lastCameraPosition = cam.transform.position;
			}

		#endregion



		#region BOUNDARIES

		private void updateWorldBoundaries()
		{			
			worldBoundaries.x = cam.transform.position.x-(worldSize.x/2);
			worldBoundaries.y = cam.transform.position.y-(worldSize.y/2);
			worldBoundaries.width = worldSize.x;
			worldBoundaries.height = worldSize.y;

			if (!worldBoundsReady)
				worldBoundsReady = true;
		}


		private void updateWorldSize()
			{
			Vector2 topLeft = cam.ViewportToWorldPoint(new Vector2(0,1));		
			Vector2 bottomRight = cam.ViewportToWorldPoint(new Vector2(1,0));	
			worldSize.x = bottomRight.x-topLeft.x;	
			worldSize.y = topLeft.y-bottomRight.y;
			}

		#endregion



		#region BOBBING

		private void handleHandHeldEffect()
			{
			if (!handheldEffect)
				return;

			if (Vector2.Distance(handHeldOffset, handHeldTarget)<0.1f)
				handHeldTarget = UnityEngine.Random.insideUnitCircle*handHeldDistance;
				else
				handHeldOffset = Vector2.SmoothDamp(handHeldOffset, handHeldTarget, ref handHeldVelocity, handHeldDamping, 
					Mathf.Infinity, Time.deltaTime);
			}

		#endregion



		#region SCREENSHAKE


		public void shake(ShakeData shakeData, bool traditionalZoom = true)
            {
            if (traditionalZoom)
				shake(shakeData.cycles, shakeData.duration, shakeData.xOffset, shakeData.yOffset);
				else
				shake2(shakeData.cycles, shakeData.duration, shakeData.xOffset, shakeData.yOffset);
			}


		public void shake(int cycles, float duration, float xOffset, float yOffset)
            {
            if (shakeRoutine!=null)
                Timing.kill(shakeRoutine);

			shakeRoutine = shakeCoRoutine(cycles, duration, xOffset, yOffset);
			Timing.run(shakeRoutine);
			}



		private IEnumerator <float> shakeCoRoutine(int cycles, float duration, float xOffset, float yOffset)
			{
			float cycleDuration = duration/cycles;
			bool right = UnityEngine.Random.value>0.5f;
			bool down = UnityEngine.Random.value>0.5f;
			for (int i = 0; i<cycles; i++)
				{
				shakeOffset.x = right ? xOffset : -xOffset;
				shakeOffset.y = down ? yOffset : -yOffset;
				right = !right;
				down = !down;
				yield return Timing.waitForSeconds(cycleDuration);//let it return back to position
				}

			shakeOffset = Vector2.zero;
			}


		public void shake2(int cycles, float duration, float xOffset, float yOffset)
            {
            if (shakeRoutine!=null)
                Timing.kill(shakeRoutine);

			DOTween.Kill(zoomTweenId);
			shakeRoutine = shakeCoRoutine2(cycles, duration, xOffset, yOffset);
			Timing.run(shakeRoutine);
			}



		private IEnumerator <float> shakeCoRoutine2(int cycles, float duration, float xOffset, float yOffset)
			{
			float frustrumHeight = 2*cam.nearClipPlane*Mathf.Tan(cam.fieldOfView*0.5f*Mathf.Deg2Rad);
			float frustrumWidth = frustrumHeight*cam.aspect;
			float cycleDuration = duration/cycles;		

			Matrix4x4 mat = cam.projectionMatrix;
			Matrix4x4 originalMatrix = cam.projectionMatrix;
			Vector2 offset = Vector2.one*0.004f;
			offset.x *= xOffset;
			offset.y *= yOffset;
			float cycleStartTime;

			for (int i = 0; i<cycles; i++)
				{
				offset.x *= UnityEngine.Random.value>0.5f ? 1f : -1f;
				offset.y *= UnityEngine.Random.value>0.5f ? 1f : -1f;
				cycleStartTime = Time.time;
				mat[0, 2] = 2 * offset.x / frustrumWidth;
				mat[1, 2] = 2 * offset.y / frustrumHeight;

				while (Time.time-cycleStartTime<cycleDuration)
					{
					cam.projectionMatrix = mat;
					yield return 0;
					}

				cam.projectionMatrix = originalMatrix;
				yield return Timing.waitForSeconds(cycleDuration);//let it return back to position
				}

			cam.projectionMatrix = originalMatrix;
			zooming = false;
			DOTween.TogglePause(zoomTweenId);
			}


		#endregion



		#region ZOOM

		public float baseSize
			{
			get
				{
				return defaultSize+zoomStep*zoomLevel*-1;
				}
			}

                 
		public void resetZoom(float duration = 0f, Ease ease = Ease.InOutCubic)
			{
			zoom(zoomLevel, duration, ease);
			}


        public void zoom(float level, float duration, Ease ease = Ease.InOutCubic)
			{
			//when we shake we modify the Y of the camera viewport rect
			//but that affects the "zoom" so we have to account for that
			//in our orthographic size
			//so we have to make sure also when shaking that the
			//zoom functionality does not override our value
			zooming = true;
			float targetSize = defaultSize;

			if (level!=0)
				targetSize += zoomStep*level*-1;

			if (targetSize>cam.orthographicSize)
				{
				zoomingOut = true;
				zoomingIn = false;
				}
				else
				{
				zoomingIn = true;
				zoomingOut = false;
				}

			DOTween.Kill(zoomTweenId);
			DOTween.To(() => cam.orthographicSize, x => cam.orthographicSize = x, targetSize, duration)
				.SetEase(ease).SetId(zoomTweenId).OnComplete(()=>
				{
				zooming = false;
				zoomingOut = false;
				zoomingIn = false;
				});
			}

		#endregion



		#region OFFSETS

		public void setHorizontalOffset(float offset, float duration, Ease ease = Ease.Linear)
			{
			const string tweenId = "CameraHorizontalOffsetTween";
			float targetOffset = originalTargetOffsets.x+offset;

			DOTween.Kill(tweenId);
			DOTween.To(() => targetFollower.offsets.x, x => targetFollower.offsets.x = x, targetOffset, duration)
				.SetEase(ease).SetId(tweenId);
			}


		public void setVerticalOffset(float offset, float duration, Ease ease = Ease.Linear)
			{
			const string tweenId = "CameraVerticalOffsetTween";
			float targetOffset = originalTargetOffsets.y+offset;

			DOTween.Kill(tweenId);
			DOTween.To(() => targetFollower.offsets.y, x => targetFollower.offsets.y = x, targetOffset, duration)
				.SetEase(ease).SetId(tweenId);
			}

            		#endregion



        #region POSTPROCESS

        private void loadPostProcessingProfiles()
        	{
			postProcessing = cam.GetComponent<PostProcessingBehaviour>();
			PostProcessingProfile[] profiles = Resources.LoadAll<PostProcessingProfile>("CameraProfiles");

			for (int i = 0; i<profiles.Length; i++)
				postProcessingProfiles.Add(profiles[i].name, profiles[i]);
        	}

        public void loadPostProcessProfile(string profileName)
			{
			if (!postProcessingProfiles.ContainsKey(profileName))
				{
				Debug.LogWarning("CameraController: Could not find profile "+profileName+", make sure it's in the Resources/CameraProfiles folder");
				return;
				}

			postProcessing.profile = postProcessingProfiles[profileName];
        	}

		#endregion
		}
	}
