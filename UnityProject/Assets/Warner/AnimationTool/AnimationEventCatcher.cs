using UnityEngine;

namespace Warner.AnimationTool
	{
	public class AnimationEventCatcher: MonoBehaviour
		{
		#region MEMBER FIELDS

		public delegate void EventsHandler(AnimationEvent type);
		public event EventsHandler onEventFired;

		#endregion



		#region EVENTS STUFF
			
		public void eventFired(AnimationEvent data)
			{
			if (onEventFired!=null && data!=null)
				onEventFired(data);
			}	
			
		#endregion
		}
	}