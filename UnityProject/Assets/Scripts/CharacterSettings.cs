using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warner;
using System;

public class CharacterSettings : MonoBehaviour
{
    [Range (1,100)] public int health;
    public Vector2 healthBarOffset;
    public AttackData punch;
    public AttackData kick;

    [Serializable]
    public struct AttackData
        {
        public int damage;
        public float knockBack;
        public CharacterAttacks.HitShakeData shake;
        }

    private Character character;

    private void Awake()
    {
        character = GetComponent<Character>();
        character.spriteFlasher.addSpriteRenderer(character.transforms.lineArt.GetComponent<SpriteRenderer>());
        }


    private void OnEnable()
        {
        character.health = health;

        if (character.healthBar.enabled)
            character.healthBar.ui.offset = healthBarOffset;

        character.attacks.damages.light = punch.damage;
        character.attacks.damages.strong = kick.damage;
        character.attacks.receiverData.hitStun.light = punch.knockBack;
        character.attacks.receiverData.hitStun.strong = kick.knockBack;
        character.attacks.receiverData.lightHitShake = punch.shake;
        character.attacks.receiverData.strongHitShake = kick.shake;
        }
    }
