using System.Collections;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Warner
	{
	public class EditorCoroutine
		{
		readonly IEnumerator routine;

		public static EditorCoroutine start(IEnumerator _routine)
			{
			EditorCoroutine coroutine = new EditorCoroutine(_routine);
			coroutine.start();
			return coroutine;
			}


		EditorCoroutine(IEnumerator _routine)
			{
			routine = _routine;
			}

		private void start()
			{
			EditorApplication.update += Update;
			}

		public void Stop()
			{
			EditorApplication.update -= Update;
			}

		private void Update()
			{
			if (!routine.MoveNext())
				{
				Stop();
				}
			}
		}
	}