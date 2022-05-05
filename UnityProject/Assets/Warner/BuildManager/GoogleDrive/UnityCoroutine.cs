using System;
using System.Collections;
using System.Collections.Generic;

namespace GDrive
{
	class UnityCoroutine : IEnumerator
	{
		private bool _isDone = false;
        
		public bool isDone
		{
			get
			{
				return _isDone;
			}

			protected set
			{
				_isDone = value;

				if (_isDone && !isCoroutine && done != null)
				{
					done(this);
					done = null;
				}
			}
		}
        
		private Action<UnityCoroutine> _done = null;
        
		public Action<UnityCoroutine> done 
		{
			get
			{
				return _done;
			}
			set
			{
				_done = value;

				if (isDone)
				{
					_done(this);
					_done = null;
				}
			}
		}
        
		protected UnityCoroutine() : this(null) { }


		public UnityCoroutine(Action<UnityCoroutine> doneCallback)
		{
			done = doneCallback;
		}
        
		public void DoSync()
		{
			while (MoveNext()) ;
		}
        
		protected Queue<Action> routines = new Queue<Action>();


		private bool isCoroutine = false;
        
		public bool MoveNext()
		{
			isCoroutine = true;

			if (!isDone && routines.Count > 0)
			{
				Action routine = routines.Dequeue();
				routine();
			}

			if (isDone && done != null)
			{
				done(this);
				done = null;
			}

			return !isDone;
		}
        
		public void Reset()
		{
			routines.Clear();
		}


		public object Current
		{
			get
			{
				return routines.Count;
			}
		}
	}
}
