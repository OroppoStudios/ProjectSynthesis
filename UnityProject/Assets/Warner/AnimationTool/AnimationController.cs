using UnityEngine;
using LitJson;
using System;
using System.Collections.Generic;

namespace Warner.AnimationTool
	{
	public class AnimationController: PoolObject
		{
		#region MEMBER FIELDS

        public string animationsObjectName;

		[NonSerialized] public List<Animator> animators;
        [NonSerialized] public SpriteRenderer[] spriteRenderers;  
		[NonSerialized] public SortingLayer currentSortingLayer;     
		[NonSerialized] public AudioSource audioSource;
		[NonSerialized] public ShadowTrail shadowTrail;
		[NonSerialized] public SpriteFlasher spriteFlasher;
		[NonSerialized] public Character owner;
		[NonSerialized] public bool frameIsPaused;
		[NonSerialized] public HashSet<string> singleTimeMap = new HashSet<string>();
		[NonSerialized] public AnimationData currentAnimation;
		[NonSerialized] public bool animatorsReady;
		[NonSerialized] public AnimationEventCatcher eventCatcher;
		[NonSerialized] public List<string> animations = new List<string>();

		public delegate void EventsHandler(object data);
		public event EventsHandler onAnimatorsReadyEvent;
		public event EventsHandler onAnimationFrameUpdate;
		public event EventsHandler onAnimationChanged;

		public struct AnimationData
			{
			public string name;
			public float frameDuration;
			public bool loop;
			}

        public static List<Animator> sceneAnimators = new List<Animator>();

        public const string resourcesPath = "Assets/Resources/";
        public const string path = "AnimationTool/";               

        private IEnumerator<float> pauseRoutine;
        private bool animatorsPaused;	
		private float timeWeUpdatedState;

        private const string preFullPath = "Assets/Game/Resources/";
		private const string baseQuality = "High";

		#endregion



        #region STATIC


        public static void pauseSceneAnimators()
            {
            for (int i = 0; i<sceneAnimators.Count; i++)
                sceneAnimators[i].speed = 0;
            }


        public static void resumeSceneAnimators()
            {
            for (int i = 0; i<sceneAnimators.Count; i++)
                sceneAnimators[i].speed = 1;
            }


        private static void addAnimatorToSceneList(Animator animator)
            {
            sceneAnimators.Add(animator);
            }


        private static void removeAnimatorFromSceneList(Animator animator)
            {
            sceneAnimators.Remove(animator);
            }

        #endregion



		#region INIT	

		protected override void Awake()
			{
			base.Awake();

			shadowTrail = GetComponent<ShadowTrail>();
			spriteFlasher = GetComponent<SpriteFlasher>();
			spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            animators = GetComponentsInChildren<Animator>().toList();		           			

            if (animationsObjectName==string.Empty)
                animationsObjectName = name.Replace("(Clone)", "");

			setAnimatorControllers();
			}


		protected override void Start()
			{
			base.Start();
			initAudioSource();
			}


        protected override void OnEnable()
            {
            base.OnEnable();

            for (int i = 0; i<animators.Count; i++)
                {
                animators[i].enabled = true;//make sure our animators are enabled in case the object got deactivated during a halt/pause
                addAnimatorToSceneList(animators[i]);            
                }

			eventCatcher.onEventFired += onEventFired;
            }

		#endregion



		#region UPDATE

		protected virtual void Update()
			{
			animatorsReadyCheck();
			}


		protected virtual void LateUpdate()
			{
			checkAnimationNormalizedTime();
			}

		private void animatorsReadyCheck()
			{
			if (animatorsReady)
				return;

			for (int i = 0; i<animators.Count; i++)
				if (!animators[i].gameObject.activeSelf)
					return;

			animatorsReady = true;
			onAnimatorsReady();
			}


		protected virtual void onAnimatorsReady()
			{
			if (onAnimatorsReadyEvent!=null)
				onAnimatorsReadyEvent(null);
			}


		protected virtual void animationFrameUpdate(bool isRepeated)
			{
			if (onAnimationFrameUpdate!=null)
				onAnimationFrameUpdate(isRepeated);
			}

		#endregion



        #region DESTROY

		protected override void OnDisable()
            {
            base.OnDisable();

            for (int i = 0; i<animators.Count; i++)
                removeAnimatorFromSceneList(animators[i]);   

            singleTimeMap.Clear();

			eventCatcher.onEventFired -= onEventFired;    
			animatorsReady = false;
            }

        #endregion



        #region AUDIO

        private void initAudioSource()
			{
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.outputAudioMixerGroup = AudioManager.instance.audioSources.sfx.outputAudioMixerGroup;
			}

		#endregion



        #region EVENTS
       
		private void attachEventsToAnimation(AnimationClip animationClip, JsonData jsonData, string objectName)
			{
			const string functionName = "eventFired";

			animationClip.events = new UnityEngine.AnimationEvent[0];
			UnityEngine.AnimationEvent theEvent;
			AnimationEvent eventData;


			//attach the added events
			if (jsonData.Contains("events"))
				{
				JsonData events = jsonData["events"];
				string[] sText;
				string paramName;
				string value;
				bool disabledEvent;

				for (int k = 0; k<events.Count; k++)
					{
					eventData = ScriptableObject.CreateInstance<AnimationEvent>();
					eventData.frame = int.Parse(events[k]["frame"].ToString())-1;
					disabledEvent = false;

					try
						{
						eventData.type = Misc.parseEnum<AnimationEventType>(events[k]["type"].ToString());
						}
					catch (Exception)
						{
						Debug.LogWarning("The event "+events[k]["type"]+" (object: "+objectName+", clip: "+animationClip.name+") doesnt exist in the enum definition");
						}

					if (events[k].Contains("parameters"))
						{
						eventData.parameters = new Dictionary<string, object>();

						for (int i = 0; i<events[k]["parameters"].Count; i++)
							{
							paramName = events[k]["parameters"][i]["name"].ToString();
							value = events[k]["parameters"][i]["value"].ToString();
							sText = value.Split(',');

							if (paramName=="disabled" && value=="true")
								{
								disabledEvent = true;
								break;
								}

							switch (sText.Length)
								{
								case 2:
									eventData.parameters.Add(paramName, new Vector2(float.Parse(sText[0]), float.Parse(sText[1])));
								break;
								case 3:
									eventData.parameters.Add(paramName, new Vector3(float.Parse(sText[0]), 
										float.Parse(sText[1]), float.Parse(sText[2])));
								break;
								default:
									eventData.parameters.Add(paramName, value);
								break;
								}                                                   
							}
						}

					if (disabledEvent)
						continue;

					eventData.animationName = animationClip.name;

					theEvent = new UnityEngine.AnimationEvent();
					theEvent.time = eventData.frame*(1/animationClip.frameRate);

					theEvent.functionName = functionName;
					theEvent.objectReferenceParameter = eventData;

					animationClip.AddEvent(theEvent);
					}
				}


			//attach the internal event change that we use to detect changes on the animation and it's frames
			//we do this at the end so that they get executed first when playing animations
			int framesCount = Convert.ToInt16(animationClip.length/(1/animationClip.frameRate));
			float frameDuration = (1/animationClip.frameRate);

			for (int i = 0; i<framesCount; i++)
				{
				eventData = ScriptableObject.CreateInstance<AnimationEvent>();
				eventData.isUpdateFrame = true;
				eventData.animationName = animationClip.name;
				eventData.frameDuration = 1/animationClip.frameRate;
				eventData.loop = animationClip.isLooping;
				eventData.frame = i;
				eventData.type = AnimationEventType.Custom;
				eventData.parameters = new Dictionary<string, object>();
				
				theEvent = new UnityEngine.AnimationEvent();
				theEvent.time = i*frameDuration;
				theEvent.functionName = functionName;
				theEvent.objectReferenceParameter = eventData;
				animationClip.AddEvent(theEvent);
				}            
			}				


		protected virtual void onEventFired(AnimationEvent data)
            {
            switch (data.type)
                {
				case AnimationEventType.Custom:                    
					customEvent(data);
                break;
				case AnimationEventType.Pause:                    
					pause(data.get<float>("duration"), 1);
                break;
                case AnimationEventType.RepeatFrame:                    
					repeatFrame(data);
                break;
                case AnimationEventType.Sfx:
                	animationEventSfx(data);
                break;
                case AnimationEventType.ShadowTrail:
                	animationEventShadowTrail(data);
                break;
                }
            }


		private void customEvent(AnimationEvent data)
			{
			if (data.isUpdateFrame)
				{
				if (data.animationName!=currentAnimation.name)
					{
					currentAnimation.name = data.animationName;
					currentAnimation.frameDuration = data.frameDuration;
					currentAnimation.loop = data.loop;
					singleTimeMap.Clear();

					if (onAnimationChanged!=null)
						onAnimationChanged(currentAnimation.name);
					}

				animationFrameUpdate(false);
				}
			}


		public bool conditionalPass(AnimationEvent data)
			{
			if (owner==null)
				{
				Debug.LogWarning("AnimationController: owner Character is not assigned on "+name);
				return false;
				}

			string conditional = data.get<string>("conditional");

			switch (conditional)
				{
				case "lastState":
					if (owner.lastState==Misc.parseEnum<CharacterState>(data.get<string>("conditionalValue"), true))
						return true;
				break;
				case "minXSpeed":
					if (Mathf.Abs(owner.movements.rigidBody.velocity.x)>data.get<float>("conditionalValue"))
						return true;
				break;
				case "rawMoving":
					if (owner.control.rawMovementDirection.x!=0)
						return true;
				break;
				case "notRawMoving":
					if (owner.control.rawMovementDirection.x==0)
						return true;
				break;
				case "movingSameRawDirection":
					if (owner.movements.movingRawSameDirection())
						return true;
				break;
				case "movingOppositeRawDirection":
					if (owner.movements.movingOppositeRawDirection())
						return true;
				break;
				case "validCombo":
					//print(owner.attacks.currentComboStatus);
					if (owner.attacks.currentComboStatus!=ComboManager.ComboStatus.Invalid 
						&& owner.attacks.currentComboStatus!=ComboManager.ComboStatus.Starting)
						return true;
				break;
				default:
				return true;
				}

			return false;
			}


		private void animationEventShadowTrail(AnimationEvent data)
			{
			if (!conditionalPass(data))
				return;

			bool stop = data.contains("stop");

			if (stop)
				shadowTrail.stop(data.contains("clear"));
				else
				{
				float distance = data.contains("distance") ? data.get<float>("distance") : shadowTrail.followDistance;
				float duration = data.contains("duration") ? data.get<float>("duration") : shadowTrail.duration;
				float delay = data.get<float>("delay");

				shadowTrail.method = Misc.parseEnum<ShadowTrail.Method>
					(data.get<string>("method"), true);

				shadowTrail.direction = Misc.parseEnum<ShadowTrail.Direction>
					(data.get<string>("direction"), true);

				shadowTrail.start(distance, duration, delay, data.contains("singleFrame"));
				}
			}



		protected virtual void animationEventSfx(AnimationEvent data)
			{
			bool singleTime = data.get<bool>("singleTime");
			string sfxName = data.get<string>("name");

			if (singleTime)
				{
				if (singleTimeMap.Contains("sfx-"+sfxName))
					return;

				singleTimeMap.Add("sfx-"+sfxName);
				}


			AudioManager.instance.playSfx(sfxName, data.get<float>("volume"), 0f, audioSource);
			}


		private void repeatFrame(AnimationEvent data)
			{
			int count = data.get<int>("count");
			bool halfTime = data.contains("halfTime");

			if (count==0)
				count = 1;

			pause(halfTime ? currentAnimation.frameDuration*0.5f : currentAnimation.frameDuration, count, true);
			}


       	private void pause(float frameDuration, int count, bool isFrameRepeat = false)
       		{
			if (pauseRoutine!=null)
            	Timing.kill(pauseRoutine);

            frameIsPaused = true;
			pauseRoutine = pauseCoRoutine(frameDuration, count, isFrameRepeat);
			Timing.run(pauseRoutine);
       		}


		private IEnumerator<float> pauseCoRoutine(float frameDuration, int count, bool isFrameRepeat)
			{
			pauseAnimators();

			for (int i = 0; i<count; i++)
				{
				if (isFrameRepeat && i>0)
					animationFrameUpdate(true);

				yield return Timing.waitForSeconds(frameDuration);
				}
			
        	resumeAnimators();
			frameIsPaused = false;						
        	}

		#endregion



		#region JSON DATA

		public static TextAsset getJsonAsset(string path, bool showWarning = true)
			{
			TextAsset jsonAsset = Resources.Load<TextAsset>(path);
			if (jsonAsset==null)
				{
                if (showWarning)
				    Debug.LogWarning("Could not find json file: "+path);
				return null;
				}

			return jsonAsset;
			}


		public static JsonData getJsonData(string path)
			{
			TextAsset jsonAsset = getJsonAsset(path);

			return prepareJsonData(jsonAsset.text);
			}


		public static JsonData getJsonData(TextAsset jsonAsset)
			{
			return prepareJsonData(jsonAsset.text);
			}


		private static JsonData prepareJsonData(string jsonData)
			{
			JsonData data = JsonMapper.ToObject(jsonData);

			for (int i = 0; i<data.Count; i++)
				for (int j = 0; j<data.Count; j++)
					if (i!=j && data[i]["name"].Equals(data[j]["name"]))
						{
						Debug.LogWarning("Duplicate animation ("+data[i]["name"]+") on "+path);
						break;
						}

			return data;
			}


		public static JsonData getAnimationData(JsonData data, string animationName)
			{
			for (int i = 0; i<data.Count; i++)
				if (data[i]["name"].ToString()==animationName)
					return data[i];

			return null;
			}

		#endregion



		#region ANIMATORS AND SPRITE RENDERERS

		public void setSpriteRenderersSortingLayer(SortingLayer sortingLayer)
			{
			for (int i = 0; i<spriteRenderers.Length; i++)
				spriteRenderers[i].sortingLayerName = sortingLayer.name;

			currentSortingLayer = sortingLayer;
			}


		private void setAnimatorControllers()
			{
			JsonData animationsData = getJsonData(path+animationsObjectName+"/"+animationsObjectName);

			if (animationsData==null)
				return;

			AnimationClip[] clips;
			JsonData animationData;
			RuntimeAnimatorController animatorController;
			RuntimeAnimatorController lastAnimatorController = null;

			for (int i = 0; i<animators.Count; i++)
				{
				animatorController = Resources.Load<RuntimeAnimatorController>
                    (path+animationsObjectName+"/"+animators[i].transform.name+"/"+animators[i].transform.name);

				if (animatorController==null)//try to load single animator structure
                    animatorController = Resources.Load<RuntimeAnimatorController>
                        (path+animationsObjectName+"/"+animationsObjectName);

				if (animatorController==null)//if its still empty use the last one, this way we can have animators using the same animations cascading
                	animatorController = lastAnimatorController;

				if (animatorController==null)
					continue;

				lastAnimatorController = animatorController;

				animators[i].runtimeAnimatorController = animatorController;	

				//only attach events to the first animator
				if (i==0)
					{
					eventCatcher = animators[i].gameObject.AddComponent<AnimationEventCatcher>();
					clips = animators[i].runtimeAnimatorController.animationClips;


					for (int j = 0; j<clips.Length; j++)
						{
						animations.Add(clips[j].name);
						animationData = getAnimationData(animationsData, clips[j].name);
						if (animationData!=null)
							attachEventsToAnimation(clips[j], animationData, animationsObjectName);
						}	
					}
				}	

			
			}
					

		/// <summary>
		/// Make sure you holdPosition for the animator to actually change to the animation by using waitForAnimationUpdate
		/// </summary>
		public float getCurrentAnimationNormalizedTime()
			{
			if (animators.Count==0)
				return 0;

			return animators[0].GetCurrentAnimatorStateInfo(0).normalizedTime;
			}

		/// <summary>
		/// This is used to holdPosition for unity's animator to actually update to the changed animation
		/// </summary>
		public IEnumerator<float> waitForAnimationUpdate()
			{
			if (animators.Count==0)
				yield break;

			//unity doesnt update the animator state immediatly so we have to holdPosition for a change, and then
			//we can get the correct duration of the anim
			string clipName = animators[0].GetCurrentAnimatorClipInfo(0)[0].clip.name;		

			while (true)
				{
				yield return 0;

				if (animators[0].GetCurrentAnimatorClipInfo(0)[0].clip.name!=clipName)
					break;
				}
			}		


		public IEnumerator<float> waitForAnimationDuration()
			{
			if (animators.Count==0)
				yield break;

			//unity doesnt update the animator state immediatly so we have to holdPosition for a change, and then
			//we can get the correct duration of the anim
			string clipName = animators[0].GetCurrentAnimatorClipInfo(0)[0].clip.name;		

			while (true)
				{
				yield return 0;

				if (animators[0].GetCurrentAnimatorClipInfo(0)[0].clip.name!=clipName)
					break;
				}
							
			yield return Timing.waitForSeconds(animators[0].GetCurrentAnimatorStateInfo(0).length);
			}


		public float getAnimationDuration()
			{
			if (animators.Count==0)
				return 0;

			return animators[0].GetCurrentAnimatorStateInfo(0).length;
			}


        public virtual void pauseAnimators()
            {
			if (animatorsPaused)
				return;

			for (int i = 0; i<animators.Count; i++)
                animators[i].speed = 0;

			animatorsPaused = true;
            }


        public virtual void resumeAnimators()
            {
			if (!animatorsPaused)
				return;

            for (int i = 0; i<animators.Count; i++)
                animators[i].speed = 1;

			animatorsPaused = false;
            }
                      

		#endregion



		#region ANIMATIONS

		protected virtual float checkAnimationNormalizedTime()
			{
			if (Time.fixedTime-timeWeUpdatedState<0.01f)//allow some breath time for the animation to have changed/kicked in
                return 0;

			float elapsed = getCurrentAnimationNormalizedTime();

			return elapsed;
			}


        public void playAnimation(string animationName)
            {
            for (int i = 0; i<animators.Count; i++)
                animators[i].Play(animationName, 0, 0);

			timeWeUpdatedState = Time.fixedTime;
            }


        public void setInteger(string paramName, int value)
			{
			for (int i = 0; i<animators.Count; i++)
				animators[i].SetInteger(paramName, value);

			timeWeUpdatedState = Time.fixedTime;
            }
		
		#endregion
		}
	}
