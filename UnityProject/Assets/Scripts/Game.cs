using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warner;

namespace Game
	{
	public class Game : MonoBehaviour
		{
		#region INIT

		private void Awake()
			{
			GlobalSettings.onSettingsLoaded += onSettingsLoaded;
			}


		private void OnEnable()
			{
			initConsoleCommands();
			}


		private void onSettingsLoaded()
			{
			if (GlobalSettings.settings.playerControls.Length > 0)
				return;

			GlobalSettings.PlayerControl playerControl = new GlobalSettings.PlayerControl();
			playerControl.player = GlobalSettings.InputPlayer.One;

			List<GlobalSettings.Action> actions = new List<GlobalSettings.Action>();
			actions.Add(new GlobalSettings.Action("Left", JoystickButton.LStickLeft, "LeftArrow"));
			actions.Add(new GlobalSettings.Action("Right", JoystickButton.LStickRight, "RightArrow"));
			actions.Add(new GlobalSettings.Action("Up", JoystickButton.LStickUp, "UpArrow"));
			actions.Add(new GlobalSettings.Action("Down", JoystickButton.LStickDown, "DownArrow"));
			actions.Add(new GlobalSettings.Action("Jump", JoystickButton.A, "Space"));
			actions.Add(new GlobalSettings.Action("LightPunch", JoystickButton.X, "S"));
			actions.Add(new GlobalSettings.Action("StrongPunch", JoystickButton.Y, "D"));
			actions.Add(new GlobalSettings.Action("Special1", JoystickButton.LB, "E"));

			/*
			actions.Add(new GlobalSettings.Action("Special2", JoystickButton.RB, "Q"));			
			actions.Add(new GlobalSettings.Action("RLeft", JoystickButton.RStickLeft, "J"));
			actions.Add(new GlobalSettings.Action("RRight", JoystickButton.RStickRight, "L"));
			actions.Add(new GlobalSettings.Action("RUp", JoystickButton.RStickUp, "I"));
			actions.Add(new GlobalSettings.Action("RDown", JoystickButton.RStickDown, "K"));
			*/
			playerControl.actions = actions.ToArray();
			GlobalSettings.settings.playerControls = new GlobalSettings.PlayerControl[]{ playerControl };
			}


		#endregion



		#region DESTROY

		private void OnDestroy()
			{
			GlobalSettings.onSettingsLoaded -= onSettingsLoaded;
			}

		#endregion



		#region CONSOLE COMMANDS

		private void initConsoleCommands()
			{
			DebugConsole.instance.init();

			DebugConsole.instance.registerCommand("spawn",
				(args) =>
				{
					if (args.Length == 0)
						return "the second argument must be the thingy you want to spawn";

					Vector2 position = new Vector2(LevelMaster.instance.getSinglePlayerCharacter().transform.position.x - 5f, -0.5f);

					switch ((string)args[0])
					{
						case "clone":
							if (Director.instance.spawnCharacter(1, position) != null)
								return "clone spawned";
							else
								return "could not spawn, enemy limit already reached";
						break;
						case "sentry":
							if (Director.instance.spawnCharacter(2, position) != null)
								return "sentry spawned";
							else
								return "could not spawn, enemy limit already reached";
						break;
						case "gunner":
							if (Director.instance.spawnCharacter(3, position) != null)
								return "gunner spawned";
							else
								return "could not spawn, enemy limit already reached";
						break;
						case "orb":
							if (Director.instance.spawnCharacter(4, position) != null)
								return "orb spawned";
							else
								return "could not spawn, enemy limit already reached";
							break;
						}

					return args[0] + " is not a spawnable thingy";
				});

			}

		#endregion
	}
}
