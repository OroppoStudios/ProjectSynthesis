using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warner;
using Warner.AnimationTool;
using System;

namespace Game
	{
	public class BouncerCharacter : Character
		{
		public float attackRange;
		public Vector3 attackOffset;
		public LayerMask targetsLayer;
		public Transform pointerCircle;
		public Transform shieldTransform;

		[NonSerialized]
		public Orb orb;

		private IEnumerator<float> powerUpRoutine;
		private float originalSpeed;
		private float originalStartSpeed;
		private bool _pointerVisible;
		private Vector2 direction;
		private SpriteRenderer shieldRenderer;
		private IEnumerator<float> shieldRoutine;


		#region INIT


		protected override void Awake()
			{
			base.Awake();

			if (pointerCircle!=null)
				pointerCircle.gameObject.SetActive(false);

			if (shieldTransform != null)
				{
				shieldTransform.gameObject.SetActive(false);
				shieldRenderer = shieldTransform.GetComponent<SpriteRenderer>();
				}

			if (control.controlMode==CharacterControl.ControlMode.Human)
				InputManager.onButtonsPressed += onButtonsPressed;
			}

		protected override void Start()
			{
			base.Start();

			originalSpeed = movements.horizontal.constantSpeed;
			originalStartSpeed = movements.horizontal.startSpeed;
			
			}


		#endregion



		#region DESTROY

		private void OnDestroy()
			{
			InputManager.onButtonsPressed -= onButtonsPressed;
			}

		#endregion



		#region UPDATE

		protected override void Update()
			{
			base.Update();
			checkForAimingDirection();
			}


		private void checkForAimingDirection()
			{
			direction = control.rightMovementDirection;

			if ((movements.aimingRight && direction.x < 0) || (!movements.aimingRight && direction.x > 0))
				{
				pointerVisible = false;
				return;
				}

			if (!direction.Equals(Vector2.zero))
				{
				if (!movements.aimingRight)
					direction *= -1;

				var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
				pointerCircle.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
				pointerVisible = true;
				}
				else
				pointerVisible = false;
			}


		bool pointerVisible
			{
			set
				{
				if (value)
					{
					if (!_pointerVisible)
						pointerCircle.gameObject.SetActive(true);

					_pointerVisible = true;
					}
					else
					{
					if (_pointerVisible)
						pointerCircle.gameObject.SetActive(false);

					_pointerVisible = false;
					}
				}
			get
				{
				return _pointerVisible;
				}
			}

		#endregion



		#region EVENTS


		private void onButtonsPressed(List<InputButton> buttons)
			{
			for (int i = 0;i < buttons.Count;i++)
				{
				switch (buttons[i].getAction(control.inputPlayer))
                    {
					case "Special2":
						DebugConsole.instance.executeCommand("spawn orb");
						orbAttack();						
						break;
					}
				}
			}


		protected override float checkAnimationNormalizedTime()
			{
			float elapsed = base.checkAnimationNormalizedTime();

			if (elapsed>1f)
				{

				}

			return elapsed;
			}

		protected override void onEventFired(Warner.AnimationTool.AnimationEvent data)
			{
			base.onEventFired(data);

			switch (data.type)
				{
				case AnimationEventType.Custom:
				break;
                }
            }

		#endregion



		#region CUSTOM ATTACKS

		private void orbAttack()
			{
			if (orb == null || !pointerVisible)
				return;

			if (direction.Equals(Vector2.zero))
				return;

			Vector2 targetPosition = transform.position.to2() + (direction * 9f);

			if (!movements.aimingRight)
				direction *= -1;

			float addedDistance = direction.y==0 ? 12f : 8f;

			orb.attack(transform.position.to2() + (direction * addedDistance));
			if (direction.y>0)
				setState(CharacterState.SpecialAttack2Up);
				else
				setState(CharacterState.SpecialAttack2Normal);
			}


		protected override void onAttackTime(ComboManager.Attack attack, List<ComboManager.Attack> comboProgression, Character[] targets)
			{
			base.onAttackTime(attack, comboProgression, targets);

			/*
			string comboText = "";

			for (int i = 0; i < comboProgression.Count; i++)
				{
				comboText += comboProgression[i].type.ToString();

				if (i < comboProgression.Count - 1)
					comboText += " - ";
				}
			*/

			if (attacks.attackConnected)
				activateShield();
			}


		private void activateShield()
			{
			if (shieldRoutine != null)
				Timing.kill(shieldRoutine);

			shieldRoutine = shieldCoRoutine();
			Timing.run(shieldRoutine);
			}

		private IEnumerator<float> shieldCoRoutine()
			{
			shieldTransform.gameObject.SetActive(true);
			Color color = shieldRenderer.color;
			bool down = true;
			float maxAlpha = 1f;

			while (true)
				{
				yield return Timing.waitForSeconds(0.085f);

				if (down)
					{
					color.a -= 0.1f;

					if (color.a <= maxAlpha - 0.5)
						{
						down = false;

						if (maxAlpha <= 0.5f)
							yield break;
						}
					}
					else
					{
					color.a += 0.1f;

					if (color.a >= maxAlpha)
						{
						down = true;
						maxAlpha -= 0.035f;
					}
					}

				shieldRenderer.color = color;
				}

			yield break;
			}

		#endregion
		}
    }
