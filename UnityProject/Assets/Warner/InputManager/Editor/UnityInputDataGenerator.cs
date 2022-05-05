using UnityEditor;
using UnityEngine;


namespace Warner
	{
	public class UnityInputDataGenerator
		{
		#region MEMBER FIELDS

		public enum AxisType{KeyOrMouseButton, MouseMovement, JoystickAxis}

		public class InputData
			{
			public string name;
			public string descriptiveName;
			public string descriptiveNegativeName;
			public string negativeButton;
			public string positiveButton;
			public string altNegativeButton;
			public string altPositiveButton;
			public float gravity;
			public float dead;
			public float sensitivity = 1;
			public bool snap = true;
			public bool invert;
			public AxisType type;
			public int axis = 1;
			public int joyNum;
			}

		private static SerializedObject inputManagerAsset;

		#endregion



		#region INIT

		[MenuItem("Tools/CreateUnityInputData")]
		public static void Init()
			{
			inputManagerAsset = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
			clearCurrentInputs();
			createDefaultUnityInputs();
			createKeyboardInputs();
			createAllJoysticksInputs();
			Debug.Log("UnityInputDataGenerator: Done");
			}


		private static void createKeyboardInputs()
			{
			InputData inputData = new InputData();
			inputData.name = "Submit";
			inputData.positiveButton = "enter";
			inputData.altPositiveButton = "space";
			inputData.gravity = 1000;
			inputData.dead = 0.001f;
			addInput(inputData);

			inputData = new InputData();
			inputData.name = "Cancel";
			inputData.positiveButton = "escape";
			inputData.gravity = 1000;
			inputData.dead = 0.001f;
			addInput(inputData);
			}


		private static void createDefaultUnityInputs()
			{
			InputData inputData = new InputData();
			inputData.name = "Horizontal";
			inputData.descriptiveName = "Dummy for unity";
			addInput(inputData);

			inputData = new InputData();
			inputData.name = "Vertical";
			inputData.descriptiveName = "Dummy for unity";
			addInput(inputData);
			}


		private static void createAllJoysticksInputs()
			{
			for (int i = 1; i<=InputManager.supportedJoystickCount; i++)
				createJoystickInputs(i);
			}


		private static void createJoystickInputs(int joyNum)
			{
			InputData inputData;

			for (int i = 1; i<=28; i++)
				{
				inputData = new InputData();
				inputData.name = "Joystick"+joyNum+"-Axis"+i;
				inputData.type = AxisType.JoystickAxis;
				inputData.axis = i;
				inputData.joyNum = joyNum;
				addInput(inputData);
				}
			}

		#endregion


		#region ASSET MODIFICATIONS

		private static void clearCurrentInputs()
			{
			SerializedProperty axesProperty = inputManagerAsset.FindProperty("m_Axes");
			axesProperty.ClearArray();
			inputManagerAsset.ApplyModifiedProperties();
			}


		private static SerializedProperty getChildProperty(SerializedProperty parent, string name)
			{
			SerializedProperty child = parent.Copy();
			child.Next(true);
			do {if (child.name == name) return child;}
				while (child.Next(false));

			return null;
			}


		private static void addInput(InputData axis)
			{
			SerializedProperty axesProperty = inputManagerAsset.FindProperty("m_Axes");
			axesProperty.arraySize++;
			inputManagerAsset.ApplyModifiedProperties();

			SerializedProperty axisProperty = axesProperty.GetArrayElementAtIndex(axesProperty.arraySize-1);

			getChildProperty(axisProperty, "m_Name").stringValue = axis.name;
			getChildProperty(axisProperty, "descriptiveName").stringValue = axis.descriptiveName;
			getChildProperty(axisProperty, "descriptiveNegativeName").stringValue = axis.descriptiveNegativeName;
			getChildProperty(axisProperty, "negativeButton").stringValue = axis.negativeButton;
			getChildProperty(axisProperty, "positiveButton").stringValue = axis.positiveButton;
			getChildProperty(axisProperty, "altNegativeButton").stringValue = axis.altNegativeButton;
			getChildProperty(axisProperty, "altPositiveButton").stringValue = axis.altPositiveButton;
			getChildProperty(axisProperty, "gravity").floatValue = axis.gravity;
			getChildProperty(axisProperty, "dead").floatValue = axis.dead;
			getChildProperty(axisProperty, "sensitivity").floatValue = axis.sensitivity;
			getChildProperty(axisProperty, "snap").boolValue = axis.snap;
			getChildProperty(axisProperty, "invert").boolValue = axis.invert;
			getChildProperty(axisProperty, "type").intValue = (int) axis.type;
			getChildProperty(axisProperty, "axis").intValue = axis.axis-1;
			getChildProperty(axisProperty, "joyNum").intValue = axis.joyNum;
			inputManagerAsset.ApplyModifiedProperties();
			}

		#endregion
		}
	}