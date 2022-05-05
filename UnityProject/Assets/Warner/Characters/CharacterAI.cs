using UnityEngine;
using System;
using System.Collections.Generic;
using Warner.AnimationTool;

namespace Warner
    {
    [RequireComponent(typeof(Character))]
    public class CharacterAI : MonoBehaviour
        {
        #region MEMBER FIELDS

        public LayerMask targetsLayer;
        public bool canAttack;
        public Distances distances;
		public ReactionTimes reactionTimes;
		public Taunt taunt;
		[Range (0f, 1f)] public float blockChance = 0.5f;

		[NonSerialized] public Character target;

		[Serializable]
		public class Distances
			{
			public float closeToTarget = 1.5f;
			public float mediumToTarget = 5f;
        	public float closeToSameTypeOther = 4f;
        	public float minMovingDistance = 6f;
			}

		[Serializable]
		public class ReactionTimes
			{
			public Vector2 attackAfterCombo = new Vector2(0.25f, 0.4f);
			public Vector2 holdPosition = new Vector2(0.35f, 1.5f);
			public Vector2 block = new Vector2(0.25f, 0.65f);
			}

		[Serializable]
		public class Taunt
			{
			[Range (0f, 1f)] public float chanceWhileHoldingPosition = 0.25f;
			[Range (0f, 1f)] public float chanceToHoldPositionOnFinish = 0.5f;
			public Vector2 startDelayWhileHoldingPosition = new Vector2(0.2f, 0.4f);
			}


		public enum State {Inactive, Idle, Chase, CloseToTarget, 
			CloseRangeAttack, HoldPosition, ReceivingDamage, Blocking, 
			Taunt, SpecialAttack}

		public delegate void RoutineEventHandler();
		public event RoutineEventHandler onChasing;
		public State state;        
       
        protected Character character;

        private IEnumerator<float> stateRoutine;
		private IEnumerator<float> closeToTargetCheckRoutine;
		private Wait wait = new Wait();	        

        private enum DistanceRange {Close, Medium, Far}
		private delegate void StateHandler();
		private bool active;

        private class Wait
        	{
        	public float duration;
        	private bool _active;
			private float time;

        	public bool active
				{
				get
					{
					if (!_active)
						return false;

					_active = Time.time-time<duration;
					return _active;
					}
				set
					{
					time = Time.time;
					_active = value;
					}
				}
        	}

		#endregion



		#region INIT

		protected virtual void Awake()
            {
            character = GetComponent<Character>();
            }

		protected virtual void OnEnable()
			{
			active = true;
			character.attacks.onComboCleared += onComboCleared;
			character.attacks.onReceiveDamage += onReceiveDamage;
			character.attacks.onFinishedReceivingDamage += onFinishedReceivingDamage;
			character.movements.onTauntEnd += onTauntEnd;
			initRoutines();
			}		

		public void initRoutines()
			{
			setState(State.Idle, idleCoRoutine());
			}

		#endregion



		#region DESTROY

		protected virtual void OnDisable()
			{
			character.attacks.onComboCleared -= onComboCleared;
			character.attacks.onReceiveDamage -= onReceiveDamage;
			character.attacks.onFinishedReceivingDamage -= onFinishedReceivingDamage;
			character.movements.onTauntEnd -= onTauntEnd;
			killRoutines();
			}


		public void killRoutines()
			{
			if (closeToTargetCheckRoutine!=null)
				{
				Timing.kill(closeToTargetCheckRoutine);
				closeToTargetCheckRoutine = null;
				}

			if (stateRoutine!=null)
				{
				Timing.kill(stateRoutine);
				stateRoutine = null;
				}

			state = State.Inactive;
			}

		#endregion



        #region EVENT HANDLERS

        private void onReceiveDamage(ComboManager.Attack attack)
	        {
			if (getTargetDirection().x < 0f)
				character.movements.aimingRight = false;
				else
				character.movements.aimingRight = true;

			setState(State.ReceivingDamage);
	        }

		private void onFinishedReceivingDamage(ComboManager.Attack attack)
	        {
	        if (state!=State.Blocking)	       
				setState(State.Idle, idleCoRoutine());
	        }


		private void onComboCleared()
			{
			if (state!=State.CloseRangeAttack)
				return;

			weHaveToTurnCheck(true);

			if (character.isRunningAwayFromUs(target))
				setState(State.HoldPosition, holdPositionCoRoutine(true));
				else
				{
				setState(State.CloseToTarget, closeToTargetCoRoutine());
				wait.active = true;
				wait.duration = reactionTimes.attackAfterCombo.getRandom();
				}
	        }


		private void onTauntEnd(object data)
	        {
			if (UnityEngine.Random.value>1f-taunt.chanceToHoldPositionOnFinish)
				setState(State.HoldPosition, holdPositionCoRoutine());
				else
				setState(State.Idle, idleCoRoutine());
	        }


	    public void targetAttackingNearUs()
			{
			if (active && enabled)
				{								
				//dont try block if we are receiving damage 
				//(this allow combos to come nicely through us)
				//but, do allow to block if we are on a finisher hit
				if (!(state==State.ReceivingDamage && !character.state.isFinisherHit())
					&& (UnityEngine.Random.value>1f-blockChance || state==State.Blocking))//random or always block if already blocking
					setState(State.Blocking, blockingCoRoutine());
				}
			}

		#endregion



        #region STATES		

		public void setState(State targetState, IEnumerator<float> coRoutine)
			{	
			setState(targetState);

			stateRoutine = coRoutine;
			Timing.run(stateRoutine);							
			}	


		public void setState(State targetState, bool killCoRoutine = true)
			{
			if (killCoRoutine && stateRoutine!=null)
				Timing.kill(stateRoutine);

			if (closeToTargetCheckRoutine==null)
				{
				closeToTargetCheckRoutine = closeToTargetCheckCoRoutine();
				Timing.run(closeToTargetCheckRoutine);
				}

			wait.active = false;
			state = targetState;
        	}	

		protected virtual IEnumerator<float> closeToTargetCheckCoRoutine()
			{
			//we use this routine to always be checking
			//if we are now close to the target
			//this helps acount scenarios were we might be doing something 
			//where we dont move like holding position 
			//but the target came close to us
			while (true)
				{
				yield return 0;

				if (target==null)
					continue;

				//we also check if we are dead or going to
				if (character.pendingDeath || character.dead)
					{
					enabled = false;
					yield break;
					}

				switch (state)
					{
					case State.Chase:
					case State.HoldPosition:
					case State.Idle:
						if (rangeToTarget==DistanceRange.Close)
							setState(State.CloseToTarget, closeToTargetCoRoutine());
					break;
					}
				}
			}


		protected virtual IEnumerator<float> idleCoRoutine()
			{
			if (!character.movements.jumping)
				character.setState(CharacterState.Idle);

			while (true)
				{
				yield return 0;

				if (target==null)
					{
					target = acquireTarget();
					continue;
					}

				if (rangeToTarget!=DistanceRange.Close)
					{
					setState(State.Chase, chaseCoRoutine());
					yield break;
					}	
				}
			}


		protected virtual IEnumerator<float> chaseCoRoutine()
			{
			Vector2 targetDirection;

			while (true)
				{
				yield return 0;

				targetDirection = getTargetDirection();

				switch (checkOtherCharactersWhileChasing(targetDirection))
					{
					case State.HoldPosition:
						character.control.rawMovementDirection = Vector2.zero;
						setState(State.HoldPosition, holdPositionCoRoutine(true));
						yield break;
					}

				//dont move on air for the moment cause this makes us move after we received 
				//an air attack and we are falling, if later on we want movement here
				//we need to account for that

				if (!character.movements.jumping)
					{
					character.control.rawMovementDirection = targetDirection;

					if (onChasing!=null)
						onChasing();
					}
				}
			}


		protected virtual IEnumerator<float> closeToTargetCoRoutine()
			{
			while (true)
				{
				yield return 0;

				if (state==State.CloseRangeAttack)
					continue;

				if (wait.active)
					continue;

				if (rangeToTarget!=DistanceRange.Close && state!=State.CloseRangeAttack)
					{
					setState(State.Chase, chaseCoRoutine());						
					yield break;
					}

				character.control.rawMovementDirection = Vector2.zero;
					
				if (canAttack)
					{
					if (target.state.isHit() || target.state.isAttack())
						continue;

					//check if someone is already attacking our target
					bool friendAttackingOurTarget = false;
					foreach (Character friend in Director.instance.getFriendCharacters(character))
						{
						if (friend.ai.state == State.CloseRangeAttack &&		friend.ai.target.instanceId == target.instanceId)
							{
							friendAttackingOurTarget = true;
							break;
							}
						}

					if (!friendAttackingOurTarget)
						{
						weHaveToTurnCheck(true, false);
						setState(State.CloseRangeAttack, false);
						autoCombo();
						yield break;
						}
					}
				}
			}


		protected virtual IEnumerator<float> holdPositionCoRoutine(bool canTaunt = false)
			{
			float duration = reactionTimes.holdPosition.getRandom();
			float startTime = Time.time;
			bool triedToTaunt = false;
			float tauntAfterTime = taunt.startDelayWhileHoldingPosition.getRandom();
			float elapsed;

			while (true)
				{
				yield return 0;
				elapsed = Time.time-startTime;

				if (elapsed>0.1f)
					weHaveToTurnCheck(true);

				character.control.rawMovementDirection = Vector2.zero;

				if (!triedToTaunt && canTaunt && elapsed>=tauntAfterTime)
					{
					if (UnityEngine.Random.value>1f-taunt.chanceWhileHoldingPosition)
						{
						setState(State.Taunt);
						character.movements.taunt();
						yield break;
						}

					triedToTaunt = true;
					}

				if (character.state!=CharacterState.Taunt 
					&& elapsed>duration && rangeToTarget!=DistanceRange.Close)
					{	
					setState(State.Chase, chaseCoRoutine());						
					yield break;
					}
				}
			}	


		protected virtual IEnumerator<float> blockingCoRoutine()
			{
			float startTime = Time.time;
			character.movements.block();
			float duration = reactionTimes.block.getRandom();

			while (Time.time-startTime<duration)
				{
				yield return 0;
				character.control.blockPressed = true;
				}

			character.control.blockPressed = false;

			while (character.state.isBlock())
				yield return 0;

			setState(State.Idle, idleCoRoutine());
			}

		#endregion



		#region MISC

		private State checkOtherCharactersWhileChasing(Vector2 targetDirection)
			{
			Vector2 offsetWithFriend;
			float friendDistance;
			bool friendInFrontOfUs = false;		

			float targetDistance = Vector2.Distance(target.transform.position, transform.position);
											
			foreach (Character friend in Director.instance.getFriendCharacters(character))
				{
				offsetWithFriend = (friend.transform.position-transform.position).normalized;
				friendDistance = Vector2.Distance(friend.transform.position, transform.position);
				friendInFrontOfUs = (offsetWithFriend.x<0 && targetDirection.x<0) || ((offsetWithFriend.x>0 && targetDirection.x>0));

				if (friendInFrontOfUs && friendDistance>targetDistance)
					friendInFrontOfUs = false;

				if (friendInFrontOfUs)
					{
					if (friendDistance<=distances.closeToSameTypeOther
					    && !friend.state.isTurn())
						{
						return State.HoldPosition;
						}
					}

				//TODO we need to check if a friend just stopped cause he is close range to player
				//and we are about to do the same almost in the same position
				//if so we need to move to another position or hold position moving back
				//or something that doesnt leave us almost in the same spot as our friend
				}

			return State.Chase;
			}

		private Vector2 getTargetDirection()
			{
			Vector2 direction = (target.transform.position-transform.position).normalized;

			if (direction.x!=0)
				direction.x = direction.x>0 ? 1 : -1;

			if (direction.y!=0)
				direction.y = direction.x>0 ? 1 : -1;

			return direction;
			}	


		private bool weHaveToTurnCheck(bool autoTurn = false, bool playTurnAnimation = true)
			{
			bool behind = character.isBehindUs(target.transform.position);

			if (behind && autoTurn)
				character.idleTurnAround(playTurnAnimation);

			return behind;
			}


		private DistanceRange rangeToTarget
			{
			get
				{
				float distance;

				switch (character.type)
					{
					case Director.CharacterType.Player:
					case Director.CharacterType.GroundEnemy:
						distance = Mathf.Abs(target.transform.position.x-transform.position.x);
					break;
					default://air later down the road
						distance = Vector2.Distance(target.transform.position, transform.position);
					break;
					}	

				if (distance<distances.closeToTarget)
					return DistanceRange.Close;

				if (distance<distances.mediumToTarget)
					return DistanceRange.Medium;

				return DistanceRange.Far;		
				}
			}

		#endregion



		#region TARGET

		protected Character acquireTarget()
			{
			Character[] characters = UnityEngine.Object.FindObjectsOfType<Character>();
            characters = characters.transformTo<Character, Character>(obj=>
                {
				if (targetsLayer.contains(obj.gameObject.layer) && obj!=character && !obj.pendingDeath && !obj.dead)
                    return obj;
                    else
                    return null;
                }, true);

            if (characters.Length>0)
                return characters.getRandom();
                else
                return null;
			}

		#endregion



		#region AUTOCOMBO

		public void autoCombo()
			{
			if (character.attacks.availableCombos.Length==0)
				return;

			ComboManager.Attack[] combo = character.attacks.availableCombos.getRandom().progression;

			for (int i = 0; i<combo.Length; i++)
				character.control.attack(combo[i]);
			}

        #endregion
        }
    }
