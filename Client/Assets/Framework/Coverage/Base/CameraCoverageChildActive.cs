using UnityEngine;

namespace Framework.Coverage
{
    public class CameraCoverageChildActive : CoverageChild
    {
        public CameraCoverage coverage;
        private void Start()
        {
            if (coverage == null)
                coverage = GetComponentInParent<CameraCoverage>();
            if (coverage != null)
                coverage.RegisterChild(this);
        }
    }
}
