using UnityEngine;
using System.Collections.Generic;

namespace Warner
	{
	public static class PhysicsExtensions
		{
		public static Vector2 calculateVelocityFromDirectionToCreateArc(Vector2 direction, Rigidbody2D rigidBody, float angleVariation = 10f)
			{
			float originalY = direction.y;
			direction.y = 0;//only work with the x axis
			float horizontalDistance = direction.magnitude;
			direction.y = angleVariation;
			horizontalDistance += originalY;//add the original Y

			if (horizontalDistance<0)
				horizontalDistance = 0;

			float vel = Mathf.Sqrt(horizontalDistance * (Physics2D.gravity.magnitude*rigidBody.gravityScale));

			return vel * direction.normalized;//apply the original direction
			}


		public static Vector3 calculatePositionInTime(Vector2 start, Vector2 startVelocity, float time)
			{
			return start + startVelocity*time + Physics2D.gravity*time*time*0.5f;
			}


		public static Vector3[] calculateArcPathFromVelocity(Vector2 startPosition,Vector2 velocity,float simulationDuration = 0.5f,float stepTime = 0.01f)
			{
			Vector2 previousPosition = startPosition;
			Vector2 currentPos;

			List<Vector3> pointsList = new List<Vector3>();
			pointsList.Add(startPosition);

			for (int i=1;;i++)
				{
				float t = stepTime*i;
				if (t>simulationDuration) 
					break;			
				
				currentPos = calculatePositionInTime(startPosition,velocity,t);								
				
				if (currentPos==previousPosition)		
					break;
				
				previousPosition = currentPos;
				pointsList.Add(currentPos);
				}

			return pointsList.ToArray();
			}


		public static Vector2 calculateVelocityFromDirection(Vector2 direction)
			{
			return (Mathf.Sqrt(direction.magnitude * Physics2D.gravity.magnitude)* direction) * 0.1f;
			}
		}
	}