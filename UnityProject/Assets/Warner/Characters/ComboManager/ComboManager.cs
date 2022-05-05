using UnityEngine;
using System.Collections.Generic;

namespace Warner
	{
	public class ComboManager: MonoBehaviour
		{
		#region MEMBER FIELDS

		public string[] characters;
		        
        public static ComboManager instance;   

        public struct Attack
        	{
        	public AttackType type;
        	public bool airTrigger;
			public bool finisher;
        	}

		public enum AttackType {None, LightPunchNormal, LightPunchDown, LightPunchUp, 
			StrongPunchNormal, StrongPunchDown, StrongPunchUp, LightAirHit, StrongAirHit, 
        LightAirPunch, LightAirPunchDown, StrongAirPunch, StrongAirPunchDown, 
		SpecialAnyNormal, SpecialAnyDown, SpecialAnyUp,AirSpecialAnyNormal, AirSpecialAnyUp, AirSpecialAnyDown,
		AirSpecial1Normal, AirSpecial1Up, AirSpecial1Down, Special1Normal, Special1Down, Special1Up,
		Special2Normal, Special2Down, Special2Up, Special3Down, Special3Normal, Special3Up, 
		AirSpecial2Normal, AirSpecial2Up, AirSpecial2Down, AirSpecial3Down, AirSpecial3Up, AirSpecial3Normal}

		public enum ComboStatus {Invalid, Starting, Valid, AirTrigger, Finisher}

		public class Combo
			{
			public Attack[] progression;
			public bool isAssisted;
			}

		private Dictionary<string, Combo[]> combos = new Dictionary<string, Combo[]>();
        private const string configFile = "ComboManagerData";

		#endregion



		#region INIT STUFF

		private void Awake()
			{
			instance = this;
            loadCombos();
			}


		private void loadCombos()
			{
			TextAsset textAsset;

			for (int i = 0; i<characters.Length; i++)
				{
				textAsset = Misc.loadConfigAsset("ComboManager/"+characters[i]);

				if (textAsset!=null)
					combos.Add(characters[i], getCharacterCombos(textAsset));
				}			
			}


		private Combo[] getCharacterCombos(TextAsset textAsset)
            {
            string[] lines = textAsset.text.Split('\n');

			return lines.transformTo<string, Combo>(comboLine=>
                {
				Combo comboData = new Combo();
				comboData.isAssisted = comboLine.IndexOf("*")!=-1;

				comboData.progression = comboLine.Split(',').transformTo<string, Attack>(attackString =>
                    parseAttack(attackString));

                bool validFound = false;
				for (int i = 0; i < comboData.progression.Length; i++)
					if (comboData.progression[i].type!=AttackType.None)
                        {
                        validFound = true;
                        break;
                        }
                                        
				return validFound ? comboData : null;
                });
            }

		#endregion



		#region COMBO

		public ComboStatus isCombo(List<ComboManager.Attack> comboProgression, Combo[] availableCombos, Attack nextAttack)
			{
			ComboStatus comboStatus = ComboStatus.Invalid;
			return ComboStatus.Valid;

			for (int i = 0; i<availableCombos.Length; i++)
				{
				if (comboProgression.Count>availableCombos[i].progression.Length)
					{
					comboStatus = ComboStatus.Invalid;
					continue;
					}

				//assume the combo is valid and prove otherwise
				comboStatus = ComboStatus.Valid;

				for (int j = 0; j<comboProgression.Count; j++)
					if (comboProgression[j].type!=availableCombos[i].progression[j].type)
						comboStatus = ComboStatus.Invalid;

				//if valid progression check the next attack
				if (comboStatus==ComboStatus.Valid)
					{
					if (availableCombos[i].progression.Length<=comboProgression.Count)
						continue;

					if (availableCombos[i].progression[comboProgression.Count].type==nextAttack.type 
						|| (availableCombos[i].progression[comboProgression.Count].isAnySpecial() && nextAttack.isSpecial()))
						{
						if (availableCombos[i].progression[comboProgression.Count].airTrigger)
							comboStatus = ComboStatus.AirTrigger;
							else
							if (availableCombos[i].progression[comboProgression.Count].finisher)
								comboStatus = ComboStatus.Finisher;
								else
								if (comboProgression.Count==0)
									comboStatus = ComboStatus.Starting;
						break;
						}
						else
						comboStatus = ComboStatus.Invalid;
                    }
				}

			return comboStatus;
			}

        #endregion



        #region MISC

        public Combo[] getCombos(string character)
			{
			if (combos.ContainsKey(character))
				return combos[character];
				else
				{
				Debug.LogWarning("No combos found for "+character+" under /Resources/ComboManager or on the component list");
				return new Combo[0];
				}
			}

        public static Attack parseAttack(string attackString)
			{
			attackString = attackString.Trim().ToUpper();

			Attack attack = new Attack();


			if (isStringSpecialPunch(attackString))
				{
				if (isStringDownPunch(attackString))
					{
					if (isStringSpecial1Punch(attackString))
						attack.type = isStringAirSpecial(attackString) ? AttackType.AirSpecial1Down : AttackType.Special1Down;
						else
						if (isStringSpecial2Punch(attackString))
							attack.type = isStringAirSpecial(attackString) ? AttackType.AirSpecial2Down : AttackType.Special2Down;
							else
							if (isStringSpecial3Punch(attackString))
								attack.type = isStringAirSpecial(attackString) ? AttackType.AirSpecial3Down : AttackType.Special3Down;
								else
								attack.type = isStringAirSpecial(attackString) ? AttackType.AirSpecialAnyDown : AttackType.SpecialAnyDown;
					}else

				if (isStringUpPunch(attackString))
					{
					if (isStringSpecial1Punch(attackString))
						attack.type = isStringAirSpecial(attackString) ? AttackType.AirSpecial1Up : AttackType.Special1Up;
						else
						if (isStringSpecial2Punch(attackString))
							attack.type = isStringAirSpecial(attackString) ? AttackType.AirSpecial2Up : AttackType.Special2Up;
							else
							if (isStringSpecial3Punch(attackString))
								attack.type = isStringAirSpecial(attackString) ? AttackType.AirSpecial3Up : AttackType.Special3Up;
								else
								attack.type = isStringAirSpecial(attackString) ? AttackType.AirSpecialAnyUp : AttackType.SpecialAnyUp;
					}else
					//regular
					{
					if (isStringSpecial1Punch(attackString))
						attack.type = isStringAirSpecial(attackString) ? AttackType.AirSpecial1Normal : AttackType.Special1Normal;
						else
						if (isStringSpecial2Punch(attackString))
							attack.type = isStringAirSpecial(attackString) ? AttackType.AirSpecial2Normal : AttackType.Special2Normal;
							else
							if (isStringSpecial3Punch(attackString))
								attack.type = isStringAirSpecial(attackString) ? AttackType.AirSpecial3Normal : AttackType.Special3Normal;
								else
								attack.type = isStringAirSpecial(attackString) ? AttackType.AirSpecialAnyNormal : AttackType.SpecialAnyNormal;
					}
				}else

			if (isStringAirLightPunch(attackString))
				{
				if (isStringDownPunch(attackString))
					attack.type = AttackType.LightAirPunchDown;
					else
					attack.type = AttackType.LightAirPunch;
				}else

			if (isStringAirStrongPunch(attackString))
				{
				if (isStringDownPunch(attackString))
					attack.type = AttackType.StrongAirPunchDown;
					else
					attack.type = AttackType.StrongAirPunch;				
				}else

			if (isStringLightPunch(attackString))
				{
				attack.type = AttackType.LightPunchNormal;

				if (isStringDownPunch(attackString))
					attack.type = AttackType.LightPunchDown;
					else
					if (isStringUpPunch(attackString))
						attack.type = AttackType.LightPunchUp;
				}else

			if (isStringStrongPunch(attackString))
				{
				attack.type = AttackType.StrongPunchNormal;

				if (isStringDownPunch(attackString))
					attack.type = AttackType.StrongPunchDown;
					else
					if (isStringUpPunch(attackString))
						attack.type = AttackType.StrongPunchUp;
				}

			if (isStringAirTrigger(attackString))
				attack.airTrigger = true;

			if (isStringFinisher(attackString))
				attack.finisher = true;

            return attack;
            }


		public static bool isStringSpecialPunch(string attackString)
            {
            return attackString.IndexOf("X")!=-1;
            }


		public static bool isStringSpecial1Punch(string attackString)
            {
            return attackString.IndexOf("X1")!=-1;
            }


		public static bool isStringSpecial2Punch(string attackString)
            {
            return attackString.IndexOf("X2")!=-1;
            }


		public static bool isStringSpecial3Punch(string attackString)
            {
            return attackString.IndexOf("X3")!=-1;
            }


		public static bool isStringAirSpecial(string attackString)
            {
            return attackString.IndexOf("AX")!=-1;
            }


		public static bool isStringAirLightPunch(string attackString)
            {
            return attackString.IndexOf("ALP")!=-1;
            }

		public static bool isStringAirStrongPunch(string attackString)
            {
            return attackString.IndexOf("ASP")!=-1;
            }

        public static bool isStringLightPunch(string attackString)
            {
            return attackString.IndexOf("LP")!=-1;
            }

        public static bool isStringStrongPunch(string attackString)
            {
            return attackString.IndexOf("SP")!=-1;
            }

        public static bool isStringDownPunch(string attackString)
            {
            return attackString.IndexOf("D")!=-1;
            }

        public static bool isStringUpPunch(string attackString)
            {
            return attackString.IndexOf("U")!=-1;
            }

		public static bool isStringAirTrigger(string attackString)
            {
            return attackString.IndexOf("AT")!=-1;
            }

		public static bool isStringFinisher(string attackString)
            {
            return attackString.IndexOf("FI")!=-1;
            }

        #endregion
		}
	}


