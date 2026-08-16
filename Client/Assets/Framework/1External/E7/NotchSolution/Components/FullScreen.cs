using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace E7.NotchSolution
{


    //全屏
    public class FullScreen : MonoBehaviour
    {
        private int screenWidth;
        private int screenHeight;
        
        private void Awake()
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
        }

        private void Update()
        {
            if (screenWidth != Screen.width || screenHeight != Screen.height)
            {
                
                DelayedUpdate();
                screenWidth = Screen.width;
                screenHeight = Screen.height;
            }
        }


        private void OnEnable()
        {
            UpdateRect();
        }
        

        public void DelayedUpdate() => StartCoroutine(DelayedUpdateRoutine());
        
        private IEnumerator DelayedUpdateRoutine()
        {
            yield return null;
            #if UNITY_EDITOR
            yield return null;
            #endif
            UpdateRect();
        }

        public virtual void UpdateRect()
        {
            var parent1 = SafePadding.Instance.GetComponent<RectTransform>();
            var parent2 = parent1.parent.GetComponent<RectTransform>();
            var rectTransform = GetComponent<RectTransform>();

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = parent2.rect.size;
            rectTransform.anchoredPosition3D = -parent1.anchoredPosition3D;
        }

        
    }
}
