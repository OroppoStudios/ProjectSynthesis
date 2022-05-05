using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Warner;
using Warner.AnimationTool;

public class SphereEnemy : CharacterAI
{
	private IEnumerator<float> mainAttackRoutine;
	private CharacterPathFollower pathFollower;
	private float distance;

	protected override void Awake()
		{
		base.Awake();
		pathFollower = GetComponent <CharacterPathFollower>();
		}


	protected override void OnEnable()
		{
		mainAttackRoutine = mainAttackCoRoutine();
		Timing.run(mainAttackRoutine);
		}


	private IEnumerator<float> mainAttackCoRoutine()
		{
		//first aquire target
		while (target == null)
			{
			yield return Timing.waitForSeconds(0.1f);
			target = acquireTarget();
			}
		

		while (true)
			{
			yield return Timing.waitForSeconds(0.25f);

			if (Vector2.Distance(target.transform.position, transform.position) < 4f)
				{
				if (!character.isBehindUs(target.transform.position))
					{
					pathFollower.freeze = true;
					ComboManager.Attack theAttack = new ComboManager.Attack();
					theAttack.type = ComboManager.AttackType.LightPunchNormal;
					character.attacks.execute(theAttack);

					yield return Timing.waitForSeconds(1.5f);
					pathFollower.freeze = false;
					}
				}								
			}
		}


	protected override void OnDisable()
		{
		Timing.kill(mainAttackRoutine);
		}
	}
