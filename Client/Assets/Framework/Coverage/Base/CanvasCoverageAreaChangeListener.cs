//**************************************************************************************
//Create By szx on 2022/7/18
//
//@Description canvas coverage 区域改变监听
//**************************************************************************************

using UnityEngine;
using UnityEngine.EventSystems;

namespace Framework.Coverage
{
    public class CanvasCoverageAreaChangeListener : UIBehaviour
    {
        protected override void OnRectTransformDimensionsChange()
        {
            var rt = transform as RectTransform;
            base.OnRectTransformDimensionsChange();
            // Debug.Log($"区域改变   {rt.sizeDelta}  {Time.frameCount}", gameObject);
        }
    }
}
