using UnityEngine;
using System.Collections.Generic;

namespace Warner
	{
	public static class BuildManagerFlags
		{
		public static TextAsset dataFileAsset;

		public const string flagsFileName = "BuildManagerFlags";

		public static Dictionary<string, string> flags = new Dictionary<string, string>();

		public static void init()
			{
			dataFileAsset = Misc.loadConfigAsset(flagsFileName);
			flags = parseFlags(dataFileAsset);
			}


		public static bool getFlag(string flagName)
			{
			if (flags.ContainsKey(flagName))
				return flags[flagName].Trim().ToLower().Equals("true");

			return false;            
			}


		private static Dictionary<string, string> parseFlags(TextAsset textAsset)
			{
			Dictionary<string, string> theFlags = new Dictionary<string, string>();

			if (textAsset==null)
				return theFlags;    

			List<string> lines = textAsset.text.Split('\n').toList();
			string[] sText;
			for (int i = 0; i<lines.Count; i++)
				{
				sText = lines[i].Split('=');

				if (sText.Length!=2)
					continue;

				theFlags.Add(sText[0], sText[1]);
				}

			return theFlags;
			}
		}
	}