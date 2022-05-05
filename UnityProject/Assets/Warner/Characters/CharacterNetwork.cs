using System.Collections;
using UnityEngine;

namespace Warner
	{
	public class CharacterNetwork: MonoBehaviour
		{
		#region MEMBER FIELDS

		public Character character;

		private Queue networkPackets = new Queue();
        private NetworkPosition lastPacket;
        private NetworkPosition currentPacket;
        private Vector2 networkMovementVelocity;

        private class NetworkPosition
            {
            public int side;
            public Vector2 position;
            public float serverTime;
            }

        private const int networkDataPacketBufferSize = 3;

		#endregion



		#region INIT

		private void Awake()
			{
			character = GetComponent<Character>();
			}

		#endregion



		#region FRAME UPDATES

        private void Update()
            {
            networkUpdate();    
            }

        #endregion



		#region NETWORK MOVEMENT

        public void networkMoveData(int side, Vector2 targetPosition, float serverPacketTime)
            {
            if (NetworkManager.instance.isServer)
                {
                character.movements.movingSideX = side;
                return;
                }

            if (lastPacket!=null && serverPacketTime < lastPacket.serverTime)
                {
                Debug.Log("Network: Dropped an old packet");
                return;
                }

            NetworkPosition packet = new NetworkPosition();
            packet.side = side;
            packet.position = targetPosition;
            packet.serverTime = serverPacketTime;

            networkPackets.Enqueue(packet);
            lastPacket = packet;
            }


        private void networkUpdate()
            {
            pickNextNetworkPosition();
            moveToNextNetworkPosition();    
            }


        private void pickNextNetworkPosition()
            {
            if (currentPacket!=null || networkPackets.Count < networkDataPacketBufferSize)
                return;                             

            currentPacket = (NetworkPosition)networkPackets.Dequeue();

            if (currentPacket.position==transform.position.to2())
                {
                currentPacket = null;
                pickNextNetworkPosition();
                return;
                }
            }


        private void moveToNextNetworkPosition()
            {
            if (currentPacket!=null)
                {
                transform.position = currentPacket.position;
                currentPacket = null;
                }
            }


        #endregion
		}
	}
