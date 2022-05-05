using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

namespace Warner
	{
	public class DebugConsole : MonoBehaviour
		{
		#region MEMBER FIELDS

		public KeyCode toggleKey;
		public Color fontColor = Color.green;
		public float verticalOffset = 10f;	
		public Transform parent;

		public delegate void StatusHandler(bool active);
		public delegate string CommandEvent(object[] args);
		public event StatusHandler onToggle;

		public static DebugConsole instance;

		private Canvas canvas;
		private ConsoleInputField inputField;
		private Dictionary<string, CommandEvent> commands = new Dictionary<string, CommandEvent>();
		private RectTransform consoleBox;
		private bool active;
		private Text commandsText;
		private Config config;

		[Serializable]
		private class Config
			{
			public Dictionary<KeyCode, string> binds = new Dictionary<KeyCode, string>();
			}

		private const float height = 720f;
		private const float fadeSpeed = 0.35f;
		private const float inputHeight = 46f;
		private const float padding = 18;
		private const int fontSize = 20;
		private const string dataFile = "debugConsole.dat";

		#endregion



		#region INIT

		protected virtual void Awake()
			{
			instance = this;
			config = Misc.loadDataFile<Config>(Application.persistentDataPath+"/"+
				dataFile);
			}


		public void init()
			{
			createGOStructure();
			commands.Clear();
			}

		#endregion



		#region CREATION

		private void createGOStructure()
			{
			GameObject mainGo = new GameObject("DebugConsole");
			canvas = mainGo.AddComponent<Canvas>();
			canvas.sortingOrder = 9999;
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.pixelPerfect = true;

			if (parent!=null)
				mainGo.transform.SetParent(parent, true);

			CanvasScaler scaler = mainGo.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920, 1080);
			mainGo.AddComponent<GraphicRaycaster>();

			consoleBox = createRectTransformGO("ConsoleBox", mainGo, Vector2.zero,
				Vector2.zero);
			consoleBox.pivot = new Vector2(0.5f, 0f);			

			RectTransform innerConsoleBox = createRectTransformGO("Inner", consoleBox.gameObject, 
				new Vector2(padding, 0f), new Vector2(-padding, -height));
							
			RectTransform inputBoxRT = 
				createRectTransformGO("InputBox", innerConsoleBox.gameObject, Vector2.zero, 
				new Vector2(0f, -(scaler.referenceResolution.y+innerConsoleBox.offsetMax.y-inputHeight)));

			Color bgColor = new Color(1f, 1f, 1f, 0.8f);
			Sprite bgSprite = Resources.Load<Sprite>("DebugConsoleBg");
			Image inputBoxImage = inputBoxRT.gameObject.AddComponent<Image>();
			inputBoxImage.color = bgColor;
			inputBoxImage.type = Image.Type.Sliced;
			inputBoxImage.sprite = bgSprite;
			inputBoxImage.raycastTarget = true;


			RectTransform inputFieldRT = createRectTransformGO("InputField", inputBoxRT.gameObject, 
				new Vector2(padding, 0f), new Vector2(-padding, 0f));

			inputField = inputFieldRT.gameObject.AddComponent<ConsoleInputField>();
			inputField.onEndEdit.AddListener(text=>
				{
				executeCommand(text.Trim());
				inputField.text = string.Empty;
				inputField.ActivateInputField();
				});

			RectTransform inputRT = createRectTransformGO("Text", inputField.gameObject, Vector2.zero, Vector2.zero);
			Text inputText = inputRT.gameObject.AddComponent<Text>();
			inputField.textComponent = inputText;
			inputText.font = Resources.Load<Font>("consola");
			inputText.fontSize = fontSize;
			inputText.alignment = TextAnchor.MiddleLeft;
			inputText.raycastTarget = true;
			inputText.supportRichText = false;
			inputText.color = fontColor;

			Color shadowColor = Color.black;
			Vector2 shadowSize = new Vector2(2f, -2f);

			Shadow shadow = inputText.gameObject.AddComponent<Shadow>();
			shadow.effectColor = shadowColor;
			shadow.effectDistance = shadowSize;

			RectTransform commandsBoxRT = createRectTransformGO("CommandsBox", 
				innerConsoleBox.gameObject, new Vector2(0f, inputHeight+2f), Vector2.zero);

			Image commandsBoxImage = commandsBoxRT.gameObject.AddComponent<Image>();
			commandsBoxImage.type = Image.Type.Sliced;
			commandsBoxImage.color = bgColor;
			commandsBoxImage.sprite = bgSprite;
			commandsBoxImage.raycastTarget = true;


			RectTransform commandsTextRT = createRectTransformGO("Text", commandsBoxRT.gameObject, 
				new Vector2(padding, padding+2), new Vector2(-padding, -(padding+2)));
			commandsText = commandsTextRT.gameObject.AddComponent<Text>();
			commandsText.font = Resources.Load<Font>("consola");
			commandsText.fontSize = fontSize;
			commandsText.alignment = TextAnchor.UpperLeft;
			commandsText.raycastTarget = false;
			commandsText.supportRichText = false;
			commandsText.color = fontColor;

			shadow = commandsText.gameObject.AddComponent<Shadow>();
			shadow.effectColor = shadowColor;
			shadow.effectDistance = shadowSize;
			toggle(false, false);
			}

		private RectTransform createRectTransformGO(string name, GameObject parent, Vector2 offsetMin, Vector2 offsetMax)
			{
			GameObject go = new GameObject(name);
			go.transform.SetParent(parent.transform, false);
			RectTransform rectTransform = go.AddComponent<RectTransform>();

			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = offsetMin;
			rectTransform.offsetMax = offsetMax;

			return rectTransform;
			}


		#endregion



		#region UPDATE

		private void Update()
			{
			if (Input.GetKeyDown(toggleKey))
				toggle(!active);

			if (!active)
				foreach (KeyValuePair<KeyCode, string> key in config.binds)
					if (Input.GetKeyDown(key.Key))
						executeCommand(key.Value);
			}

		#endregion



		#region OPEN/CLOSE


		public void toggle(bool on, bool animate = true)
			{
			active = on;
			float duration = animate ? fadeSpeed : 0;

			if (active)
				{
				inputField.interactable = true;
				inputField.ActivateInputField();
				consoleBox.anchoredPosition = new Vector2(0, verticalOffset);				
				}
				else
				{
				inputField.interactable = false;
				consoleBox.DOAnchorPosY(-height*0.75f, duration, true).SetEase(Ease.InQuad);
				}

			if (onToggle!=null)
				onToggle(active);
			}

		#endregion



		#region COMMANDS

		public bool registerCommand(string name, CommandEvent callBack)
			{
			if (commands.ContainsKey(name) || callBack==null)
				return false;

			commands.Add(name, callBack);
			return true;
			}


		public bool executeCommand(string text)
			{
			if (string.IsNullOrEmpty(text))
				return false;

			logCommandText("$ "+text);

			List<object> words = text.Split(' ').
					transformTo<string, object>(word => word).toList();

			string cmd = (string) words[0];
			words.RemoveAt(0);

			switch (cmd)
				{
				case "bind":
					return bind(text);
				case "unbind":
					return unBind(words);
				default:
					if (commands.ContainsKey(cmd))
						{
						string response;
						try
							{						
							response = commands[cmd](words.ToArray());
							logCommandText("bash: "+response);
							}
						catch (Exception e)
							{
							Debug.LogError(e.Message);
							logCommandText("bash: error");
							}

						return true;
						}
				break;
				}

			logCommandText("bash: command not found: '"+cmd+"'");
			return false;
			}

		private void logCommandText(string text)
			{
			List<string> separated = commandsText.text.Split('\n').toList();

			if (separated.Count==13)
				separated.RemoveAt(0);

			commandsText.text = string.Join("\n", separated.ToArray())+text+"\n";
			}

		#endregion



		#region BIND

		public bool bind(string text)
			{
			text = text.Replace("bind ", "");
			string command = text.Substring(0, text.LastIndexOf(' '));
			string sKey = text.Substring(text.LastIndexOf(' ')+1, 1);

			if (!(command[0]=='"' && command[command.Length-1]=='"'))
				{
				logCommandText("bash: incorrect syntax");
				return false;
				}
				else
				command = command.Substring(1, command.Length-2);

			KeyCode key = Misc.parseEnum<KeyCode>(sKey, true);
			if (!config.binds.ContainsKey(key))
				{
				config.binds.Add(key, command);

				Misc.saveConfigFile<Config>(config, Application.persistentDataPath+"/"+
				dataFile);

				logCommandText("bash: bind created");
				return true;
				}

			logCommandText("bash: "+sKey+" already binded");
			return false;
			}


		private bool unBind(List<object> args)
			{
			if (args.Count==0)
				{
				logCommandText("bash: incorrect syntax");
				return false;
				}

			string sKey = (string) args[0];
			KeyCode key = Misc.parseEnum<KeyCode>(sKey, true);
			if (config.binds.ContainsKey(key))
				{
				config.binds.Remove(key);

				Misc.saveConfigFile<Config>(config, Application.persistentDataPath+"/"+
				dataFile);

				logCommandText("bash: bind deleted");
				return true;
				}


			logCommandText("bash: bind not found");
			return false;
			}

		#endregion
		}
	}
