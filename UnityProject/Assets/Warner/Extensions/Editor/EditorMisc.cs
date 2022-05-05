using UnityEngine;
using UnityEditor;

namespace Warner
	{
	public static class EditorMisc
		{
		#region FILES

		public static T loadConfig<T>(string fileName, ref string configPath) where T: new()
			{
			TextAsset configAsset = Misc.loadConfigAsset(fileName);
			configPath = AssetDatabase.GetAssetPath(configAsset);
			return Misc.loadConfigFile<T>(configAsset);
			}

		public static void saveConfigFile<T>(T config, string filePath)
			{
			Misc.saveConfigFile<T>(config, filePath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			}

		#endregion
		}
	}
