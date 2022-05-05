using System;

namespace Warner.AnimationTool
	{
	public enum CharacterState 
		{
		None,
		AirBlock, 
		AirBlockDownJump, 
		AirBlockHold, 
		AirFinisherGroundHit, 
		AirFinisherHit, 
		AirFinisherHitGround, 
		AirFinisherHitGroundDeath, 
		AirFinisherHitGroundHit, 
		AirHit, 
		AirHitBack, 
		AirJump, 
		AirTriggerAttack, 
		AirTriggerHit, 
		BigFall, 
		Block, 
		BlockHold, 
		BlockToIdle, 
		Death, 
		DodgeBack, 
		DodgeBackTurn, 
		DodgeFront, 
		DodgeToIdle, 
		DownJump, 
		GroundFinisherDeath, 
		GroundFinisherHit, 
		Hit, 
		HitBack, 
		HitUp, 
		HitUpBack, 
		Idle, 
		IdleToRun, 
		IdleTurn, 
		JumpIdleLanding, 
		JumpRunLanding, 
		LightAirPunch, 
		LightAirPunchBack, 
		LightAirPunchDown, 
		LightAirPunchDownBack, 
		LightPunchDown, 
		LightPunchDownBack, 
		LightPunchNormal, 
		LightPunchNormalBack, 
		LightPunchUp, 
		LightPunchUpBack, 
		PreRun, 
		Run, 
		RunStop, 
		RunTurn, 
		SpecialAirAttack2Down, 
		SpecialAirAttack2Normal, 
		SpecialAirAttack2Up, 
		SpecialAirAttack3Down, 
		SpecialAirAttack3Normal, 
		SpecialAirAttackDown, 
		SpecialAirAttackNormal, 
		SpecialAirAttackUp, 
		SpecialAttack2Down, 
		SpecialAttack2Normal, 
		SpecialAttack2Up, 
		SpecialAttack3Normal, 
		SpecialAttackDown, 
		SpecialAttackNormal, 
		SpecialAttackUp, 
		StrongAirPunch, 
		StrongAirPunchDown, 
		StrongPunchDown, 
		StrongPunchDownBack, 
		StrongPunchNormal, 
		StrongPunchNormalBack, 
		StrongPunchUp, 
		StrongPunchUpBack, 
		Taunt, 
		TestKick, 
		TestKickBack, 
		TestPunch, 
		TestPunchBack, 
		UpJump, 
		Vaulting
		}

	[Serializable]
	public struct CharacterStates 
		{
		public bool AirBlock;
		public bool AirBlockDownJump;
		public bool AirBlockHold;
		public bool AirFinisherGroundHit;
		public bool AirFinisherHit;
		public bool AirFinisherHitGround;
		public bool AirFinisherHitGroundDeath;
		public bool AirFinisherHitGroundHit;
		public bool AirHit;
		public bool AirHitBack;
		public bool AirJump;
		public bool AirTriggerAttack;
		public bool AirTriggerHit;
		public bool BigFall;
		public bool Block;
		public bool BlockHold;
		public bool BlockToIdle;
		public bool Death;
		public bool DodgeBack;
		public bool DodgeBackTurn;
		public bool DodgeFront;
		public bool DodgeToIdle;
		public bool DownJump;
		public bool GroundFinisherDeath;
		public bool GroundFinisherHit;
		public bool Hit;
		public bool HitBack;
		public bool HitUp;
		public bool HitUpBack;
		public bool Idle;
		public bool IdleToRun;
		public bool IdleTurn;
		public bool JumpIdleLanding;
		public bool JumpRunLanding;
		public bool LightAirPunch;
		public bool LightAirPunchBack;
		public bool LightAirPunchDown;
		public bool LightAirPunchDownBack;
		public bool LightPunchDown;
		public bool LightPunchDownBack;
		public bool LightPunchNormal;
		public bool LightPunchNormalBack;
		public bool LightPunchUp;
		public bool LightPunchUpBack;
		public bool PreRun;
		public bool Run;
		public bool RunStop;
		public bool RunTurn;
		public bool SpecialAirAttack2Down;
		public bool SpecialAirAttack2Normal;
		public bool SpecialAirAttack2Up;
		public bool SpecialAirAttack3Down;
		public bool SpecialAirAttack3Normal;
		public bool SpecialAirAttackDown;
		public bool SpecialAirAttackNormal;
		public bool SpecialAirAttackUp;
		public bool SpecialAttack2Down;
		public bool SpecialAttack2Normal;
		public bool SpecialAttack2Up;
		public bool SpecialAttack3Normal;
		public bool SpecialAttackDown;
		public bool SpecialAttackNormal;
		public bool SpecialAttackUp;
		public bool StrongAirPunch;
		public bool StrongAirPunchDown;
		public bool StrongPunchDown;
		public bool StrongPunchDownBack;
		public bool StrongPunchNormal;
		public bool StrongPunchNormalBack;
		public bool StrongPunchUp;
		public bool StrongPunchUpBack;
		public bool Taunt;
		public bool TestKick;
		public bool TestKickBack;
		public bool TestPunch;
		public bool TestPunchBack;
		public bool UpJump;
		public bool Vaulting;
		}
	}