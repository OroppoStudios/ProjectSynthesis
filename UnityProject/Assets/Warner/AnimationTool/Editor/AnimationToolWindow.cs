using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using LitJson;
using System.IO;
using System.Linq;

namespace Warner.AnimationTool
	{
	public class AnimationToolWindow: EditorWindow
		{
		#region MEMBER FIELDS

		[Serializable]
		private class Config
			{
			public string statesFilePath = "Warner/AnimationTool/";
			public string test;
			public Dictionary<string, string> stateNames = new Dictionary<string, string>();
            public Dictionary<string, List<string>> animators = new Dictionary<string, List<string>>();
			}

		private static List<AnimatorState> statesList;
		private Config config;
		private string configPath;

		private Rect[] rects;
		private bool toggleAll;
		private List<AnimationData> objectsToUpdate = new List<AnimationData>();

		private class AnimationData
			{
			public string name;
			public bool selected;
			public string path;
			public int assetId;
			public bool visibleInEditor;
			public JsonData data;
			public string statesName;
            public List<string> animators = new List<string>();

			public AnimationData(string name, string path, bool selected = false)
				{
				this.name = name;
				this.selected = selected;
				this.path = path;
				}
			}

		private Vector2 charactersScrollPosition;
		private Dictionary<string, List<string>> statesMap = new Dictionary<string, List<string>>();

		private const string dataFile = "AnimationToolData";

		#endregion



		#region INIT

		[MenuItem("Tools/Animation Tool")]
		private static void Init()
			{
			EditorWindow window = EditorWindow.GetWindow<AnimationToolWindow>();
			window.minSize = new Vector2(320f, 560f);
			window.Show();	
			}


		private void OnEnable()
			{
			config = EditorMisc.loadConfig<Config>(dataFile, ref configPath);
			}


		private void refresh()
            {
            OnEnable();
            getAnimationsData();

            for (int i = 0; i<objectsToUpdate.Count; i++)
                {
                if (config.stateNames.ContainsKey(objectsToUpdate[i].name))
                    objectsToUpdate[i].statesName = config.stateNames[objectsToUpdate[i].name];

                if (config.animators!=null && config.animators.ContainsKey(objectsToUpdate[i].name))
                    objectsToUpdate[i].animators = config.animators[objectsToUpdate[i].name];
                }
			}


		private void getAnimationsData()
            {
            AnimationData animationData;

            DirectoryInfo[] folders = new DirectoryInfo(AnimationController.resourcesPath+
                                      AnimationController.path).GetDirectories();
            objectsToUpdate.Clear();

            TextAsset jsonAsset;
            DirectoryInfo[] animationFolders;
            DirectoryInfo[] animationFolderDirectories;
            List<string> animationFoldersWithFrames = new List<string>();
            bool animationDataFound;
            string animationName;
            bool doSave = false;

            for (int i = 0; i<folders.Length; i++)
                {
                animationData = new AnimationData(folders[i].Name, 
                    folders[i].FullName.Replace(folders[i].Name, "")); 

                jsonAsset = AnimationController.getJsonAsset(AnimationController.path+animationData.name
                +"/"+animationData.name, false);

                if (jsonAsset==null)
                    jsonAsset = createJsonAsset(animationData.path, animationData.name);

                animationData.data = AnimationController.getJsonData(jsonAsset);
                animationData.assetId = jsonAsset.GetInstanceID();	

                //check that if we have animations folder, they are on the json
                animationFolders = new DirectoryInfo(AnimationController.resourcesPath+
                AnimationController.path+animationData.name).GetDirectories();
                animationFoldersWithFrames.Clear();

                //make sure we are not picking the animators folders as animation folder
                for (int j = 0; j<animationFolders.Length; j++)
                    {
                    animationFolderDirectories = new DirectoryInfo(AnimationController.resourcesPath+
                    AnimationController.path+animationData.name+"/"+animationFolders[j].Name).GetDirectories();

                    if (animationFolderDirectories.Length>0)//means we have animators, so check them files
                        {
                        for (int k = 0; k<animationFolderDirectories.Length; k++)
                            if (folderHasFrames(AnimationController.resourcesPath+
                                AnimationController.path+animationData.name+"/"+animationFolders[j].Name+"/"+animationFolderDirectories[k].Name))
                                {
                                animationFoldersWithFrames.Add(animationFolderDirectories[k].Name);
                                }
                                                        
                        }
                     else//if not, check for single animator animations
                        {
                        if (folderHasFrames(AnimationController.resourcesPath+
                            AnimationController.path+animationData.name+"/"+animationFolders[j].Name))
                            {
                            animationFoldersWithFrames.Add(animationFolders[j].Name);
                            }
                        }
                    }  


                for (int j = 0; j<animationFoldersWithFrames.Count; j++)
                    {
                    animationName = animationFoldersWithFrames[j];
                    animationDataFound = false;

                    for (int k = 0; k<animationData.data.Count; k++)
                        if (animationData.data[k]["name"].ToString()==animationName)
                            {
                            animationDataFound = true;
                            break;
                            }

                    if (!animationDataFound)
                        {
                        addNewAnimationData(animationData.data, animationName, false);
                        doSave = true;
                        }
                    }


                objectsToUpdate.Add(animationData);
				}	

            if (doSave)						
                save(false);
			}


        private bool folderHasFrames(string path)
            {
            FileInfo[] files  = new DirectoryInfo(path).GetFiles();

            for (int k = 0; k<files.Length; k++)
                if (files[k].Extension==".png")
                return true;

            return false;
            }


		private TextAsset createJsonAsset(string path, string theName)
			{
			JsonData data = new JsonData();
			data.SetJsonType(JsonType.Array);
			Misc.saveFile(data.ToJson(), path+theName+"/"+theName+".json");
			AssetDatabase.Refresh();

			return AnimationController.getJsonAsset(AnimationController.path+theName
				+"/"+theName);
			}

		#endregion



		#region GUI

		private void OnGUI()
			{
			if (objectsToUpdate.Count==0)
				refresh();

			guiConfig();
			guiObjectData();
			guiButtons();
			}


		private void guiConfig()
			{
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Settings");
			EditorGUILayout.Space();

			rects = GUIExtensions.getRects(3, 20f, new Vector2(7f, 7f));
			EditorGUI.LabelField(rects[0], "States file path: ");

			config.statesFilePath = EditorGUI.TextField(rects[1], config.statesFilePath);
			if (GUI.Button(rects[2], "Select folder"))
				{
				config.statesFilePath = EditorUtility.SaveFolderPanel("Select the folder", 
					Application.dataPath+config.statesFilePath, "AnimationTool").Replace(Application.dataPath, "");
				}
			}


		private void guiObjectData()
            {
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Objects");
            EditorGUILayout.Space();

            charactersScrollPosition = EditorGUILayout.BeginScrollView(charactersScrollPosition, 
                GUILayout.ExpandWidth(true));

            for (int i = 0; i<objectsToUpdate.Count; i++)
                {
                EditorGUI.indentLevel = 0;

                rects = GUIExtensions.getRects(3, 20f, new Vector2(7f, 7f));

                objectsToUpdate[i].visibleInEditor = EditorGUI.Foldout(rects[0], 
                    objectsToUpdate[i].visibleInEditor, objectsToUpdate[i].name+" ("+objectsToUpdate[i].data.Count+")");	

                objectsToUpdate[i].selected = EditorGUI.Toggle(rects[1], objectsToUpdate[i].selected);	

                if (GUI.Button(rects[2], "Open JSON"))
                    {
                    AssetDatabase.OpenAsset(objectsToUpdate[i].assetId);
                    }

                if (objectsToUpdate[i].visibleInEditor)
                    {
                    //states name
                    EditorGUILayout.Space();
                    rects = GUIExtensions.getRects(2, 20f, new Vector2(21f, 7f));
                    EditorGUI.LabelField(rects[0], "States name: ");		
                    objectsToUpdate[i].statesName = EditorGUI.TextField(rects[1], 
                        string.IsNullOrEmpty(objectsToUpdate[i].statesName) ? "" : objectsToUpdate[i].statesName.Replace(" ", ""));								

                    //animators
                    rects = GUIExtensions.getRects(1, 20f, new Vector2(21f, 7f));
                    EditorGUI.LabelField(rects[0], "Animators: ");        

                    for (int j = 0; j<objectsToUpdate[i].animators.Count; j++)
                        {
                        rects = GUIExtensions.getRects(2, 20f, new Vector2(21f, 7f));
                              
                        objectsToUpdate[i].animators[j] = EditorGUI.TextField(rects[0], objectsToUpdate[i].animators[j]);

                        if (GUI.Button(rects[1], "Remove"))
                            objectsToUpdate[i].animators.RemoveAt(j);
                        }


                    //buttons
                    EditorGUILayout.Space();
                    rects = GUIExtensions.getRects(2, 20f, new Vector2(21f, 7f));

                    if (GUI.Button(rects[0], "Add Animator"))
                        objectsToUpdate[i].animators.Add(string.Empty);

					if (GUI.Button(rects[1], "Add Animation"))
                        addNewAnimationData(objectsToUpdate[i].data, "New", true);


                    //Animations
                    EditorGUILayout.Space();

					for (int j = 0; j<objectsToUpdate[i].data.Count; j++)
						guiAnimationInfo(objectsToUpdate[i].data[j], objectsToUpdate[i].data);

					EditorGUILayout.Space();
					}

				EditorGUILayout.Space();
				}

			EditorGUILayout.EndScrollView();
			}


        private void addNewAnimationData(JsonData data, string animationName, bool visible)
            {
            JsonData animationData = new JsonData();
            animationData.SetJsonType(JsonType.Object);
            animationData["name"] = animationName;
            animationData["frameRate"] = 1;
            animationData["loop"] = false;
            animationData["visibleInEditor"] = visible;

            data.Add(animationData);
            }


		private void createPropertyIfDoesntExists<T>(JsonData data, string property, T value)
			{
			if (!data.Contains(property))
				data[property] = new JsonData(value);
			}


		private void guiAnimationInfo(JsonData animationData, JsonData parent)
			{
			EditorGUI.indentLevel = 1;

			//Visible in editor
			createPropertyIfDoesntExists<bool>(animationData, "visibleInEditor", false);

			animationData["visibleInEditor"] = EditorGUILayout.Foldout((bool) animationData["visibleInEditor"], 
				animationData["name"].ToString());

			EditorGUI.indentLevel = 2;

			if ((bool) animationData["visibleInEditor"])
				{
				//Name
				rects = GUIExtensions.getRects(2, 20f, new Vector2(0f, 7f));
				EditorGUI.LabelField(rects[0], "Name");
				animationData["name"] = EditorGUI.TextField(rects[1], animationData["name"].ToString());

				//FrameRate
				rects = GUIExtensions.getRects(2, 20f, new Vector2(0f, 7f));
				EditorGUI.LabelField(rects[0], "FrameRate");

				float frameRate;
				if (animationData["frameRate"].IsFloat)
					frameRate = (float) animationData["frameRate"];
					else
					frameRate = float.Parse(animationData["frameRate"].ToString());

				animationData["frameRate"] = EditorGUI.FloatField(rects[1], frameRate);

				//Loop
				rects = GUIExtensions.getRects(2, 20f, new Vector2(0f, 7f));
				EditorGUI.LabelField(rects[0], "Loop");
				animationData["loop"] = EditorGUI.Toggle(rects[1], (bool) animationData["loop"]);

				//Events
				rects = GUIExtensions.getRects(1, 20f, new Vector2(0f, 7f));

				createPropertyIfDoesntExists<bool>(animationData, "eventsVisibleInEditor", false);

				int eventsCount = animationData.Contains("events") 
					? animationData["events"].Count : 0;

				animationData["eventsVisibleInEditor"] = EditorGUI.Foldout(rects[0], 
					(bool) animationData["eventsVisibleInEditor"], "Events ("+eventsCount+")");

				if ((bool) animationData["eventsVisibleInEditor"])
					{
					if (animationData.Contains("events"))
						for (int i = 0; i<animationData["events"].Count; i++)
							guiAnimationEventInfo(animationData["events"][i], animationData["events"]);
					}

				rects = GUIExtensions.getRects(2, 20f, new Vector2(31f, 7f));
				if (GUI.Button(rects[0], "Add Event"))
					{
					if (!animationData.Contains("events"))
						{
						JsonData events = new JsonData();
						events.SetJsonType(JsonType.Array);
						animationData["events"] = events;
						}
					
					JsonData theEvent = new JsonData();
					theEvent.SetJsonType(JsonType.Object);
					theEvent["type"] = "";
					theEvent["frame"] = 1;
					theEvent["visibleInEditor"] = true;

					animationData["events"].Add(theEvent);
					animationData["eventsVisibleInEditor"] = true;
					}

				if (GUI.Button(rects[1], "Delete Animation"))
					if (EditorUtility.DisplayDialog("Animation Tool", 
						"Do you really want to delete the "+animationData["name"]+" animation?", 
						"Yes", "No"))
						{
						parent.Remove(animationData);
						}
				
				EditorGUILayout.Space();
				EditorGUILayout.Space();
				}
			}


		private void guiAnimationEventInfo(JsonData eventData, JsonData parent)
            {
            EditorGUI.indentLevel = 3;

            rects = new Rect[1];
            rects = GUIExtensions.getRects(2, 20f, new Vector2(0f, 7f));

            createPropertyIfDoesntExists<bool>(eventData, "visibleInEditor", false);

            string eventTypeLabel = eventData["type"].ToString();
            if (eventTypeLabel==string.Empty)
                eventTypeLabel = "Empty";

            eventData["visibleInEditor"] = EditorGUI.Foldout(rects[0], 
                (bool) eventData["visibleInEditor"], eventTypeLabel);

            if (!((bool) eventData["visibleInEditor"]))
                return;

            EditorGUI.indentLevel = 4;

            //Type
            rects = new Rect[2];
            rects = GUIExtensions.getRects(2, 20f, new Vector2(0f, 7f));
            EditorGUI.LabelField(rects[0], "Type");	
            AnimationEventType type = Misc.parseEnum<AnimationEventType>
					(eventData["type"].ToString());

            eventData["type"] = EditorGUI.EnumPopup(rects[1], (AnimationEventType) type).ToString(); 
            
          
            //Frame
            rects = GUIExtensions.getRects(2, 20f, new Vector2(0f, 7f));
            EditorGUI.LabelField(rects[0], "Frame");
            eventData["frame"] = EditorGUI.IntField(rects[1], (int) eventData["frame"]);           

            //params
            if (!eventData.Contains("parameters"))
                {
                JsonData parameters = new JsonData();
                parameters.SetJsonType(JsonType.Array);
                eventData["parameters"] = parameters;
                }


            for (int i = 0; i<eventData["parameters"].Count; i++)
                {
                rects = GUIExtensions.getRects(2, 20f, new Vector2(0f, 7f));
                eventData["parameters"][i]["name"] = EditorGUI.TextField(rects[0], eventData["parameters"][i]["name"].ToString());

                Rect buttonRect = rects[1];
                buttonRect.width = 25;
                buttonRect.x += 15;

                if (GUI.Button(buttonRect, "x"))
                    {
                    eventData["parameters"].Remove(eventData["parameters"][i]);
                    break;
                    }
                                    
                eventData["parameters"][i]["value"] = EditorGUI.TextField(rects[1], eventData["parameters"][i]["value"].ToString());           
                }
                

            rects = GUIExtensions.getRects(2, 20f, new Vector2(119, 7f));
            if (GUI.Button(rects[1], "Add Parameter"))
                {
                JsonData parameter = new JsonData();
                parameter.SetJsonType(JsonType.Object);
                parameter["name"] = "name";
                parameter["value"] = string.Empty;

                eventData["parameters"].Add(parameter);
                }


            //end and delete button
            EditorGUILayout.Space();

            string typeLabel = eventData["type"].ToString();
            if (typeLabel==string.Empty)
                typeLabel = "Empty";

            rects = GUIExtensions.getRects(1, 20f, new Vector2(119, 7f));
            if (GUI.Button(rects[0], "Delete Event"))
                if (EditorUtility.DisplayDialog("Character Animation Tool", 
                    "Do you want to delete '"+typeLabel+"' event?", 
                    "Yes", "No"))
                    {
                    parent.Remove(eventData);
                    }

			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			EditorGUILayout.Space();
			}


		private void guiButtons()
			{
			EditorGUILayout.Space();

			rects = GUIExtensions.getRects(4, 20f, new Vector2(7f, 7f));

			if (GUI.Button(rects[0], "Save"))
				{
				save();
				}

			if (GUI.Button(rects[1], "Refresh"))
				refresh();
							
			if (GUI.Button(rects[2], "Toggle On/Off"))
				{
				toggleAll = !toggleAll;

				for (int i = 0; i<objectsToUpdate.Count; i++)
					objectsToUpdate[i].selected = toggleAll;
				}


			if (GUI.Button(rects[3], "Generate"))
				build();

			EditorGUILayout.Space();
			}



		#endregion



		#region BUILD STUFF


		private void build()
            {
            save(false);

			createStateEnumsClass();

            int count = 0;

            for (int i = 0; i<objectsToUpdate.Count; i++)
                {
                if (!objectsToUpdate[i].selected)
                    continue;

                if (objectsToUpdate[i].animators.Count==0)
                    createAnimatorAndAnimations(objectsToUpdate[i].name, objectsToUpdate[i], true);       
                    else
                    for (int j = 0; j<objectsToUpdate[i].animators.Count; j++)
                        createAnimatorAndAnimations(objectsToUpdate[i].animators[j], objectsToUpdate[i], false);       

				count++;
				}

			AssetDatabase.Refresh();

			if (count>0)
				EditorUtility.DisplayDialog("Animation Tool", "Animations successfully generated", "OK");						
			}


        void createAnimatorAndAnimations(string animatorName, AnimationData animationData, bool singleAnimator)
            {
            AnimatorController animatorController = createAnimatorController(animationData.name, 
                animatorName, animationData.data, animationData.statesName);

            if (animatorController!=null)
                createAnimations(animatorController, animationData.name, 
                    animatorName, animationData.data, singleAnimator);

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(animatorController));
            }


		private void createStateEnumsClass()
			{
			statesMap.Clear();

			for (int i = 0; i<objectsToUpdate.Count; i++)
				{
				if (string.IsNullOrEmpty(objectsToUpdate[i].statesName))
					continue;

				//check if we already have a list for this states
				if (!statesMap.ContainsKey(objectsToUpdate[i].statesName))
					statesMap.Add(objectsToUpdate[i].statesName, new List<string>());									

				//go through the animations and add each one to the state list they belong
				for (int j = 0; j < objectsToUpdate[i].data.Count; j++)
					{
					if (!statesMap[objectsToUpdate[i].statesName].Contains(objectsToUpdate[i].data[j]["name"].ToString()))
						statesMap[objectsToUpdate[i].statesName].Add(objectsToUpdate[i].data[j]["name"].ToString());
					}
				}

			foreach (KeyValuePair<string, List<string>> statesPair in statesMap)
				statesPair.Value.Sort();

			string text = "using System;\n\nnamespace Warner.AnimationTool\n\t{\n\t";


			//enums
			string enums = "";
			string values;

			foreach (KeyValuePair<string, List<string>> statesPair in statesMap)
				{
				values = "\n\t\tNone,";

				for (int j = 0; j<statesPair.Value.Count; j++)
					{
					values += "\n\t\t"+statesPair.Value[j];

					if (j<statesPair.Value.Count-1)
						values += ", ";
					}

				if (enums!="")
					enums += "\n\t";

				enums += "public enum "+statesPair.Key+
				" \n\t\t{"+values+"\n\t\t}";
				}

			//struct
			string structs = "";
			string declarations;
			foreach (KeyValuePair<string, List<string>> theEnum in statesMap)
				{
				declarations = "";

				for (int j = 0; j<theEnum.Value.Count; j++)
					declarations += "\n\t\tpublic bool "+theEnum.Value[j]+";";

				structs += "[Serializable]\n\tpublic struct "+theEnum.Key+"s"+
					" \n\t\t{"+declarations+"\n\t\t}";
				}


			text += enums+"\n\n\t"+structs+ "\n\t}";


			if (config.statesFilePath[config.statesFilePath.Length-1]!='/'
				&& config.statesFilePath[config.statesFilePath.Length-1]!='\\')
				config.statesFilePath += "/";

			Misc.saveFile(text, Application.dataPath+"/"+config.statesFilePath+"AnimationStates.cs");
			}

		#endregion



		#region ANIMATORS


		private AnimatorController createAnimatorController(string objectName, string animatorName, JsonData jsonData, string statesName)
			{
			AnimatorController animatorController = AnimatorController.
				CreateAnimatorControllerAtPath("Assets/Resources/"+objectName+"_"+animatorName+".controller");

//			AnimatorControllerParameter animatorControllerParameter = new AnimatorControllerParameter();
//			animatorControllerParameter.name = "State";
//			animatorControllerParameter.type = AnimatorControllerParameterType.Int;
//			animatorController.AddParameter(animatorControllerParameter);

			if (statesMap.ContainsKey(statesName))
				{
				List<string> statesNames = statesMap[statesName];
				for (int i = 0; i<statesNames.Count; i++)
					createState(animatorController, statesNames[i], i);
				}
				else
				{
				for (int i = 0; i<jsonData.Count; i++)
					createState(animatorController, jsonData[i]["name"].ToString(), i);
				}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			return animatorController;
			}

		private AnimatorState createState(AnimatorController animatorController, string stateName, int stateConditional)
			{
			AnimatorState animatorState = animatorController.layers[0].stateMachine.AddState(stateName);

			AnimatorStateTransition animatorStateTransition = animatorController.layers[0].stateMachine.AddAnyStateTransition(animatorState);
			animatorStateTransition.hasExitTime = false;
			animatorStateTransition.exitTime = 0;
			animatorStateTransition.hasFixedDuration = false;
			animatorStateTransition.duration = 0;
			animatorStateTransition.canTransitionToSelf = false;
			animatorStateTransition.AddCondition(AnimatorConditionMode.Equals, stateConditional, "State");

			return animatorState;
			}

		private AnimatorState[] getAnimatorStates(AnimatorStateMachine stateMachine)
			{
			statesList.Clear();

			appendStates(stateMachine);

			for (int i = 0; i<stateMachine.stateMachines.Length; i++)
				appendStates(stateMachine.stateMachines[i].stateMachine);

			return statesList.ToArray();
			}


		private void appendStates(AnimatorStateMachine stateMachine)
			{
			for (int i = 0; i<stateMachine.states.Length; i++)
				statesList.Add(stateMachine.states[i].state);
			}

		
		private void copyAnimatorValues(AnimatorController originController, AnimatorController targetController)
			{
			AnimatorStateMachine originStateMachine;
			AnimatorStateMachine targetStateMachine;

			AnimatorControllerLayer targetLayer = targetController.layers[0];
			AnimatorControllerLayer originLayer = originController.layers[0];

			cloneStates(originLayer.stateMachine, targetLayer.stateMachine);
//			copyTransitions(originLayer.stateMachine.anyStateTransitions
//				, null, targetLayer.stateMachine, targetLayer.stateMachine);															

			for (int i = 0; i<originLayer.stateMachine.stateMachines.Length; i++)
				{
				originStateMachine = originLayer.stateMachine.stateMachines[i].stateMachine;

				targetStateMachine = targetLayer.stateMachine.AddStateMachine(originStateMachine.name);
				cloneStates(originStateMachine, targetStateMachine);

//				copyTransitions(originLayer.stateMachine.anyStateTransitions, null, targetStateMachine, targetLayer.stateMachine);									
				}		

			

			targetController.parameters = originController.parameters;
			}


		private void cloneStates(AnimatorStateMachine originStateMachine, AnimatorStateMachine targetStateMachine)
			{
			AnimatorState originState;
			AnimatorState targetState;

			for (int i = 0; i<originStateMachine.states.Length; i++)
				{
				originState = originStateMachine.states[i].state;
				targetStateMachine.AddState(originState.name);
				}		
					

			for (int i = 0; i<originStateMachine.states.Length; i++)
				{
				originState = originStateMachine.states[i].state;
				targetState = targetStateMachine.states[i].state;

				copyTransitions(originState.transitions, targetState, targetStateMachine, null);
				}
			}


		private void copyTransitions(AnimatorStateTransition[] transitions, AnimatorState targetState, AnimatorStateMachine stateMachine, AnimatorStateMachine rootStateMachine)
			{
			AnimatorCondition originCondition;
			AnimatorStateTransition originTransition;
			AnimatorStateTransition targetTransition;
			AnimatorState targetTransitionDestinationState;

			for (int j = 0; j<transitions.Length; j++)
				{
				originTransition = transitions[j];
				targetTransitionDestinationState = getStateByName(stateMachine, originTransition.destinationState.name);

				if (targetState==null)//this is anyStateTransition
					targetTransition = rootStateMachine.AddAnyStateTransition(targetTransitionDestinationState);
				else
					targetTransition = targetState.AddTransition(targetTransitionDestinationState);

				targetTransition.hasExitTime = originTransition.hasExitTime;
				targetTransition.exitTime = originTransition.exitTime;
				targetTransition.hasFixedDuration = originTransition.hasFixedDuration;
				targetTransition.duration = originTransition.duration;
				targetTransition.offset = originTransition.offset;
				targetTransition.canTransitionToSelf = originTransition.canTransitionToSelf;

				for (int k = 0; k<originTransition.conditions.Length; k++)
					{
					originCondition = originTransition.conditions[k];
					targetTransition.AddCondition(originCondition.mode, originCondition.threshold, originCondition.parameter);
					}
				}
			}



		private AnimatorState getStateByName(AnimatorStateMachine stateMachine, string stateName)
			{
			for (int i = 0; i<stateMachine.states.Length; i++)
				if (stateMachine.states[i].state.name==stateName)
					return stateMachine.states[i].state;

			return null;
			}


		#endregion



		#region ANIMATION STUFF


		private void createAnimations(AnimatorController baseAnimatorController, string objectName, string animatorName, JsonData jsonData, bool singleAnimator)
			{
			statesList = new List<AnimatorState>();
			AnimatorState[] states;
			AnimationClip animationClip;
			AnimatorController newAnimatorController;

			string checkPath;

			if (singleAnimator)
				checkPath = AnimationController.resourcesPath+
				AnimationController.path+objectName;
			else
				checkPath = AnimationController.resourcesPath+
				AnimationController.path+objectName+"/"+animatorName;

			if (!Directory.Exists(checkPath))
				{
				Debug.LogWarning("Directory for "+objectName+"/"+animatorName+" does not exist.");
				return;
				}	
                            
			string controllerPath;

			if (singleAnimator)
				controllerPath = AnimationController.resourcesPath+AnimationController.path+
				objectName+"/"+animatorName+".controller";
			else
				controllerPath = AnimationController.resourcesPath+AnimationController.path+
				objectName+"/"+animatorName+"/"+animatorName+".controller";

			newAnimatorController = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                            
			copyAnimatorValues(baseAnimatorController, newAnimatorController);

			states = getAnimatorStates(newAnimatorController.layers[0].stateMachine);

			for (int i = 0; i<states.Length; i++)
				{
				for (int j = 0; j<jsonData.Count; j++)
					if (jsonData[j]["name"].Equals(states[i].name))
						{
						animationClip = createAnimation(objectName, animatorName, states[i].name, jsonData[j], singleAnimator);

						if (animationClip!=null)
							states[i].motion = animationClip;

						break;
						}
				}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			}


        private AnimationClip createAnimation(string objectName, string animatorName, string animationName, JsonData animationData, bool singleAnimator)
			{
			AnimationClip animationClip = new AnimationClip();
			animationClip.wrapMode = ((bool) animationData["loop"]) ? WrapMode.Loop : WrapMode.Once;
			animationClip.name = animationName;
			animationClip.frameRate = float.Parse(animationData["frameRate"].ToString());

			int frameCount = 0;

			string animationFolder;

            if (singleAnimator)
                animationFolder = AnimationController.resourcesPath+
                AnimationController.path+objectName+"/"+animationName;
                else
                animationFolder = AnimationController.resourcesPath+
                AnimationController.path+objectName+"/"+animatorName+"/"+animationName;

			if (!Directory.Exists(animationFolder))
				Directory.CreateDirectory(animationFolder);

			FileInfo[] files = new DirectoryInfo(animationFolder).GetFiles();

			for (int i = 0; i<files.Length; i++)
				if (files[i].Extension==".png")
					frameCount++;	

            string pngPath;                            

			ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[frameCount];

			for (int k = 0; k<frameCount; k++)
				{
				keyFrames[k] = new ObjectReferenceKeyframe();
				keyFrames[k].time = k*(1/animationClip.frameRate);

                if (singleAnimator)
                    pngPath = AnimationController.path+
                    objectName+"/"+animationName+"/"+(k+1);
                    else
                    pngPath = AnimationController.path+
                    objectName+"/"+animatorName+"/"+animationName+"/"+(k+1);

				keyFrames[k].value = Resources.Load<Sprite>(pngPath);
				}

			AnimationClipSettings animationClipSettings = new AnimationClipSettings();
			animationClipSettings.loopTime = ((bool) animationData["loop"]);
			AnimationUtility.SetAnimationClipSettings(animationClip, animationClipSettings);

			EditorCurveBinding editorCurveBinding = new EditorCurveBinding();
			editorCurveBinding.type = typeof(SpriteRenderer);
			editorCurveBinding.path = "";
			editorCurveBinding.propertyName = "m_Sprite";

			AnimationUtility.SetObjectReferenceCurve(animationClip, editorCurveBinding, keyFrames);

            string finalPath;

            if (singleAnimator)
                finalPath = AnimationController.resourcesPath+AnimationController.path+
                objectName+"/"+animationName+"/"+animationName+".anim";
                else
                finalPath = AnimationController.resourcesPath+AnimationController.path+
                objectName+"/"+animatorName+"/"+animationName+"/"+animationName+".anim";

			AssetDatabase.CreateAsset(animationClip, finalPath);

			return animationClip;
			}

		#endregion



		#region SAVE

		private void save(bool showMessage = true)
			{
			saveConfig();
			//removeEditorProperties();

			for (int i = 0; i<objectsToUpdate.Count; i++)
				{
				Misc.saveFile(objectsToUpdate[i].data.ToJson(true),
					objectsToUpdate[i].path+objectsToUpdate[i].name+"/"+objectsToUpdate[i].name+".json");
				}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			if (showMessage)
				Debug.Log("Data successfully saved");
			}


		private void saveConfig()
            {
            config.stateNames.Clear();
            config.animators.Clear();

            for (int i = 0; i<objectsToUpdate.Count; i++)
                {
                config.stateNames.Add(objectsToUpdate[i].name, objectsToUpdate[i].statesName);
                config.animators.Add(objectsToUpdate[i].name, objectsToUpdate[i].animators);
                }

			config.test = "Yeah";
			Misc.saveConfigFile<Config>(config, configPath);
			}


		private void removeEditorProperties()
			{
			JsonData data;
			for (int i = 0; i<objectsToUpdate.Count; i++)
				for (int j = 0; j<objectsToUpdate[i].data.Count; j++)
					{
					data = objectsToUpdate[i].data[j];
					data.Remove("visibleInEditor");
					data.Remove("eventsVisibleInEditor");

					if (data.Contains("events"))
						for (int k = 0; k < data["events"].Count; k++) 
							{												
							data["events"][k].Remove("visibleInEditor");
							}
					}
			}

		#endregion
		}
	}