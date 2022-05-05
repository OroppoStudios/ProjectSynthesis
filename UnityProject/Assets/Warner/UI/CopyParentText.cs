
using UnityEngine;
using UnityEngine.UI;

namespace Warner
	{
	public class CopyParentText : MonoBehaviour
		{
		private Text text;
		private Text parentText;

		private void Awake()
			{
			parentText = transform.parent.GetComponent<Text>();
			text = GetComponent<Text>();
			}

		private void Update()
			{
			if (parentText==null)
				return;

			if (parentText.text!=text.text)
				text.text = parentText.text;
			}
		}
	}
