using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Warner
	{
	public class Timing: MonoBehaviour
		{
		private class WaitingProcess
			{
			public Timing instance;
			public Segment timing;
			public IEnumerator<float> process;
			public IEnumerator<float> trigger;
			public readonly List<IEnumerator<float>> tasks = new List<IEnumerator<float>>();
			}

		public enum Segment
		    {
			Update,
			FixedUpdate,
			LateUpdate,
			SlowUpdate
		    }

		public float timeBetweenSlowUpdateCalls = 1f/7f;
		public int numberOfUpdateCoroutines;
		public int numberOfFixedUpdateCoroutines;
		public int numberOfLateUpdateCoroutines;
		public int numberOfSlowUpdateCoroutines;

		public System.Action<System.Exception, IEnumerator<float>> onError;
		public static System.Func<IEnumerator<float>, Segment, IEnumerator<float>> replacementFunction;
		private readonly List<WaitingProcess> waitingProcesses = new List<WaitingProcess>();

		private bool runningUpdate;
		private bool runningFixedUpdate;
		private bool runningLateUpdate;
		private bool runningSlowUpdate;
		private int nextUpdateProcessSlot;
		private int nextLateUpdateProcessSlot;
		private int nextFixedUpdateProcessSlot;
		private int nextSlowUpdateProcessSlot;
		private ushort framesSinceUpdate;

		private float lastSlowUpdateCallTime;
		private const ushort framesUntilMaintenance = 64;
		private const int processArrayChunkSize = 128;

		private IEnumerator<float>[] updateProcesses = new IEnumerator<float>[processArrayChunkSize*4];
		private IEnumerator<float>[] lateUpdateProcesses = new IEnumerator<float>[processArrayChunkSize];
		private IEnumerator<float>[] fixedUpdateProcesses = new IEnumerator<float>[processArrayChunkSize];
		private IEnumerator<float>[] slowUpdateProcesses = new IEnumerator<float>[processArrayChunkSize];

		private static Timing _instance;

		public static Timing instance
			{
			get
				{
				if (_instance==null || !_instance.gameObject)
					{
					GameObject instanceHome = GameObject.Find("Timing");

					if (instanceHome==null)
						{
						instanceHome = new GameObject { name = "Timing" };

						System.Type movementType = System.Type.GetType("Timing");
						if (movementType!=null)
							instanceHome.AddComponent(movementType);

						_instance = instanceHome.AddComponent<Timing>();
						}
						else
						{
						System.Type movementType = System.Type.GetType("Timing.Movement");
						if (movementType!=null && instanceHome.GetComponent(movementType)==null)
							instanceHome.AddComponent(movementType);

						_instance = instanceHome.GetComponent<Timing>() ?? instanceHome.AddComponent<Timing>();
						}
					}

				return _instance;
				}

			set
				{
				_instance = value;
				}
			}

		void Awake()
			{
			if (_instance==null)
				_instance = this;
			}

		void OnDestroy()
			{
			if (_instance==this)
				_instance = null;
			}

		void Update()
			{
			runningUpdate = true;

			for (int i = 0; i<nextUpdateProcessSlot; i++)
				{
				Profiler.BeginSample("Processing Coroutine");

				if (updateProcesses[i]!=null && !(Time.time<updateProcesses[i].Current))
					{
					try
						{
						if (!updateProcesses[i].MoveNext())
							{
							updateProcesses[i] = null;
							}
						else
						if (float.IsNaN(updateProcesses[i].Current))
								{
								if (replacementFunction==null)
									updateProcesses[i] = null;
								else
									{
									updateProcesses[i] = replacementFunction(updateProcesses[i], Segment.Update);

									replacementFunction = null;
									i--;
									}
								}
						}
					catch (System.Exception ex)
						{
						if (onError==null)
							Debug.LogError("Exception while running coroutine. (Aborting coroutine)\n"+ex.Message+"\n"+ex.StackTrace);
							else
							onError(ex, updateProcesses[i]);

						updateProcesses[i] = null;
						}
					}

				Profiler.EndSample();
				}

			runningUpdate = false;


			if (lastSlowUpdateCallTime+timeBetweenSlowUpdateCalls<Time.time)
				{
				runningSlowUpdate = true;
				lastSlowUpdateCallTime = Time.time;

				for (int i = 0; i<nextSlowUpdateProcessSlot; i++)
					{
					Profiler.BeginSample("Processing Coroutine (Slow Update)");

					if (slowUpdateProcesses[i]!=null && !(Time.time<slowUpdateProcesses[i].Current))
						{
						try
							{
							if (!slowUpdateProcesses[i].MoveNext())
								{
								slowUpdateProcesses[i] = null;
								}
							else
							if (float.IsNaN(slowUpdateProcesses[i].Current))
									{
									if (replacementFunction==null)
										slowUpdateProcesses[i] = null;
									else
										{
										slowUpdateProcesses[i] = replacementFunction(slowUpdateProcesses[i], Segment.SlowUpdate);

										replacementFunction = null;
										i--;
										}
									}
							}
						catch (System.Exception ex)
							{
							if (onError==null)
								Debug.LogError("Exception while running coroutine. (Aborting coroutine)\n"+ex.Message+"\n"+ex.StackTrace);
							else
								onError(ex, slowUpdateProcesses[i]);

							slowUpdateProcesses[i] = null;
							}
						}

					Profiler.EndSample();
					}

				runningSlowUpdate = false;
				}

			if (++framesSinceUpdate>framesUntilMaintenance)
				{
				framesSinceUpdate = 0;

				Profiler.BeginSample("Maintenance Task");

				removeUnused();

				Profiler.EndSample();
				}
			}

		void FixedUpdate()
			{
			runningFixedUpdate = true;

			for (int i = 0; i<nextFixedUpdateProcessSlot; i++)
				{
				Profiler.BeginSample("Processing Coroutine");

				if (fixedUpdateProcesses[i]!=null && !(Time.time<fixedUpdateProcesses[i].Current))
					{
					try
						{
						if (!fixedUpdateProcesses[i].MoveNext())
							{
							fixedUpdateProcesses[i] = null;
							}
						else
						if (float.IsNaN(fixedUpdateProcesses[i].Current))
								{
								if (replacementFunction==null)
									fixedUpdateProcesses[i] = null;
								else
									{
									fixedUpdateProcesses[i] = replacementFunction(fixedUpdateProcesses[i], Segment.FixedUpdate);

									replacementFunction = null;
									i--;
									}
								}
						}
					catch (System.Exception ex)
						{
						if (onError==null)
							Debug.LogError("Exception while running coroutine. (Aborting coroutine)\n"+ex.Message+"\n"+ex.StackTrace);
						else
							onError(ex, fixedUpdateProcesses[i]);

						fixedUpdateProcesses[i] = null;
						}
					}

				Profiler.EndSample();
				}

			runningFixedUpdate = false;
			}

		void LateUpdate()
			{
			runningLateUpdate = true;

			for (int i = 0; i<nextLateUpdateProcessSlot; i++)
				{
				Profiler.BeginSample("Processing Coroutine");

				if (lateUpdateProcesses[i]!=null && !(Time.time<lateUpdateProcesses[i].Current))
					{
					try
						{
						if (!lateUpdateProcesses[i].MoveNext())
							{
							lateUpdateProcesses[i] = null;
							}
						else
						if (float.IsNaN(lateUpdateProcesses[i].Current))
								{
								if (replacementFunction==null)
									lateUpdateProcesses[i] = null;
								else
									{
									lateUpdateProcesses[i] = replacementFunction(lateUpdateProcesses[i], Segment.LateUpdate);

									replacementFunction = null;
									i--;
									}
								}
						}
					catch (System.Exception ex)
						{
						if (onError==null)
							Debug.LogError("Exception while running coroutine. (Aborting coroutine)\n"+ex.Message+"\n"+ex.StackTrace);
						else
							onError(ex, lateUpdateProcesses[i]);

						lateUpdateProcesses[i] = null;
						}
					}

				Profiler.EndSample();
				}

			runningLateUpdate = false;
			}


		public static void killAll()
			{
			if (_instance!=null)
				Destroy(_instance);
			}


		public static void pauseAll()
			{
			if (_instance!=null)
				_instance.enabled = false;
			}


		public static void resumeAll()
			{
			if (_instance!=null)
				_instance.enabled = true;
			}

		private void removeUnused()
			{
			int i, j;
			for (i = j = 0; i<nextUpdateProcessSlot; i++)
				{
				if (updateProcesses[i]!=null)
					{
					if (i!=j)
						updateProcesses[j] = updateProcesses[i];
					j++;
					}
				}
			for (i = j; i<nextUpdateProcessSlot; i++)
				updateProcesses[i] = null;

			numberOfUpdateCoroutines = nextUpdateProcessSlot = j;

			for (i = j = 0; i<nextFixedUpdateProcessSlot; i++)
				{
				if (fixedUpdateProcesses[i]!=null)
					{
					if (i!=j)
						fixedUpdateProcesses[j] = fixedUpdateProcesses[i];
					j++;
					}
				}
			for (i = j; i<nextFixedUpdateProcessSlot; i++)
				fixedUpdateProcesses[i] = null;

			numberOfFixedUpdateCoroutines = nextFixedUpdateProcessSlot = j;

			for (i = j = 0; i<nextLateUpdateProcessSlot; i++)
				{
				if (lateUpdateProcesses[i]!=null)
					{
					if (i!=j)
						lateUpdateProcesses[j] = lateUpdateProcesses[i];
					j++;
					}
				}
			for (i = j; i<nextLateUpdateProcessSlot; i++)
				lateUpdateProcesses[i] = null;

			numberOfLateUpdateCoroutines = nextLateUpdateProcessSlot = j;

			for (i = j = 0; i<nextSlowUpdateProcessSlot; i++)
				{
				if (slowUpdateProcesses[i]!=null)
					{
					if (i!=j)
						slowUpdateProcesses[j] = slowUpdateProcesses[i];
					j++;
					}
				}
			for (i = j; i<nextSlowUpdateProcessSlot; i++)
				slowUpdateProcesses[i] = null;

			numberOfSlowUpdateCoroutines = nextSlowUpdateProcessSlot = j;
			}


		public static IEnumerator<float> run(IEnumerator<float> coroutine)
			{
			return instance.runOnInstance(coroutine, Segment.Update);
			}


		public static IEnumerator<float> run(IEnumerator<float> coroutine, Segment timing)
			{
			return instance.runOnInstance(coroutine, timing);
			}


		public IEnumerator<float> runOnInstance(IEnumerator<float> coroutine, Segment timing)
			{
			if (timing==Segment.Update)
				{
				if (nextUpdateProcessSlot>=updateProcesses.Length)
					{
					IEnumerator<float>[] oldArray = updateProcesses;
					updateProcesses = new IEnumerator<float>[updateProcesses.Length+processArrayChunkSize];
					for (int i = 0; i<oldArray.Length; i++)
						updateProcesses[i] = oldArray[i];
					}

				int currentSlot = nextUpdateProcessSlot;
				nextUpdateProcessSlot++;

				updateProcesses[currentSlot] = coroutine;

				if (!runningUpdate)
					{
					try
						{
						if (!updateProcesses[currentSlot].MoveNext())
							{
							updateProcesses[currentSlot] = null;
							}
						else
						if (float.IsNaN(updateProcesses[currentSlot].Current))
								{
								if (replacementFunction==null)
									updateProcesses[currentSlot] = null;
								else
									{
									updateProcesses[currentSlot] = replacementFunction(updateProcesses[currentSlot], timing);

									replacementFunction = null;

									if (updateProcesses[currentSlot]!=null)
										updateProcesses[currentSlot].MoveNext();
									}
								}
						}
					catch (System.Exception ex)
						{
						if (onError==null)
							Debug.LogError("Exception while running coroutine. (Aborting coroutine)\n"+ex.Message+"\n"+ex.StackTrace);
						else
							onError(ex, updateProcesses[currentSlot]);

						updateProcesses[currentSlot] = null;
						}
					}

				return coroutine;
				}
            
			if (timing==Segment.FixedUpdate)
				{
				if (nextFixedUpdateProcessSlot>=fixedUpdateProcesses.Length)
					{
					IEnumerator<float>[] oldArray = fixedUpdateProcesses;
					fixedUpdateProcesses = new IEnumerator<float>[fixedUpdateProcesses.Length+processArrayChunkSize];
					for (int i = 0; i<oldArray.Length; i++)
						fixedUpdateProcesses[i] = oldArray[i];
					}

				int currentSlot = nextFixedUpdateProcessSlot;
				nextFixedUpdateProcessSlot++;

				fixedUpdateProcesses[currentSlot] = coroutine;

				if (!runningFixedUpdate)
					{
					try
						{
						if (!fixedUpdateProcesses[currentSlot].MoveNext())
							{
							fixedUpdateProcesses[currentSlot] = null;
							}
						else
						if (float.IsNaN(fixedUpdateProcesses[currentSlot].Current))
								{
								if (replacementFunction==null)
									fixedUpdateProcesses[currentSlot] = null;
								else
									{
									fixedUpdateProcesses[currentSlot] = replacementFunction(fixedUpdateProcesses[currentSlot], timing);

									replacementFunction = null;

									if (fixedUpdateProcesses[currentSlot]!=null)
										fixedUpdateProcesses[currentSlot].MoveNext();
									}
								}
						}
					catch (System.Exception ex)
						{
						if (onError==null)
							Debug.LogError("Exception while running coroutine. (Aborting coroutine)\n"+ex.Message+"\n"+ex.StackTrace);
						else
							onError(ex, fixedUpdateProcesses[currentSlot]);

						fixedUpdateProcesses[currentSlot] = null;
						}
					}

				return coroutine;
				}
            
			if (timing==Segment.LateUpdate)
				{
				if (nextLateUpdateProcessSlot>=lateUpdateProcesses.Length)
					{
					IEnumerator<float>[] oldArray = lateUpdateProcesses;
					lateUpdateProcesses = new IEnumerator<float>[lateUpdateProcesses.Length+processArrayChunkSize];
					for (int i = 0; i<oldArray.Length; i++)
						lateUpdateProcesses[i] = oldArray[i];
					}

				int currentSlot = nextLateUpdateProcessSlot;
				nextLateUpdateProcessSlot++;

				lateUpdateProcesses[currentSlot] = coroutine;

				if (!runningLateUpdate)
					{
					try
						{
						if (!lateUpdateProcesses[currentSlot].MoveNext())
							{
							lateUpdateProcesses[currentSlot] = null;
							}
						else
						if (float.IsNaN(lateUpdateProcesses[currentSlot].Current))
								{
								if (replacementFunction==null)
									lateUpdateProcesses[currentSlot] = null;
								else
									{
									lateUpdateProcesses[currentSlot] = replacementFunction(lateUpdateProcesses[currentSlot], timing);

									replacementFunction = null;

									if (lateUpdateProcesses[currentSlot]!=null)
										lateUpdateProcesses[currentSlot].MoveNext();
									}
								}
						}
					catch (System.Exception ex)
						{
						if (onError==null)
							Debug.LogError("Exception while running coroutine. (Aborting coroutine)\n"+ex.Message+"\n"+ex.StackTrace);
						else
							onError(ex, lateUpdateProcesses[currentSlot]);

						lateUpdateProcesses[currentSlot] = null;
						}
					}

				return coroutine;
				}

			if (timing==Segment.SlowUpdate)
				{
				if (nextSlowUpdateProcessSlot>=slowUpdateProcesses.Length)
					{
					IEnumerator<float>[] oldArray = slowUpdateProcesses;
					slowUpdateProcesses = new IEnumerator<float>[slowUpdateProcesses.Length+processArrayChunkSize];
					for (int i = 0; i<oldArray.Length; i++)
						slowUpdateProcesses[i] = oldArray[i];
					}

				int currentSlot = nextSlowUpdateProcessSlot;
				nextSlowUpdateProcessSlot++;

				slowUpdateProcesses[currentSlot] = coroutine;

				if (!runningSlowUpdate)
					{
					try
						{
						if (!slowUpdateProcesses[currentSlot].MoveNext())
							{
							slowUpdateProcesses[currentSlot] = null;
							}
						else
						if (float.IsNaN(slowUpdateProcesses[currentSlot].Current))
								{
								if (replacementFunction==null)
									slowUpdateProcesses[currentSlot] = null;
								else
									{
									slowUpdateProcesses[currentSlot] = replacementFunction(slowUpdateProcesses[currentSlot], timing);

									replacementFunction = null;

									if (slowUpdateProcesses[currentSlot]!=null)
										slowUpdateProcesses[currentSlot].MoveNext();
									}
								}
						}
					catch (System.Exception ex)
						{
						if (onError==null)
							Debug.LogError("Exception while running coroutine. (Aborting coroutine)\n"+ex.Message+"\n"+ex.StackTrace);
						else
							onError(ex, slowUpdateProcesses[currentSlot]);

						slowUpdateProcesses[currentSlot] = null;
						}
					}

				return coroutine;
				}

			return null;
			}


		public static bool kill(IEnumerator<float> coroutine)
			{
			return _instance!=null && _instance.killOnInstance(coroutine);
			}


		public bool killOnInstance(IEnumerator<float> coroutine)
			{
			killChildProcess(coroutine);


			for (int i = 0; i<nextUpdateProcessSlot; i++)
				{
				if (updateProcesses[i]==coroutine)
					{
					updateProcesses[i] = null;
					return true;
					}
				}

			for (int i = 0; i<nextFixedUpdateProcessSlot; i++)
				{
				if (fixedUpdateProcesses[i]==coroutine)
					{
					fixedUpdateProcesses[i] = null;
					return true;
					}
				}

			for (int i = 0; i<nextLateUpdateProcessSlot; i++)
				{
				if (lateUpdateProcesses[i]==coroutine)
					{
					lateUpdateProcesses[i] = null;
					return true;
					}
				}

			return false;
			}


		private void killChildProcess(IEnumerator<float> coroutine)
			{
			for (int i = 0; i<waitingProcesses.Count; i++)
				for (int j = 0; j<waitingProcesses[i].tasks.Count; j++)
					if (waitingProcesses[i].tasks[j]==coroutine)
						{
						killOnInstance(waitingProcesses[i].process);
						waitingProcesses.RemoveAt(i);
						return;
						}
			}


		public bool killOnInstance(IEnumerator<float> coroutine, out Segment segmentFoundOn)
			{
			for (int i = 0; i<nextUpdateProcessSlot; i++)
				{
				if (updateProcesses[i]==coroutine)
					{
					updateProcesses[i] = null;
					segmentFoundOn = Segment.Update;
					return true;
					}
				}

			for (int i = 0; i<nextFixedUpdateProcessSlot; i++)
				{
				if (fixedUpdateProcesses[i]==coroutine)
					{
					fixedUpdateProcesses[i] = null;
					segmentFoundOn = Segment.FixedUpdate;
					return true;
					}
				}

			for (int i = 0; i<nextLateUpdateProcessSlot; i++)
				{
				if (lateUpdateProcesses[i]==coroutine)
					{
					lateUpdateProcesses[i] = null;
					segmentFoundOn = Segment.LateUpdate;
					return true;
					}
				}

			for (int i = 0; i<nextSlowUpdateProcessSlot; i++)
				{
				if (slowUpdateProcesses[i]==coroutine)
					{
					slowUpdateProcesses[i] = null;
					segmentFoundOn = Segment.SlowUpdate;
					return true;
					}
				}

			segmentFoundOn = (Segment) (-1); // An invalid value.
			return false;
			}


		public static float waitForRoutine(IEnumerator<float> otherCoroutine, Segment segment = Segment.Update)
			{
			run(otherCoroutine, segment);

			return waitForRoutine(otherCoroutine, true, instance);
			}


		public static float waitForRoutine(IEnumerator<float> otherCoroutine, bool warnIfNotFound, Timing instance)
			{
			if (instance==null)
				throw new System.ArgumentNullException();

			for (int i = 0; i<instance.waitingProcesses.Count; i++)
				{
				if (instance.waitingProcesses[i].trigger==otherCoroutine)
					{
					WaitingProcess proc = instance.waitingProcesses[i];
					replacementFunction = (input, timing) =>
						{
						proc.tasks.Add(input);
						return null;
						};

					return float.NaN;
					}

				for (int j = 0; j<instance.waitingProcesses[i].tasks.Count; j++)
					{
					if (instance.waitingProcesses[i].tasks[j]==otherCoroutine)
						{
						WaitingProcess proc = new WaitingProcess();
						proc.instance = instance;
						proc.timing = instance.waitingProcesses[i].timing;
						proc.trigger = otherCoroutine;
						proc.process = startWhenDone(proc);

						instance.waitingProcesses[i].tasks[j] = proc.process;

						proc.process.MoveNext();

						replacementFunction = (input, timing) =>
							{
							proc.timing = timing;
							proc.tasks.Add(input);

							return null;
							};

						return float.NaN;
						}
					}
				}

			Segment otherCoroutineSegment;

			if (instance.killOnInstance(otherCoroutine, out otherCoroutineSegment))
				{
				replacementFunction = (input, timing) =>
					{
					WaitingProcess proc = new WaitingProcess();
					proc.instance = instance;
					proc.timing = timing;
					proc.trigger = otherCoroutine;
					proc.process = startWhenDone(proc);
					proc.tasks.Add(input);

					if (timing!=otherCoroutineSegment)
						{
						instance.runOnInstance(proc.process, otherCoroutineSegment);
						return null;
						}

					return proc.process;
					};

				return float.NaN;
				}

			if (warnIfNotFound)
				Debug.LogWarning("WaitUntilDone cannot hold: The coroutine instance that was passed in was not found.");

			return 0f;
			}

		private static IEnumerator<float> startWhenDone(WaitingProcess processData)
			{
			processData.instance.waitingProcesses.Add(processData);

			yield return processData.trigger.Current;

			while (processData.trigger.MoveNext())
				{
				yield return processData.trigger.Current;
				}

			processData.instance.waitingProcesses.Remove(processData);

			for (int i = 0; i<processData.tasks.Count; i++)
				{
				processData.instance.runOnInstance(processData.tasks[i], processData.timing);
				}
			}


		public static float waitForRoutine(WWW wwwObject)
			{
			replacementFunction = (input, timing) => startWhenDone(wwwObject, input);
			return float.NaN;
			}

		private static IEnumerator<float> startWhenDone(WWW www, IEnumerator<float> pausedProc)
			{
			while (!www.isDone)
				yield return 0f;

			replacementFunction = (input, timing) => pausedProc;
			yield return float.NaN;
			}


		public static float waitForSeconds(float waitTime)
			{
			if (float.IsNaN(waitTime))
				waitTime = 0f;

			return Time.time+waitTime;
			}


		public static void callDelayed<TRef>(TRef reference, float delay, System.Action<TRef> action)
			{
			if (action==null)
				return;

			if (delay>0f)
				run(_callDelayBack(reference, delay, action));
			else
				action(reference);
			}

		private static IEnumerator<float> _callDelayBack<TRef>(TRef reference, float delay, System.Action<TRef> action)
			{
			yield return Time.time+delay;

			callDelayed(reference, 0f, action);
			}


		public static void callDelayed(float delay, System.Action action)
			{
			if (action==null)
				return;

			if (delay>0f)
				run(_callDelayBack(delay, action));
			else
				action();
			}

		private static IEnumerator<float> _callDelayBack(float delay, System.Action action)
			{
			yield return Time.time+delay;

			callDelayed(0f, action);
			}


		public static void callPeriodically(float timeframe, float period, System.Action action, System.Action onDone = null)
			{
			if (action!=null)
				run(_callContinuously(timeframe, period, action, onDone), Segment.Update);
			}


		public static void callPeriodically(float timeframe, float period, System.Action action, Segment timing, System.Action onDone = null)
			{
			if (action!=null)
				run(_callContinuously(timeframe, period, action, onDone), timing);
			}


		public static void callContinuously(float timeframe, System.Action action, System.Action onDone = null)
			{
			if (action!=null)
				run(_callContinuously(timeframe, 0f, action, onDone), Segment.Update);
			}


		public static void callContinuously(float timeframe, System.Action action, Segment timing, System.Action onDone = null)
			{
			if (action!=null)
				run(_callContinuously(timeframe, 0f, action, onDone), timing);
			}

		private static IEnumerator<float> _callContinuously(float timeframe, float period, System.Action action, System.Action onDone)
			{
			float startTime = Time.time;
			while (Time.time<=startTime+timeframe)
				{
				yield return period;

				action();
				}

			if (onDone!=null)
				onDone();
			}


		public static void callContinuously<T>(T reference, float timeframe, System.Action<T> action, System.Action<T> onDone = null)
			{
			run(_callContinuously(reference, timeframe, action, onDone), Segment.Update);
			}


		public static void callContinuously<T>(T reference, float timeframe, System.Action<T> action, 
			Segment timing, System.Action<T> onDone = null)
			{
			run(_callContinuously(reference, timeframe, action, onDone), timing);
			}

		private static IEnumerator<float> _callContinuously<T>(T reference, float timeframe,
			System.Action<T> action, System.Action <T> onDone = null)
			{
			float startTime = Time.time;
			while (Time.time<=startTime+timeframe)
				{
				yield return 0f;

				action(reference);
				}

			if (onDone!=null)
				onDone(reference);
			}	
		}
	}