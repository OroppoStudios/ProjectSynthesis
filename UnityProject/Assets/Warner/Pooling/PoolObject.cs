using UnityEngine;
using System.Collections.Generic;
using System;

namespace Warner 
	{
	public class PoolObject: MonoBehaviour
		{
		#region MEMBER FIELDS

		public bool offScreenDestroy;
		public Vector2 offScreenOffsets = new Vector2(1.5f, 1.5f);
		public Timing.Segment DestroySegment = Timing.Segment.SlowUpdate;

		public delegate void PoolEventHandler(bool offScreen);
		public event PoolEventHandler onDestroy;

		[NonSerialized] public bool onScreenAtLeastOneTime;

		[Serializable]
		public class SingleAnimation
			{
			public bool enabled;
			public string stateName = "Default";
			}

		private IEnumerator <float> offScreenDestroyCheckRoutine;
		private Rect onScreenOffsetBoundaries;



		#endregion



		#region INIT STUFF

		protected virtual void Awake()
			{

			}


		protected virtual void Start()
			{

			}


		protected virtual void OnEnable()
            {
            offScreenDestroyCheckRoutine = offScreenDestroyCheckCoRoutine();
            Timing.run(offScreenDestroyCheckRoutine);

            onScreenAtLeastOneTime = false;
			}

		#endregion



		#region ON SCREEN STUFF


		public bool onScreen
			{
			get
				{
				if (!CameraController.instance.worldBoundsReady)
					return true;
					else
					return CameraController.instance.worldBoundaries.Contains(transform.position.to2());
				}
			}


		private IEnumerator <float> offScreenDestroyCheckCoRoutine()
			{
			while (true)
				{
				yield return Timing.waitForSeconds(0.15f);

				if (!CameraController.instance.worldBoundsReady)
					continue;

				onScreenOffsetBoundaries = CameraController.instance.worldBoundaries;
				onScreenOffsetBoundaries.x -= offScreenOffsets.x;
				onScreenOffsetBoundaries.y -= offScreenOffsets.y;
				onScreenOffsetBoundaries.width += offScreenOffsets.x*2;
				onScreenOffsetBoundaries.height += offScreenOffsets.y*2;

				if (offScreenDestroy && !onScreenOffsetBoundaries.Contains(transform.position.to2()))
					{
					triggerOnDestroyEvent(true);

					yield return 0;

					PoolManager.Destroy(gameObject);

					yield break;
					}

				if (onScreen && !onScreenAtLeastOneTime)
					onScreenAtLeastOneTime = true;
				}
			}

		#endregion

		
		
		#region DESTROY STUFF

		protected virtual void OnDisable()
            {
            Timing.kill(offScreenDestroyCheckRoutine);
            }


		private void triggerOnDestroyEvent(bool offScreen)
			{
			if (onDestroy!=null)
				onDestroy(offScreen);
			}


		public void reset()
			{
			gameObject.SetActive(true);		
			}


		public void disable(float DestroyTime)
			{
			if (!gameObject.activeSelf)//if already deactivated dont do anything
				return;

			triggerOnDestroyEvent(false);

			Timing.kill(offScreenDestroyCheckRoutine);

			if (DestroyTime>0)
				Timing.run(disableAfterTime(DestroyTime), DestroySegment);
				else
				gameObject.SetActive(false);					
			}


		private IEnumerator <float> disableAfterTime(float DestroyTime)
			{
			float startTime = Time.time;
			float currentTime = Time.time;
			bool gotPaused = false;		

			while (currentTime-startTime<DestroyTime)
				{
				if (TimeManager.instance.paused)
					gotPaused = true;
					else
					{
					if (gotPaused)
						{	
						startTime += Time.time-TimeManager.instance.pauseStartTime;
						gotPaused = false;
						}

					currentTime = Time.time;			
					}

				yield return 0;
				}

			gameObject.SetActive(false);
			}

            		#endregion
		}
	}