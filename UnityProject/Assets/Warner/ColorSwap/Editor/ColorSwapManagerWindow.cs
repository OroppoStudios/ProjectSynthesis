using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Warner
	{
	public class ColorSwapManagerWindow : EditorWindow
		{
		#region MEMEBER FIELDS

		private ColorSwap.Config config;
		private string configFilePath;
		private Vector2 swapObjectsScrollPosition; 


		#endregion



		#region INIT

		[MenuItem("Tools/Color Swap Manager")]
		static void Init()
			{
			EditorWindow window = EditorWindow.GetWindow<ColorSwapManagerWindow>();
			window.Show();
			}


		private void OnEnable()
			{
			config = EditorMisc.loadConfig<ColorSwap.Config>(ColorSwap.configFileName, ref configFilePath);
			}


		#endregion



		#region GUI

		private void OnGUI()
			{
			EditorGUILayout.Space();
			drawObjectsSection();		

			Rect[] rects = GUIExtensions.getRects(2, 20f, new Vector2(5f, 3f));

			if (GUI.Button(rects[0], "Refresh"))
				{
				OnEnable();
				}

			if (GUI.Button(rects[1], "Save"))
				{
				EditorMisc.saveConfigFile<ColorSwap.Config>(config, configFilePath);
				Debug.Log("Data successfully saved");
				}
			}


		private void drawObjectsSection()
			{
			if (GUILayout.Button("Add Color Swappable Object"))
				config.swapColorObjects.Add(new ColorSwap.SwapColorObject());

			EditorGUILayout.LabelField("Color Swap Objects ("+config.swapColorObjects.Count+")");

			swapObjectsScrollPosition = EditorGUILayout.BeginScrollView(swapObjectsScrollPosition, 
				GUILayout.ExpandWidth(true), GUILayout.Height(820f));

			for (int i = 0; i<config.swapColorObjects.Count; i++)
				putSwapColorOption(config.swapColorObjects, i);

			EditorGUILayout.EndScrollView();
			}


		private void putSwapColorOption(List<ColorSwap.SwapColorObject> colorObjects, int index)
			{
			EditorGUI.indentLevel = 0;

			ColorSwap.SwapColorObject colorObject = colorObjects[index];

			colorObject.editorVisible = EditorGUILayout.Foldout(
				colorObject.editorVisible, colorObjects[index].name);

			if (colorObject.editorVisible)
				{
				EditorGUI.indentLevel = 1;

				//BUTTONS
				Rect[] rects = GUIExtensions.getRects(2, 20f, new Vector2(19f, 3f));

				if (GUI.Button(rects[0], "Add color data"))
					colorObject.colorDataInfo.Add(new ColorSwap.ColorDataPart());

				if (GUI.Button(rects[1], "Delete Color Swap Object"))
					colorObjects.RemoveAt(index);


				//NAME
				EditorGUILayout.LabelField("Name");
				colorObject.name = EditorGUILayout.TextField(colorObject.name);


				//GRADIENT
				guiColorGradients(ref colorObject.topColors, "Top");
				guiColorGradients(ref colorObject.bottomColors, "Bottom");
				

				//PARTS
				EditorGUILayout.Space();
				EditorGUILayout.Space();

				colorObject.editorPartsVisible = EditorGUILayout.Foldout(
					colorObject.editorPartsVisible, "Color scheme ("+colorObject.colorDataInfo.Count+")");									

				if (colorObject.editorPartsVisible)
					{
					for (int j = 0; j<colorObject.colorDataInfo.Count; j++)
						putColorData(colorObject.colorDataInfo, j);
					}

				EditorGUILayout.Space();
				}
			}


		private void guiColorGradients(ref List<ColorSwap.ColorData> colors, string type)
			{
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			Rect[] rects = GUIExtensions.getRects(2, 20f, new Vector2(5f, 3f));
			EditorGUI.LabelField(rects[0], type+" gradient colors");
			EditorGUILayout.Space();

			if (colors==null)
				colors = new List<ColorSwap.ColorData>();

			if (GUI.Button(rects[1], "Add variant color"))
				colors.Add(new ColorSwap.ColorData(Color.white));

			for (int i = 0; i<colors.Count; i++)
				{
				rects = GUIExtensions.getRects(2, 20f, new Vector2(5f, 3f));

				colors[i] = new ColorSwap.ColorData(
					EditorGUI.ColorField(rects[0], colors[i].getColor()));

				if (GUI.Button(rects[1], "Delete"))
					colors.RemoveAt(i);
				}
			}


		private void putColorData(List<ColorSwap.ColorDataPart> data, int index)
			{
			EditorGUI.indentLevel = 2;

			EditorGUILayout.LabelField("Name");
			ColorSwap.ColorDataPart colorData = data[index];
			colorData.name = EditorGUILayout.TextField(colorData.name);

			EditorGUILayout.LabelField("Red value");
			colorData.redChannel = EditorGUILayout.IntSlider(colorData.redChannel, 0, 255);

            EditorGUILayout.LabelField("TextureIndex");
            colorData.textureIndex = EditorGUILayout.IntSlider(colorData.textureIndex, 0, 10);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Color variants");
			for (int i = 0; i<colorData.colorVariants.Count; i++)
				putColorVariant(colorData.colorVariants, i);

			EditorGUILayout.Space();
			Rect[] rects = GUIExtensions.getRects(2, 20f, new Vector2(33f, 3f));

			if (GUI.Button(rects[0], "Add variant"))
				colorData.colorVariants.Add(new ColorSwap.ColorData());							

			if (GUI.Button(rects[1], "Delete color data"))
				data.RemoveAt(index);
			}


		private void putColorVariant(List<ColorSwap.ColorData> colors, int index)
			{
			Rect[] rects = GUIExtensions.getRects(3, 20f, new Vector2(5f, 3f));

			EditorGUI.LabelField(rects[0], index.ToString());

			colors[index] = new ColorSwap.ColorData(EditorGUI.ColorField(rects[1], colors[index].getColor()));

			if (GUI.Button(rects[2], "Delete variant"))
				colors.RemoveAt(index);
			}


		#endregion
		}
	}
