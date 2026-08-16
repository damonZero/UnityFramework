using UnityEngine;

namespace Framework.URPExtension
{
    /// <summary>材质属性块封装：每个 render 唯一，避免材质属性块错乱。对应参考项目 Package/URPExtension/MaterialPropertyBlockCache.cs。</summary>
    [ExecuteAlways]
    public class MaterialPropertyBlockCache : MonoBehaviour
    {
        private MaterialPropertyBlock[] _mpbList;
        private bool _isInit;

        private MaterialPropertyBlock[] MpbList
        {
            get
            {
                if (_mpbList == null) Init();
                return _mpbList;
            }
        }

        private void Init()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && _mpbList == null) _isInit = false;
#endif
            if (_isInit) return;
            _isInit = true;

            var subMeshCount = 0;
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
                subMeshCount = meshFilter.sharedMesh.subMeshCount;

            var render = GetComponent<SkinnedMeshRenderer>();
            if (render != null && render.sharedMesh != null)
                subMeshCount = render.sharedMesh.subMeshCount;

            var count = subMeshCount == 0 ? 1 : subMeshCount;
            _mpbList = new MaterialPropertyBlock[count];
        }

        public MaterialPropertyBlock this[int index]
        {
            get
            {
                Init();
                if (_mpbList == null || _mpbList.Length <= index) return null;
                return _mpbList[index] ?? (_mpbList[index] = new MaterialPropertyBlock());
            }
        }

        public void SetProperty(Renderer mr, int property, float value)
        {
            for (var i = 0; i < MpbList.Length; i++)
            {
                var mbp = MpbList[i];
                if (mbp == null) continue;
                mbp.SetFloat(property, value);
                mr.SetPropertyBlock(mbp, i);
            }
        }

        public void SetProperty(Renderer mr, int property, Texture value)
        {
            for (var i = 0; i < MpbList.Length; i++)
            {
                var mbp = MpbList[i];
                if (mbp == null) continue;
                mbp.SetTexture(property, value);
                mr.SetPropertyBlock(mbp, i);
            }
        }

        public void SetProperty(Renderer mr, int property, Color value)
        {
            for (var i = 0; i < MpbList.Length; i++)
            {
                var mbp = MpbList[i];
                if (mbp == null) continue;
                mbp.SetColor(property, value);
                mr.SetPropertyBlock(mbp, i);
            }
        }

        public void SetProperty(Renderer mr, int property, Vector4 value)
        {
            for (var i = 0; i < MpbList.Length; i++)
            {
                var mbp = MpbList[i];
                if (mbp == null) continue;
                mbp.SetVector(property, value);
                mr.SetPropertyBlock(mbp, i);
            }
        }
    }
}
