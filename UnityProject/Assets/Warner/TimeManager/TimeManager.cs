using UnityEngine;
using System;

namespace Warner 
	{
	public class TimeManager: MonoBehaviour
		{
		#region MEMBER FIELDS

		[NonSerialized] public float pauseStartTime;

		public delegate void EventsHandler(bool isPaused);

		public event EventsHandler onPauseToggle;

		public static TimeManager instance;

		private bool _paused;


		#endregion


		
		#region INIT STUFF

		private void Awake()
			{
			instance = this;				
			}

		#endregion



		#region PAUSE STUFF

		public bool paused
			{
			get
				{
				return _paused;
				}
			set
				{
				if (_paused==value)
					return;

				_paused = value;

				if (_paused)
					{
					pauseStartTime = Time.time;
					Time.timeScale = 0;
					}
				else
					Time.timeScale = 1;		

				if (onPauseToggle!=null)
					onPauseToggle(_paused);
				}
			}

		#endregion
		}
	}