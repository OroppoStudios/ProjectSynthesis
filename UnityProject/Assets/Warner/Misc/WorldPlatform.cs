using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Warner
    {
    public class WorldPlatform : MonoBehaviour
        {

        #region MEMBER FIELDS

        public Vector2 scale = new Vector2(1, 1);
        public Floatiness floatiness;

        [Serializable]
        public class Floatiness
            {
            public float speed = 2f;
            public float amplitude = 0.15f;
            }

        [NonSerialized]
        public SpriteRenderer spriteRenderer;

        private Character character;
        private EdgeCollider2D collider;
        private bool colliderEnabled;
        private Vector2 swingPosition;

        #endregion


        #region INIT
        private void Awake()
            {
            collider = GetComponent<EdgeCollider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            }

        private void Start()
            {
            transform.localScale = scale;
            colliderEnabled = collider.enabled;
            character = LevelMaster.instance.getSinglePlayerCharacter();
            }

        #endregion


        #region UPDATE STUFF

        private void Update()
            {
            swingPosition = transform.position;
            swingPosition.y += (Mathf.Sin(Time.time * floatiness.speed) * Time.deltaTime) * floatiness.amplitude;
            transform.position = swingPosition;

            if (!collider || !character || !character.transforms.platformingColliderPoint)
                return;

            if (character.transforms.platformingColliderPoint.transform.position.y < transform.position.y && colliderEnabled)
                {
                colliderEnabled = false;
                collider.enabled = false;
                spriteRenderer.sortingLayerName = LevelMaster.instance.sortingLayers.platformsInFrontCharacters.name;
                return;
                }


            if (character.transforms.platformingColliderPoint.transform.position.y > transform.position.y && !colliderEnabled)
                {
                if (character.movements.rigidBody.velocity.y < 0)
                    {
                    colliderEnabled = true;
                    collider.enabled = true;
                    spriteRenderer.sortingLayerName = LevelMaster.instance.sortingLayers.platformsBehindCharacters.name;
                    }
                }
            }

        #endregion
        }
    }
