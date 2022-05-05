using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Warner;
using DG.Tweening;
using Warner.AnimationTool;

namespace Game
	{
	public class Orb : CharacterAI
		{

        #region MEMBER FIELDS

        public Vector2 offset = new Vector2(0, 25f);
		public Vector2 dampings = new Vector2(0.5f, 0.5f);

		private Vector2 currentVelocity;
		private BouncerCharacter mainCharacter;
		private bool attacking;
		private IEnumerator<float> attackRoutine;
		private IEnumerator<float> collisionRoutine;
		private const string tweenId = "attackTween";

        #endregion



        #region INIT

        protected override void Awake()
			{
			base.Awake();

			mainCharacter = LevelMaster.instance.getSinglePlayerCharacter().GetComponent<BouncerCharacter>();
			mainCharacter.orb = this;
			}


		protected override void OnEnable()
			{
			character.movements.rigidBody.gravityScale = 0;
			}

        #endregion



        #region UPDATE

        private void Update()//we use update loop cause we want to ensure that the camera controller happens always after this with late update
			{
			followPlayer();
			}


		private void followPlayer()
			{
			if (attacking)
				return;

			Vector2 targetPosition = mainCharacter.transform.position.to2();

			Vector2 toGoPosition = Vector2.zero;
			toGoPosition.x = targetPosition.x - offset.x;
			toGoPosition.y = targetPosition.y - offset.y;

			Vector2 deltas = Vector2.zero;
			float length = (toGoPosition - transform.position.to2()).magnitude;
			deltas.x = (toGoPosition.x - transform.position.x) * 0.1f * length;
			deltas.y = (toGoPosition.y - transform.position.y) * 0.1f * length;

			toGoPosition.x = Mathf.SmoothDamp(transform.position.x, transform.position.x + deltas.x,
				ref currentVelocity.x, dampings.x * 0.25f);

			toGoPosition.y = Mathf.SmoothDamp(transform.position.y, transform.position.y + deltas.y,
					ref currentVelocity.y, dampings.y * 0.25f);

			if (!float.IsNaN(toGoPosition.x) && !float.IsNaN(toGoPosition.y))
				transform.position = toGoPosition;
			}

		#endregion


		#region COLLISIONS

		private void OnTriggerEnter2D(Collider2D collider)
			{
			DOTween.Kill(tweenId);
			collisionRoutine = collisionCoRoutine(collider);
			Timing.run(collisionRoutine);
			}

		private IEnumerator<float> collisionCoRoutine(Collider2D collider)
			{
			Character[] targets = mainCharacter.attacks.getHittingTargets(0.5f, transform.position);

			if (targets.Length == 0)
				yield break;

			ComboManager.Attack theAttack = new ComboManager.Attack();
			theAttack.type = ComboManager.AttackType.Special1Normal;
			mainCharacter.attacks.currentAttack = theAttack;
			VfxManager.instance.destroyVfxByName("OrbVfx");
			character.setState(CharacterState.Hit);

			Vector2 direction = collider.transform.position - transform.position;
			for (int i = 0; i < targets.Length; i++)
				{
				Debug.Log(targets[i]);
				targets[i].attacks.receiveDamage(character.attacks.damages.strong, mainCharacter, direction, CharacterState.Hit, i == 0);
				}

			mainCharacter.playVfxOnHittingTargets(theAttack, targets);

			yield return Timing.waitForSeconds(0.25f);
			attacking = false;
			}

        #endregion


        #region DESTROY

        protected override void OnDisable()
			{
			
			}

        #endregion



        #region ATTACKS

        public void attack(Vector2 targetPosition)
			{
			if (attacking)
				return;

			attacking = true;
			attackRoutine = attackCoRoutine(targetPosition);
			Timing.run(attackRoutine);
			}

		private IEnumerator<float> attackCoRoutine(Vector2 targetPosition)
			{
			
			float duration = 0.1f;
			int cycles = 10;
			float cycleDuration = duration / cycles;
			Vector2 originalPos = transform.position;
			Vector2 pos = originalPos;
			for (int i = 0; i < cycles; i++)
				{
				pos += UnityEngine.Random.insideUnitCircle*0.05f;
				transform.position = pos;
				yield return Timing.waitForSeconds(cycleDuration);//let it return back to position
				}

			transform.position = originalPos;

			duration = 0.2f;
			for (int i = 0; i < cycles; i++)
				{
				pos += UnityEngine.Random.insideUnitCircle * 0.1f;
				transform.position = pos;
				yield return Timing.waitForSeconds(cycleDuration);//let it return back to position
				}


			transform.position = originalPos;
			

			transform.DOLocalMove(targetPosition, 0.9f).SetEase(Ease.OutQuint).SetId(tweenId).OnComplete(()=> 
				{
				Timing.run(afterAttackCoRoutine());
				});

			yield break;
			}


		private IEnumerator<float> afterAttackCoRoutine()
			{
			yield return Timing.waitForSeconds(1f);
			attacking = false;
			}

			#endregion


		}
    }