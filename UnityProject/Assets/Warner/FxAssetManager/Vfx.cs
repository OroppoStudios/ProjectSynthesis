using UnityEngine;
using Warner.AnimationTool;
using System;
using System.Collections.Generic;

namespace Warner
    {
    public class Vfx: AnimationController
        {
        #region MEMBER FIELDS

		[NonSerialized] public PoolObject poolObject;
		[NonSerialized] public Character ownerCharacter;
		[NonSerialized] public int instanceId;
		[NonSerialized] public AutoMove autoMove;
		[NonSerialized] public Glow[] glowComponents;

		private IEnumerator<float> destroyRoutine;
		bool lastSortWasFront;

		#endregion



		#region INIT

		protected override void Awake()
            {
            base.Awake();

            poolObject = GetComponent<PoolObject>();
            glowComponents = GetComponentsInChildren<Glow>();
			autoMove = GetComponent<AutoMove>();
			instanceId = GetInstanceID();
            }      

        #endregion



        #region DESTROY

        protected override void OnDisable()
			{
			base.OnDisable();
			VfxManager.instance.removeVfxInstance(instanceId);
			}


		public void fadeAndDestroy(float fade)
			{
			if (fade==0f)
				{
				PoolManager.Destroy(gameObject);
				return;
				}

			if (destroyRoutine!=null)
				Timing.run(destroyRoutine);

			destroyRoutine = destroyCoRoutine(fade);
			Timing.run(destroyRoutine);
			}

		private IEnumerator<float> destroyCoRoutine(float fade)
			{
			Color color;

			for (int i = 0; i<glowComponents.Length; i++)
				{
				glowComponents[i].stopGlow(fade*0.5f);

				color = glowComponents[i].tintData.originalColor;
				color.a = 0;
				glowComponents[i].stopTint(color, fade);
				}

			yield return Timing.waitForSeconds(fade);
			PoolManager.Destroy(gameObject);
			}

		#endregion



		#region EVENTS HANDLER

		protected override void onEventFired(Warner.AnimationTool.AnimationEvent data)
			{
			base.onEventFired(data);

			switch (data.type)
				{
				case AnimationEventType.Vfx:
					vfxEvent(data);
				break;
				}
			}


		private void vfxEvent(Warner.AnimationTool.AnimationEvent data)
			{
			string sort = data.get<string>("sort");

			switch (sort)
				{
				case "frontPlayers":
					sortWithPlayers(data);
				break;
				case "backPlayers":
					sortWithPlayers(data);
				break;
				case "switchPlayers":
					sortWithPlayers(data, true);
				break;
				}
			}		


		private void sortWithPlayers(Warner.AnimationTool.AnimationEvent data, bool isSwitch = false)
			{
			if (ownerCharacter==null)
				return;

			bool front = isSwitch ? !lastSortWasFront : true;

			if (!data.contains("ignoreOwner") && !isSwitch)
				front = ownerCharacter.attacks.attackIsBack;

			SortingLayer sortingLayer;

			if (front)
				{
				//let this always be also in front of enemies
				sortingLayer = LevelMaster.instance.sortingLayers.vfxInFrontPlayers;
				}
				else
				{
				if (ownerCharacter.currentSortingLayer.name
					==LevelMaster.instance.sortingLayers.playersBehindEnemies.name)
					sortingLayer = LevelMaster.instance.sortingLayers.vfxBehindPlayersBehindEnemies;
					else
					sortingLayer = LevelMaster.instance.sortingLayers.vfxBehindPlayers;
				}

			lastSortWasFront = front;
			setSpriteRenderersSortingLayer(sortingLayer);
			}								

		#endregion



		#region ANIMATIONS

		protected override float checkAnimationNormalizedTime()
			{
			float elapsed = base.checkAnimationNormalizedTime();					

			if (elapsed<1f || currentAnimation.loop)
				return elapsed;

			PoolManager.Destroy(gameObject);
			return elapsed;
			}

		#endregion
        }
    }
