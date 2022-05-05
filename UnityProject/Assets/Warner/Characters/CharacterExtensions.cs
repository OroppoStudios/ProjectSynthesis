using UnityEngine;
using Warner.AnimationTool;

namespace Warner
	{
	public static class CharacterExtensions
        {
        #region STATES

		public static CharacterState[] groundHits = new CharacterState[]{CharacterState.Hit, 
			CharacterState.HitBack, CharacterState.HitUp, CharacterState.HitUpBack};


		public static bool hasAirAttack(this ComboManager.Attack[] progression)
            {
			bool hasAir = false;

			for (int i = 0; i < progression.Length; i++)
				if (progression[i].isAirAttack())
					{
					hasAir = true;
					break;
					}

            return hasAir;
            }


        public static bool isValidAttack(this ComboManager.Attack attack)
            {
            return attack.type!=ComboManager.AttackType.None;
            }


        public static bool isAirAttack(this ComboManager.Attack attack)
            {
            switch (attack.type)
                {
				case ComboManager.AttackType.LightAirPunch:
				case ComboManager.AttackType.LightAirPunchDown:
				case ComboManager.AttackType.StrongAirPunch:
				case ComboManager.AttackType.StrongAirPunchDown:
				case ComboManager.AttackType.AirSpecial1Normal:
				case ComboManager.AttackType.AirSpecial1Up:
				case ComboManager.AttackType.AirSpecial1Down:
				case ComboManager.AttackType.AirSpecial2Normal:
				case ComboManager.AttackType.AirSpecial2Up:
				case ComboManager.AttackType.AirSpecial2Down:
				case ComboManager.AttackType.AirSpecial3Normal:
				case ComboManager.AttackType.AirSpecial3Up:
				case ComboManager.AttackType.AirSpecial3Down:
                return true;
                }

            return false;
            }


		public static bool isAnySpecial(this ComboManager.Attack attack)
            {
            switch (attack.type)
                {
				case ComboManager.AttackType.SpecialAnyNormal:
				case ComboManager.AttackType.SpecialAnyDown:
				case ComboManager.AttackType.SpecialAnyUp:
				case ComboManager.AttackType.AirSpecialAnyNormal:
				case ComboManager.AttackType.AirSpecialAnyUp:
				case ComboManager.AttackType.AirSpecialAnyDown:
                return true;
                }

            return false;
            }


		public static bool isSpecial(this ComboManager.Attack attack)
            {
            switch (attack.type)
                {
				case ComboManager.AttackType.Special1Normal:
				case ComboManager.AttackType.Special1Down:
				case ComboManager.AttackType.Special1Up:
				case ComboManager.AttackType.Special2Normal:
				case ComboManager.AttackType.Special2Down:
				case ComboManager.AttackType.Special2Up:
				case ComboManager.AttackType.AirSpecial1Normal:
				case ComboManager.AttackType.AirSpecial1Up:
				case ComboManager.AttackType.AirSpecial1Down:
				case ComboManager.AttackType.AirSpecial2Normal:
				case ComboManager.AttackType.AirSpecial2Up:
				case ComboManager.AttackType.AirSpecial2Down:
				case ComboManager.AttackType.AirSpecial3Normal:
				case ComboManager.AttackType.AirSpecial3Up:
				case ComboManager.AttackType.AirSpecial3Down:
                return true;
                }

            return false;
            }


		public static bool isSpecial1(this ComboManager.Attack attack)
            {
            switch (attack.type)
                {
				case ComboManager.AttackType.Special1Normal:
				case ComboManager.AttackType.Special1Down:
				case ComboManager.AttackType.Special1Up:
				case ComboManager.AttackType.AirSpecial1Normal:
				case ComboManager.AttackType.AirSpecial1Up:
				case ComboManager.AttackType.AirSpecial1Down:
                return true;
                }

            return false;
            }


		public static bool isSpecial2(this ComboManager.Attack attack)
            {
            switch (attack.type)
                {
				case ComboManager.AttackType.Special2Normal:
				case ComboManager.AttackType.Special2Down:
				case ComboManager.AttackType.Special2Up:
				case ComboManager.AttackType.AirSpecial2Normal:
				case ComboManager.AttackType.AirSpecial2Up:
				case ComboManager.AttackType.AirSpecial2Down:
                return true;
                }

            return false;
            }


		public static bool isSpecial3(this ComboManager.Attack attack)
            {
            switch (attack.type)
                {
				case ComboManager.AttackType.Special3Normal:
				case ComboManager.AttackType.Special3Down:
				case ComboManager.AttackType.Special3Up:
				case ComboManager.AttackType.AirSpecial3Normal:
				case ComboManager.AttackType.AirSpecial3Up:
				case ComboManager.AttackType.AirSpecial3Down:
                return true;
                }

            return false;
            }


        public static bool isStrongAttack(this ComboManager.Attack attack)
            {
            switch (attack.type)
                {
				case ComboManager.AttackType.StrongPunchNormal:
				case ComboManager.AttackType.StrongPunchDown:
				case ComboManager.AttackType.StrongPunchUp:
				case ComboManager.AttackType.StrongAirPunch:
				case ComboManager.AttackType.StrongAirPunchDown:
                return true;
                }

            return false;
            }


        public static bool isLightAttack(this ComboManager.Attack attack)
            {
            switch (attack.type)
                {
				case ComboManager.AttackType.LightPunchNormal:
				case ComboManager.AttackType.LightPunchDown:
				case ComboManager.AttackType.LightPunchUp:
				case ComboManager.AttackType.LightAirPunch:
				case ComboManager.AttackType.LightAirPunchDown:
                return true;
                }

            return false;
            }


		public static CharacterState attackToHit(this ComboManager.Attack attack, bool backStatus, Character target)
            {
            if (attack.airTrigger)
            	return CharacterState.AirTriggerHit;

            switch (attack.type)
                {
				case ComboManager.AttackType.Special1Normal:
				case ComboManager.AttackType.Special1Down:
				case ComboManager.AttackType.Special1Up:
				case ComboManager.AttackType.Special2Normal:
				case ComboManager.AttackType.Special2Down:
				case ComboManager.AttackType.Special2Up:
				case ComboManager.AttackType.LightPunchNormal:
				case ComboManager.AttackType.StrongPunchNormal:
				case ComboManager.AttackType.LightPunchDown:
				case ComboManager.AttackType.StrongPunchDown:
                    return backStatus ? CharacterState.HitBack : CharacterState.Hit;
				case ComboManager.AttackType.LightPunchUp:
				case ComboManager.AttackType.StrongPunchUp:
					return backStatus ? CharacterState.HitUpBack : CharacterState.HitUp;
				case ComboManager.AttackType.LightAirPunch:
				case ComboManager.AttackType.LightAirPunchDown:
				case ComboManager.AttackType.StrongAirPunch:
				case ComboManager.AttackType.StrongAirPunchDown:
				case ComboManager.AttackType.AirSpecial1Normal:
				case ComboManager.AttackType.AirSpecial1Up:
				case ComboManager.AttackType.AirSpecial1Down:
				case ComboManager.AttackType.AirSpecial2Normal:
				case ComboManager.AttackType.AirSpecial2Up:
				case ComboManager.AttackType.AirSpecial2Down:
				case ComboManager.AttackType.AirSpecial3Normal:
				case ComboManager.AttackType.AirSpecial3Up:
				case ComboManager.AttackType.AirSpecial3Down:
					if (target.movements.jumping)
						return backStatus ? CharacterState.AirHit : CharacterState.AirHitBack;
						else
						return backStatus ? CharacterState.HitUpBack : CharacterState.HitUp;
                }

            return CharacterState.Idle;
            }


		public static CharacterState toCharacterState(this ComboManager.Attack attack, bool backStatus)
            {
            switch (attack.type)
                {
				case ComboManager.AttackType.StrongPunchNormal:             
                    return (backStatus) ? CharacterState.StrongPunchNormalBack
						: CharacterState.StrongPunchNormal;
				case ComboManager.AttackType.StrongPunchDown:       
                    return (backStatus) ? CharacterState.StrongPunchDownBack : CharacterState.StrongPunchDown;
				case ComboManager.AttackType.StrongPunchUp:
                    return (backStatus) ? CharacterState.StrongPunchUpBack : CharacterState.StrongPunchUp;
				case ComboManager.AttackType.LightPunchNormal:
                    return (backStatus) ? CharacterState.LightPunchNormalBack : CharacterState.LightPunchNormal;
				case ComboManager.AttackType.LightPunchDown:
                    return (backStatus) ? CharacterState.LightPunchDownBack : CharacterState.LightPunchDown;
				case ComboManager.AttackType.LightPunchUp:
                    return (backStatus) ? CharacterState.LightPunchUpBack : CharacterState.LightPunchUp;
				case ComboManager.AttackType.LightAirPunch:
					return (backStatus) ? CharacterState.LightAirPunchBack : CharacterState.LightAirPunch;                
				case ComboManager.AttackType.LightAirPunchDown:
					return (backStatus) ? CharacterState.LightAirPunchDownBack : CharacterState.LightAirPunchDown; 
				case ComboManager.AttackType.StrongAirPunch:
					return CharacterState.StrongAirPunch;                 
				case ComboManager.AttackType.StrongAirPunchDown:
					return CharacterState.StrongAirPunchDown;     
				case ComboManager.AttackType.Special1Normal:
					return CharacterState.SpecialAttackNormal;
				case ComboManager.AttackType.Special1Down:
					return CharacterState.SpecialAttackDown;
				case ComboManager.AttackType.Special1Up:
					return CharacterState.SpecialAttackUp;
				case ComboManager.AttackType.Special2Normal:
					return CharacterState.SpecialAttack2Normal;
				case ComboManager.AttackType.Special2Down:
					return CharacterState.SpecialAttack2Down;
				case ComboManager.AttackType.Special2Up:
					return CharacterState.SpecialAttack2Up;
				case ComboManager.AttackType.Special3Normal:
					return CharacterState.SpecialAttack3Normal;
				case ComboManager.AttackType.AirSpecial1Normal:
					return CharacterState.SpecialAirAttackNormal;
				case ComboManager.AttackType.AirSpecial1Up:
					return CharacterState.SpecialAirAttackUp;
				case ComboManager.AttackType.AirSpecial1Down:
					return CharacterState.SpecialAirAttackDown;
				case ComboManager.AttackType.AirSpecial2Normal:
					return CharacterState.SpecialAirAttack2Normal;
				case ComboManager.AttackType.AirSpecial2Up:
					return CharacterState.SpecialAirAttack2Up;
				case ComboManager.AttackType.AirSpecial2Down:
					return CharacterState.SpecialAirAttack2Down;
				case ComboManager.AttackType.AirSpecial3Normal:
					return CharacterState.SpecialAirAttack3Normal;
				case ComboManager.AttackType.AirSpecial3Down:
					return CharacterState.SpecialAirAttack3Down;
                }   

            return CharacterState.None;
            }


        public static bool isAttack(this CharacterState state)
            {
            switch (state)
                {
				case CharacterState.TestKick:
				case CharacterState.TestKickBack:
				case CharacterState.TestPunch:
				case CharacterState.TestPunchBack:
                case CharacterState.StrongPunchNormal:
                case CharacterState.StrongPunchNormalBack:
                case CharacterState.StrongPunchDown:
                case CharacterState.StrongPunchDownBack:
                case CharacterState.StrongPunchUp:
                case CharacterState.StrongPunchUpBack:
                case CharacterState.LightPunchNormal:
                case CharacterState.LightPunchNormalBack:
                case CharacterState.LightPunchDown:
                case CharacterState.LightPunchDownBack:
                case CharacterState.LightPunchUp:
                case CharacterState.LightPunchUpBack:
                case CharacterState.LightAirPunch:
				case CharacterState.LightAirPunchBack:
				case CharacterState.LightAirPunchDown:
				case CharacterState.LightAirPunchDownBack:
				case CharacterState.StrongAirPunch:
				case CharacterState.AirTriggerAttack:
				case CharacterState.SpecialAttackNormal:
				case CharacterState.SpecialAttackDown:
				case CharacterState.SpecialAttackUp:
				case CharacterState.SpecialAttack2Normal:
				case CharacterState.SpecialAttack2Down:
				case CharacterState.SpecialAttack2Up:
				case CharacterState.SpecialAirAttackNormal:
				case CharacterState.SpecialAirAttackUp:
				case CharacterState.SpecialAirAttackDown:
				case CharacterState.SpecialAirAttack2Normal:
				case CharacterState.SpecialAirAttack2Up:
				case CharacterState.SpecialAirAttack2Down:
				case CharacterState.SpecialAirAttack3Normal:
				case CharacterState.SpecialAirAttack3Down:
                    return true;
                default:
                    return false;
                }
            }


		public static bool isUpAttack(this ComboManager.Attack attack)
            {
            switch (attack.type)
                {
				case ComboManager.AttackType.StrongPunchUp:
				case ComboManager.AttackType.LightPunchUp:
				case ComboManager.AttackType.Special1Up:
				case ComboManager.AttackType.Special2Up:
				case ComboManager.AttackType.Special3Up:
				case ComboManager.AttackType.AirSpecial1Up:
				case ComboManager.AttackType.AirSpecial2Up:
				case ComboManager.AttackType.AirSpecial3Up:
				return true;
                }   

            return false;
            }


		public static bool isNormalAttack(this ComboManager.Attack attack)
            {
            switch (attack.type)
                {
				case ComboManager.AttackType.StrongPunchNormal:
				case ComboManager.AttackType.LightPunchNormal:
				case ComboManager.AttackType.StrongAirPunch:
				case ComboManager.AttackType.LightAirPunch:
				case ComboManager.AttackType.Special1Normal:
				case ComboManager.AttackType.Special2Normal:
				case ComboManager.AttackType.Special3Normal:
				case ComboManager.AttackType.AirSpecial1Normal:
				case ComboManager.AttackType.AirSpecial2Normal:
				case ComboManager.AttackType.AirSpecial3Normal:
				return true;
                }   

            return false;
            }


		public static bool isDownAttack(this ComboManager.Attack attack)
            {
            switch (attack.type)
                {
				case ComboManager.AttackType.StrongPunchDown:
				case ComboManager.AttackType.LightPunchDown:
				case ComboManager.AttackType.StrongAirPunchDown:
				case ComboManager.AttackType.LightAirPunchDown:
				case ComboManager.AttackType.Special1Down:
				case ComboManager.AttackType.Special2Down:
				case ComboManager.AttackType.Special3Down:
				case ComboManager.AttackType.AirSpecial1Down:
				case ComboManager.AttackType.AirSpecial2Down:
				case ComboManager.AttackType.AirSpecial3Down:
				return true;
                }   

            return false;
            }


        public static bool isAirHit(this CharacterState state)
            {
            switch (state)
                {
                case CharacterState.AirHit:
				case CharacterState.AirHitBack:
                return true;
                default:
                return false;
                }
            }

        public static bool isFinisherHit(this CharacterState state)
			{
			switch (state)
				{
				case CharacterState.AirFinisherHit:
				case CharacterState.AirFinisherHitGroundHit:
				case CharacterState.GroundFinisherHit:
					return true;
				default:
					return false;
				}
			}     


		public static bool isGroundHit(this CharacterState state)
            {
            switch (state)
                {
                case CharacterState.Hit:
                case CharacterState.HitBack:
                case CharacterState.HitUp:
				case CharacterState.HitUpBack:
				case CharacterState.GroundFinisherHit:
				case CharacterState.AirFinisherHitGroundHit:
                return true;
                default:
                return false;
                }
            }    


		public static bool isDeathHit(this CharacterState state)
            {
            switch (state)
                {
				case CharacterState.GroundFinisherDeath:
                case CharacterState.AirFinisherHitGroundDeath:
                return true;
                default:
                return false;
                }
            }


        public static bool isHit(this CharacterState state)
            {
            switch (state)
                {
                case CharacterState.AirHit:
				case CharacterState.AirHitBack:
                case CharacterState.Hit:
                case CharacterState.HitBack:
                case CharacterState.HitUp:
				case CharacterState.HitUpBack:
				case CharacterState.AirTriggerHit:
				case CharacterState.AirFinisherHit:
				case CharacterState.AirFinisherHitGroundHit:
				case CharacterState.GroundFinisherHit:
				case CharacterState.GroundFinisherDeath:
                case CharacterState.AirFinisherHitGroundDeath:
                return true;
                default:
                return false;
                }
            }

		public static bool isBlock(this CharacterState state)
            {
            switch (state)
                {
                case CharacterState.Block:
                case CharacterState.BlockHold:
                case CharacterState.BlockToIdle:
				case CharacterState.AirBlock:
                case CharacterState.AirBlockDownJump:
                case CharacterState.AirBlockHold:
                return true;
                default:
                return false;
                }
            }


		public static bool isDodge(this CharacterState state)
            {
            switch (state)
                {
                case CharacterState.DodgeBack:
                case CharacterState.DodgeFront:
                return true;
                default:
                return false;
                }
            }


		public static bool isVaulting(this CharacterState state)
			{
			return state == CharacterState.Vaulting;
			}


		public static bool isTurn(this CharacterState state)
            {
            switch (state)
                {
                case CharacterState.IdleTurn:
				case CharacterState.RunTurn:
                return true;
                default:
                return false;
                }
            }

                          #endregion


        #region VFX

		public static Character.VfxData getAdjustedData(this Character.HitVfx hitVfx, ComboManager.Attack attack)
			{
			const float normalVertOffset = -0.2f;
			const float upVertOffset = 0.3f;
			const float downVertOffset = -0.65f;
			const float airNormalVertOffset = 0.15f;
			const float airUpVertOffset = 0.3f;
			const float airDownVertOffset = -0.15f;

			Character.VfxData vfxData = attack.isAirAttack() ? hitVfx.airLight : hitVfx.light;

			if (attack.isStrongAttack())
				vfxData = attack.isAirAttack() ? hitVfx.airStrong : hitVfx.strong;

			if (attack.isSpecial1())
				vfxData = hitVfx.special1;

			vfxData.internalRotation = vfxData.randomRotation ? UnityEngine.Random.Range(0f, 360f) : vfxData.rotation;
			vfxData.internalOffset = new Vector2(0.1f, 
				attack.isAirAttack() ? airNormalVertOffset : normalVertOffset)+vfxData.offsetNormal;

			if (!attack.airTrigger)//air triggers go center/normal
				{
				if (attack.isUpAttack())
					{
					vfxData.internalOffset.y = attack.isAirAttack() ? airUpVertOffset : upVertOffset;
					vfxData.internalOffset += vfxData.offsetUp;
					}

				if (attack.isDownAttack())
					{
					vfxData.internalOffset.y =  attack.isAirAttack() ? airDownVertOffset : downVertOffset;
					vfxData.internalOffset += vfxData.offsetDown;
					}
				}

			if (string.IsNullOrEmpty(vfxData.prefab))
				{
				vfxData.prefab = "Hit";
				}

			return vfxData;
			}


		public static void spawn(this Character.VfxData vfxData, Transform target, Character owner, SortingLayer sortingLayer)
			{
			if (!vfxData.enabled)
				return;

			Vfx vfx = VfxManager.instance.playVfx(vfxData.prefab, vfxData.name, 
				(Vector2) target.position+vfxData.internalOffset, 
				vfxData.scale, vfxData.internalRotation, owner, vfxData.inPlace ? target : null);			

			vfx.setSpriteRenderersSortingLayer(sortingLayer);
			}

		#endregion
        }
	}
