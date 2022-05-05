using System;

namespace Warner
	{
	[Serializable]
	public partial class Settings
		{
		public string language;
		public float masterVolume;
		public float soundEffectsVolume;		
		public float musicVolume;
		public WindowMode windowMode;
		public GlobalSettings.TextureQuality textureQuality;
		public bool vSync;
		public bool gamePadVibration;
		public string resolution;
		public GlobalSettings.PlayerControl[] playerControls;
		}
	}