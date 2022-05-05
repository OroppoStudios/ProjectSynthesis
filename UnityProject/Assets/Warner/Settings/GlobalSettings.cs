using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;

namespace Warner 
	{
	public enum WindowMode {FullScreen, Window}

	public static class GlobalSettings
		{
		#region MEMBER FIELDS	

		public delegate void EventsHandlers();
		public static Settings settings;
		public static event EventsHandlers onChange;
		public static event EventsHandlers onSetDefaults;
		public static event EventsHandlers onSettingsLoaded;

		[Serializable]
		public enum TextureQuality{Low,	Mid,High}

		[Serializable]
		public struct PlayerControl
			{
			public InputPlayer player;
			public Action[] actions;
			}

		public enum InputPlayer {One, Two, Three, Four}

		[Serializable]
		public class Action
			{
			public string name;
			public JoystickButton button;
			public string key;

			public Action(string name, JoystickButton button, string key)
				{
				this.name = name;
				this.button = button;
				this.key = key;
				}
			}		

		private const string filename = "settings.dat";

		#endregion



		#region INIT STUFF

		public static void init(bool fromFile = true)
			{
			settings = new Settings();	
			setDefaults();

			if (fromFile)
				loadSettings();

			if (AudioManager.instance!=null)
				AudioManager.instance.updateAudioSourcesVolumes();							
				
			if (Application.platform==RuntimePlatform.IPhonePlayer)
				settings.textureQuality = TextureQuality.Low;
				else
				{
				string[] sResolution = settings.resolution.Split('x');
				int width;
				int height;
				
				int.TryParse(sResolution[0], out width);
				int.TryParse(sResolution[1], out height);
				
				bool fullScreen = settings.windowMode==0;
				Screen.SetResolution(width, height, fullScreen);
				}

			QualitySettings.vSyncCount = (settings.vSync) ? 1 : 0;

			if (onSettingsLoaded != null)
				onSettingsLoaded();
			}


		private static void setDefaults()
			{
//			settings.language = Application.systemLanguage.ToString().ToLower();
//			if (settings.language=="unknown")
			settings.language = "english";

			settings.vSync = true;
			settings.masterVolume = 0.6f;
			settings.soundEffectsVolume = 0.8f;			
			settings.musicVolume = 0.8f;
			settings.windowMode = WindowMode.FullScreen;
			settings.playerControls = new PlayerControl[]{};

			int resolutionIndex = Math.Max(0, Screen.resolutions.Length-1);
			if (Application.platform!=RuntimePlatform.IPhonePlayer)
				settings.resolution = Screen.resolutions[resolutionIndex].width+"x"+Screen.resolutions[resolutionIndex].height;

			settings.textureQuality = TextureQuality.High;		

			if (onSetDefaults!=null)
				onSetDefaults();
			}

		#endregion


		
		#region SAVE AND LOAD

		private static void loadSettings()
			{
			string filePath = Application.persistentDataPath+"/"+filename;		
				
			if (File.Exists(filePath))
				{
				BinaryFormatter binaryFormatter = new BinaryFormatter();
				FileStream fileStream = File.Open(filePath, FileMode.Open);
				try
					{
					settings = (Settings) binaryFormatter.Deserialize(fileStream);
					} 
				catch {}

				fileStream.Close();
				}
			}


		public static void saveSettings()
			{
			QualitySettings.vSyncCount = (settings.vSync && !Application.isEditor) ? 1 : 0;

			if (settings.vSync)
				Application.targetFrameRate = -1;
				else
				Application.targetFrameRate = 144;

			Misc.saveConfigFile<Settings>(settings, Application.persistentDataPath+"/"+filename);

			AudioManager.instance.updateAudioSourcesVolumes();

			if (onChange!=null)
				onChange();
			}

		#endregion
		}

	}