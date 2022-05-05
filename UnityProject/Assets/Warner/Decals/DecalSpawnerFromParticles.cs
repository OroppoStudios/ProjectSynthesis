using UnityEngine;
using System.Collections.Generic;
using Warner;

namespace Warner {

public class DecalSpawnerFromParticles: MonoBehaviour
	{
	#region MEMBER FIELDS

	public CollisionType collisionType;
	public int[] atlasItemIndexes;
	public bool randomRotation;
	public bool randomFlip;
	[Range(1,10)]public int chance;
	public Vector2 scaleRandomness;
	public Vector2 xPositionRandomness;
	public Vector2 yPositionRandomness;
	public Vector2 fakeCollisionRadius;

	public enum CollisionType{Real,Fake};
		

	private ParticleSystem particleS;
	private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

	#endregion



	#region INIT STUFF

	private void Awake()
		{
		particleS = GetComponent<ParticleSystem>();
		}

	#endregion



	#region PARTICLE COLLISION STUFF

	private void OnParticleCollision(GameObject collidedObject)
		{
		if (UnityEngine.Random.value>(chance*0.1f)*0.5f)
			return;

		if (collisionType==CollisionType.Real)
			{
			particleS.GetCollisionEvents(collidedObject,collisionEvents);
			if (collisionEvents.Count==0)
				return;
			}

//		float scale = UnityEngine.Random.Range(scaleRandomness.x,scaleRandomness.y);
//		Vector3 position = collisionEvents[0].intersection;

//		if (collisionType==CollisionType.Fake)
//			position = new Vector2(transform.position.x+UnityEngine.Random.Range(fakeCollisionRadius.x,fakeCollisionRadius.y), LevelMaster.instance.groundMainHorizontalTilingScript.transform.position.y+0.25f);		

//		HorizontalWorldTiling.Buddy buddy = LevelMaster.instance.groundMainHorizontalTilingScript.getBuddyByName(collidedObject.name);
//		position.x -= buddy.transform.position.x - UnityEngine.Random.Range(xPositionRandomness.x,xPositionRandomness.y);
//		position.y -= buddy.transform.position.y - UnityEngine.Random.Range(yPositionRandomness.x,yPositionRandomness.y);
//
//		buddy.decalsManager.DrawDecal(position,scale,atlasItemIndexes[UnityEngine.Random.Range(0,atlasItemIndexes.Length)],
//			Quaternion.Euler(0,(UnityEngine.Random.value>0.5f && randomFlip) ? 180 : 0,(randomRotation) ? UnityEngine.Random.Range(0,360) : 0));
		}

	#endregion
	}

}