using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace Warner
	{
	public class GameMaster: MonoBehaviour
		{
		#region MEMBER FIELDS

		public CanvasGroup loadingScreenCanvasGroup;

		[NonSerialized] public EventSystem eventSystem;
		[NonSerialized] public StandaloneInputModule eventSystemInputModule;
		[NonSerialized] public bool isServer = true;
		[NonSerialized] public List<Player> players = new List<Player>();

		[Serializable]
		public struct Player
			{
			public Character character;
			}

		public static GameMaster instance;

		public const float menusFadeTransitionDuration = 0.1f;
		public const float menusSoundTransitionDuration = 0.3f;

		private const float lowHealthAudioFadeInDuration = 10f;
		private const float lowHealthAudioFadeOutDuration = 5f;
		
		#endregion

						

		#region INIT STUFF

		private void Awake()
			{
			instance = this;	

			eventSystem = EventSystem.current;
			eventSystemInputModule = eventSystem.gameObject.GetComponent<StandaloneInputModule>();
				
			PlayerPrefs.DeleteAll();//delete unity player prefs so we dont get keybinding weirdness
			StickyKeys.disable();
			GlobalSettings.init();
			BuildManagerFlags.init();
			}
		
		#endregion



		#region DESTROY

		public void OnDestroy()
			{
			StickyKeys.restore();
			}

		#endregion


		
		#region MISC STUFF


		public float calculateDamagePercentageAccordingToRadius(float hitPosition, float centerPosition, float radius)
			{
			float distance = Mathf.Abs(hitPosition-centerPosition);
			float percentage = 1-Mathf.Clamp(distance/radius, 0, 1);

			return Mathf.Clamp(percentage, 0.1f, 1);
			}	


		#endregion
		}

	}