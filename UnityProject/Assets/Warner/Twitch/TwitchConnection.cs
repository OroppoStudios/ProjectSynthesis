using System;
using LitJson;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace Warner {	

public class TwitchConnection: MonoBehaviour
	{		
	#region MEMBER FIELDS

	public string nickName = "nvzgamebot";		
	public string channelName = "narcosvszombies";
	public string oAuthPath = "C:/nvztwitchoauth.txt";
	public bool connectToGroup;
	
	[NonSerialized] public bool connected;
	
	public delegate void EventsHandler (string user, string msg);
	public event EventsHandler onMessageReceived;		
	
	private string serversUrl;		
	private string server;
	private int port;
	private string oauth;						
	private TcpClient tcpClient;
	private NetworkStream ircStream;
	private StreamReader streamReader;
	private StreamWriter streamWriter;
	private Queue<string> messagesToSend = new Queue<string>();			
	private string messagesReceived = "";		
	private float messageSentLastTime;		
	
	#endregion	
					
				
						
	#region INIT STUFF
	
	public void connect()
		{			
		if (File.Exists(oAuthPath))
				oauth = File.ReadAllText(oAuthPath);

		if (oauth==null)
			{
			Debug.Log ("No twitch oauth");
			return;
			}
		
		if (connectToGroup)
			serversUrl = "http://tmi.twitch.tv/servers?cluster=group";
			else
			serversUrl = "http://tmi.twitch.tv/servers?channel="+channelName;

		Timing.run(getTwitchServers(), Timing.Segment.SlowUpdate);
		}													


	private IEnumerator <float> getTwitchServers() 
		{
		WWW www = new WWW(serversUrl);

		while(!www.isDone)
			yield return 0;		
		
		if (www.error==null)
			{
			JsonData data = JsonMapper.ToObject(www.text);
			
			if (data["servers"].Count>0)
				{
				string[] serverData = data["servers"][0].ToString().Split(':');
				server = serverData[0];
				port = int.Parse(serverData[1]);
				connectToIRC();
				}
				else
				print ("No twitch servers received");
			}
			else
			print ("Could not get twitch servers");
		}
		
	
	private void connectToIRC()
		{
		messageSentLastTime = Time.time - 1;			
		tcpClient = new TcpClient(server, port);
					
		ircStream = tcpClient.GetStream();
		streamReader = new StreamReader(ircStream);
		streamWriter = new StreamWriter(ircStream);
		
		streamWriter.WriteLine("USER " + nickName.ToLower() + "tmi twitch :" + nickName.ToLower());
		streamWriter.Flush();
		streamWriter.WriteLine("PASS " + oauth);
		streamWriter.Flush();
		streamWriter.WriteLine("NICK " + nickName.ToLower());	
		streamWriter.Flush();
		
		if (connectToGroup)
			{
			streamWriter.WriteLine("CAP REQ :twitch.tv/commands");
			streamWriter.Flush();
			}					
		}
		
	#endregion
	
	
	
	#region RECEIVING AND SENDING MESSAGES STUFF
				
								
	public void sendMessage(string msg, string user = "")
		{
		if (!connected)
			{
			print("IRC client not connected ("+server+")");
			return;
			}
			
		if (connectToGroup)
			messagesToSend.Enqueue("PRIVMSG #jtv :.w " + user + " " + msg);//whisper
			else
			messagesToSend.Enqueue("PRIVMSG #" + channelName + " :" + msg);//regular message			
		}

		
	void LateUpdate()
		{
		if (ircStream!=null && ircStream.DataAvailable)
			{
			messagesReceived = streamReader.ReadLine();

			if ((connectToGroup && messagesReceived.Contains("WHISPER")) || (!connectToGroup && messagesReceived.Contains("PRIVMSG #")))
				parseMessage(messagesReceived);
					
			if (messagesReceived.StartsWith("PING "))
				messagesToSend.Enqueue(messagesReceived.Replace("PING", "PONG"));
					
			if (messagesReceived.Split(' ')[1] == "001")
				{						
				if (connectToGroup)
					{
					messagesToSend.Enqueue("JOIN #" + channelName);
					}
					else
					{
					messagesToSend.Enqueue("MODE " + nickName + " +B");
					messagesToSend.Enqueue("JOIN #" + channelName);
					}
						
				connected = true;					
				}	
			}

		if (messagesToSend.Count>0)
			{
			if (messageSentLastTime + 0.2f < Time.time)
				{
				streamWriter.WriteLine(messagesToSend.Peek());
				streamWriter.Flush();
				messagesToSend.Dequeue();
				messageSentLastTime = Time.time;
				}
			}
		}
	
	#endregion
		
		

	#region PARSING MESSAGES STUFF

	private void parseMessage(string rawMsg)
		{
		string msgDivider;
		string msg;
		string user;

		if (connectToGroup)
			{
			user = rawMsg.Split('!')[0].Replace(':',' ').Trim();
			msg = rawMsg.Split(':')[2];
			}
			else
			{
			msgDivider = "PRIVMSG #"+channelName+" :";
			msg = rawMsg.Substring(rawMsg.IndexOf(msgDivider)+msgDivider.Length).Trim().ToLower();
			user = rawMsg.Substring(1,rawMsg.IndexOf("!")-1).Trim().ToLower();
			}

		if (onMessageReceived!=null)
			onMessageReceived(user,msg);
		}

	#endregion
	}

}