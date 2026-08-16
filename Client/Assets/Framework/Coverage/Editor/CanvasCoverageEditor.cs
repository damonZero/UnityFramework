//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description CanvasCoverage对象的Editor接管
//**************************************************************************************

using System.Collections.Generic;
using System.Linq;
using Framework.Coverage;
using UnityEditor;

using UnityEditorInternal;
using UnityEngine;
#if PROFILER_COMPIL_ENABLED
using Debug = UnityEngine.Debug;
using System.Diagnostics;
#endif


namespace Framework.Coverage.Editor
{
    /// <summary>
    /// 画布显示对象 Editor 接管
    /// </summary>
    [CustomEditor(typeof(CanvasCoverage), true)]
    public class CanvasCoverageEditor : UnityEditor.Editor
    {
        protected SerializedProperty _showAreaInfoiListProp; //显示列表
        protected SerializedProperty _coverAreaInfoListProp; //遮挡列表

        protected ReorderableList _showAreaList;
        protected ReorderableList _coverAreaList;
        protected GameObject _gameObject;
        protected CanvasCoverage _canvasCoverage;

        protected  const string COVERAGE_FOMATTER = "[{0}(COVERAGE)]";
        protected  const string COVERAGE = "COVERAGE";
        protected  static Color _showLineColor = Color.green;
        protected  static Color _coverLineColor = Color.blue;
        protected  static Color _selectLineColor = Color.magenta;

        public RectTransform selectedRt;

        protected static bool _showLline = false; //是否显示线框
        protected static string _showLineKey = "SHOW_CANVAS_COVERAGE_LINE";


        private void OnEnable()
        {
            _showLline = EditorPrefs.GetBool(_showLineKey);
            _canvasCoverage = (CanvasCoverage) serializedObject.targetObject;
            _gameObject = _canvasCoverage.gameObject;
            _canvasCoverage.SelectedArenaInfo = null;
            //初始化显示列表和遮挡列表
            _showAreaInfoiListProp = serializedObject.FindProperty("_showAreaInfos");
            _coverAreaInfoListProp = serializedObject.FindProperty("_coverAreaInfos");
            _showAreaList = new ReorderableList(serializedObject, _showAreaInfoiListProp, true, true, true, true);
            _coverAreaList = new ReorderableList(serializedObject, _coverAreaInfoListProp, true, true, true, true);
            _showAreaList.elementHeight = 55;
            _coverAreaList.elementHeight = 55;

            _showAreaList.drawHeaderCallback = rect => { GUI.Label(rect, "显示区域列表"); };
            _showAreaList.drawElementCallback = (rect, index, active, focused) =>
            {
                SerializedProperty item = _showAreaInfoiListProp.GetArrayElementAtIndex(index);
                rect.height -= 4;
                rect.y += 2;
                EditorGUI.PropertyField(rect, item, new GUIContent("Element " + index));
            };
            _showAreaList.onSelectCallback = list =>
            {
                _canvasCoverage.SelectedArenaInfo = _canvasCoverage.ShowArenaInfos[list.index];
                if (_canvasCoverage.SelectedArenaInfo != null && !_canvasCoverage.SelectedArenaInfo.Equals(null))
                {
                    //这里触发一次重绘
                    _canvasCoverage.SelectedArenaInfo.anchorTrans.gameObject.SetActive(false);
                    _canvasCoverage.SelectedArenaInfo.anchorTrans.gameObject.SetActive(true);
                }
            };


            _coverAreaList.drawHeaderCallback = rect => { GUI.Label(rect, "遮挡区域列表"); };
            _coverAreaList.drawElementCallback = (rect, index, active, focused) =>
            {
                SerializedProperty item = _coverAreaInfoListProp.GetArrayElementAtIndex(index);
                rect.height -= 4;
                rect.y += 2;
                EditorGUI.PropertyField(rect, item, new GUIContent("Element " + index));
            };
            _coverAreaList.onSelectCallback = list =>
            {
                _canvasCoverage.SelectedArenaInfo = _canvasCoverage.CoverArenaInfos[list.index];
                if (_canvasCoverage.SelectedArenaInfo != null && !_canvasCoverage.SelectedArenaInfo.Equals(null))
                {
                    //这里触发一次重绘
                    _canvasCoverage.SelectedArenaInfo.anchorTrans.gameObject.SetActive(false);
                    _canvasCoverage.SelectedArenaInfo.anchorTrans.gameObject.SetActive(true);
                }
            };
        }



        /// <summary>
        /// 重写Inspector面板绘制
        /// </summary>
        public override void OnInspectorGUI()
        {
            //绘制显示列表和遮挡列表
            serializedObject.Update();
            var showLine = GUILayout.Toggle(_showLline, "编辑器下是否显示线框");
            if (showLine != _showLline)
            {
                _showLline = showLine;
                EditorPrefs.SetBool(_showLineKey, showLine);
            }

            GUILayout.Label("显示区域定义：");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("智能识别显示区域")))
            {
                _canvasCoverage.ShowArenaInfos = CleverCoverageScanner
                    .ScanCoverages(_gameObject.transform as RectTransform, true).ToArray();
                EditorUtility.SetDirty(_gameObject);
            }

            if (GUILayout.Button(new GUIContent("整体作为显示区域")))
            {
                _canvasCoverage.ShowArenaInfos = new[] {new AreaInfo(_gameObject.transform as RectTransform)};
                EditorUtility.SetDirty(_gameObject);
            }

            GUILayout.EndHorizontal();
            if (GUILayout.Button(new GUIContent("一键清除显示区域")))
            {
                _canvasCoverage.ShowArenaInfos = new AreaInfo[0];
                EditorUtility.SetDirty(_gameObject);
            }

            _showAreaList.DoLayoutList();

            GUILayout.Label("遮挡区域定义：");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("智能识别遮挡区域")))
            {
                if (EditorUtility.DisplayDialog("提示", "请确保该界面是否为不透明全屏界面", "是全屏界面", "不是全屏界面"))
                {
                    _canvasCoverage.CoverArenaInfos = new[] {new AreaInfo(_gameObject.transform as RectTransform)};
                    EditorUtility.SetDirty(_gameObject);
                }
                else
                {
                    _canvasCoverage.CoverArenaInfos =
                        CleverCoverageScanner.ScanCoverages(_gameObject.transform as RectTransform, false).ToArray();
                    EditorUtility.SetDirty(_gameObject);
                }
            }

            if (GUILayout.Button(new GUIContent("整体作为遮挡区域")))
            {
                _canvasCoverage.CoverArenaInfos = new[] {new AreaInfo(_gameObject.transform as RectTransform)};
                EditorUtility.SetDirty(_gameObject);
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button(new GUIContent("一键清除遮挡区域")))
            {
                _canvasCoverage.CoverArenaInfos = new AreaInfo[0];
                EditorUtility.SetDirty(_gameObject);
            }

            _coverAreaList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }


        /// <summary>
        /// 构造显示对象的Rt列表，每个显示对象生成一个空的Rt,并设置好位置和锚点
        /// </summary>
        /// <param name="rtList"></param>
        /// <returns></returns>
        private List<RectTransform> GenerateCoverageRtList(List<RectTransform> rtList)
        {
            var list = new List<RectTransform>();
            foreach (var rt in rtList)
            {
                var child = rt.Find(string.Format(COVERAGE_FOMATTER, rt.name));
                if (child == null)
                {
                    var go = new GameObject();
                    go.name = string.Format(COVERAGE_FOMATTER, rt.name);
                    go.transform.parent = rt;
                    child = go.AddComponent<RectTransform>();
                }

                var rectTrans = child.transform as RectTransform;
                rectTrans.pivot = rt.pivot;
                rectTrans.sizeDelta = new Vector2(rectTrans.rect.width, rectTrans.rect.height);
                rectTrans.localPosition = Vector2.zero;
                rectTrans.localScale = Vector3.one;
                rectTrans.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 0);
                rectTrans.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 0);
                rectTrans.anchorMin = Vector2.zero;
                rectTrans.anchorMax = Vector2.one;
                list.Add(rectTrans);
            }

            return list;
        }

        /// <summary>
        /// 在场景中绘制显示区域和遮挡区域
        /// 显示区域：绿色
        /// 遮挡区域：红色
        /// 同时为显示区域和遮挡区域：蓝色
        /// </summary>
        /// <param name="coverage"></param>
        /// <param name="type"></param>
        [DrawGizmo(GizmoType.Active |
                   GizmoType.InSelectionHierarchy |
                   GizmoType.NotInSelectionHierarchy |
                   GizmoType.Pickable)]
        private static void RectGizmos(CanvasCoverage coverage, GizmoType type)
        {
            if (Application.isPlaying)
                return;
            if (!_showLline)
                return;
            if (coverage.ShowArenaInfos != null)
            {
                foreach (var info in coverage.ShowArenaInfos)
                {
                    if (info == null)
                        continue;
                    if (info.anchorTrans == null || info.anchorTrans.Equals(null))
                        continue;
                    if (!info.anchorTrans.gameObject.activeInHierarchy)
                        continue;
                    DrawAreaInfo(info, _showLineColor);
                }
            }


            if (coverage.CoverArenaInfos != null)
            {
                foreach (var info in coverage.CoverArenaInfos)
                {
                    if (info == null)
                        continue;
                    if (info.anchorTrans == null || info.anchorTrans.Equals(null))
                        continue;
                    if (!info.anchorTrans.gameObject.activeInHierarchy)
                        continue;
                    DrawAreaInfo(info, _coverLineColor);
                }
            }

            if (coverage.SelectedArenaInfo != null && !coverage.SelectedArenaInfo.Equals(null))
            {
                DrawAreaInfo(coverage.SelectedArenaInfo, _selectLineColor);
            }
        }

        private static void DrawAreaInfo(AreaInfo arenaInfo, Color color)
        {
            if (arenaInfo.anchorTrans == null || arenaInfo.anchorTrans.Equals(null))
                return;
            var oldColor = Gizmos.color;
            Gizmos.color = color;
            var rect = UICoverageArea.CalcRect(arenaInfo);

            var leftDown = new Vector2(rect.X, rect.Y);
            var rightDown = leftDown + new Vector2(rect.Width, 0);
            var rightUp = leftDown + new Vector2(rect.Width, rect.Height);
            var leftUp = leftDown + new Vector2(0, rect.Height);
            Gizmos.DrawLine(leftDown, rightDown);
            Gizmos.DrawLine(rightDown, rightUp);
            Gizmos.DrawLine(rightUp, leftUp);
            Gizmos.DrawLine(leftUp, leftDown);
            Gizmos.DrawSphere(leftUp, 6);
            Gizmos.DrawSphere(rightUp, 6);
            Gizmos.DrawSphere(leftDown, 6);
            Gizmos.DrawSphere(rightDown, 6);
            Gizmos.DrawSphere(Vector3.Lerp(leftUp, rightUp, 0.5f), 6);
            Gizmos.DrawSphere(Vector3.Lerp(leftUp, leftDown, 0.5f), 6);
            Gizmos.DrawSphere(Vector3.Lerp(rightUp, rightDown, 0.5f), 6);
            Gizmos.DrawSphere(Vector3.Lerp(leftDown, rightDown, 0.5f), 6);
            Gizmos.color = oldColor;
        }

        [InitializeOnLoadMethod]
        public static void OnInit()
        {
            PrefabUtility.prefabInstanceUpdated += OnPrefabSave;
            UnityEditor.SceneManagement.PrefabStage.prefabSaving += OnPrefabSave;
        }

        private static void OnPrefabSave(GameObject inst)
        {
#if PROFILER_COMPIL_ENABLED
            var stopwatch = Stopwatch.StartNew();
#endif
            var cov = inst.GetComponent<CanvasCoverage>();
            if (cov == null || cov.Equals(null))
                return;
            foreach (var areaInfo in cov.ShowArenaInfos)
            {
                if (areaInfo.anchorTrans == null || areaInfo.anchorTrans.Equals(null))
                    Debug.LogError($"{inst.name} 显示区域丢失!", inst);
            }

            foreach (var areaInfo in cov.CoverArenaInfos)
            {
                if (areaInfo.anchorTrans == null || areaInfo.anchorTrans.Equals(null))
                    Debug.LogError($"{inst.name} 遮挡区域丢失!", inst);
            }
#if PROFILER_COMPIL_ENABLED
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 25)
                Debug.Log($"CanvasCoverageEditor OnPrefabSave: 耗时{stopwatch.ElapsedMilliseconds}ms");
#endif
        }
    }
}
