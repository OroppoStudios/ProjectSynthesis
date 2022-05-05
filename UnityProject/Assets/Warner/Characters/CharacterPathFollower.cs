using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace Warner
    {
    public class CharacterPathFollower : MonoBehaviour
        {

        #region MEMBER FIELDS

        public Point[] path;

        [Serializable]
        public struct Point
            {
            public Vector2 pos;
            public float holdDuration;
            }     

        private Character character;
        private IEnumerator<float> followRoutine;
        private IEnumerator<float> holdRoutine;
        private bool waiting;
        private int targetPointIndex;
        private bool _freeze;

        #endregion



        #region INIT       

        public void Awake()
            {
            character = GetComponent<Character>();
            }

        public void OnEnable()
            {
            character.attacks.onReceiveDamage += onReceiveDamage;
            character.attacks.onFinishedReceivingDamage += onFinishedReceivingDamage;

            followRoutine = followCoRoutine();
            Timing.run(followRoutine);
            }


        #endregion



        #region ROUTINES

        public bool freeze
            {
            get { return _freeze; }
            set {
                _freeze = value;

                if (_freeze)
                    {
                    stop();
                    }
                    else
                    waiting = false;
                }
            }

        public IEnumerator<float> followCoRoutine()
            {
            yield return Timing.waitForSeconds(0.25f);
            float diff;

            while (true)
                {
                yield return Timing.waitForSeconds(0.1f);

                if (waiting || _freeze) continue;

                diff = transform.position.x - path[targetPointIndex].pos.x;

                if (Math.Abs(diff) <= 0.25f)
                    {
                    holdPosition();
                    continue;
                    }

                character.control.rawMovementDirection = new Vector2((diff)>0 ? -1 : 1, 0f);
                }
            }


        public void holdPosition()
            {
            stop();
            Timing.kill(holdRoutine);

            holdRoutine = holdCoRoutine(path[targetPointIndex].holdDuration);

            if (path.Length - 1 > targetPointIndex)
                targetPointIndex++;
                else
                targetPointIndex = 0;

            Timing.run(holdRoutine);
            }


        public IEnumerator<float> holdCoRoutine(float waitTime)
            {
            yield return Timing.waitForSeconds(waitTime);
            waiting = false;            
            }


        private void stop()
            {
            waiting = true;
            character.control.rawMovementDirection = Vector2.zero;
            }

        #endregion



        #region DELEGATE EVENTS

        private void onReceiveDamage(ComboManager.Attack attack)
            {
            stop();
            }

        private void onFinishedReceivingDamage(ComboManager.Attack attack)
            {
            waiting = false;
            }

        #endregion



        #region DESTROY

        public void OnDisable()
            {
            Timing.kill(followRoutine);
            character.attacks.onReceiveDamage -= onReceiveDamage;
            character.attacks.onFinishedReceivingDamage -= onFinishedReceivingDamage;
            }

        #endregion

        }
    }
