using System;
using UnityEngine;
using System.Collections.Generic;
using Warner.AnimationTool;
using System.Reflection;

namespace Warner
    {
	[RequireComponent (typeof(CharacterAI))]
	[RequireComponent (typeof(CharacterMovements))]
	[RequireComponent (typeof(CharacterAttacks))]
	[RequireComponent (typeof(CharacterControl))]
	[RequireComponent (typeof(Rigidbody2D))]
    public class Character: AnimationController
        {
        #region MEMBER FIELDS

		public CharacterStates states;
		public ColorPalette colorPalette;
		public HitVfx[] hitVfx;
		public LineArtValues lineArt;
		public Shadow shadow;
		public Color shadingColor;
		public Color silhouetteColor;
		public HealthBarData healthBar;
		

		[Serializable]
		public struct HealthBarData
			{
			public bool enabled;
			public GameObject prefab;
			[NonSerialized] public HealthBar ui;
			}

		[Serializable]
        public class ColorPalette
        	{
        	public bool enabled;
        	public string name;
        	[Range (0, 10)] public int index;
        	}


		[Serializable]
        public struct LineArtValues
        	{
			public bool overrideColor;
        	public Color color;
        	[Range (0f, 1f)] public float alphaCorrection;
        	}


		[Serializable]
		public struct Shadow
			{
			public bool enabled;
			public Color color;
			public ShadowShaderValues aimingRight;
        	public ShadowShaderValues aimingLeft;
			}


		[Serializable]
        public struct ShadowShaderValues
            {			
            public float blur;
            public float rotation;
            public Vector2 position;
            public Vector2 skew;
            }


        public class PlayerTransforms
            {
            public Transform flipable;
            public Transform lineArt;
			public Transform shading;
			public Transform fill;
			public Transform silhouette;
			public Transform shadow;
			public Transform platformingColliderPoint;

                        
            public PlayerTransforms(Transform transform)
                {
                flipable = transform.Find("Flipable");
				platformingColliderPoint = transform.Find("PlatformingColliderPoint");
                lineArt = flipable.Find("LineArt");
				shading = flipable.Find("Shading");
				fill = flipable.Find("Fill");
				silhouette = flipable.Find("Silhouette");
				shadow = flipable.Find("Shadow");
				shadow = flipable.Find("Shadow");
                }
            }    

		[Serializable]
        public struct HitVfx
        	{
        	public string name;
        	public SortingLayer sortingLayer;
			public bool enabled;
        	public VfxData light;
			public VfxData strong;
			public VfxData airLight;
			public VfxData airStrong;
			public VfxData airTrigger;
			public VfxData special1;
			public VfxData special2;
			public VfxData special3;
        	}


		[Serializable]
        public struct VfxData
        	{
        	public bool enabled;
        	public string name;
			public string prefab;
        	public Vector2 scale;
        	public float rotation;
        	public bool randomRotation;
			public bool inPlace;
			public Vector2 offsetUp;
			public Vector2 offsetNormal;
			public Vector2 offsetDown;
			[NonSerializedAttribute] public float internalRotation;
			[NonSerializedAttribute] public Vector2 internalOffset;
        	}


        [NonSerialized] public CharacterControl control;
        [NonSerialized] public CharacterMovements movements;
        [NonSerialized] public CharacterAttacks attacks;
		[NonSerialized] public CharacterAI ai;
        [NonSerialized] public PlayerTransforms transforms;
        [NonSerialized] public IEnumerator <float> actionRoutine;
        [NonSerialized] public ColorSwap colorSwap;

        [NonSerialized] public int health = 100;
        [NonSerialized] public int initialHealth = 100;
        [NonSerialized] public bool dead;
		[NonSerialized] public bool pendingDeath;
		[NonSerialized] public CharacterState lastState;
		[NonSerialized] public IEnumerator<float> stateRoutine;
		[NonSerialized] public Director.CharacterType type;
		[NonSerialized] public int classIndex;
		[NonSerialized] public int instanceId;
		[NonSerialized] public Material shadowMaterial;

        public CharacterState state {get{return _state;}}

        public delegate void StateEventHandler(CharacterState state);
		public event StateEventHandler onStateSet;
        public event StateEventHandler onStateCompleted;

		private CharacterState _state;
        private HashSet<int> availableStates = new HashSet<int>();
		private string[] stateIndexToName;
		private enum PendingMovementAction{None, Block, Release};
		private enum PendingActionsAction{None, Block, Release}
        private PendingMovementAction pendingFixedMovementAction;
        private PendingActionsAction pendingFixedActionsAction;
                    
        private const float jumpDustTrailScale = 1f;
        private const float jumpLandedDustTrailScale = 0.8f;
        private const float dustTrailScale = 0.8f;

        #endregion



        #region INIT STUFF

        protected override void Awake()
			{
			base.Awake();

			attacks = GetComponent<CharacterAttacks>();
			control = GetComponent<CharacterControl>();
			movements = GetComponent<CharacterMovements>();
			ai = GetComponent<CharacterAI>();
			transforms = new PlayerTransforms(transform);   
			owner = this;

			colorSwap = GetComponentInChildren<ColorSwap>();
			colorSwap.lineArtSpriteRenderer = transforms.lineArt.GetComponent<SpriteRenderer>();

			transforms.shading.GetComponent<SpriteRenderer>().color = shadingColor;
			transforms.silhouette.GetComponent<SpriteRenderer>().color = silhouetteColor;

			if (shadowTrail)
				{
				shadowTrail.setSpriteRenderer(transforms.silhouette.GetComponent<SpriteRenderer>());
				shadowTrail.parent = transforms.flipable;
				}

			if (spriteFlasher)
				spriteFlasher.addSpriteRenderer(transforms.fill.GetComponent<SpriteRenderer>());

			updateAvailableStates();
			stateIndexToName = state.valuesList().ToArray();			
			}   


        protected override void Start()
            {
           	base.Start();

			if (colorPalette.enabled)
				colorSwap.swapColor(colorPalette.name, colorPalette.index);							
			//MOD
			//initLineArtShader();
			initShadow();
            }


        protected override void OnEnable()
            {               
            base.OnEnable();

            dead = false;
            pendingDeath = false;
			toggleShadow(true);
			movements.onAimingRightChange += onAimingRightChange;
			movements.onJump += onJump;
			movements.onLandedFromJump += onLandedFromJump;
			attacks.onAttackTime += onAttackTime;

			initHealthBar();
			}


		protected override void onAnimatorsReady()
			{
			base.onAnimatorsReady();
			setState(CharacterState.Idle);
			}

		private void initLineArtShader()
			{
			for (int i = 0; i<spriteRenderers.Length; i++)
				if (spriteRenderers[i].transform==transforms.lineArt)
					{
					spriteRenderers[i].material = new Material(Shader.Find("Warner/Sprites/LineArt"));
					spriteRenderers[i].material.SetFloat("_OverrideColor", lineArt.overrideColor ? 1 : 0);
					spriteRenderers[i].material.SetColor("_Color", lineArt.color);
					spriteRenderers[i].material.SetFloat("_AlphaCorrection", lineArt.alphaCorrection);
					}
			}


        #endregion

        
                
        #region DESTROY STUFF

        protected virtual void cancelActions()
            {
            movements.movingSideX = 0;
            Timing.kill(actionRoutine);
            }


        protected override void OnDisable()
            {
            base.OnDisable();
            cancelActions();

			movements.onAimingRightChange -= onAimingRightChange;
			movements.onJump -= onJump;
			movements.onLandedFromJump -= onLandedFromJump;
			attacks.onAttackTime -= onAttackTime;

			Director.instance.characterDied(this);
            }

        #endregion



        #region EVENT HANDLERS STUFF

		protected virtual void onAimingRightChange(object data)
        	{
			checkForShadowFlip();
        	}


		private void onJump(object data = null)
        	{
			toggleShadow(false);
        	}


		private void onLandedFromJump (object data = null)
        	{
			toggleShadow(true);
        	}


		protected virtual void onAttackTime(ComboManager.Attack attack, 
			List<ComboManager.Attack> comboProgression, Character[] targets)
            {
			playVfxOnHittingTargets(attack, targets);
            } 


		public void playVfxOnHittingTargets(ComboManager.Attack attack, Character[] targets)
			{
			VfxData vfxData;

			for (int i = 0; i<targets.Length; i++)
				for (int j = 0; j<hitVfx.Length; j++)
					{
					if (!hitVfx[j].enabled)
					 continue;					

					vfxData = hitVfx[j].getAdjustedData(attack);
					vfxData.spawn(targets[i].transform, this, hitVfx[j].sortingLayer);
					}	
			}	

        #endregion



        #region ANIMATION EVENTS


		protected override void onEventFired(Warner.AnimationTool.AnimationEvent data)
			{
			base.onEventFired(data);

			if (Misc.parseEnum<CharacterState>(data.animationName)!=state)
				return;

			switch (data.type)
				{
				case AnimationEventType.ToggleShadow:
					toggleShadow(data.get<bool>("enabled"));
				break;
				case AnimationEventType.Custom:
                	animationEventCustom(data);
                break;
                case AnimationEventType.Vfx:
                    animationEventVfx(data);
                break;
                case AnimationEventType.AddForce:
                    animationEventAddForce(data);
                break;
				case AnimationEventType.resetVelocity:
                    movements.rigidBody.velocity = Vector2.zero;
                break;
				case AnimationEventType.FreezeMovement:
					animationEventFreezeMovement(data);
                break;
				case AnimationEventType.UnFreezeMovement:
					movements.unFreeze();
                break;
				case AnimationEventType.BlockActions:
					blockActions();
                break;
                case AnimationEventType.ReleaseActions:
                    releaseActions();
                break;
				case AnimationEventType.DisableMovement:
                    control.disableInput(CharacterControl.InputType.Movement);
                break;
				case AnimationEventType.ReleaseMovement:
                    control.allowInput(CharacterControl.InputType.Movement);
                break;
                case AnimationEventType.AttackTime:
                    attacks.attackTime(data);
                break;
                case AnimationEventType.Move:                	
					animationEventAutoMove(data);
                break;
				case AnimationEventType.PositionMove:
					animationEventPositionMove(data);
				break;
				case AnimationEventType.SetState:
                	animationEventSetState(data);
                break;
                }
            }


		private void animationEventCustom(AnimationTool.AnimationEvent data)
			{
			if (data.contains("handleMovement"))
				{
				movements.handleMovement = data.get<bool>("handleMovement");
				}
        	}


		private void animationEventFreezeMovement(AnimationTool.AnimationEvent data)
			{
			if (!conditionalPass(data))
				return;

			movements.freeze(data.get<string>("axis"));
			}


        private void animationEventSetState(AnimationTool.AnimationEvent data)
			{
			if (!conditionalPass(data))
				return;

			string stateName = data.get<string>("name");

			CharacterState targetState = Misc.parseEnum<CharacterState>(stateName);

			setState(targetState);
			}


        private void animationEventAutoMove(AnimationTool.AnimationEvent data)
			{
			if (!conditionalPass(data))
				return;	

			if (data.contains("x"))
				{
				int x = data.get<int>("x");

				if (x == 0)
					{
					movements.autoMoving = false;
					control.allowInput(CharacterControl.InputType.Movement);
					} 
					else
					{
					movements.autoMoving = true;
					control.disableInput(CharacterControl.InputType.Movement);
					}

				movements.movingSideX = x * (movements.aimingRight ? 1 : -1);
				}
			}



		private void animationEventPositionMove(AnimationTool.AnimationEvent data)
			{
			if (!conditionalPass(data))
				return;

			if (data.contains("offset"))
				{
				Vector2 offset = data.get<Vector2>("offset");
				movements.transform.position += new Vector3(offset.x, offset.y, 0);
				}
			}



		private void animationEventVfx(AnimationTool.AnimationEvent data)
			{
			if (!conditionalPass(data))
				return;	

			string vfxName = data.get<string>("name");
			bool isChild = data.contains("isChild");
			bool destroy = data.contains("destroy");
			bool singleTime = data.get<bool>("singleTime");

			if (singleTime)
				{
				if (singleTimeMap.Contains("vfx-"+vfxName))
					return;

				singleTimeMap.Add("vfx-"+vfxName);
				}


			if (destroy)
				{
				VfxManager.instance.destroyVfxByName(vfxName, data.get<float>("fade"));
				return;
				}
									         			          
			float rotation = data.get<string>("randomRotation") == "true" ? 
				UnityEngine.Random.Range(0f, 360f) :data.get<float>("rotation");

            Vector2 offset = data.get<Vector2>("position");
            Vector2 scale = data.contains("scale") ? data.get<Vector2>("scale") : Vector2.one;

            if (!movements.aimingRight)
                {
                scale.x *= -1;
                rotation *= -1;
                offset.x *= -1;
                }

			VfxManager.instance.playVfx(vfxName, data.get<string>("animation"), transform.position.to2() + offset, 
				scale, rotation, this, isChild ? transform : null);
            }


		protected override void animationEventSfx(AnimationTool.AnimationEvent data)
			{
			if (!conditionalPass(data))
				return;			

			base.animationEventSfx(data);
			}


        private void animationEventAddForce(AnimationTool.AnimationEvent data)
			{
			if (!conditionalPass(data))
				return;		

			ForceMode2D forceType = data.contains("smooth") ? ForceMode2D.Force : ForceMode2D.Impulse;
			Vector2 force = data.get<Vector2>("force");

			if (forceType==ForceMode2D.Force)
				force *= 80f;

			movements.addForce(force, data.get<bool>("adjustWithVelocity"), forceType);
            }


		private void animationEventMove(AnimationTool.AnimationEvent data)
			{
			if (!conditionalPass(data))
				return;		

			movements.moveToPosition(data.get<Vector2>("offset"));
            }
        

        #endregion



        #region ACTION BLOCKS

        public void blockActions()
            {
            control.disableInput(CharacterControl.InputType.Jumping);
            control.disableInput(CharacterControl.InputType.Dodging);
            control.disableInput(CharacterControl.InputType.Attack);
            control.disableInput(CharacterControl.InputType.Block);
            }

        public void releaseActions()
			{           
			control.allowInput(CharacterControl.InputType.Jumping);
			control.allowInput(CharacterControl.InputType.Dodging);
			control.allowInput(CharacterControl.InputType.Attack);
			control.allowInput(CharacterControl.InputType.Block);
			}

		public void releaseActionsAndMovement()
			{
			releaseActions();
			control.allowInput(CharacterControl.InputType.Movement);
			}


		#endregion



		#region HEALTH & DEATH

		private void initHealthBar()
			{
			if (!healthBar.enabled)
				return;

			GameObject healthObject = PoolManager.instantiate(healthBar.prefab, Vector2.zero, LevelMaster.instance.stageItemsUI);
			healthBar.ui = healthObject.GetComponent<HealthBar>();
			}

        public virtual void takeDamage(int amount)
            {
            health -= amount;
            health = Mathf.Clamp(health, 0, 100);

            if (type==Director.CharacterType.Player && health==0)
            	health = 100;

			if (healthBar.enabled)
				healthBar.ui.update(health*0.01f);

            if (health==0)
                preDeath();
            }


		private void preDeath()
			{
			control.disableInput(CharacterControl.InputType.Movement);
			cancelActions();
			blockActions();
			pendingDeath = true;

			if (healthBar.enabled)
				PoolManager.Destroy(healthBar.ui.gameObject);
            }   


		public void die()
			{
			if (dead)
				return;

			dead = true;
			Timing.run(dieCoRoutine());
			}


		private IEnumerator<float> dieCoRoutine()
			{
			setSpriteRenderersSortingLayer(LevelMaster.instance.sortingLayers.deadCharacters);			

			if (!state.isDeathHit())
				{
				setState(CharacterState.Death);
				}

			PoolManager.Destroy(gameObject);

			yield break;
			}


		#endregion



		#region FRAME UPDATE


		protected override void LateUpdate()
			{
			base.LateUpdate();

			if (healthBar.enabled)
				healthBar.ui.updatePosition(transform.position);
			}


		protected virtual void FixedUpdate()
            {
			executeFixedPendingActions();
            }


        private void executeFixedPendingActions()
			{
			//we use this method when we want to disable/enable actions or movement
			//but in a deferred way, waiting to do it until next frame
			// this is helful so that we can match to the physics engine sometimes

			switch (pendingFixedMovementAction)
				{
				case PendingMovementAction.Block:
					control.disableInput(CharacterControl.InputType.Movement);
				break;
				case PendingMovementAction.Release:
					control.allowInput(CharacterControl.InputType.Movement);
				break;
				}

			switch (pendingFixedActionsAction)
				{
				case PendingActionsAction.Block:
					blockActions();
				break;
				case PendingActionsAction.Release:
					releaseActions();
				break;
				}

			pendingFixedMovementAction = PendingMovementAction.None;
			pendingFixedActionsAction = PendingActionsAction.None;
			}


        #endregion



        #region STATE STUFF

		private void updateAvailableStates()
			{
			availableStates.Clear();
			MemberInfo[] memberInfo = states.GetType().GetMembers();
			FieldInfo fieldInfo;

			for (int i = 0; i<memberInfo.Length; i++)
				{
				fieldInfo = memberInfo[i] as FieldInfo;

				if (fieldInfo!=null && (bool) fieldInfo.GetValue(states))
					availableStates.Add(stateNameToInt(fieldInfo.Name));
				}
        	}


        private int stateNameToInt(string stateName)
			{
			FieldInfo[] fieldInfos = typeof(CharacterState).GetFields(BindingFlags.Public|BindingFlags.Static);

			for (int i = 0; i<fieldInfos.Length; i++)
				if (fieldInfos[i].Name.Equals(stateName))
					return (int) fieldInfos[i].GetRawConstantValue();

			return -1;
			}


        public bool stateAvailable(CharacterState theState)
			{
			return availableStates.Contains((int) theState);
			}


        public void setState(CharacterState nextState, IEnumerator <float> nextStateRoutine, bool blockTheActions = true, bool disableMovement = true, Timing.Segment segment = Timing.Segment.Update)
            {
            setState(nextState);

            if (nextStateRoutine!=null)
                {
                if (blockTheActions)
                	blockActions();

				if (disableMovement)
					control.disableInput(CharacterControl.InputType.Movement);

                stateRoutine = nextStateRoutine;
                Timing.run(stateRoutine, segment);
                }
            }


        public void setState(CharacterState nextState, bool killStateRoutine = true)
			{
			if (!animatorsReady)
				return;		

			if (!stateAvailable(nextState))
				{
				switch (nextState)
					{
					case CharacterState.JumpIdleLanding:
						nextState = CharacterState.Idle;
					break;
					case CharacterState.JumpRunLanding:
						nextState = stateAvailable(CharacterState.PreRun) ? 
							CharacterState.PreRun : CharacterState.Run;
					break;
					case CharacterState.IdleToRun:
						nextState = CharacterState.PreRun;
					break;
					case CharacterState.PreRun:
						nextState = CharacterState.Run;
					break;
					case CharacterState.RunStop:
						nextState = CharacterState.Idle;
					break;
					case CharacterState.DodgeBackTurn:
						nextState = CharacterState.RunTurn;
					break;
					}

				//still if the next state we picked is not available then we return too
				if (!stateAvailable(nextState))
					{
					switch (nextState)
						{
						case CharacterState.PreRun:
							nextState = CharacterState.Run;
						break;
						default:
						return;
						}
					}
				}


			if (killStateRoutine && stateRoutine!=null)
				{
				Timing.kill(stateRoutine);
				stateRoutine = null;

				//we might be canceling the attack state, so we make sure we finish it properly
				if (state.isAttack())
					attacks.attackFinished(true);

				//similar if we are receiving damage and the next one is not receiving damage
				//we might be canceling the damage state routine, so we make sure we finish it properly
				if (state.isHit() && !nextState.isHit())
					attacks.receiveDamageFinished(true);

				if (state.isDodge())
					movements.dodgeEnded(state==CharacterState.DodgeFront);

				if (state.isVaulting())
					movements.vaultEnded();

				if (state==CharacterState.Taunt)
					movements.tauntEnded();

				movements.handleMovement = true;									
				releaseActionsAndMovement();
				}
							
			resumeAnimators();
			lastState = _state;
            _state = nextState;			
			playAnimation(stateIndexToName[(int) nextState]);
            onStateWasSet();	
            }


        protected virtual void onStateWasSet()
			{
			if (onStateSet!=null)
				onStateSet(_state);
			}


        #endregion



        #region ANIMATIONS

		protected override float checkAnimationNormalizedTime()
			{
			float elapsed = base.checkAnimationNormalizedTime();

			if (elapsed<1f)
				return elapsed;

			if (pendingDeath && (state.isGroundHit() || state.isDeathHit()))
				{
				die();
				return elapsed;
				}

			CharacterState targetState;

			switch (state)
				{
				case CharacterState.IdleTurn:
					targetState = control.rawMovementDirection.x!=0 ? CharacterState.IdleToRun : CharacterState.Idle;
				break;
				case CharacterState.JumpIdleLanding:    
					targetState = control.rawMovementDirection.x!=0 ? CharacterState.IdleToRun : CharacterState.Idle;
				break;
				case CharacterState.IdleToRun: 					
					targetState = control.rawMovementDirection.x!=0 ? CharacterState.PreRun : CharacterState.RunStop;
				break;
				case CharacterState.JumpRunLanding:
				case CharacterState.PreRun: 
				case CharacterState.RunTurn:  
				case CharacterState.DodgeBackTurn:  
					targetState = control.rawMovementDirection.x!=0 ? CharacterState.Run : CharacterState.RunStop;
				break;
				case CharacterState.LightAirPunch:
				case CharacterState.LightAirPunchBack:
				case CharacterState.LightAirPunchDown:
				case CharacterState.LightAirPunchDownBack:
				case CharacterState.StrongAirPunch:
				case CharacterState.StrongAirPunchDown:
				case CharacterState.SpecialAirAttackNormal:
				case CharacterState.SpecialAirAttackUp:
				case CharacterState.SpecialAirAttackDown:
				case CharacterState.SpecialAirAttack2Normal:
				case CharacterState.SpecialAirAttack2Up:
				case CharacterState.SpecialAirAttack2Down:
				case CharacterState.SpecialAirAttack3Normal:
				case CharacterState.SpecialAirAttack3Down:
					if (movements.jumping)
						targetState = CharacterState.DownJump;
					else
						targetState = control.rawMovementDirection.x!=0 ? CharacterState.IdleToRun : CharacterState.Idle;
				break;
				case CharacterState.DownJump:
					if (stateAvailable(CharacterState.BigFall))
						targetState = CharacterState.BigFall;
						else
						{
						stateCompleted();
						return elapsed;
						}
				break;
				case CharacterState.Taunt:
					targetState = control.movementDirection.x!=0 ? CharacterState.IdleToRun : CharacterState.Idle;
				break;
				case CharacterState.Vaulting:
					targetState = CharacterState.Idle;
				break;
				case CharacterState.DodgeBack:
					targetState = CharacterState.DodgeToIdle;
				break;
				case CharacterState.DodgeFront:
					if (control.rawMovementDirection.x==0)
						{
						movements.movingSideX = 0;
						targetState = CharacterState.DodgeToIdle;
						}
					else
					if (movements.movingOppositeRawDirection())
							targetState = CharacterState.RunTurn;
						else
							targetState = CharacterState.Run;
				break;
				case CharacterState.Hit:
				case CharacterState.HitBack:
				case CharacterState.HitUp:
				case CharacterState.HitUpBack:
				case CharacterState.GroundFinisherHit:
				case CharacterState.AirFinisherHitGroundHit:
				case CharacterState.DodgeToIdle:
                case CharacterState.RunStop:
				case CharacterState.TestKick:
				case CharacterState.TestKickBack:
				case CharacterState.TestPunch:
				case CharacterState.TestPunchBack:
                case CharacterState.StrongPunchNormal:
                case CharacterState.StrongPunchNormalBack:
                case CharacterState.StrongPunchDown:
                case CharacterState.StrongPunchDownBack:
                case CharacterState.StrongPunchUp:
                case CharacterState.StrongPunchUpBack:
                case CharacterState.LightPunchNormal:
                case CharacterState.LightPunchNormalBack:
                case CharacterState.LightPunchDown:
                case CharacterState.LightPunchDownBack:
                case CharacterState.LightPunchUp:
                case CharacterState.LightPunchUpBack:				
				case CharacterState.AirTriggerAttack:
				case CharacterState.SpecialAttackNormal:
				case CharacterState.SpecialAttackDown:
				case CharacterState.SpecialAttackUp:
				case CharacterState.SpecialAttack2Normal:
				case CharacterState.SpecialAttack2Down:
				case CharacterState.SpecialAttack2Up:
				case CharacterState.SpecialAttack3Normal:
                case CharacterState.BlockToIdle:
					targetState = control.rawMovementDirection.x!=0 ? CharacterState.IdleToRun : CharacterState.Idle;
                break;
                default: 
                    stateCompleted();
                return elapsed;
                }
			
            stateCompleted();
            releaseActions();//safe measurement to always release at the end of an anim regardless it may already been released by the frame event
            setState(targetState); 
            return elapsed;   
            }


        protected virtual void stateCompleted()
            {
            if (onStateCompleted==null)
                return;
            
            onStateCompleted(state);
            }

        #endregion



		#region SHADOW

        private void initShadow()
			{
			for (int i = 0; i<spriteRenderers.Length; i++)
                if (spriteRenderers[i].material.HasProperty("_ShadowEnabled"))
                    {
                    shadowMaterial = spriteRenderers[i].material;
                    break;
                    }

			toggleShadow(shadow.enabled);
			checkForShadowFlip();
			}


        private void checkForShadowFlip()
            {
            setShadowValues(movements.aimingRight ? shadow.aimingRight : shadow.aimingLeft);                
            }


        private void setShadowValues(ShadowShaderValues values)
            {
            if (shadowMaterial==null)
            	return;

           	shadowMaterial.SetFloat("_Blur", values.blur);
           	shadowMaterial.SetColor("_Color", shadow.color);
            shadowMaterial.SetFloat("_RotationDegrees", values.rotation);
			shadowMaterial.SetFloat("_VerticalPosition", values.position.y);
            shadowMaterial.SetFloat("_HorizontalPosition", values.position.x);
			shadowMaterial.SetFloat("_HorizontalSkew", values.skew.x);
			shadowMaterial.SetFloat("_VerticalSkew", values.skew.y);
            shadowMaterial.SetFloat("_FlipAmount", movements.aimingRight ? 0 : 1);
            }


        public void toggleShadow(bool active)
			{
			if (shadowMaterial!=null)
            	shadowMaterial.SetFloat("_ShadowEnabled", active && shadow.enabled ? 1 : 0);
            }        

        #endregion



        #region ANIMATORS

        public void pauseAnimatorsAndVfx()
			{
			pauseAnimators();

			for (int i = 0; i<VfxManager.instance.instances.Count; i++)
				if (VfxManager.instance.instances[i].ownerCharacter.instanceId==owner.instanceId)
					VfxManager.instance.instances[i].pauseAnimators();
			}


		public override void resumeAnimators()
			{
			base.resumeAnimators();

			for (int i = 0; i<VfxManager.instance.instances.Count; i++)
				if (VfxManager.instance.instances[i].ownerCharacter && VfxManager.instance.instances[i].ownerCharacter.instanceId==owner.instanceId)
					VfxManager.instance.instances[i].resumeAnimators();
			}

       	#endregion



        #region MISC

		public bool isRunningAwayFromUs(Character target)
			{
			float diff = target.transform.position.x-transform.position.x;

			return (diff>0 && target.movements.movingSideX>0)
				|| (diff<0 && target.movements.movingSideX<0);
			}

        public bool isBehindUs(Vector2 targetPosition)
            {
            float diff = targetPosition.x-transform.position.x;

            return (diff>0 && !movements.aimingRight)
                || (diff<0 && movements.aimingRight);
            }

		public void idleTurnAround(bool playTurnAnimation = true)
            {
            movements.aimingRight = !movements.aimingRight;
			if (playTurnAnimation)
            	setState(CharacterState.IdleTurn);
            }


//		public static TargetCharacter getNearestCharacter(Character character, List<GameMaster.PlayerData> charactersToLookOn)
//            {
//            Character.TargetCharacter target = null;
//            float distance;
//            bool pickThisOne;
//
//            for (int i = 0;i<charactersToLookOn.Count;i++)
//                {
//                pickThisOne = false;
//                distance = character.transform.position.x-charactersToLookOn[i].character.transform.position.x;
//
//                if (target==null)
//                    {
//                    target = new Character.TargetCharacter();
//                    pickThisOne = true;
//                    }
//                    else
//                    if (target.absDistance>Mathf.Abs(distance))
//                        pickThisOne = true;
//
//                if (pickThisOne)
//                    {
//                    target.distance = distance;
//                    target.absDistance = Mathf.Abs(distance);
//                    target.character = charactersToLookOn[i].character;
//                    }
//                                        
//                target.behindUs = target.distance>0;
//
//                target.movingTowardUs = target.character.movements.movingSide!=0 && 
//                    (target.behindUs && target.character.movements.movingSide==1 
//                        || !target.behindUs && target.character.movements.movingSide==-1);
//
//                target.onSight = (target.behindUs && !character.movements.aimingRight) 
//                    || (!target.behindUs && character.movements.aimingRight);
//                }
//                    
//            return target;
//            }

        #endregion
        }   
    }