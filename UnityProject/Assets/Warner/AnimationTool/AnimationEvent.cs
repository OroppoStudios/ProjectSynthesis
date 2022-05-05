using UnityEngine;
using System.Collections.Generic;

namespace Warner.AnimationTool
	{
	public class AnimationEvent: ScriptableObject
		{
		public int frame;
		public string animationName;
		public float frameDuration;
		public bool loop;
		public bool isUpdateFrame;
		public AnimationEventType type;
		public Dictionary<string, object> parameters;

		public bool contains(string key)
			{
			return parameters.ContainsKey(key);
			}

        public T get<T>(string key)
            {
            object value;

            if (!parameters.ContainsKey(key))
                {
                if (typeof(T)==typeof(bool))
                    value = false;
                    else
                if (typeof(T)==typeof(string))
                    value = "";
                    else
                if (typeof(T)==typeof(int))
                    value = 0;
                    else
                if (typeof(T)==typeof(float))
                    value = 0f;
                    else
                if (typeof(T)==typeof(Vector2))
                    value = Vector2.zero;
                    else
                if (typeof(T)==typeof(Vector3))
                    value = Vector3.zero;
                    else
                    value = string.Empty;
                }
                else
                {
                value = parameters[key];

                if (typeof(T)==typeof(bool))
                    value = bool.Parse((string) value);
                else
                if (typeof(T)==typeof(int))
                    value = int.Parse((string) value);
                else
                if (typeof(T)==typeof(float))
                    value = float.Parse((string) value);
                }

            return (T) value;
            }
		}
	}
