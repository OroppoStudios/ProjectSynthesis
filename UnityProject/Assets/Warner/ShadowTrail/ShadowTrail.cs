using System.Collections.Generic;
using UnityEngine;
using System;

namespace Warner
	{
	public class ShadowTrail : MonoBehaviour
		{
		#region MEMBER FIELDS

		public Color tint = Color.white;
		[Range (1, 10)] public int count = 2;
		public float duration = 0.2f;
		public float followDistance = 0.5f;
		public Method method;
		public Direction direction;

		public enum Method {InPlace, Follow}
		public enum Direction {Horizontal, Vertical, Dual}

		[NonSerialized] public Transform parent;

		private SpriteRenderer followedSpriteRenderer;
		private SpriteRenderer[] renderers;
		private Transform container;
		private IEnumerator<float>[] routines = new IEnumerator<float>[10];
		private IEnumerator<float> stopRoutine;
		private bool _active;

		static private Dictionary<string, Transform> containers = new Dictionary<string, Transform>();
		static private GameObject prefab;

		#endregion



		#region INIT

		private void Awake()
			{		
			string containerName = "ShadowTrailPool_"+name.Replace("(Clone)", "");

			if (containers.ContainsKey(containerName))
				container = containers[containerName];
				else
				{
				container = new GameObject(containerName).transform;
				container.SetParent(PoolManager.instance.getMainPoolContainer().transform, true);
				containers.Add(containerName, container);
				}

			if (prefab==null)
				{
				prefab = new GameObject("ShadowTrailRendererPrefab");
				prefab.AddComponent<PoolObject>();
				prefab.AddComponent<SpriteRenderer>();
				}

			Color color = tint;
			color.a = 0;

			renderers = new SpriteRenderer[count];

			for (int i = 0; i<count; i++)
				{
				renderers[i] = PoolManager.instantiate(prefab, Vector2.zero, container).
					GetComponent<SpriteRenderer>();

				renderers[i].transform.localPosition = Vector2.zero;
				renderers[i].color = color;
				}

			for (int i = 0; i<count; i++)
				PoolManager.Destroy(renderers[i].gameObject);

			prefab.SetActive(false);

			setSpriteRenderer(GetComponent<SpriteRenderer>());
			}	


		public void setSpriteRenderer(SpriteRenderer spriteRenderer)
			{
			followedSpriteRenderer = spriteRenderer;
			}
		

		#endregion



		#region EFFECT

		private void clearRenderersAndRoutines()
			{
			for (int i = 0; i<renderers.Length; i++)
				{
				Timing.kill(routines[i]);
				renderers[i].sprite = null;
				renderers[i].gameObject.SetActive(false);
				}
			}


		public bool active
			{
			get
				{
				return _active;
				}
			}


		public void stop(bool clear = false)
			{
			_active = false;

			if (clear)
				clearRenderersAndRoutines();
			}
		

		public void start(float distance, float duration, float delay, bool singleFrame = false)
			{
			start(new Vector2(distance, distance), duration, delay, singleFrame);
			}

		public void start(Vector2 distance, float duration, float delay, bool isOneFrame = false)
			{
			if (followedSpriteRenderer==null)
				{
				Debug.LogWarning("AfterImage: Sprite renderer to follow not set. Use SetSpriteRenderer()");
				return;
				}

			clearRenderersAndRoutines();
			_active = true;

			for (int i = 0; i<count; i++)
				{
				renderers[i].sortingLayerName = followedSpriteRenderer.sortingLayerName;
				renderers[i].sortingOrder = followedSpriteRenderer.sortingOrder-1;
				renderers[i].gameObject.layer = followedSpriteRenderer.gameObject.layer;
				}

			for (int i = 0; i<count; i++)
				{
				if(i>0)
					delay += duration/count;

				routines[i] = animateRenderer(i, distance, duration, delay, isOneFrame);
				Timing.run(routines[i]);

				if (isOneFrame)
					break;
				}
			}


		private IEnumerator<float> animateRenderer(int index, Vector2 distance, float duration, float delay, bool isOneFrame)
			{
			yield return Timing.waitForSeconds(delay);

			Vector3 nextPosition;
			Vector3 targetPosition;
			float startTime = Time.time;
			Color color = tint;
			float elapsed;
			float percent;

			renderers[index].gameObject.SetActive(true);
			updateRenderer(index);
			Vector3 displacementDistance = calculateDisplacementDistance(distance);//only calculate every sprite change if nto we would get weird displacements on every frame if scale changes

			while (true)
				{
				elapsed = Time.time-startTime;
				percent = elapsed/duration;

				color.a = Mathf.Lerp(tint.a, 0f, percent);
				renderers[index].color = color;	

				if (method==Method.Follow)
					{
					targetPosition = followedSpriteRenderer.transform.position;
					targetPosition -= displacementDistance;
					nextPosition = Vector3.Lerp(followedSpriteRenderer.transform.position, 
						targetPosition, percent);
					renderers[index].transform.position = nextPosition;
					}

				if (percent>=1)
					{
					if (!_active || isOneFrame)
						{
						renderers[index].sprite = null;
						renderers[index].gameObject.SetActive(false);
						yield break;
						}

					updateRenderer(index);
					displacementDistance = calculateDisplacementDistance(distance);//only calculate every sprite change if nto we would get weird displacements on every frame if scale changes
					startTime = Time.time;
					color.a = tint.a;
					}

				yield return 0;
				}
			}


		private Vector3 calculateDisplacementDistance(Vector2 desiredDistance)
			{
			Vector3 displacement = Vector3.zero;

			if (parent!=null)
				{
				if (direction==Direction.Horizontal || direction==Direction.Dual)
					displacement.x = (parent.localScale.x>0 ? desiredDistance.x : -desiredDistance.x);

				if (direction==Direction.Vertical || direction==Direction.Dual)
					displacement.y = (parent.localScale.y>0 ? desiredDistance.y : -desiredDistance.y);
				}
				else
				{
				if (direction==Direction.Horizontal || direction==Direction.Dual)
					displacement.x = desiredDistance.x;

				if (direction==Direction.Vertical || direction==Direction.Dual)
					displacement.y = desiredDistance.x;
				}

			return displacement;
			}


		private void updateRenderer(int index)
			{
			renderers[index].transform.position = followedSpriteRenderer.transform.position;
			renderers[index].sprite = followedSpriteRenderer.sprite;
			renderers[index].transform.localScale = parent.localScale;
			}

		#endregion
		}
	}
