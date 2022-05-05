using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Warner
    {

    public class HealthBar : MonoBehaviour
        {
        public Vector2 offset;

        private Image bar;
        private float barWidth;
        private RectTransform rectTransform;

        private void Awake()
            {            
            Transform barGO = transform.Find("Bar");

            if (barGO == null)
                {
                Debug.Log("HealthBar: Couldnt find Bar GameObject");
                return;
                }

            bar = barGO.GetComponent<Image>();
            barWidth = bar.rectTransform.sizeDelta.x;
            rectTransform = GetComponent<RectTransform>();
            }


        private void OnEnable()
            {
            bar.rectTransform.sizeDelta = new Vector2(barWidth, bar.rectTransform.sizeDelta.y);
            }

        public void update(float amount = 1f)//1 equals full size
            {
            if (bar == null)
                return;

            float newWidth = amount * barWidth;
            bar.rectTransform.sizeDelta = new Vector2(newWidth, bar.rectTransform.sizeDelta.y);
            }


        public void updatePosition(Vector2 worldPosition)
            {
            rectTransform.position = CameraController.instance.cam.WorldToScreenPoint(worldPosition)+new Vector3(offset.x, offset.y);
            }
        }

    }