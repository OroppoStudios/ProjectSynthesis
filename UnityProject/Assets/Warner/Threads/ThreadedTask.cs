using System;
using System.Threading;
using System.Collections.Generic;


namespace Warner {

public class ThreadedTask
	{
	#region MEMBER FIELDS

	private bool _isDone;
	private object handle = new object();
	private Thread thread;
	private IEnumerator <float> updateRoutine;
	private Action execute;
	private Action onFinish;

	#endregion



	#region THREAD STUFF


	public ThreadedTask(Action executeMethod,Action onFinishMethod)
		{
		execute = executeMethod;
		onFinish = onFinishMethod;
		thread = new Thread(run);
		thread.Start();
		updateRoutine = updateCoRoutine();
		Timing.run(updateRoutine);
		}


	private void run()
		{
		execute();
		isDone = true;
		}

	public virtual void abort()
		{
		thread.Abort();
		}


	private IEnumerator <float> updateCoRoutine()
		{
 		while(true)
 			{
			if (isDone)
				{
				onFinish();
				Timing.kill(updateRoutine);
				yield break;
				}

 			yield return 0;
 			}
		}


	public bool isDone
		{
		get
			{
			bool tmp;
			lock (handle)
				tmp = _isDone;
			return tmp;
			}
		set
			{
			lock (handle)
				_isDone = value;
			}
		}

	#endregion


	}

}