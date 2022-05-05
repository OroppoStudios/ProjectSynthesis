using System;
using UnityEngine;
using Warner;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Audio;

namespace Warner
	{
	public class AudioManager: MonoBehaviour
		{
		#region MEMBER FIELDS

		public enum AudioType {music, sfx, ambience}

		public AudioSources audioSources;

		public static AudioManager instance;

		public struct AudioSources
			{
			public AudioSource music;
			public AudioSource sfx;
			public AudioSource ambience;
			}


		[Serializable]
		private struct ClipsData
			{
			public List<AudioClip> audioClips;
			public int lastPlayingIndex;
			}

		private List<int> playingSounds = new List<int>();
		private Dictionary<string, ClipsData> sfxClips = new Dictionary<string, ClipsData>();
		private Dictionary<string, AudioMixerGroup> mixerGroups = new Dictionary<string, AudioMixerGroup>();

		private const string audioMixerName = "Main";

		#endregion


		
		#region INIT STUFF

		protected virtual void Awake()
			{
			instance = this;
			getAudioMixerGroups();
			createAudioSources();
			loadSfxClips();
			}


		protected virtual void Start()
			{
			updateAudioSourcesVolumes();
			}



		private void getAudioMixerGroups()
			{
			AudioMixer audioMixer = Resources.Load<AudioMixer>("AudioManager/"+audioMixerName);

			if (audioMixer==null)
				{
				Debug.LogWarning("AudioManager: Mixer file not found under Resources/AudioManager");
				return;
				}

			AudioMixerGroup[] mixerGroupsArray = audioMixer.FindMatchingGroups("/Master");

			for (int i = 0; i<mixerGroupsArray.Length; i++)
				mixerGroups.Add(mixerGroupsArray[i].name, mixerGroupsArray[i]);
			}


		private void createAudioSources()
			{
			GameObject audioSourcesGO = new GameObject("AudioSources");
			audioSourcesGO.transform.SetParent(transform, false);
			audioSourcesGO.AddComponent<AudioListener>();

			audioSources.music = createAudioSource(audioSourcesGO.transform, "Music");
			audioSources.sfx = createAudioSource(audioSourcesGO.transform, "Sfx");
			audioSources.ambience = createAudioSource(audioSourcesGO.transform, "Ambience");
			}


		private AudioSource createAudioSource(Transform parent, string theName)
			{
			GameObject audioSourceGO = new GameObject(theName);
			audioSourceGO.transform.SetParent(parent, false);
			AudioSource audioSource = audioSourceGO.AddComponent<AudioSource>();
			audioSource.outputAudioMixerGroup = getMixerGroup(theName);
			return audioSource;
			}


		private void loadSfxClips()
			{
			AudioClip[] audioClips = Resources.LoadAll<AudioClip>("AudioManager");
			string[] separatedName;
			string clipName;
			int number;

			//go throu all of the files and save them by name in our dictionary
			//each dictionary/sfx key contains a list of clips, if we found same sfx name with numeric
			//values, meaning it was the same sound but different variant

			for (int i = 0; i<audioClips.Length; i++)
				{
				separatedName = audioClips[i].name.Split('_');
				clipName = string.Empty;

				for (int j = 0; j<separatedName.Length; j++)
					{
					try
						{
						number = int.Parse(separatedName[j]);
						}
					catch
						{
						number = -1;
						}

					if (number==-1)
						clipName += separatedName[j]+"_";
					}

				if (clipName[clipName.Length-1]=='_')
					clipName = clipName.TrimEnd('_');

				if (!sfxClips.ContainsKey(clipName))
					{
					ClipsData clipsData = new ClipsData();
					clipsData.audioClips = new List<AudioClip>();
					sfxClips.Add(clipName, clipsData);	
					}

				sfxClips[clipName].audioClips.Add(audioClips[i]);
				}
			}

		#endregion



		#region PLAY/STOP


		public void playSfx(string sfxName, float volume, float pitchMod = 0f, AudioSource audioSource = null)
			{
			if (!sfxClips.ContainsKey(sfxName))
				{
				Debug.LogWarning("AudioManager: "+sfxName+" sfx was not found.");
				return;
				}

			int index = sfxClips[sfxName].lastPlayingIndex;
			while (index==sfxClips[sfxName].lastPlayingIndex && sfxClips[sfxName].audioClips.Count>1)
				index = sfxClips[sfxName].audioClips.getRandomIndex();

			ClipsData data = sfxClips[sfxName];
			data.lastPlayingIndex = index;
			sfxClips[sfxName] = data;

			playSfx(sfxClips[sfxName].audioClips[index], volume, pitchMod, audioSource);
			}


		public void playSfx(AudioClip audioClip, float volume, float pitchMod = 0f, AudioSource audioSource = null)
			{
			int clipId = audioClip.GetInstanceID();

			if (playingSounds.Contains(clipId))
				return;		

			playingSounds.Add(clipId);

			if (BuildManagerFlags.getFlag("debugAudio"))
				Debug.Log("AudioManager: Playing "+audioClip.name);

			Timing.run(playSfxCoRoutine(audioClip, volume, clipId, pitchMod, audioSource));
			}


		private IEnumerator<float> playSfxCoRoutine(AudioClip audioClip,
			float volume, int clipId, float pitchMod = 0f, AudioSource audioSource = null,
			AudioMixerGroup mixerGroup = null)
			{
			if (audioSource==null)
				audioSource = audioSources.sfx;

			if (mixerGroup!=null)
				audioSource.outputAudioMixerGroup = mixerGroup;

			//audioSource.panStereo = calculatePan(audioSource);
			audioSource.pitch = 1f+UnityEngine.Random.Range(-pitchMod, pitchMod);
			audioSource.PlayOneShot(audioClip, volume);

			yield return Timing.waitForSeconds(0.05f);

			playingSounds.Remove(clipId);
			}


		private float calculatePan(AudioSource audioSource)
			{
			float pan = 0;

			//calculate pan according to the player
			if (CameraController.instance.targetFollower.target!=null)
				{
				const float centerNoPanOffset = 2f;
				const float panMaxSpread = 0.35f;

				//calculate the distance to the player
				float diff = audioSource.transform.position.x-CameraController.instance.targetFollower.target.transform.position.x;

				if (Mathf.Abs(diff)>centerNoPanOffset)//if distance greater than the min close offset, then pan the sound
					{	
					if (diff>0)
						{
						diff -= centerNoPanOffset;//remove the close offset
						//calculate the percent from 0 to 1 taking as full percent the distance the player has to the border of the screen
						float playerDistanceToRightBorder = (CameraController.instance.worldBoundaries.xMax+centerNoPanOffset)-CameraController.instance.targetFollower.target.transform.position.x;
						pan = Mathf.Min((diff*panMaxSpread)/playerDistanceToRightBorder, 1);
						} else
						{
						diff += centerNoPanOffset;//remove the close offset
						//calculate the percent from 0 to 1 taking as full percent the distance the player has to the border of the screen
						float playerDistanceToLeftBorder = CameraController.instance.targetFollower.target.transform.position.x-(CameraController.instance.worldBoundaries.xMin-centerNoPanOffset);
						pan = Mathf.Min((diff*panMaxSpread)/playerDistanceToLeftBorder, 1);
						}
					}
				}

			return pan;
			}


		public void playMusic(AudioClip clip, float volume, float fadeInDuration = 1f)
			{
			//first calculate whats the percentage we are playing this
			float volumePercentage = (volume*100/1);
			//then calculate the target volume we will assign
			float targetVolume = (volumePercentage*GlobalSettings.settings.musicVolume)/100;

			playLoopAudio(audioSources.ambience, clip, targetVolume, fadeInDuration);
			}


		public void playAmbience(AudioClip clip, float volume, float fadeInDuration = 1f)
			{
			//first calculate whats the percentage we are playing this
			float volumePercentage = (volume*100/1);
			//then calculate the target volume we will assign
			float targetVolume = (volumePercentage*GlobalSettings.settings.masterVolume)/100;

			playLoopAudio(audioSources.ambience, clip, targetVolume, fadeInDuration);
			}


		private void playLoopAudio(AudioSource audioSource, AudioClip clip, float targetVolume, float fadeInDuration = 1f)
			{
			DOTween.Kill(audioSource.name);
			audioSource.volume = 0;
			audioSource.clip = clip;
			audioSource.Play();	
			audioSource.loop = true;
			audioSource.DOFade(targetVolume, fadeInDuration).SetId(audioSource.name);
			}

		public void stopLoopedAudio(AudioSource audioSource, float fadeOutDuration = 1f)
			{
			DOTween.Kill(audioSource.name);
			audioSource.DOFade(0, fadeOutDuration).SetId(audioSource.name).OnComplete(() => loopedAudioFadedOut(audioSource));
			}

		private void loopedAudioFadedOut(AudioSource audioSource)
			{
			audioSource.Stop();
			}


		#endregion



		#region MISC CALLS


		public AudioMixerGroup getMixerGroup(string theName)
			{
			return mixerGroups.ContainsKey(theName) ? mixerGroups[theName] : null;
			}


		public void clear()
			{
			playingSounds.Clear();
			}


		public float getVolumeSourceByType(AudioType audioType)
			{
			switch (audioType)
				{
				case AudioType.ambience:
				case AudioType.music:
					return GlobalSettings.settings.musicVolume;
				case AudioType.sfx:
					return GlobalSettings.settings.soundEffectsVolume;
				default:
				return 0;
				}
			}

		public void updateAudioSourcesVolumes()
			{
			audioSources.music.volume = GlobalSettings.settings.musicVolume;
			audioSources.sfx.volume = GlobalSettings.settings.soundEffectsVolume;
			audioSources.ambience.volume = GlobalSettings.settings.soundEffectsVolume;
			}

		#endregion
		}

	}