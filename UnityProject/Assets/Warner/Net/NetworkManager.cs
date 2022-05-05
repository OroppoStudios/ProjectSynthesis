using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Collections;

namespace Warner
	{
	public class NetworkManager: MonoBehaviour
		{
		#region MEMBER FIELDS

		public bool testing;
		public bool isServer;
		public int port;
		public string serverIp = "192.168.0.10";
		public float snapshotsPerSecond = 15f;
		public float serverActionsLag = 0.1f;

		[NonSerialized] public bool initialized;

		public delegate void EventsHandler();
		public delegate void MessageHandler(Packet[] snapshot);
		public event EventsHandler onConnected;
		public event EventsHandler onDisconnected;
		public event EventsHandler onBeforeSendSnapshot;
		public event MessageHandler onDataReceived;

		[Serializable]
		public struct Packet
			{
			public float time;
			public int type;
			public object data;
			}

		public static NetworkManager instance;

		private int serverHostId = -1;
		private int serverClientConnectionId = -1;
		private int clientHostId = -1;
		private IEnumerator <float> serverSnapshotsRoutine;
		private Queue simulatedSnapshotsForServer;
		private List<Packet> snapshot;
		private float lastTimeWeSentSnapshot;
		private byte[] emptyBuffer = new byte[1024];

		private struct ServerSnapshot
			{
			public float time;
			public Packet[] packets;
			}

		#endregion



		#region INIT STUFF

		private void Awake()
			{
			instance = this;

			snapshot = new List<Packet>();

			if (testing)
				{
				if (isServer)
					host(port);
					else
					connectToHost(serverIp, port);
				}
			}


		#endregion



		#region CONNECT STUFF

		private HostTopology getTopology()
			{
			ConnectionConfig cc = new ConnectionConfig();
			cc.AddChannel(QosType.Reliable);
			return new HostTopology(cc, 2);
			}


		public void host(int port)
			{
//			NetworkTransport.Init();
//			HostTopology topology = getTopology();

//			serverHostId = NetworkTransport.AddHost(topology, port);
//
//			if(serverHostId<0)
//				{
//				Debug.Log("Could not start server");
//				return;
//				}
//				else
//				Debug.Log("Server started");
//							
//			simulatedSnapshotsForServer = new Queue();
//			serverSnapshotsRoutine = serverSnapshotsCoRoutine();
//			Timing.run(serverSnapshotsRoutine);
//
//			initialized = true;
//			connected = true;
			}



		public void connectToHost(string ip, int port)
			{
//			NetworkTransport.Init();
//
//			byte error;
//			HostTopology topology = getTopology();
//			clientHostId = NetworkTransport.AddHost(topology);
//			NetworkTransport.Connect(0, ip, port, 0, out error);
//
//			if (error!=(byte) NetworkError.Ok)
//				{
//				Debug.Log("Could not connect to server.");
//				return;
//				}
//
//			initialized = true;
			}


		private bool connected
			{
			set
				{
				if (value && onConnected!=null)
					onConnected();
					else
					if (!value && onDisconnected!=null)
						onDisconnected();
				}
			} 
		

		#endregion



		#region FRAME UPDATE

		private void Update()
			{
			checkForMessages();
			tryToSendServerSnapshot();
			}

		

		private void checkForMessages()
			{
			if(!initialized)
				return;
		
			int receivedHostId; 
			int receivedConnectionId;
			int channelId;
			int receivedSize;
			byte[] buffer = emptyBuffer;
			byte error;
			
			NetworkEventType networkEvent;
			do
				{
				networkEvent = NetworkTransport.Receive(out receivedHostId, out receivedConnectionId, 
					out channelId, buffer, buffer.Length, out receivedSize, out error);

				switch(networkEvent)
					{
					case NetworkEventType.Nothing:
					break;
					case NetworkEventType.ConnectEvent:
						Debug.Log("Client connected");
						if (receivedHostId==serverHostId)	
							{
							serverClientConnectionId = receivedConnectionId;				
							Debug.Log("New client connected with connectionId: "+receivedConnectionId);
							}

						if (receivedHostId==clientHostId)
							{
							connected = true;
							Debug.Log("Connected to server");
							}
					break;				
					case NetworkEventType.DataEvent:
						dataReceived(buffer);
					break;
					case NetworkEventType.DisconnectEvent:
						if(receivedHostId==serverHostId)
							Debug.Log ("Server: Received disconnect from " + receivedConnectionId.ToString());

						if(receivedHostId==clientHostId)
							Debug.Log ("Client: Disconnected from server!");
					break;
					}
				
				}
				while (networkEvent!=NetworkEventType.Nothing);
			}

		#endregion



		#region MESSAGES STUFF

		private void tryToSendServerSnapshot()
			{
			if (!isServer || Time.unscaledTime-lastTimeWeSentSnapshot<(1/snapshotsPerSecond))
				return;

			snapshot.Clear();

			if (onBeforeSendSnapshot!=null)
				onBeforeSendSnapshot();

			if (snapshot.Count==0)
				return;

			Packet[] packets = snapshot.ToArray();
			byte error;
			byte[] buffer = emptyBuffer;
			Stream stream = new MemoryStream(buffer);

			BinaryFormatter binaryFormatter = new BinaryFormatter();
			binaryFormatter.Serialize(stream, packets);	
			NetworkTransport.Send(serverHostId, serverClientConnectionId, 0, buffer, (int) stream.Position, out error);							
			lastTimeWeSentSnapshot = Time.unscaledTime;
			}


		public void addDataToSnapshot(int type, object data)
			{
			Packet msg = new Packet();
			msg.type = type;
			msg.data = data;
			snapshot.Add(msg);
			}


		private void dataReceived(byte[] buffer)
			{
			Stream stream = new MemoryStream(buffer);
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			dataReceived((Packet[]) binaryFormatter.Deserialize(stream));
			}


		private void dataReceived(Packet[] packet)
			{
			if (onDataReceived!=null)
				onDataReceived(packet);
			}


		#endregion



		#region SERVER ACTIONS

		private IEnumerator <float> serverSnapshotsCoRoutine()
			{
			ServerSnapshot serverSnapshot;

			while (true)
				{
				if (simulatedSnapshotsForServer.Count==0)
					{
					yield return 0;
					continue;
					}

				serverSnapshot = (ServerSnapshot) simulatedSnapshotsForServer.Peek();

				if (Time.unscaledTime-serverSnapshot.time<serverActionsLag)
					{
					yield return 0;
					continue;
					}

				serverSnapshot = (ServerSnapshot) simulatedSnapshotsForServer.Dequeue();

				dataReceived(serverSnapshot.packets);
				}
			}

		#endregion
		}	
	}