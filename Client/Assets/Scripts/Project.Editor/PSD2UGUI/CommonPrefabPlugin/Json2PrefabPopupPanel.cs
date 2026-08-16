using System;
using System.Collections.Generic;
using TMPro;
using Package.PSD2UGUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Project.Editor
{
    public class Json2PrefabPopupPanel :Json2PrefabCommonPrefabPluginBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnPopupPanel"
        };
        
        private static readonly string[] _suffixes = new[]
        {
            "small", // 小弹窗
            "mid", // 中弹窗
            "big" // 大弹窗
        };
        
        private static readonly Dictionary<string,string> _timelines = new()
        {
            { _suffixes[0], "SmallPopupPanelTimeline" },
            { _suffixes[1], "MiddlePopupPanelTimeline" },
            { _suffixes[2], "LargePopupPanelTimeline" }
        };
        
        private static Dictionary<string, float> _panelHeight = new()
        {
            { _suffixes[0], 740 },
            { _suffixes[1], 1068 },
            { _suffixes[2], 1314 }
        };
        
        public override Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;
        public override Json2PrefabEnum UseSelfSize => Json2PrefabEnum.None;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            // 从PSD节点名解析__N后缀序号，设置UIState状态
            var suffix = Psd2UguiTool.GetCommonPrefabNameSuffix(node.name);
            if (_timelines.TryGetValue(suffix, out var timelineName))
            {
                var pd = instance.GetComponent<PlayableDirector>();
                SetTimeline(pd, timelineName);
            }
            if (_panelHeight.TryGetValue(suffix, out var height))
            {
                var rectTransform = instance.GetComponent<RectTransform>();
                // 1. 设置 Anchor (水平 Stretch, 垂直 Bottom)
                rectTransform.anchorMin = new Vector2(0f, 0f);
                rectTransform.anchorMax = new Vector2(1f, 0f);

                // 2. 设置 Pivot (Y=0 保证 PosY=0 时贴紧底部)
                rectTransform.pivot = new Vector2(0.5f, 0f);

                // 3. 设置 Left/Right 与 Height
                // 水平 Stretch 状态下，sizeDelta.x = -(Left + Right) = 0
                var offsetMin = rectTransform.offsetMin;
                var offsetMax = rectTransform.offsetMax;
                offsetMin.x = 0;
                offsetMax.x = 0;
                rectTransform.offsetMin = offsetMin;
                rectTransform.offsetMax = offsetMax;
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
                
                rectTransform.anchoredPosition = new Vector2(0f, 0f);
            }
            
            SetTitle(node, instance);
            SetTitleDesc(node, instance);
        }
        
        private void SetTimeline(PlayableDirector pd, string timelineName)
        {
            if (pd == null) return;
            var guids = AssetDatabase.FindAssets($"{timelineName} t:TimelineAsset");

            if (guids.Length == 0)
            {
                Debug.LogError("[TimelineEditorHelper] 未找到名为 'TestTimeline' 的 Timeline 资源。");
                return;
            }

            // 2. 将 GUID 转换为 Project 中的文件路径
            var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);

            // 3. 加载 Timeline 资源
            var timelineAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(assetPath);

            if (timelineAsset != null)
            {
                // 支持 Undo 操作，便于在编辑器中一键撤销赋值
                Undo.RecordObject(pd, "Assign Timeline Asset");

                // 4. 赋值给 PlayableDirector
                pd.playableAsset = timelineAsset;

                // 标记对象已被修改，确保场景或 Prefab 能保存变更
                EditorUtility.SetDirty(pd);

                Debug.Log($"[TimelineEditorHelper] 成功将 '{assetPath}' 赋值给 {pd.name}");
            }
        }
        
        private void SetTitle(PsdNodeBase node, GameObject instance)
        {
            var t2ds = instance.GetComponentsInChildren<TextMeshProUGUI>(true);
            var t2d = Array.Find(t2ds, (item)=> item.gameObject.name == "t2dTitle");
            if (t2d == null) return;
            var nd = node.ChildrenNodes[0].ChildrenNodes[5].ChildrenNodes[0];
            if (nd is PsdNodeText textNode)
            {
                var text = textNode.content;
                var firstChar = text.Substring(0, 1);
                text = text.Substring(1);
                t2d.SetText($"<size=66>{firstChar}</size>{text}");
            }
        }

        private void SetTitleDesc(PsdNodeBase node, GameObject instance)
        {
            var t2ds = instance.GetComponentsInChildren<TextMeshProUGUI>(true);
            var t2d = Array.Find(t2ds, (item)=> item.gameObject.name == "t2dDecs");
            if (t2d == null) return;
            var nd = node.ChildrenNodes[0].ChildrenNodes[5].ChildrenNodes[1];
            if (nd is PsdNodeText textNode)
            {
                Json2PrefabFactory.SetTMPContent(t2d, textNode, true);
            }
        }
    }
}