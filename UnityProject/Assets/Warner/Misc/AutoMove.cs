using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Warner
	{
	public class AutoMove : MonoBehaviour
		{

		#region MEMBER FIELDS

		public float speed;
		public float movementOffset = 0.05f;
		public Side movingSide;
		public enum Side {Left,Right, Custom};

		[NonSerialized] public Vector2 movingSideDirection;



		#endregion




		#region UPDATE STUFF
		
		private void Update()
			{		
			if (speed==0)
				return;

			Vector3 targetPosition = transform.position;

			if (movingSideDirection==Vector2.zero)
				targetPosition.x = transform.position.x+(movingSide==Side.Left ? -movementOffset : movementOffset);
				else
				targetPosition = transform.position.to2()+(movingSideDirection*movementOffset);

			Vector3 dest = Vector3.Lerp(transform.position, targetPosition, speed*Time.deltaTime);
			transform.position = dest;
			}

		#endregion
		}
	}
