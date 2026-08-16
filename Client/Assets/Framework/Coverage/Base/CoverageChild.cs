//**************************************************************************************
//Create By szx on 2020/12/1
//
//@Description Coverage子节点基类，用于随着coverage显示与隐藏
//**************************************************************************************

using UnityEngine;

namespace Framework.Coverage
{
    public class CoverageChild : MonoBehaviour
    {
        private void Start()
        {
            var coverage = GetComponentInParent<BaseCoverage>();
            if (coverage != null)
                coverage.RegisterChild(this);
        }

        public virtual void OnShow()
        {
            gameObject.SetActive(true);
        }

        public virtual void OnHide()
        {
            gameObject.SetActive(false);
        }
    }
}
