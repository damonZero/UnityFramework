//*****************************************************************************
// 预制体预览类(移植自 P33 Core.Editor/Prefab/PrefabPreview.cs)
// 原命名空间 Core, 移植后归入 Package.PSD2UGUI
//*****************************************************************************

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Package.PSD2UGUI
{
    public static class PrefabPreview
    {
        //节点显示layer，摄像机也只渲染该层，避免渲染了其他的东西
        private const int RENDER_LAYER = 18;
        //节点显示位置
        private static readonly Vector3 _showPos = new Vector3(-1000, -1000, -1000);

        //获取预制体预览图
        //zoom: 缩放系数（1=自动取景，>1 放大，<1 缩小）；offsetX/offsetY: 水平/垂直偏移（像素）
        public static Texture GetPrefabPreview(GameObject obj, int texWidth = 128, int texHeight = 128, bool previewRootNode = false,
            float zoom = 1f, float offsetX = 0f, float offsetY = 0f)
        {
            GameObject canvasObj = null;
            var clone = Object.Instantiate(obj);
            var cloneTransform = clone.transform;
            var isUINode = false;
            if (cloneTransform is RectTransform)
            {
                //UGUI节点需放在Canvas下
                canvasObj = new GameObject("render canvas", typeof(Canvas));
                //需校正以下UI显示，否则布局会错乱
                var canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(750, 1624);
                cloneTransform.SetParent(canvasObj.transform, false);
                cloneTransform.localPosition = Vector3.zero;
                //隐藏显示用于重新自动布局
                clone.SetActive(false);
                clone.SetActive(true);
                //还原
                canvas.renderMode = RenderMode.WorldSpace;

                canvasObj.transform.position = _showPos;
                canvasObj.layer = RENDER_LAYER;
                isUINode = true;
                LayoutRebuilder.ForceRebuildLayoutImmediate(cloneTransform as RectTransform);
            }
            else
                cloneTransform.position = _showPos;

            //设置所有子节点层级
            var all = clone.GetComponentsInChildren<Transform>();
            foreach (var trans in all)
            {
                trans.gameObject.layer = RENDER_LAYER;
            }
            return GetPreviewTex(clone, isUINode, canvasObj, texWidth, texHeight, previewRootNode, zoom, offsetX, offsetY);
        }
        //获取预览图
        private static Texture GetPreviewTex(GameObject clone, bool isUINode, GameObject canvasObj, int texWidth, int texHeight, bool previewRootNode,
            float zoom, float offsetX, float offsetY)
        {
            //获取包围盒
            var bounds = GetBounds(clone, previewRootNode);
            var min = bounds.min;
            var max = bounds.max;
            var cameraObj = new GameObject("render camera");

            var renderCamera = cameraObj.AddComponent<Camera>();
            renderCamera.backgroundColor = new Color(1f, 1f, 1f, 0f);
            renderCamera.clearFlags = CameraClearFlags.Color;
            renderCamera.cameraType = CameraType.Preview;
            renderCamera.cullingMask = 1 << RENDER_LAYER;
            var cloneTransform = clone.transform;
            if (isUINode)
            {
                var position = cloneTransform.position;
                var centerPos = new Vector3((max.x + min.x) * 0.5f - offsetX, (max.y + min.y) * 0.5f - offsetY,
                    position.z - 100);
                var width = max.x - min.x;
                var height = max.y - min.y;

                //摄像机z值偏移是为了将节点拍到
                cameraObj.transform.position = centerPos;

                renderCamera.orthographic = true;
                //预览图要尽量少点空白
                float maxCameraSize;
                if (width > height)
                    maxCameraSize = Mathf.Max(width, height);
                else
                    maxCameraSize = Mathf.Max(height * 0.5f, width);
                //zoom>1 放大，<1 缩小；默认 1 保持自动取景
                renderCamera.orthographicSize = maxCameraSize / Mathf.Max(0.1f, zoom);
            }
            else
            {
                cameraObj.transform.position =
                    new Vector3((max.x + min.x) / 2f, (max.y + min.y) / 2f, max.z + (max.z - min.z));
                var position = cloneTransform.position;
                var center = new Vector3(position.x, (max.y + min.y) / 2f, position.z);
                cameraObj.transform.LookAt(center);

                var angle = (int) (Mathf.Atan2((max.y - min.y) / 2, (max.z - min.z)) * 180 / 3.1415f * 2);
                renderCamera.fieldOfView = angle;
            }
            var texture = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.Default);
            renderCamera.targetTexture = texture;

            //不知道为什么要删掉再Undo回来后才Render得出来UI的节点，3D节点是没这个问题的，估计是Canvas创建后没那么快有效？
            Undo.DestroyObjectImmediate(cameraObj);
            Undo.PerformUndo();

            // RenderDontRestore 不会恢复 RenderTexture.active，会残留指向 texture，
            // 污染编辑器 / Game 视图的渲染目标（表现为背景变灰），需手动保存并恢复
            var previousActive = RenderTexture.active;
            renderCamera.RenderDontRestore();
            var tex = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.Default);
            Graphics.Blit(texture, tex);
            RenderTexture.active = previousActive;

            Object.DestroyImmediate(clone);
            Object.DestroyImmediate(canvasObj);
            Object.DestroyImmediate(cameraObj);
            Object.DestroyImmediate(texture);

            return tex;
        }

        //获取边界框
        private static Bounds GetBounds(GameObject obj, bool previewRootNode)
        {
            var min = new Vector3(99999, 99999, 99999);
            var max = new Vector3(-99999, -99999, -99999);
            var renders = obj.GetComponentsInChildren<MeshRenderer>();
            if (renders.Length > 0)
            {
                foreach (var render in renders)
                {
                    if (render.bounds.min.x < min.x)
                        min.x = render.bounds.min.x;
                    if (render.bounds.min.y < min.y)
                        min.y = render.bounds.min.y;
                    if (render.bounds.min.z < min.z)
                        min.z = render.bounds.min.z;

                    if (render.bounds.max.x > max.x)
                        max.x = render.bounds.max.x;
                    if (render.bounds.max.y > max.y)
                        max.y = render.bounds.max.y;
                    if (render.bounds.max.z > max.z)
                        max.z = render.bounds.max.z;
                }
            }
            else
            {
                var rectTrans = obj.GetComponentsInChildren<RectTransform>();
                if (previewRootNode)
                {
                    rectTrans = new[]
                    {
                        obj.GetComponent<RectTransform>()
                    };
                }

                var corner = new Vector3[4];
                foreach (var rectTran in rectTrans)
                {
                    //获取节点的四个角的世界坐标，分别按顺序为左下左上，右上右下
                    rectTran.GetWorldCorners(corner);
                    if (corner[0].x < min.x)
                        min.x = corner[0].x;
                    if (corner[0].y < min.y)
                        min.y = corner[0].y;
                    if (corner[0].z < min.z)
                        min.z = corner[0].z;

                    if (corner[2].x > max.x)
                        max.x = corner[2].x;
                    if (corner[2].y > max.y)
                        max.y = corner[2].y;
                    if (corner[2].z > max.z)
                        max.z = corner[2].z;
                }
            }

            var center = (min + max) / 2;
            var size = new Vector3(max.x - min.x, max.y - min.y, max.z - min.z);
            return new Bounds(center, size);
        }
    }
}
