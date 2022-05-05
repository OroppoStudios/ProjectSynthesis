using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using UnityEditorInternal;
using System.Collections.Generic;

namespace Warner
	{
	[CustomPropertyDrawer(typeof(Layer), true)]
	public class LayerProperyDrawer: PropertyDrawer
		{
		private string[] layerNames;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
			//EditorGUI.PropertyField(position, property, label, true);
					
		    EditorGUI.BeginProperty(position, label, property);

			layerNames = GetLayerNames();
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
			SerializedProperty layerProperty = property.FindPropertyRelative("name");
			int index = Mathf.Max (0, Array.IndexOf(layerNames, layerProperty.stringValue));
			index = EditorGUI.Popup(position, index, layerNames);
			layerProperty.stringValue = layerNames[index];

			EditorGUI.EndProperty();
			}

//		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//			{
//			return EditorGUI.GetPropertyHeight(property);
//			}

		public string[] GetLayerNames()
			{
            List<string> layers = new List<string>();
            string layerName;

            for(int i=8; i<=31; i++)
                {
                layerName = LayerMask.LayerToName(i);

                if (layerName.Length>0)
                    layers.Add(layerName);
                }

			return layers.ToArray();
			}
		}
	}