using UnityEngine;
using UnityEditor;
using System;
using Ionic.Zip;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using GDrive;

namespace Warner
	{
	public class BuildManagerWindow: EditorWindow
		{
		#region MEMBER FIELDS

		public delegate bool BeforeBuildHandler(Config config);

		public delegate bool BuildDoneHandler(Config config, BuildTarget buildTarget, string path, string extension);

		public static BeforeBuildHandler onBeforeBuild;
		public static BuildDoneHandler onBuildDone;

		private string dataFilePath;
		private Config config;
		private Rect[] _rects;
		private bool _showGeneralSettings = true;
		private bool _showFlags = true;
		private float progress;
		private string flagsDataFilePath;
		private string _newFlagName = string.Empty;
		private GoogleDrive gDrive;

		[Serializable]
		public struct ToUploadFile
			{
			public string path;
			public string name;
			}

		[Serializable]
		public struct Config
			{
			public bool windowsBuild;
			public bool macBuild;
			public bool linuxBuild;
			public bool zipFiles;
			public bool uploadToGDrive;
			public string gDriveFolderName;
			public bool splashScreenEnabled;
			public bool resolutionDialogEnabled;
			public bool developmentMode;
			public bool logFlagsFile;
			public string executableName;
			public string zipName;
			public List<ToUploadFile> gDriveToUploadFiles;
			public string gDriveFolderId;
			}

		private const string dataFile = "BuildManagerData";

		#endregion



		#region INIT

		[MenuItem("Tools/Build Manager")]
		public static void Init()
			{            
			EditorWindow window = EditorWindow.GetWindow(typeof(BuildManagerWindow));
			window.minSize = new Vector2(300, 500);
			window.Show();
			}


		private void OnEnable()
			{
			config = EditorMisc.loadConfig<Config>(dataFile, ref dataFilePath);
			BuildManagerFlags.init();
			initGoogleDrive();

			EditorCoroutine.start(googleDriveCheckPendingUploads());
			}


		#endregion



		#region GUI

		private void OnGUI()
			{
			generalOptionsGUI();
			flagsGUI();

			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUI.indentLevel = 0;
			EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
			EditorGUILayout.Space();

			_rects = GUIExtensions.getRects(2, 20f, new Vector2(5f, 5f));

			if (GUI.Button(_rects[0], "Save"))
				saveConfig();

			if (GUI.Button(_rects[1], "Create build"))
				EditorCoroutine.start(createBuild());

			}


		private void flagsGUI()
			{
			EditorGUILayout.Space();

			_showFlags = EditorGUILayout.Foldout(_showFlags, "Flags");

			if (!_showFlags)
				return;

			EditorGUI.indentLevel++;

			List<string> flags = new List<string>(BuildManagerFlags.flags.Keys);

			foreach (string key in flags)
				{
				_rects = GUIExtensions.getRects(2, 20f, new Vector2(5f, 5f));

				BuildManagerFlags.flags[key] = EditorGUI.Toggle(_rects[0], key, 
					BuildManagerFlags.getFlag(key)).ToString();

				if (GUI.Button(_rects[1], "Remove Flag"))
					{
					BuildManagerFlags.flags.Remove(key);
					saveConfig();
					}
				}

			EditorGUILayout.Space();
			_rects = GUIExtensions.getRects(2, 20f, new Vector2(5f, 5f));

			_newFlagName = EditorGUI.TextField(_rects[0], _newFlagName).Trim();

			if (GUI.Button(_rects[1], "Add Flag") && _newFlagName!=string.Empty)
				{
				if (BuildManagerFlags.flags.ContainsKey(_newFlagName))
					{
					Debug.Log("Build Manager: Flag already exists");
					return;
					}

				BuildManagerFlags.flags.Add(_newFlagName, "False");
				_newFlagName = string.Empty;
				saveConfig();
				}
			}


		private void generalOptionsGUI()
			{
			EditorGUILayout.Space();

			_showGeneralSettings = EditorGUILayout.Foldout(_showGeneralSettings, "General settings");

			if (!_showGeneralSettings)
				return;

			EditorGUILayout.Space();
			EditorGUI.indentLevel++;

			config.windowsBuild = EditorGUILayout.Toggle("Windows", config.windowsBuild);

			config.macBuild = EditorGUILayout.Toggle("Mac", config.macBuild);

			config.linuxBuild = EditorGUILayout.Toggle("Linux", config.linuxBuild);

			EditorGUILayout.Space();

			config.zipFiles = EditorGUILayout.Toggle("Zip files", config.zipFiles);

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			config.uploadToGDrive = EditorGUILayout.Toggle("Upload to G-Drive", config.uploadToGDrive);

			EditorGUILayout.LabelField("G-Drive target folder");
			config.gDriveFolderName = EditorGUILayout.TextField(config.gDriveFolderName);

			_rects = GUIExtensions.getRects(1, 20f, new Vector2(19f, 5f));
			if (GUI.Button(_rects[0], "G-Drive DeAuthorize"))
				EditorCoroutine.start(googleDriveDeAuthorize());

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			config.splashScreenEnabled = EditorGUILayout.Toggle("Splash screen", config.splashScreenEnabled);
			config.resolutionDialogEnabled = EditorGUILayout.Toggle("Resolution dialog", config.resolutionDialogEnabled);
			config.developmentMode = EditorGUILayout.Toggle("Development mode", config.developmentMode);
			config.logFlagsFile = EditorGUILayout.Toggle("Log flags file", config.logFlagsFile);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Executable name");
			config.executableName = EditorGUILayout.TextField(config.executableName);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Zip name");
			config.zipName = EditorGUILayout.TextField(config.zipName);

			EditorGUILayout.Space();
			EditorGUI.indentLevel--;
			}

		#endregion



		#region CONFIG



		public void saveConfig()
			{
			Misc.saveConfigFile<Config>(config, dataFilePath);

			//Save the flags
			if (string.IsNullOrEmpty(flagsDataFilePath))
				flagsDataFilePath = AssetDatabase.GetAssetPath(BuildManagerFlags.dataFileAsset);

			List<string> flags = new List<string>(BuildManagerFlags.flags.Keys);
			int c = 0;
			string text = "";
			foreach (KeyValuePair<string, string> flag in BuildManagerFlags.flags)
				{
				text += flag.Key+"="+flag.Value+
				(c<flags.Count-1 ? "\n" : "");

				c++;
				}

			Misc.saveFile(text, flagsDataFilePath);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			}

		#endregion



		#region BUILD

		private IEnumerator createBuild()
			{     
			if (!config.windowsBuild && !config.macBuild && !config.linuxBuild)
				{
				EditorUtility.DisplayDialog("Build Manager", "Please select a target platform", "OK");
				yield break;
				}


			if (string.IsNullOrEmpty(config.executableName))
				{
				EditorUtility.DisplayDialog("Build Manager", "Please set the executable name", "OK");
				yield break;
				}


			if (config.zipFiles && string.IsNullOrEmpty(config.zipName))
				{
				EditorUtility.DisplayDialog("Build Manager", "Please set the zip name", "OK");
				yield break;
				}


			if (config.uploadToGDrive && !config.zipFiles)
				{
				EditorUtility.DisplayDialog("Build Manager", "Please select Zip files in order to upload to Google Drive", "OK");
				yield break;
				}

			if (config.uploadToGDrive && !gDrive.IsAuthorized)
				{
				EditorUtility.DisplayProgressBar("Build Manager", "Checking G-Drive Auth", 0);

				IEnumerator auth = gDrive.Authorize();

				while (auth.MoveNext())
					yield return 0;

				if (!gDrive.IsAuthorized)
					{
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog("Build Manager", "Please authorize your Google Drive account", "OK");
					yield break;
					}
				}


			GoogleDrive.File gDriveFolder = null;

			if (config.uploadToGDrive)
				{
				EditorUtility.DisplayProgressBar("Build Manager", "Checking G-Drive Target Folder", 0);
				IEnumerator listFiles = gDrive.ListAllFiles();
				yield return EditorCoroutine.start(listFiles);
				List<GoogleDrive.File> gDriveFiles = null;

				while (listFiles.MoveNext())
					{
					GoogleDrive.AsyncSuccess asyncSuccess = listFiles.Current as GoogleDrive.AsyncSuccess;

					if (asyncSuccess!=null)
						gDriveFiles = (List<GoogleDrive.File>) asyncSuccess.Result;

					yield return 0;
					}

				if (gDriveFiles!=null)
					{
					foreach (GoogleDrive.File file in gDriveFiles)
						{
						if (file.IsFolder && file.Title==config.gDriveFolderName)
							{
							gDriveFolder = file;
							break;
							}
						}
					} 

				if (gDriveFolder==null)
					{
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog("Build Manager", "Google Drive folder was not found", "OK");
					yield break;
					}
					else
					{
					config.gDriveFolderId = gDriveFolder.ID;
					}
				}

			string buildsFolder = EditorUtility.SaveFolderPanel("Select builds path", 
				                               Application.dataPath.Replace("Assets", ""), "");

			if (!Directory.Exists(buildsFolder))
				{
				try
					{
					Directory.CreateDirectory(buildsFolder);
					}
				catch (Exception)
					{
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog("Build Manager", "Could not create/find the build directory", "OK");
					yield break;
					}
				}

			if (config.uploadToGDrive)
				{
				EditorUtility.DisplayProgressBar("Build Manager", "Clearing G-Drive Target Folder", 0.05f);
				IEnumerator toDelete = gDrive.ListFiles(gDriveFolder);
				yield return EditorCoroutine.start(toDelete);
				List<GoogleDrive.File> toDeleteFiles = null;

				while (toDelete.MoveNext())
					{
					GoogleDrive.AsyncSuccess asyncSuccess = toDelete.Current as GoogleDrive.AsyncSuccess;

					if (asyncSuccess!=null)
						toDeleteFiles = (List<GoogleDrive.File>) asyncSuccess.Result;

					yield return 0;
					}

				for (int i = 0; i<toDeleteFiles.Count; i++)
					{
					yield return EditorCoroutine.start(gDrive.DeleteFile(toDeleteFiles[i]));
					}
				}

			if (onBeforeBuild!=null)
				{
				bool quit = false;

				Delegate[] delegates = onBeforeBuild.GetInvocationList();

				for (int i = 0; i<delegates.Length; i++)
					{
					BeforeBuildHandler del = ((BeforeBuildHandler) delegates[i]);
					if (!del(config))
						quit = true;
					}

				if (quit)
					yield break;
				}

			saveConfig();
			progress = 0.1f;
			EditorUtility.ClearProgressBar();

			config.gDriveToUploadFiles = new List<ToUploadFile>();   

			if (config.windowsBuild)
				doBuild(BuildTarget.StandaloneWindows64, "Win", "exe", buildsFolder);

			if (config.macBuild)
				doBuild(BuildTarget.StandaloneOSX, "Mac", "app", buildsFolder);

			if (config.linuxBuild)
				doBuild(BuildTarget.StandaloneLinuxUniversal, "Linux", "x64", buildsFolder);

			saveConfig();
			EditorUtility.ClearProgressBar();

			if (!config.uploadToGDrive)
				EditorUtility.DisplayDialog("Build Manager", "Builds completed", "OK");
			}


		private bool doBuild(BuildTarget buildTarget, string subFolder, string extension, string buildsFolder)
			{
			progress += 0.1f;
			EditorUtility.DisplayProgressBar("Build Manager", "Creating "+subFolder+" build...", progress);

			string targetFolder = buildsFolder+"/"+subFolder+"/";

			if (Directory.Exists(targetFolder))
				Directory.Delete(targetFolder, true);

			Directory.CreateDirectory(targetFolder);

			PlayerSettings.SplashScreen.show = config.splashScreenEnabled;
			PlayerSettings.displayResolutionDialog = 
                config.resolutionDialogEnabled ? ResolutionDialogSetting.Enabled 
                : ResolutionDialogSetting.Disabled;

			BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.transformTo( (t) => t.path),
				targetFolder+config.executableName+"."+extension, 
				buildTarget, config.developmentMode ? BuildOptions.Development : BuildOptions.None);                              

			string path = buildsFolder+"/"+subFolder+"/";
			logBuildFlags(path, subFolder);                      

			if (config.zipFiles)
				zipBuild(targetFolder, config.zipName+"_"+subFolder);	

			if (config.uploadToGDrive)
				{
				ToUploadFile toUpload = new ToUploadFile();
				toUpload.path = targetFolder;
				toUpload.name = config.zipName+"_"+subFolder;
				config.gDriveToUploadFiles.Add(toUpload);
				}

			if (onBuildDone!=null)
				onBuildDone(config, buildTarget, path, extension);

			return true;
			}


		private void logBuildFlags(string prePath, string platformFolder)
			{
			if (BuildManagerFlags.dataFileAsset==null)
				{
				Debug.LogWarning("BuildManager: Couldnt find the build flags file");
				return;
				}

			string editorFlagsPath = Application.dataPath.Replace("Assets", "")+
				AssetDatabase.GetAssetPath(BuildManagerFlags.dataFileAsset);    

			string path;

			if (platformFolder=="Mac")
				path = prePath+config.executableName+".app/Contents/Resources/Data/";
				else
				path = prePath+"/"+config.executableName+"_Data/";

			string fileName = path+"flags.txt";

			if (config.logFlagsFile && File.Exists(editorFlagsPath))
				{
				File.Copy(editorFlagsPath, fileName, true);
				}
				else
				{
				if (File.Exists(fileName))
					File.Delete(fileName);
				}
			}


		private string zipBuild(string path, string zipFileName)
			{
			progress += 0.1f;
			EditorUtility.DisplayProgressBar("Build Manager", "Compressing "+zipFileName+"...", progress);
			return zipFilesAtFolder(path, zipFileName);
			}


		#endregion



		#region COMPRESS

		public static string zipFilesAtFolder(string path, string zipFileName)
			{      
			string zipFullPath = path+zipFileName+".zip";

			using (ZipFile zipFile = new ZipFile())
				{
				zipFile.ParallelDeflateThreshold = -1;
				addFolderFilesToZip(path, zipFile);
				zipFile.Save(zipFullPath);
				}

			return zipFullPath;
			}


		private static void addFolderFilesToZip(string path, ZipFile zipFile, string subFolderName = "")
			{
			DirectoryInfo dir = new DirectoryInfo(path);
			FileInfo[] files = dir.GetFiles();

			for (int i = 0; i<files.Length; i++)
				zipFile.AddFile(files[i].FullName, subFolderName);


			DirectoryInfo[] dirs = dir.GetDirectories();       

			for (int i = 0; i<dirs.Length; i++)
				addFolderFilesToZip(dirs[i].FullName, zipFile, subFolderName+"/"+dirs[i].Name);
			}


		#endregion



		#region GOOGLE DRIVE

		void initGoogleDrive()
			{
			gDrive = new GoogleDrive();
			gDrive.ClientID = "167237021133-rcq1lvs5dogkgbllo0ebngbmhha0kocq.apps.googleusercontent.com";
			gDrive.ClientSecret = "1dpqjrVuIQ7EwUf6ihdKTiOk";
			}


		private IEnumerator googleDriveDeAuthorize()
			{
			yield return EditorCoroutine.start(gDrive.Unauthorize());

			Debug.Log("BuildManager: Google Drive successfully deauthorized ");
			}

		private IEnumerator googleDriveCheckPendingUploads()
			{
			if (config.gDriveToUploadFiles==null || config.gDriveToUploadFiles.Count==0)
				yield break;

			ToUploadFile[] files = config.gDriveToUploadFiles.ToArray();
			config.gDriveToUploadFiles.Clear();
			saveConfig();

			float progress = 0f;
			IEnumerator routine;

			for (int i = 0; i<files.Length; i++)
				{
				progress += 0.25f;
				EditorUtility.DisplayProgressBar("Build Manager", "Uploading "+files[i].name, progress);
				routine = googleDriveUploadFile(files[i]);
				while (routine.MoveNext())
					yield return routine.Current;
				}
            
			EditorUtility.ClearProgressBar();
			EditorUtility.DisplayDialog("Build Manager", "Builds completed and uploaded", "OK");
			}



		private IEnumerator googleDriveUploadFile(ToUploadFile file)
			{           
			byte[] bytes = File.ReadAllBytes(file.path+file.name+".zip");
 
			IEnumerator upload = gDrive.UploadFile(file.name+".zip", config.gDriveFolderId, "application/zip", bytes);

			while (upload.MoveNext())
				yield return upload.Current;
			}


		#endregion
		}
	}