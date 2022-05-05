using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using UnityEditorInternal;

namespace Warner
	{
	[CustomPropertyDrawer(typeof(SortingLayer), true)]
	public class SortingLayerProperyDrawer: PropertyDrawer
		{
		private string layer;
		private string[] sortingLayerNames;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
			//EditorGUI.PropertyField(position, property, label, true);
					
		    EditorGUI.BeginProperty(position, label, property);

			sortingLayerNames = GetSortingLayerNames();
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
			SerializedProperty layerProperty = property.FindPropertyRelative("name");
			int index = Mathf.Max (0, Array.IndexOf(sortingLayerNames, layerProperty.stringValue));
			index = EditorGUI.Popup(position, index, sortingLayerNames);
			layerProperty.stringValue = sortingLayerNames[index];

			EditorGUI.EndProperty();
			}

//		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//			{
//			return EditorGUI.GetPropertyHeight(property);
//			}

		public string[] GetSortingLayerNames()
			{
			Type internalEditorUtilityType = typeof(InternalEditorUtility);
			PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
			var sortingLayers = (string[])sortingLayersProperty.GetValue(null, new object[0]);
			return sortingLayers;
			}
		}
	}