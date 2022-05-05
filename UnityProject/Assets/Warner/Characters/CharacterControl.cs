using UnityEngine;
using System.Collections.Generic;
using System;
using Game;

namespace Warner
	{
	public class CharacterControl: MonoBehaviour
		{
		#region MEMBER FIELDS

		public ControlMode controlMode;
		public float possibleStopIgnoreTime = 0.07f;

		[NonSerialized] public Vector2 movementDirection;
		[NonSerialized] public Vector2 rawMovementDirection;
		[NonSerialized] public Vector2 rightMovementDirection;
		[NonSerialized] public Vector2 rawRightMovementDirection;
		[NonSerialized] public Character character;
		[NonSerialized] public bool blockPressed;
		[NonSerialized] public bool jumpPressed;
		[NonSerialized] public GlobalSettings.InputPlayer inputPlayer;

		public enum InputType {Movement, Attack, Dodging, Jumping, Block}
		public enum ControlMode {Human, AI, Network}

		private List<InputType> inputsEnabled = new List<InputType>()
			{InputType.Movement, InputType.Jumping, InputType.Attack, InputType.Dodging, InputType.Block};
		private Vector2 inputAxis;
		private bool possibleStop;
		private float possibleStopTime;
		private Queue<ComboManager.Attack> attacksBuffer = new Queue<ComboManager.Attack>();


		#endregion

		
		
		#region INIT STUFF

		protected virtual void Awake()
			{
			character = GetComponent<Character>();
			}


		protected virtual void OnEnable()
			{			
			InputManager.onButtonsPressed += onButtonsPressed;
			}

		#endregion



		#region DESTROY STUFF

		protected virtual void OnDisable()
			{
			InputManager.onButtonsPressed -= onButtonsPressed;
			}

		#endregion



		#region INPUTS ALLOWED


		public bool inputEnabled(InputType type)
			{
			for (int i = 0; i<inputsEnabled.Count; i++)
				if (inputsEnabled[i]==type)
					return true;

			return false;
			}


		public void allowInput(InputType type)
			{
			if (!inputEnabled(type))
				inputsEnabled.Add(type);
			}


		public void disableInput(InputType type)
			{
			if (inputEnabled(type))
				{
				if (type==InputType.Movement)
					{
					rawMovementDirection = Vector2.zero;
					character.movements.movingSideX = 0;
					character.movements.movingSideY = 0;
					}

				inputsEnabled.Remove(type);
				}
			}
		

		#endregion



		#region UPDATE

		protected virtual void Update()
			{
			if (TimeManager.instance.paused || character.movements.autoMoving)
				return;
			
			switch (controlMode)
				{
				default:
					manualCharacterMovement();
				break;
				case ControlMode.AI:
					int sideX = (int) rawMovementDirection.x;
					int sideY = (int) rawMovementDirection.y;

					if (character.movements.autoMoving)
						{		
						//dont override automoving side if we are not moving
						//and if we are moving, stop the automoving
						if (sideX==0)
							break;
							else
							character.movements.autoMoving = false;									
						}

					character.movements.movingSideX = sideX;
					character.movements.movingSideY = sideY;
				break;
				case ControlMode.Network:
				break;
				}

			deQueueAttacks();			
			}


		private void manualCharacterMovement()
			{
			checkMovementButtons();

			bool assignMovement = inputEnabled(InputType.Movement);

			if (rawMovementDirection.x==0)
				{
				if (!possibleStop && character.movements.lastMovingSideX!=0)
					{
					possibleStop = true;
					possibleStopTime = Time.time;
					}

				if (possibleStop && Time.time-possibleStopTime<possibleStopIgnoreTime)
					assignMovement = false;
				}
				else
				possibleStop = false;					
			

			if (assignMovement)
				{
				//dont allow switching sides while attacking/blocking in air or on big air (when its going up)
				if (((character.state.isAttack() || character.state.isBlock()) && character.movements.jumping)
					|| (character.movements.jumping && character.movements.rigidBody.velocity.y>=0
						&& character.movements.jumpType==CharacterMovements.JumpType.Big))
					{
					if ((character.movements.aimingRight && rawMovementDirection.x<0)
						|| (!character.movements.aimingRight && rawMovementDirection.x>0))
							return;
					}

				movementDirection = rawMovementDirection;
				rightMovementDirection = rawRightMovementDirection;
				character.movements.autoMoving = false;
				character.movements.movingSideX = (int) rawMovementDirection.x;
				character.movements.movingSideY = (int) rawMovementDirection.y;
				}			
			}


		protected virtual void checkMovementButtons()
			{	
			Vector2 direction = Vector2.zero;
			Vector2 rightDirection = Vector2.zero;
			bool startBlocking = false;
			string action;
			rawMovementDirection = Vector2.zero;
			blockPressed = false;

			for (int i = 0; i<InputManager.buttonsHold.Count; i++)
				{				
				action = InputManager.buttonsHold[i].getAction(inputPlayer);

				//Enable this to check how each platform maps the raw button and fix on InputManager depending on the vendor
				//Debug.Log(InputManager.buttonsHold[i].joystickButtonName+" - "+ InputManager.buttonsHold[i].rawName);

				switch (action)
					{	
					case "Left":
						direction.x = -1;
					break;
					case "Right":
						direction.x = 1;
					break;
					case "Up":
						direction.y = 1;
					break;
					case "Down":
						direction.y = -1;
					break;
					case "RLeft":
						rightDirection.x = -1;
					break;
					case "RRight":
						rightDirection.x = 1;
					break;
					case "RUp":
						rightDirection.y = 1;
					break;
					case "RDown":
						rightDirection.y = -1;
					break;
					case "Block":
                    	blockPressed = true;

                        if (inputEnabled(InputType.Block))
							startBlocking = true;                 	
                    break;
					}				
				}	
							
			direction = InputManager.deadZonePass(direction);	
			direction.x = Mathf.Round(direction.x);
			direction.y = Mathf.Round(direction.y);
			rawMovementDirection = direction;

			rightDirection = InputManager.deadZonePass(rightDirection);
			rightDirection.x = Mathf.Round(rightDirection.x);
			rightDirection.y = Mathf.Round(rightDirection.y);
			rawRightMovementDirection = rightDirection;

			if (startBlocking)//we do the blocking at the end so all the raw movement is assign when calling it
				character.movements.block();  
			}

		#endregion



		#region EVENTS HANDLER


		protected virtual void onButtonsPressed(List<InputButton> buttons)
			{	
			if (TimeManager.instance.paused
			    || controlMode == ControlMode.AI
			    || controlMode == ControlMode.Network)
				return;

			string action;
			jumpPressed = false;

			for (int i = 0; i < buttons.Count; i++)
				{									
				action = buttons[i].getAction(inputPlayer);

				switch (action)
					{	
					case "Jump":
						jumpPressed = true;

						if (inputEnabled(InputType.Jumping))
							character.movements.jump();
					break;
					case "Dodge":
						if (inputEnabled(InputType.Dodging))
							character.movements.dodge();
					break;
					case "LightPunch":						
					case "StrongPunch":
					case "Special1":
						attack(action, rawMovementDirection);
					break;
					}							
				}	

			return;		
			}
		
		#endregion



		#region ATTACKS

		public void attack(string attackName, Vector2 direction)
			{
			ComboManager.Attack theAttack = new ComboManager.Attack();

			//Light punch is the Visible button binded for the user in main menu
			//then he asigns the visible button (lightpunch) to an action which would be our AttackType
			//depending on the stick position (vertical)

			BindingsUI.ActionType action = BindingsUI.instance.getActionByAttackAndStickPosition(BindingsUI.attackNameToAttack(attackName), BindingsUI.directionToStickPosition(direction));

			switch (action)
				{
				case BindingsUI.ActionType.Punch:
					theAttack.type = ComboManager.AttackType.LightPunchNormal;
					break;
				case BindingsUI.ActionType.Kick:
					theAttack.type = ComboManager.AttackType.StrongPunchNormal;
					break;
				case BindingsUI.ActionType.GuileKick:
					theAttack.type = ComboManager.AttackType.StrongPunchUp;
					break;
				case BindingsUI.ActionType.AvatarPunch:
					theAttack.type = ComboManager.AttackType.Special1Normal;
				break;
				default: return;
				}

			if (theAttack.toCharacterState(false)!=Warner.AnimationTool.CharacterState.None)
				attack(theAttack);      
			}


		public void attack(ComboManager.Attack theAttack)
			{
			attacksBuffer.Enqueue(theAttack);
			}


		private void deQueueAttacks()
			{
			if (inputEnabled(InputType.Attack) && attacksBuffer.Count > 0)
				{
				character.attacks.execute(attacksBuffer.Dequeue());
				}
			}

		public void clearAttacksBuffer()
			{
			if (attacksBuffer.Count>0)
				attacksBuffer.Clear();
			}

		#endregion
		}
	}