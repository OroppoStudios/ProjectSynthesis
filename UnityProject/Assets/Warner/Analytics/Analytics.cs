using System;
using UnityEngine;
using System.Collections.Generic;

namespace Warner {

public class Analytics: MonoBehaviour
	{
	#region MEMBER FIELDS

	public string[] ignoredLocations;

	public static Analytics instance;

	[NonSerialized] public Data data;
	[NonSerialized] public int fps;
	[NonSerialized] List<FpsData> fpsDataList = new List<FpsData>();

	[Serializable]
	public partial struct Data
		{
		public string resolution;
		public string platform;
		public string gameName;
		public FpsData[] fps;
		}


	[Serializable]
	public struct FpsData
		{
		public string location;
		public int avgFps;
		public int minFps;
		public int maxFps;
		public int playTime;
		}

	private string _location;
	private FpsData currentFpsData;
	private int totalFrameCount;
	private int sumOfFps;
	private int frameCount;
	private float deltaTime;
	private float startedCapturingTime;

	private const float updateRate = 4f;
	private const float breatheTimeToStartCapturing = 1;
	private const string apiUrl = "http://narcosvszombies.com/analytics/main.php";

	#endregion



	#region INIT STUFF

	private void Awake()
		{
		instance = this;
		enabled = false;
		}

	#endregion



	#region LOCATION STUFF

	public string location
		{
		get
			{
			return _location;
			}
		set
			{
			if (_location!=null)//store latest data playtime
				currentFpsData.playTime = (int) Mathf.Round(Time.time-startedCapturingTime);


			fpsDataList.Add(currentFpsData);
			currentFpsData = new FpsData();			
			currentFpsData.location = value;
			currentFpsData.minFps = 1000;

			totalFrameCount = 0;
			sumOfFps = 0;
			frameCount = 0;
			fps = 0;

			startedCapturingTime = Time.time;

			_location = value;		
			}
		}

	#endregion



	#region FRAME STUFF

	private void Update()
		{
		if (Time.time-startedCapturingTime<breatheTimeToStartCapturing)
			return;

		checkFps();
		}

	#endregion



	#region FPS STUFF

	private void checkFps()
		{
		totalFrameCount++;
		frameCount++;
		deltaTime += Time.deltaTime;
		sumOfFps += fps;

		if (deltaTime>(1/updateRate))
			{
			fps = (int) Math.Floor(frameCount/deltaTime);
			frameCount = 0;
			deltaTime -= 1/updateRate;

			if (fps>currentFpsData.maxFps)
				currentFpsData.maxFps = fps;

			if (fps<currentFpsData.minFps)
				currentFpsData.minFps = fps;

			currentFpsData.avgFps = (int) Mathf.Floor(sumOfFps/totalFrameCount);
			}
		}

	#endregion



	#region DATA SUBMIT STUFF

	public void submitData()
		{
		for (int i = fpsDataList.Count-1;i>=0;i--)
			if (fpsDataList[i].location=="" || fpsDataList[i].minFps==1000 || fpsDataList[i].avgFps==0)
				fpsDataList.RemoveAt(i);

		data.fps = fpsDataList.ToArray();
		data.platform = Application.platform.ToString();
		data.resolution = Screen.width+"x"+Screen.height;

		Timing.run(sendToServer());
		}


	private IEnumerator <float> sendToServer()
		{
		WWWForm form = new WWWForm();
		form.AddField("data", "{\"type\":  \"submitData\",\"data\":  "+JsonUtility.ToJson(data)+"}");
		WWW www = new WWW(apiUrl, form);

		while (!www.isDone)
			yield return 0;

		if (www.error!=null || www.text=="ERROR" || www.text=="")
			Debug.Log("Analytics: Error saving data");
		else
			Debug.Log("Analytics: Data saved");
		}

	#endregion
	}

}