using System;
using UnityEngine;
using System.Collections.Generic;
using Warner.AnimationTool;

namespace Warner
    {
    [RequireComponent (typeof(Character))]
    public class CharacterAttacks: MonoBehaviour
        {
        #region MEMBER FIELDS

        public LayerMask targetLayers;
		public ComboMode comboMode;
		public float comboSpeed = 0.04f;
        public float assistedComboSpeed = 0.05f;
        public float avgAttackRange;
		public Damages damages;
		public PunchDelayData clearComboDelay;		
		[Range (0f, 1f)] public float finisherDeathToIdleDeathChance = 0.5f;
		public float hitStop;
		public float airHitStop;
		public CameraController.ShakeData finisherHitScreenShake;
		public float finisherHitStop;
		public ReceiverData receiverData;

        [NonSerialized] public Character currentAttacker;
        [NonSerialized] public bool attacking;
		[NonSerialized] public bool receivingDamage;
        [NonSerialized] public ComboManager.Attack currentReceivedAttack;
        [NonSerialized] public Vector2 currentReceivedAttackDirection;
        [NonSerialized] public bool attackConnected;
		[NonSerialized] public ComboManager.Attack currentAttack;
		[NonSerialized] public ComboManager.Combo[] availableCombos;
		[NonSerialized] public ComboManager.Combo[] assistedLightGroundCombos;
		[NonSerialized] public ComboManager.Combo[] assistedStrongGroundCombos;
		[NonSerialized] public ComboManager.Combo[] assistedLightAirCombos;
		[NonSerialized] public ComboManager.Combo[] assistedStrongAirCombos;
		[NonSerialized] public bool attackIsBack;
		[NonSerialized] public List<ComboManager.Attack> currentCombo = new List<ComboManager.Attack>();
		[NonSerialized] public ComboManager.ComboStatus currentComboStatus;

		[SerializeField] private GameObject HitEffect = null;

		[Serializable]
		public class ReceiverData
			{
			public PunchData hitStun;
			public PunchData blockHitStun;
			public Vector2 finisherStun;
			public Vector2 airHitStun;
			[Range (0.5f, 1.5f)] public float airHitStunMultiplier = 1f;
			public Vector2 airFinisherStun;
			public Vector2 airDownFinisherStun;
			public HitShakeData lightHitShake;
			public HitShakeData strongHitShake;
			public HitShakeData finisherHitShake;
			public HitShakeData airHitShake;
			public HitShakeData airFinisherHitShake;
			public HitShakeData airTriggerHitShake;
			}

		[Serializable] public class PunchDelayData
			{
			[Range (0f, 1f)] public float light = 0.5f;
			[Range (0f, 1f)] public float strong = 0.5f;
			[Range (0f, 1f)] public float airTrigger = 0.5f;
			[Range (0f, 1f)] public float finisher = 0.5f;
			}

		[Serializable] public class PunchData
			{
			[Range (0f, 15f)] public float light;
			[Range (0f, 15f)] public float strong;
			}

		[Serializable] public class HitShakeData
			{
			[Range (0f, 0.1f)] public float force = 0.02f;
			[Range (0, 10f)] public int cycles = 4;
			}

		[Serializable]
		public struct Damages
			{
			public int light;
			public int strong;
			}

		public enum ComboMode {Manual, Assisted}

		public delegate void AttackEventHandler(ComboManager.Attack attack, List<ComboManager.Attack> comboProgression, bool connected);
        public delegate void AttackReceivedEventHandler(ComboManager.Attack attack);
		public delegate void ComboEventHandler();
		public delegate void AttackTimeEventHandler(ComboManager.Attack attack, List<ComboManager.Attack> comboProgression, Character[] targets);
        public event AttackReceivedEventHandler onReceiveDamage;
        public event AttackReceivedEventHandler onFinishedReceivingDamage;
		public event AttackEventHandler onAttack;
		public event AttackTimeEventHandler onAttackTime;
        public event AttackEventHandler onAttackFinished;
        public event ComboEventHandler onComboCleared;

        private Character character;		
        private bool lastReceiveDamageWasBack;
		private IEnumerator<float> clearComboRoutine;
		private List<CharacterState> multiPurposeStateList = new List<CharacterState>();
		private bool isMainAttackFrame;
		private bool attackTimeHappened;
		private AssistedCombo assistedCombo;
		private ComboManager.Combo[] tempCombos;

		private struct AssistedCombo
			{
			public ComboManager.Attack[] progression;
			public int index;
			}


        #endregion



        #region INIT


        private void Awake()
            {
            character = GetComponent<Character>();
            }

        private void Start()
        	{
			availableCombos = ComboManager.instance.getCombos(name.Replace("(Clone)", ""));

			assistedLightGroundCombos = availableCombos.transformTo((combo)=>
				{
				if (!combo.isAssisted || combo.progression[0].isStrongAttack()
					|| combo.progression.hasAirAttack())
					return null;	

				return combo;
				});

			assistedStrongGroundCombos = availableCombos.transformTo((combo)=>
				{
				if (!combo.isAssisted || combo.progression[0].isLightAttack()
					|| combo.progression.hasAirAttack())
					return null;	

				return combo;
				});


			assistedLightAirCombos = availableCombos.transformTo((combo)=>
				{
				if (!combo.isAssisted || combo.progression[0].isStrongAttack()
					|| !combo.progression.hasAirAttack())
					return null;	

				return combo;
				});

			assistedStrongAirCombos = availableCombos.transformTo((combo)=>
				{
				if (!combo.isAssisted || combo.progression[0].isLightAttack()
					|| !combo.progression.hasAirAttack())
					return null;	

				return combo;
				});
        	}


        #endregion       



        #region ATTACKS

        public bool execute(ComboManager.Attack attack)
			{
			float clearComboDelayTime = attack.isLightAttack() ? clearComboDelay.light : clearComboDelay.strong;

			//ASSISTED
			if (comboMode==ComboMode.Assisted && !attack.isSpecial())
				{
				if (currentCombo.Count==0)
					{
					if (attack.isLightAttack())
						tempCombos = assistedLightGroundCombos;
					else
						tempCombos = assistedStrongAirCombos;

					if (tempCombos.Length==0)
						return false;

					assistedCombo.progression = tempCombos.getRandom().progression;
					assistedCombo.index = 0;
					}						

				if (assistedCombo.index>=assistedCombo.progression.Length
				    || (character.movements.jumping && attack.isStrongAttack()))
					return false;	

				attack = assistedCombo.progression[assistedCombo.index];
				assistedCombo.index++;
				}
			
			CharacterState targetState = attack.toCharacterState(attackIsBack);

			if (!attack.isValidAttack()
			    || (attack.isAirAttack() && !character.movements.jumping)
			    || (!attack.isAirAttack() && character.movements.jumping)
			    || !character.stateAvailable(targetState))
				{
				if (comboMode==ComboMode.Assisted)
					assistedCombo.index--;

				return false;
				}

			currentComboStatus = ComboManager.instance.isCombo(currentCombo, availableCombos, attack);

			if (currentCombo.Count>0)
				{
				if (currentComboStatus==ComboManager.ComboStatus.Invalid)
					{
					//dont cancel specials unless the last attack was the same special
					if (!(attack.isSpecial() && attack.type!=currentAttack.type))
						{
						if (currentAttack.airTrigger)
							{
							clearCombo();
							return execute(attack);
							}
							else
							return false;
						}
					}
				}							

			if (attacking)//this mean we received input while doing a combo and we were still on halt time
				Timing.kill(character.stateRoutine);

			if (!character.movements.jumping)
				character.movements.resetHorizontalMovement();

			if (currentComboStatus==ComboManager.ComboStatus.AirTrigger)
				{
				attack.airTrigger = true;
				targetState = CharacterState.AirTriggerAttack;
				clearComboDelayTime = clearComboDelay.airTrigger;
				}

			if (currentComboStatus==ComboManager.ComboStatus.Finisher)
				{
				attack.finisher = true;	
				clearComboDelayTime = clearComboDelay.finisher;
				}

			currentAttack = attack;
			currentCombo.Add(attack);

			attackConnected = false;
			attackIsBack = attack.finisher ? true : !attackIsBack;
			attacking = true; 
			isMainAttackFrame = false;
			attackTimeHappened = false;
			character.movements.rigidBody.velocity.setY(0f);

			if (clearComboRoutine!=null)
				Timing.kill(clearComboRoutine);

			clearComboRoutine = clearComboCoRoutine(clearComboDelayTime);
			Timing.run(clearComboRoutine);

			notifyCloseTargetsTheyAreAboutToGetHit();
			character.movements.forceAimingRightUpdateOnRawControl();

            character.setState(targetState, attackCoRoutine(), true, !character.movements.jumping);
            return true;
            }



		private void notifyCloseTargetsTheyAreAboutToGetHit()
			{
			Character[] targets = getHittingTargets(avgAttackRange);

			for (int i = 0; i<targets.Length; i++)
				targets[i].ai.targetAttackingNearUs();
			}



		public void attackTime(AnimationTool.AnimationEvent data)
			{
			Vector2 direction = new Vector2(character.movements.aimingRight ? 1f : -1f, 0f);
			float range = data.get<float>("range");
			isMainAttackFrame = !data.get<bool>("extra");
			attackTimeHappened = true;

			switch (currentAttack.type)
				{
				case ComboManager.AttackType.LightPunchDown:
				case ComboManager.AttackType.StrongPunchDown:
					direction.y = -0.5f;
				break;
				case ComboManager.AttackType.LightPunchUp:
				case ComboManager.AttackType.StrongPunchUp:
					direction.y = 0.5f;
				break;
				}

			Character[] targets = getHittingTargets(range);

			if (targets.Length==0)
				{
				character.releaseActions();
				character.control.clearAttacksBuffer();						
				}
				else
				{
				//calculate the hitState, nearest enemy gets the correct one according to the attack
				//but the other ones get a random
				CharacterState targetHitState = currentAttack.attackToHit(targets[0].attacks.lastReceiveDamageWasBack, targets[0]);

				if (!character.movements.jumping)
					{
					multiPurposeStateList.Clear();
					for (int i = 0; i<CharacterExtensions.groundHits.Length; i++)
						if (CharacterExtensions.groundHits[i]!=targetHitState)
							multiPurposeStateList.Add(CharacterExtensions.groundHits[i]);
					}

				int damage;
				if (currentAttack.finisher)
					damage = damages.strong * 2;
				else
					if (currentAttack.isStrongAttack())
					damage = damages.strong;
				else
					damage = damages.light;

				int randomIndex;
				for (int i = 0; i<targets.Length; i++)
					{
					attackConnected = targets[i].attacks.receiveDamage(damage, 
						character, direction, targetHitState, i==0);

					if (!character.movements.jumping)
						{
						randomIndex = multiPurposeStateList.getRandomIndex();
						targetHitState = multiPurposeStateList[randomIndex];
						multiPurposeStateList.RemoveAt(randomIndex);
						}
					}

				Timing.run(releaseActionsCoRoutine());
				}

            if (onAttackTime!=null)
				onAttackTime(currentAttack, currentCombo, targets);
            }


		private IEnumerator<float> releaseActionsCoRoutine()
			{
			yield return Timing.waitForSeconds(comboMode==ComboMode.Assisted ? assistedComboSpeed : comboSpeed);
			character.releaseActions();
			}


		public Character[] getHittingTargets(float range)
			{
			return getHittingTargets(range, transform.position);
			}

		public Character[] getHittingTargets(float range, Vector2 originPosition)
			{
			Collider2D[] hits = Physics2D.OverlapCircleAll(originPosition, range, targetLayers);

			Character[] targets = hits.transformTo<Collider2D, Character>(obj =>
				{
				Transform hitTransform = obj.gameObject.transform.parent;

				if (hitTransform!=transform && !character.isBehindUs(hitTransform.position))
					{
					Character targetCharacter = hitTransform.GetComponent<Character>();

					if (!targetCharacter.dead && !targetCharacter.state.isDodge()
					    && !targetCharacter.state.isDeathHit())
						return targetCharacter;
					}
				return null;
				}, true);


			//put the closest one (main receiver) at the begining
			float minDistance = 99f;
			float distance;
			int closestIndex = 0;

			for (int i = 0; i<targets.Length; i++)
				{
				distance = Vector2.Distance(originPosition, targets[i].transform.position);
				if (distance<minDistance)
					{
					minDistance = distance;
					closestIndex = i;
					}
				}

			if (closestIndex!=0)
				{
				Character swapTarget = targets[0];
				targets[0] = targets[closestIndex];
				targets[closestIndex] = swapTarget;
				}

			return targets;
			}


        private IEnumerator<float> attackCoRoutine()
			{
			yield return Timing.waitForRoutine(character.waitForAnimationUpdate());
			float duration = character.getAnimationDuration();
			float startTime = Time.time;

			if (onAttack!=null)
				onAttack(currentAttack, currentCombo, false);

			while (true)
				{
				//holdPosition for the attack connected frame and then holdPosition 1 visual frame to hitstop on the next one
				if (attackConnected && attackTimeHappened)
					{
					if (isMainAttackFrame && currentAttack.finisher)
						{
						character.pauseAnimatorsAndVfx();

						CameraController.instance.shake(finisherHitScreenShake);

						yield return Timing.waitForSeconds(finisherHitStop);

						character.resumeAnimators();
						break;
						}
						else
						{
						character.pauseAnimators();
						yield return Timing.waitForSeconds(character.movements.jumping ? airHitStop : hitStop);
						character.resumeAnimators();

						if (isMainAttackFrame)
							break;
							else
							attackTimeHappened = false;							
						}
					}

            	yield return 0;
				}

			yield return Timing.waitForSeconds(duration-(Time.time-startTime));

			character.control.clearAttacksBuffer();

            attackFinished(true);
            }


        public void attackFinished(bool propagateEvent)
			{
			if (propagateEvent && onAttackFinished!=null)
				onAttackFinished(currentAttack, currentCombo, attackConnected);

			attacking = false;
			attackConnected = false;
            }    


        public void clearCombo()
        	{
        	currentComboStatus = ComboManager.ComboStatus.Invalid;
			currentCombo.Clear();

			if (onComboCleared!=null)
				onComboCleared();
        	}

		

        #endregion



        #region RECEIVE DAMAGE

        public bool receiveDamage(int damage, Character attacker, Vector2 hitDirection, CharacterState hitState, bool isMainReceiver)
			{
			if (currentAttacker!=null && character.type==Director.CharacterType.Player
			    && character.state.isHit()
			    && currentAttacker.instanceId!=attacker.instanceId)
				return false;//we are already getting hit by someone else

			ReceiverData rData = attacker.attacks.receiverData;
			receiveDamageFinished(false);//reset in case we are receiving damage upon receiving damage, but dont propagate event of finishing...

			if (character.state.isBlock())
				{
				if ((hitDirection.x>0 && !character.movements.aimingRight)
				    || (hitDirection.x<0 && character.movements.aimingRight))//only block attacks from front
					{
					float force = attacker.attacks.currentAttack.isLightAttack() 
						? rData.blockHitStun.light : rData.blockHitStun.strong;

					character.movements.rigidBody.AddForce(new Vector2(force*hitDirection.x, 0), ForceMode2D.Impulse);
					return false;
					}
				}	

			character.control.clearAttacksBuffer();
			character.takeDamage(damage);
			//Spawn Particle System
			SpawnHitEffect(transform.position);
			currentAttacker = attacker;
            receivingDamage = true;
            currentReceivedAttackDirection = hitDirection;
            currentReceivedAttack = attacker.attacks.currentAttack;
			character.movements.resetHorizontalMovement();

			character.setState(hitState, receiveDamageCoRoutine(attacker, hitDirection, isMainReceiver));

			lastReceiveDamageWasBack = !lastReceiveDamageWasBack;

            if (onReceiveDamage!=null)
                onReceiveDamage(attacker.attacks.currentAttack);

            return true;
            }


		private IEnumerator <float> receiveDamageCoRoutine(Character attacker, Vector2 hitDirection, bool isMainReceiver)
			{
			yield return Timing.waitForRoutine(character.waitForAnimationUpdate());
			Vector2 force = Vector3.zero;
			ReceiverData rData = attacker.attacks.receiverData;
			bool attackIsFinisher = attacker.attacks.currentAttack.finisher 
				&& attacker.attacks.isMainAttackFrame && isMainReceiver;

			if ((attackIsFinisher || character.state==CharacterState.AirTriggerHit) 
				&& character.isBehindUs(attacker.transform.position))
				character.movements.aimingRight = !character.movements.aimingRight;
					
			character.pauseAnimators();
			character.movements.rigidBody.velocity = Vector2.zero;
			attacker.movements.rigidBody.velocity = Vector2.zero;

			if (character.spriteFlasher!=null)
				character.spriteFlasher.flash(0f, 0.075f, 0.15f);

			if (character.state==CharacterState.AirTriggerHit)
				{
				yield return Timing.waitForRoutine(shake(hitDirection.x, rData.airTriggerHitShake), Timing.Segment.FixedUpdate);
				character.resumeAnimators();

				float oldJumpDelay = character.movements.jumpSettings.forceDelay;
				character.movements.jumpSettings.forceDelay = 0f;
				character.movements.jump(CharacterMovements.JumpType.BigHit, true);

				//check for upppress to follow
				float checkStartTime = Time.time;
				Vector2 followTimes = new Vector2(0.02f, 0.1f);
				bool pressed = false;
				float elapsed = 0f;

				while (elapsed<=followTimes.y)
					{
					yield return 0;

					if (attacker.control.rawMovementDirection.y>0)
						pressed = true;

					if (pressed && elapsed>followTimes.x)
						break;

					elapsed = Time.time-checkStartTime;
					}

				if (pressed)
					attacker.movements.jump(CharacterMovements.JumpType.Big, true);

				character.movements.jumpSettings.forceDelay = oldJumpDelay;

				//check if we start falling down 
				while (true)
					{
					if (character.movements.rigidBody.velocity.y<=0)
						{
						character.setState(CharacterState.DownJump, false);
						receiveDamageFinished(true);
						yield break;
						}

					yield return 0;
					}
				}	


			//air hits
			if (character.movements.jumping)
				{
				character.movements.freeze("y");
				attacker.movements.freeze("y");

				if (attackIsFinisher)
					yield return Timing.waitForRoutine(shake(hitDirection.x, rData.airFinisherHitShake), Timing.Segment.FixedUpdate);
					else
					yield return Timing.waitForRoutine(shake(hitDirection.x, rData.airHitShake), Timing.Segment.FixedUpdate);

				character.movements.unFreeze();
				attacker.movements.unFreeze();
				character.resumeAnimators();
				force = rData.airHitStun;
				attacker.movements.addForce(force);

				if (attackIsFinisher)
					{
					character.setState(CharacterState.AirFinisherHit, false);
					force = attacker.attacks.currentAttack.type==ComboManager.AttackType.StrongAirPunchDown
						? rData.airDownFinisherStun : rData.airFinisherStun;
					force.x *= -1;
					character.movements.handleMovement = false;
					character.movements.addForce(force);
					yield break;
					}
					else
					{			
					force = rData.airHitStun;
					force.y *= rData.airHitStunMultiplier;
					force.x *= character.isBehindUs(attacker.transform.position) ? 1 : -1;
					character.movements.addForce(force);

					//holdPosition for us to start going down and that means we are done
					while (character.movements.rigidBody.velocity.y>0)
						yield return 0;

					receiveDamageFinished(true);
					character.setState(CharacterState.DownJump, false);														
					yield break;
					}
				}


			//ground hits
			if (attackIsFinisher)
				yield return Timing.waitForRoutine(shake(hitDirection.x, rData.finisherHitShake), Timing.Segment.FixedUpdate);
			else
				{
				if (attacker.attacks.currentAttack.isLightAttack())
					yield return Timing.waitForRoutine(shake(hitDirection.x, rData.lightHitShake), Timing.Segment.FixedUpdate);
					else
					yield return Timing.waitForRoutine(shake(hitDirection.x, rData.strongHitShake), Timing.Segment.FixedUpdate);
				}
										
			character.resumeAnimators();	

			if (attackIsFinisher)
				{
				checkForFinsherDeathOrFinisherHit();
				force = rData.finisherStun;
				force.x *= -1;
				character.movements.handleMovement = false;
				character.movements.addForce(force);
				yield break;
				}
				else
				{
				force.x = attacker.attacks.currentAttack.isLightAttack() 
					? rData.hitStun.light : rData.hitStun.strong;
				force.x *= hitDirection.x*(character.movements.aimingRight ? 1 : -1);
				character.movements.addForce(force);
				}
            }

        
        public void receiveDamageFinished(bool propagateEvent)
			{
			if (propagateEvent && onFinishedReceivingDamage!=null)
				onFinishedReceivingDamage(currentReceivedAttack);

			currentReceivedAttack = new ComboManager.Attack();
			currentAttacker = null;
			receivingDamage = false;
            }


        public void checkForFinsherDeathOrFinisherHit(bool isAir = false)
			{
			//if we are actually pending death we need to decide if we either play the finisher hit DEATH
			//or play the regular and go idle death
			float chance = character.pendingDeath ? finisherDeathToIdleDeathChance : 1f;

			if (isAir)
				{
				character.setState(UnityEngine.Random.value>chance 
					? CharacterState.AirFinisherHitGroundDeath : CharacterState.AirFinisherHitGroundHit, false);
				}
				else
				{
				character.setState(UnityEngine.Random.value>chance 
					? CharacterState.GroundFinisherDeath : CharacterState.GroundFinisherHit, false);
				}
			}
		// Function for Spawning in a Particle Effect Prefab
		private void SpawnHitEffect(Vector3 position)
       {
			Instantiate(HitEffect, position, Quaternion.identity);
       }

		#endregion



		#region MISC
	
		private IEnumerator<float> clearComboCoRoutine(float delay)
			{
			yield return Timing.waitForSeconds(delay);
			clearCombo();
			}

		private IEnumerator<float> shake(float direction, HitShakeData shakeData)
			{
			bool sideFlag = !(direction*(character.movements.aimingRight ? 1 : -1)>0);
			Vector2 shakeOffset = Vector2.zero;

			for (int i = 0; i<shakeData.cycles; i++)
				{
				shakeOffset.x = sideFlag ? shakeData.force 
					: -shakeData.force;

				yield return Timing.waitForRoutine(character.movements.moveToPositionCoRoutine(shakeOffset), Timing.Segment.FixedUpdate);
				sideFlag = !sideFlag;
				}
			}

		#endregion
        }   
    }