using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warner;
using Warner.AnimationTool;
using System;
using System.Linq;

namespace Game
	{
	public class BindingsUI : MonoBehaviour
		{
		public GameObject bindingOptionPrefab;

		private Config config;

		[Serializable]
		public class Config
			{
			public List<Action> actions = new List<Action>();
			}

		[Serializable]
		public class Action
			{
			public ActionType type;
			public Stick stick;
			public Attack attack;
			}

		[Serializable]
		public enum Stick
			{
			Center, Up, Right, Down, Left
			}

		[Serializable]
		public enum Attack
			{
			Attack1, Attack2, Attack3
			}

		[Serializable]
		public enum ActionType
			{
			Punch, AvatarPunch, Kick, GuileKick, None
			}


		public static BindingsUI instance;

		public const string configFileName = "BindingUIData";

		public void Awake()
			{
			instance = this;

			loadConfig();

			if (config.actions.Count == 0)
				{
				config.actions.Add(createAction(ActionType.Punch, Attack.Attack1, Stick.Center));	
				config.actions.Add(createAction(ActionType.Kick, Attack.Attack2, Stick.Center));
				config.actions.Add(createAction(ActionType.GuileKick, Attack.Attack2, Stick.Up));
				config.actions.Add(createAction(ActionType.AvatarPunch, Attack.Attack3, Stick.Center));
				}

			for (int i = 0; i < config.actions.Count; i++)
				createBindingItem(config.actions[i], i);


			}


		public ActionType getActionByAttackAndStickPosition(Attack attack, Stick stickPosition)
			{
			for (int i = 0; i < config.actions.Count; i++)
				if (config.actions[i].attack==attack && config.actions[i].stick==stickPosition)
					return config.actions[i].type;

			return ActionType.None;
			}


		public static Attack attackNameToAttack(string attackName)
			{
			switch (attackName)
				{				
				case "StrongPunch": return Attack.Attack2;
				case "Special1": return Attack.Attack3;
				default: return Attack.Attack1;//lightPunch
				}
			}


		public static Stick directionToStickPosition(Vector2 pos)
			{
			switch (pos.x)
				{
				case 1: return Stick.Right;
				case -1: return Stick.Left;
				}

			switch (pos.y)
				{
				case 1: return Stick.Up;
				case -1: return Stick.Down;
				}

			return Stick.Center;
			}


		private Action createAction(ActionType type, Attack attack, Stick stickPosition)
			{
			Action action = new Action();
			action.type = type;
			action.stick = stickPosition;
			action.attack = attack;
			return action;
			}


		private void createBindingItem(Action action, int index)
			{
			GameObject itemGameObject = (GameObject)Instantiate(bindingOptionPrefab, transform);
			itemGameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, ((index*90)+25)*-1);
			BindingOption bindingOption = itemGameObject.GetComponent<BindingOption>();
			bindingOption.label.text = action.type.ToString();
			bindingOption.stickPosition.text = action.stick.ToString();
			bindingOption.button.text = action.attack.ToString();
			bindingOption.buttonComponent.onClick.AddListener(() => reBindFromClick(index));
			}


		private void loadConfig()
			{
			TextAsset configAsset = Misc.loadConfigAsset(configFileName);
			config = Misc.loadConfigFile<Config>(configAsset);
			}


		public void save()
			{
			//Misc.saveConfigFile<Settings>(settings, Application.persistentDataPath + "/" + filename);
			}


		public void reBindFromClick(int index)
			{
			Debug.Log(config.actions[index].type.ToString());
			}
		}
	}