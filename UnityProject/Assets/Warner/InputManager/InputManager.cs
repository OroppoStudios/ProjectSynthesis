using System;
using UnityEngine;
using System.Collections.Generic;
using XInputDotNetPure;

namespace Warner 
	{
	public enum InputDirection {Up, Down, Left, Right, None};
	public enum JoystickRumbleSide {Left, Right, Both};
	public enum DeviceVendor {Unknown, Microsoft, Sony, Invalid};

	//this button is a generic perception of how i see gamepads maps should all be organized, 
	//basically almost as xbox controllers :P
	public enum JoystickButton{Start, Select, LStickUp, LStickRight, LStickDown, LStickLeft, 
		A, B, X, Y, LB, RB, LT, RT, DPadUp, DPadRight, DPadDown, DPadLeft, RStickUp, RStickRight,
		RStickDown, RStickLeft, Invalid}

	public struct Joystick
		{
		public string rawName;
		public DeviceVendor vendor;
		public int deviceId;
		public GlobalSettings.InputPlayer inputPlayer;
		}

	public struct InputButton
		{
		public string name;
		public string rawName;	
		public JoystickButton joystickButtonName;
		public float value;
		public float time;
		public InputDevice device;
		public DeviceVendor vendor;
		public int deviceIndex;
		public bool isAxis;
		public GlobalSettings.InputPlayer ownerPlayer;
		}

	public enum InputDevice {Keyboard, Mouse, Joystick}

	public class InputManager: MonoBehaviour
		{
		#region MEMBER FIELDS

		public delegate void DirectionEventHandler(InputDirection direction);
		public delegate void ButtonsEventHandler(List<InputButton> buttons);
		public delegate void JoystickEventHandler(Joystick joystick);
		public delegate void ButtonEventHandler();

		public static event ButtonsEventHandler onButtonsUp;
		public static event ButtonsEventHandler onButtonsPressed;
		public static event ButtonsEventHandler onButtonsHold;
		public static event ButtonEventHandler onAcceptPressed;
		public static event ButtonEventHandler onCancelPressed;
		public static event ButtonEventHandler onStartPressed;
		public static event DirectionEventHandler onDirectionChange;
		public static event JoystickEventHandler onJoystickConnected;
		public static DeviceVendor overridedVendor;

		public static List<InputButton> buttonsUp = new List<InputButton>();
		public static List<InputButton> buttonsPressed = new List<InputButton>();
		public static List<InputButton> buttonsHold = new List<InputButton>();	
		public static List<Joystick> joysticks = new List<Joystick>();
		public static InputDirection lastDirection;

		public const int supportedJoystickCount = 4;

		private KeyCodeData[] keyCodes;
		private JoystickRawInputData[] joystickRawAxisNames;
		private AxisDefaultState[] axisDefaultValues = new AxisDefaultState[0];	
		private Vector2 lastMouseHiddenPosition;
		private InputButton currentButton;
		private float directionChangeStartTime;
		private float sameDirectionLastTime;
		private bool ready;

		private struct AxisDefaultState
			{
			public string name;
			public float value;
			}

		private static IEnumerator<float> rumbleRoutine;
		private static IEnumerator<float> updateJoysticksRoutine;

		private struct KeyCodeData	
			{
			public KeyCode code;
			public string toString;
			public string buttonName;
			public InputDevice device;
			public int deviceIndex;
			}

		private struct JoystickRawInputData
			{
			public string name;
			public string axisNegativeName;
			public string axisPositiveName;
			public int index;
			}

		private const float playingDeadZone = 0.15f;
		private const float mainAxisDeadZone = 0.5f;
		private const float minDistanceToToggleCursor = 50f;
		private const float checkForCursorToggleInterval = 0.25f;
		private const float sameDirectionElapsedTimeToGoMaxSpeed = 1f;
		private const float sameDirectionSlowestSpeed = 0.34f;
		private const float sameDirectionFastestSpeed = 0.08f;
		
		
		#endregion
		
		
		
		#region INIT STUFF
		
		private void Awake()
			{					
			InputManagerExtensions.createJoystickGenericMaps();			
			prepareInputNames();
			Timing.run(storeAxisDefaultValuesRoutine());
			}


		private void Start()
			{
			updateJoysticksRoutine = updateGamepadsCoRoutine();
			Timing.run(updateJoysticksRoutine);
			}


		private void prepareInputNames()
			{
			List<KeyCodeData> keysList = new List<KeyCodeData>();
			KeyCodeData keyData;
			string joyIndex;
			foreach (KeyCode vKey in Enum.GetValues(typeof(KeyCode)))
				{
				keyData = new KeyCodeData();
				keyData.code = vKey;
				keyData.toString = vKey.ToString();
				keyData.deviceIndex = 0;

				if (keyData.toString.IndexOf("Joystick")!=-1)
					{
					joyIndex = keyData.toString.Replace("Joystick", "")[0]+"";
					int.TryParse(joyIndex, out keyData.deviceIndex);

					if (keyData.deviceIndex==0)//device 0 is all joys input on unity
						continue;

					keyData.deviceIndex--;

					if (keyData.deviceIndex>supportedJoystickCount)
						continue;

					keyData.buttonName = keyData.toString.Replace("Joystick"+(keyData.deviceIndex+1), "");
					keyData.device = InputDevice.Joystick;
					}
					else
					if (keyData.toString.IndexOf("Mouse")!=-1)
						{
						keyData.buttonName = keyData.toString.Replace("Mouse", "");
						keyData.device = InputDevice.Mouse;
						}
						else
						{
						keyData.buttonName = keyData.toString;
						keyData.device = InputDevice.Keyboard;
						}

				keysList.Add(keyData);
				}
			keyCodes = keysList.ToArray();

			List<JoystickRawInputData> joystickRawAxisNamesList = new List<JoystickRawInputData>();
			JoystickRawInputData inputData;
			for (int i = 1; i<=supportedJoystickCount; i++)
				{					
				for (int j = 1; j<=28; j++)
					{
					inputData = new JoystickRawInputData();
					inputData.name = "Joystick"+i+"-Axis"+j;
					inputData.index = i-1;
					inputData.axisNegativeName = "Axis"+j+"A";
					inputData.axisPositiveName = "Axis"+j+"B";
					joystickRawAxisNamesList.Add(inputData);
					}
				}
			joystickRawAxisNames = joystickRawAxisNamesList.ToArray();
			}


		private IEnumerator<float> storeAxisDefaultValuesRoutine()
			{
			//here we check whats the default state of the joystick axis
			//if something is moving we ignore the axis
			const float checksDuration = 0.5f;
			float startTime = Time.time;
			List<AxisDefaultState> axisDefaultValuesList = new List<AxisDefaultState>();	
			AxisDefaultState defaultState;

			while (Time.time-startTime<checksDuration)
				{
				checkForButtons();

				for (int i = 0; i<buttonsHold.Count; i++)
					if (buttonsHold[i].isAxis)
						{
						defaultState.name = buttonsHold[i].rawName;
						defaultState.value = buttonsHold[i].value;
						axisDefaultValuesList.Add(defaultState);
						}

				axisDefaultValues = axisDefaultValuesList.ToArray();
				yield return 0;
				}

			ready = true;
			}


		private float getAxisDefaultValue(string axisName)
			{
			for (int i = 0; i<axisDefaultValues.Length; i++)
				if (axisDefaultValues[i].name==axisName)
					return axisDefaultValues[i].value;

			return 0;
			}
		
		#endregion



		#region DESTROY


		private void OnDisable()
			{
			Timing.kill(updateJoysticksRoutine);
			}

		#endregion
		
		
		
		#region FRAME STUFF

		private void Update()
			{
			if (!ready)
				return;

			checkForButtons();			
			updateMouseCursorStatus();
			}


		private void FixedUpdate()
			{
			checkForDirectionChanges();
			}
		
		#endregion
				
		
		
		#region BUTTONS

		private void checkForButtons()
			{
			buttonsHold.Clear();
			buttonsPressed.Clear();
			buttonsUp.Clear();

			float buttonPressedValue = 0;
			float buttonHoldValue = 0;
			float buttonUpValue = 0;
			for (int i = 0; i<keyCodes.Length; i++)
				{
				buttonPressedValue = Input.GetKeyDown(keyCodes[i].code) ? 1 : 0;
				buttonHoldValue = Input.GetKey(keyCodes[i].code) ? 1 : 0;
				buttonUpValue = Input.GetKeyUp(keyCodes[i].code) ? 1 : 0;
				currentButton.time = Time.time;
				currentButton.name = keyCodes[i].buttonName;
				currentButton.rawName = keyCodes[i].toString;
				currentButton.device = keyCodes[i].device;
				currentButton.deviceIndex = keyCodes[i].deviceIndex;

				if (currentButton.device == InputDevice.Joystick)
					{
					Joystick joystick = getJoystickByDeviceId(currentButton.deviceIndex);
					currentButton.vendor = joystick.vendor;
					currentButton.ownerPlayer = joystick.inputPlayer;
					}				
									
				if (buttonPressedValue==1)
					{
					currentButton.value = buttonPressedValue;

					if (currentButton.device==InputDevice.Joystick)
						currentButton.joystickButtonName = InputManagerExtensions.buttonToJoystickGenericButton(currentButton);

					buttonsPressed.Add(currentButton);
					}

				if (buttonHoldValue==1)
					{
					currentButton.value = buttonHoldValue;

					if (currentButton.device==InputDevice.Joystick)
						currentButton.joystickButtonName = InputManagerExtensions.buttonToJoystickGenericButton(currentButton);

					buttonsHold.Add(currentButton);
					}

				if (buttonUpValue==1)
					{

					currentButton.value = buttonUpValue;

					if (currentButton.device==InputDevice.Joystick)
						currentButton.joystickButtonName = InputManagerExtensions.buttonToJoystickGenericButton(currentButton);

					buttonsUp.Add(currentButton);
					}
				}


			//AXIS PRESSED
			for (int i = 0; i<joystickRawAxisNames.Length; i++)
				{
				currentButton.rawName = joystickRawAxisNames[i].name;
				currentButton.value = Input.GetAxis(currentButton.rawName);

				if (Mathf.Abs(currentButton.value)>mainAxisDeadZone
				    && currentButton.value!=getAxisDefaultValue(currentButton.rawName))
					{
					currentButton.time = Time.time;
					currentButton.isAxis = true;
					if (currentButton.value<0)
						currentButton.name = joystickRawAxisNames[i].axisNegativeName;
						else
						currentButton.name = joystickRawAxisNames[i].axisPositiveName;

					currentButton.rawName = joystickRawAxisNames[i].name;
					currentButton.device = InputDevice.Joystick;
					currentButton.deviceIndex = joystickRawAxisNames[i].index;
					currentButton.joystickButtonName = InputManagerExtensions.buttonToJoystickGenericButton(currentButton);

					Joystick joystick = getJoystickByDeviceId(currentButton.deviceIndex);
					currentButton.vendor = joystick.vendor;
					currentButton.ownerPlayer = joystick.inputPlayer;

					buttonsHold.Add(currentButton);
					}
				}


			if (buttonsHold.Count>0 && onButtonsHold!=null)
				onButtonsHold(buttonsHold);

			if (buttonsPressed.Count>0 && onButtonsPressed!=null)
				onButtonsPressed(buttonsPressed);

			if (buttonsUp.Count>0 && onButtonsUp!=null)
				onButtonsUp(buttonsUp);

			checkForMiscButtons();
			}


		private void checkForMiscButtons()
			{
			for (int i = 0; i<buttonsUp.Count; i++)
				{
				if (buttonsUp[i].device == InputDevice.Joystick)
					{
					switch (buttonsUp[i].joystickButtonName)
						{
						case JoystickButton.A:
							if (onAcceptPressed != null)
								onAcceptPressed();
							break;
						case JoystickButton.B:
							if (onCancelPressed != null)
								onCancelPressed();
							break;
						case JoystickButton.Start:
							if (onStartPressed != null)
								onStartPressed();
							break;
						}
					}
					else
					{
					switch (buttonsUp[i].rawName)
						{
						case "Return":
							if (onAcceptPressed != null)
								onAcceptPressed();
							break;
						case "Escape":
							if (onCancelPressed != null)
								onCancelPressed();
							break;
						case "Space":
							if (onStartPressed != null)
								onStartPressed();
							break;
						}
					}
				}
			}

		
		#endregion



		#region DIRECTION STUFF

		private void checkForDirectionChanges()
			{
			InputDirection direction = InputDirection.None;		

			for (int i = 0; i<buttonsHold.Count; i++)
				{
				direction = InputDirection.None;
				
				switch (buttonsHold[i].name)
					{
					case "Axis2A":
					case "UpArrow":
						direction = InputDirection.Up;
					break;
					case "Axis2B":
					case "DownArrow":
						direction = InputDirection.Down;
					break;
					case "Axis1A":
					case "LeftArrow":
						direction = InputDirection.Left;
					break;
					case "Axis1B":
					case "RightArrow":
						direction = InputDirection.Right;
					break;
					}
				}
			
			
			if (direction!=lastDirection)
				{
				if (direction!=InputDirection.None)
					{
					if (onDirectionChange!=null)
						onDirectionChange(direction);
					
					directionChangeStartTime = Time.realtimeSinceStartup;
					}

				sameDirectionLastTime = Time.realtimeSinceStartup;
				}
				else
				{
				float elapsedFromStart = Time.realtimeSinceStartup-directionChangeStartTime;
				float elapsedFromLast = Time.realtimeSinceStartup-sameDirectionLastTime;
				float percentage = elapsedFromStart/sameDirectionElapsedTimeToGoMaxSpeed;
				float holdRate = sameDirectionSlowestSpeed*(1-percentage);

				if (holdRate<sameDirectionFastestSpeed)
					holdRate = sameDirectionFastestSpeed;

				if (direction!=InputDirection.None && elapsedFromLast>holdRate)
					{
					if (onDirectionChange != null)
						onDirectionChange(direction);

					sameDirectionLastTime = Time.realtimeSinceStartup;
					}
				}
			
			lastDirection = direction;
			}

		#endregion

		

		#region DEAD ZONE STUFF

		public static Vector2 deadZonePass(Vector2 input)
			{
			if (input.magnitude<playingDeadZone)
				return Vector2.zero;
				else
				return input.normalized*((input.magnitude-playingDeadZone)/(1-playingDeadZone));
			}


		public static bool passesMainAxisDeadZone(InputButton button)
			{
			return Mathf.Abs(button.value)>mainAxisDeadZone;
			}

		#endregion



		#region JOYSTICK STUFF

		private IEnumerator<float> updateGamepadsCoRoutine()
			{
			while (true)
				{
				updateJoysticks();
				yield return Timing.waitForSeconds(2f);
				}
			}

		public static void updateJoysticks()
			{
			string[] unityJoysticks = Input.GetJoystickNames();
			Joystick joystick;
			bool found;

			for (int i = 0; i<unityJoysticks.Length; i++)
				{
				found = false;
				for (int j = 0; j<joysticks.Count; j++)
					if (joysticks[j].deviceId==i)
						{
						found = true;
						break;
						}
										
				if (!found)//new joystick connected!
					{
					joystick = new Joystick();
					joystick.deviceId = i;//its actually the index position that unity reports, but we will treat it as a device indes
					joystick.rawName = unityJoysticks[i];
					joystick.vendor = getJoystickVendor(joystick.rawName);

					if (joystick.vendor==DeviceVendor.Invalid)
						continue;

					joysticks.Add(joystick);
					if (onJoystickConnected!=null)
						onJoystickConnected(joystick);
					}
				}
			}


		private static int getJoystickIndexByDeviceId(int id)
			{
			for (int i = 0; i<joysticks.Count; i++)
				if (joysticks[i].deviceId==id)
					return i;

			return -1;
			}


		private static Joystick getJoystickByDeviceId(int id)
			{
			int index = getJoystickIndexByDeviceId(id);
			Joystick joystick;

			if (index==-1)
				joystick = new Joystick();
				else
				joystick = joysticks[index];

			if (overridedVendor != DeviceVendor.Invalid)
				joystick.vendor = overridedVendor;

			return joystick;
			}


		public static DeviceVendor getJoystickVendor(string controllerName)
			{
			if (overridedVendor!=DeviceVendor.Invalid)
				return overridedVendor;

			if (string.IsNullOrEmpty(controllerName)
			    || (controllerName=="WIRED CONTROLLER" || controllerName==" WIRED CONTROLLER")//Ignore Steam controller for the moment
			    || controllerName.IndexOf("webcam", StringComparison.OrdinalIgnoreCase)!=-1)//Unity thinks some webcams are joysticks
				return DeviceVendor.Invalid;

			DeviceVendor detectedType = DeviceVendor.Unknown;

			if (controllerName=="Wireless Controller"
			    || controllerName=="Unknown Wireless Controller")
				detectedType = DeviceVendor.Sony;
				else
				if (controllerName.IndexOf("xbox", StringComparison.OrdinalIgnoreCase)!=-1)
					detectedType = DeviceVendor.Microsoft;

			return detectedType;
			}


		


		#endregion



		#region RUMBLE

		public static void rumble(JoystickRumbleSide side,float duration)
			{
			if (!GlobalSettings.settings.gamePadVibration)
				return;

			if (rumbleRoutine!=null)
				{
				Timing.kill(rumbleRoutine);
				stopRumble();
				}

			rumbleRoutine = rumbleCoRoutine(side, duration);
			Timing.run(rumbleRoutine);
			}


		private static IEnumerator<float> rumbleCoRoutine(JoystickRumbleSide side,float duration)
			{					
			GamePad.SetVibration(0,(side==JoystickRumbleSide.Both || side==JoystickRumbleSide.Left) ? 1 : 0,(side==JoystickRumbleSide.Both || side==JoystickRumbleSide.Right) ? 1 : 0);
			yield return Timing.waitForSeconds(duration);

			stopRumble();
			}


		private static void stopRumble()
			{
			GamePad.SetVibration(0,0,0);
			}

		#endregion



		#region CURSOR STUFF

		private void updateMouseCursorStatus()
			{
			bool hideCursor = false;

			for (int i = 0; i<buttonsHold.Count; i++)
				{
				if (buttonsHold[i].device==InputDevice.Joystick)
					{
					if (passesMainAxisDeadZone(buttonsHold[i]))
						hideCursor = true;
					}
				else
					{
					if (buttonsHold[i].device==InputDevice.Keyboard)
						hideCursor = true;
					}
				}

			if (hideCursor)
				{
				Cursor.visible = false;
				lastMouseHiddenPosition = Input.mousePosition;
				}
				else
				{
				if ((lastMouseHiddenPosition-Input.mousePosition.to2()).magnitude>minDistanceToToggleCursor
				   && !Cursor.visible)
					Cursor.visible = true;	
				}

			
			}

		#endregion



		#region PLAYERS

		public static void giveJoystickToPlayer(int joystickId, GlobalSettings.InputPlayer inputPlayer)
			{
			int index = getJoystickIndexByDeviceId(joystickId);
			if (index!=-1)
				{
				Joystick joystick = joysticks[index];			
				joystick.inputPlayer = inputPlayer;
				joysticks[index] = joystick;
				}
			}

		#endregion
		}


	#region EXTENSION METHODS

	public static class InputManagerExtensions
		{
		private static Dictionary<string, JoystickButton> microsoftToGenericMap = new Dictionary<string, JoystickButton>();
		private static Dictionary<string, JoystickButton> sonyToGenericMap = new Dictionary<string, JoystickButton>();

		public static void createJoystickGenericMaps()
			{
			//here we map each type of joystick to our generic map
			microsoftToGenericMap.Add("Button7", JoystickButton.Start);
			microsoftToGenericMap.Add("Button6", JoystickButton.Select);
			microsoftToGenericMap.Add("Button0", JoystickButton.A);
			microsoftToGenericMap.Add("Button1", JoystickButton.B);
			microsoftToGenericMap.Add("Button2", JoystickButton.X);
			microsoftToGenericMap.Add("Button3", JoystickButton.Y);
			microsoftToGenericMap.Add("Button4", JoystickButton.LB);
			microsoftToGenericMap.Add("Button5", JoystickButton.RB);
			microsoftToGenericMap.Add("Axis9B", JoystickButton.LT);
			microsoftToGenericMap.Add("Axis10B", JoystickButton.RT);
			microsoftToGenericMap.Add("Axis2A", JoystickButton.LStickUp);
			microsoftToGenericMap.Add("Axis1B", JoystickButton.LStickRight);
			microsoftToGenericMap.Add("Axis2B", JoystickButton.LStickDown);
			microsoftToGenericMap.Add("Axis1A", JoystickButton.LStickLeft);
			microsoftToGenericMap.Add("Axis5A", JoystickButton.RStickUp);
			microsoftToGenericMap.Add("Axis4B", JoystickButton.RStickRight);
			microsoftToGenericMap.Add("Axis5B", JoystickButton.RStickDown);
			microsoftToGenericMap.Add("Axis4A", JoystickButton.RStickLeft);
			microsoftToGenericMap.Add("Axis7B", JoystickButton.DPadUp);
			microsoftToGenericMap.Add("Axis6B", JoystickButton.DPadRight);
			microsoftToGenericMap.Add("Axis7A", JoystickButton.DPadDown);
			microsoftToGenericMap.Add("Axis6A", JoystickButton.DPadLeft);

			sonyToGenericMap.Add("Button9", JoystickButton.Start);
			sonyToGenericMap.Add("Button8", JoystickButton.Select);
			sonyToGenericMap.Add("Button1", JoystickButton.A);
			sonyToGenericMap.Add("Button2", JoystickButton.B);
			sonyToGenericMap.Add("Button0", JoystickButton.X);
			sonyToGenericMap.Add("Button3", JoystickButton.Y);
			sonyToGenericMap.Add("Button4", JoystickButton.LB);
			sonyToGenericMap.Add("Button5", JoystickButton.RB);
			sonyToGenericMap.Add("Button6", JoystickButton.LT);//MAC button6
			sonyToGenericMap.Add("Button7", JoystickButton.RT);//MAC button7
			sonyToGenericMap.Add("Axis2A", JoystickButton.LStickUp);
			sonyToGenericMap.Add("Axis1B", JoystickButton.LStickRight);
			sonyToGenericMap.Add("Axis2B", JoystickButton.LStickDown);
			sonyToGenericMap.Add("Axis1A", JoystickButton.LStickLeft);
			sonyToGenericMap.Add("Axis4A", JoystickButton.RStickUp);
			sonyToGenericMap.Add("Axis3B", JoystickButton.RStickRight);
			sonyToGenericMap.Add("Axis4B", JoystickButton.RStickDown);
			sonyToGenericMap.Add("Axis3A", JoystickButton.RStickLeft);
			sonyToGenericMap.Add("Axis12A", JoystickButton.DPadUp);
			sonyToGenericMap.Add("Axis11B", JoystickButton.DPadRight);
			sonyToGenericMap.Add("Axis12B", JoystickButton.DPadDown);
			sonyToGenericMap.Add("Axis11A", JoystickButton.DPadLeft);
			}


		public static GlobalSettings.PlayerControl getPlayerControls(GlobalSettings.InputPlayer player)
			{
			for (int i = 0; i<GlobalSettings.settings.playerControls.Length; i++)
				if (GlobalSettings.settings.playerControls[i].player==player)
					return GlobalSettings.settings.playerControls[i];

			return new GlobalSettings.PlayerControl();
			}


		public static string getAction(this InputButton inputButton, GlobalSettings.InputPlayer player)
			{
			//in order to get the button action we first need to know if this button
			//belongs to the actual player, this applies for joysticks
			//if its a keyboard then we allos all players to use keyboards so they can share it
			if (inputButton.device==InputDevice.Joystick && inputButton.ownerPlayer!=player)
				return string.Empty;

			GlobalSettings.PlayerControl playerControl = getPlayerControls(player);

			switch (inputButton.device)
				{
				case InputDevice.Joystick:
					for (int i = 0; i<playerControl.actions.Length; i++)
						if (playerControl.actions[i].button==inputButton.joystickButtonName)
							return playerControl.actions[i].name;				
				break;
				default://keyboard
					for (int i = 0; i<playerControl.actions.Length; i++)
						if (playerControl.actions[i].key==inputButton.name)
							return playerControl.actions[i].name;
				break;
				}

			return string.Empty;
			}


		public static JoystickButton buttonToJoystickGenericButton(InputButton inputButton)
			{
			switch (inputButton.vendor)
				{
				case DeviceVendor.Sony:
					if (!sonyToGenericMap.ContainsKey(inputButton.name))
						return JoystickButton.Invalid;

				return sonyToGenericMap[inputButton.name];
				default:
					if (!microsoftToGenericMap.ContainsKey(inputButton.name))
						return JoystickButton.Invalid;

				return microsoftToGenericMap[inputButton.name];
				}
			}
		}

	#endregion
	}