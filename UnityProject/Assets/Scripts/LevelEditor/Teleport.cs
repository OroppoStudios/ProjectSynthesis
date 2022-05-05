using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
    {
    public class Teleport : MonoBehaviour
        {
        bool hit;
        void OnTriggerEnter2D(Collider2D col)
            {
            if (hit) return;
            hit = true;
            Debug.Log("EndLevel Door Found... exit");
            }

        }
    }
