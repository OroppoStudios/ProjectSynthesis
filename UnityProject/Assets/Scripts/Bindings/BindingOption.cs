using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warner;
using Warner.AnimationTool;
using System;
using UnityEngine.UI;

namespace Game
	{ 

	public class BindingOption : MonoBehaviour
		{
		public Text label;
		public Text stickPosition;
		public Text button;
		public Button buttonComponent;
		
		public void Awake()
			{
			label = transform.Find("Label").GetComponent<Text>();
			stickPosition = transform.Find("StickPosition").GetComponent<Text>();
			button = transform.Find("Button").GetComponent<Text>();
			buttonComponent =GetComponent<Button>();
			}
		}
	}