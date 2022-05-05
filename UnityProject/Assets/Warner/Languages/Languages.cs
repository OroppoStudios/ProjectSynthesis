using UnityEngine;
using LitJson;
using System;

namespace Warner
	{

	public class Languages: MonoBehaviour
		{
		#region MEMBER FIELDS

		public LanguageItem[] languages;
		public JsonData currentLanguage;

		public delegate void EventsHandler();
		public event EventsHandler onLanguageSwitched;

		public static Languages instance;

		[Serializable]
		public struct LanguageItem
			{
			public string name;
			public string text;
			public Sprite sprite;
			public JsonData texts;
			}

		#endregion



		#region INIT STUFF

		private void Awake()
			{
			instance = this;
			loadLanguages();				
			switchToLanguage(GlobalSettings.settings.language);
			}

		#endregion



		#region TEXTS STUFF
		
		public void loadLanguages()
			{
			string fileName = "";

			for (int i=0;i<languages.Length;i++)
				{
				fileName = TextExtensions.upFirst(languages[i].name);
				languages[i].texts = JsonMapper.ToObject(Resources.Load<TextAsset>("Languages/"+fileName).text);
				languages[i].sprite = Resources.Load<Sprite>("Languages/"+fileName);
				}
			}


		public void switchToLanguage(string language)
			{
			language = language.ToLower();
			for (int i = 0; i < languages.Length; i++)
				{
				if (languages[i].name.ToLower() == language)
					{
					GlobalSettings.settings.language = language;
					currentLanguage = languages[i].texts;
					if (onLanguageSwitched != null)
						onLanguageSwitched();
					break;
					}
				}
			}


		public string getText(string key)
			{
			return currentLanguage.Contains(key) ? currentLanguage[key].ToString() : "Text not defined";
			}

		#endregion
		}

	}