using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace E7.NotchSolution
{
    public class FullScreenBackground : FullScreen
    {
        public bool isFillScreen;
        
        // 是否可以更新
        public bool isCanUpdate = true;
        
        private bool updateRect = false;

        public override void UpdateRect()
        {
            if (isCanUpdate == false && updateRect == true)
            {
                return;
            }

            updateRect = true;
            base.UpdateRect();
        }
        
        public void DirectUpdateRect()
        {
            base.UpdateRect();
        }
    }
}
