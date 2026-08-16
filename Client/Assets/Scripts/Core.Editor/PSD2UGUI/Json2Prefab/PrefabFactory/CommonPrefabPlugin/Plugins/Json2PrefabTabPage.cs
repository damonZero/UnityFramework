using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Package.PSD2UGUI.Plugins
{
    public abstract class Json2PrefabTabPageBase : Json2PrefabCommonPrefabPluginBase
    {
        public Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;
        public Json2PrefabEnum UseSelfSize => Json2PrefabEnum.WidthHeight;
        public bool IsInterruption => true;

        // 解析TabPage节点信息
        public void ParseTabPage(PsdNodeBase node, GameObject instance)
        {
            var children = node.ChildrenNodes;
            if (children is not { Count: > 0 })
                return;

            int childCount = instance.transform.childCount;
            var firstObj = instance.transform.GetChild(0);
            int index = 0;
            GameObject obj = null;
            foreach (var child in children)
            {
                if (index >= childCount)
                {
                    // 生成新的子节点
                    obj = GameObject.Instantiate(firstObj.gameObject, instance.transform);
                    obj.name = firstObj.name;
                }
                else
                {
                    obj = instance.transform.GetChild(index).gameObject;
                }

                ParseTabPageButton(child, obj);
                index++;
            }
        }

        //解析TabPage按钮节点信息 第一个子节点下为高亮选中信息 第二个节点为常态信息
        protected virtual void ParseTabPageButton(PsdNodeBase node, GameObject instance)
        {
            // 解析高亮选中信息
            PsdNodeBase lightNode = node.ChildrenNodes[1];
            var lightIconNode = lightNode.ChildrenNodes[0] as PsdNodeImage;
            lightIconNode.assetPath = Json2PrefabParseTool.ParseImgPath(lightIconNode, lightNode);
            var lightImage = AssetDatabase.LoadAssetAtPath<Sprite>(lightIconNode.assetPath);

            // 解析常态信息
            PsdNodeBase normalNode = node.ChildrenNodes[0];
            var normalIconNode = normalNode.ChildrenNodes[0] as PsdNodeImage;
            normalIconNode.assetPath = Json2PrefabParseTool.ParseImgPath(normalIconNode, normalNode);
            var normalImage = AssetDatabase.LoadAssetAtPath<Sprite>(normalIconNode.assetPath);

            //设置按钮图片
            var icon = instance.GetComponentInChildren<Image>(true);
            icon.sprite = normalImage;

            //tab组件不同状态显示图片
            var tabPage = instance.GetComponentInChildren<Selectable>(true);
            tabPage.transition = Selectable.Transition.SpriteSwap;
            var tabPageSpriteState = tabPage.spriteState;
            tabPageSpriteState.disabledSprite = normalImage;
            tabPageSpriteState.highlightedSprite = lightImage;
            tabPageSpriteState.selectedSprite = lightImage;
            tabPageSpriteState.pressedSprite = lightImage;
            tabPage.spriteState = tabPageSpriteState;

            //解析按钮文字
            if (normalNode.ChildrenNodes.Count < 2)
            {
                return;
            }

            var t2d = instance.GetComponentInChildren<TextMeshProUGUI>(true);
            var t2dNode = normalNode.ChildrenNodes[1];
            Json2PrefabFactory.SetTMPContent(t2d, t2dNode as PsdNodeText);
        }
    }

    // 分页按钮列表 固定位置
    public class Json2PrefabTabPageListFixed : Json2PrefabTabPageBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "BottomPages"
        };

        public Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.XY;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            ParseTabPage(node, instance);
        }
        
        protected override void ParseTabPageButton(PsdNodeBase node, GameObject instance)
        {

            PsdNodeBase normalNode = node.ChildrenNodes[0];
            //解析按钮文字
            if (normalNode.ChildrenNodes.Count < 2)
            {
                return;
            }

            var t2d = instance.GetComponentInChildren<TextMeshProUGUI>(true);
            var newList = new List<PsdNodeBase>(normalNode.ChildrenNodes);
            newList.RemoveAt(0);
            
            Json2PrefabFactory.SetTMPContentBottomPageButton(t2d, newList.ToArray() as PsdNodeText[]);
        }
    }

    // 分页按钮列表 位置不固定
    public class Json2PrefabTabPageList : Json2PrefabTabPageBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "TopPages"
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            ParseTabPage(node, instance);
        }
    }
    
    // 分页按钮列表 位置不固定
    public class Json2PrefabTopPages_02List : Json2PrefabTabPageBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "TopPages_02"
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            ParseTabPage(node, instance);
        }
        
        //解析TabPage按钮节点信息 第一个子节点下为高亮选中信息 第二个节点为常态信息
        protected virtual void ParseTabPageButton(PsdNodeBase node, GameObject instance)
        {
            // 解析高亮选中信息
            PsdNodeBase lightNode = node.ChildrenNodes[1];
            var lightIconNode = lightNode.ChildrenNodes[2] as PsdNodeImage;
            lightIconNode.assetPath = Json2PrefabParseTool.ParseImgPath(lightIconNode, lightNode);
            var lightImage = AssetDatabase.LoadAssetAtPath<Sprite>(lightIconNode.assetPath);

            // 解析常态信息
            PsdNodeBase normalNode = node.ChildrenNodes[0];
            var normalIconNode = normalNode.ChildrenNodes[2] as PsdNodeImage;
            normalIconNode.assetPath = Json2PrefabParseTool.ParseImgPath(normalIconNode, normalNode);
            var normalImage = AssetDatabase.LoadAssetAtPath<Sprite>(normalIconNode.assetPath);

            //tab组件不同状态显示图片
            var tabPage = instance.GetComponentInChildren<Selectable>(true);
            tabPage.transition = Selectable.Transition.SpriteSwap;
            //设置按钮图片
            tabPage.targetGraphic.GetComponent<Image>().sprite = normalImage;
            
            var tabPageSpriteState = tabPage.spriteState;
            tabPageSpriteState.disabledSprite = normalImage;
            tabPageSpriteState.highlightedSprite = lightImage;
            tabPageSpriteState.selectedSprite = lightImage;
            tabPageSpriteState.pressedSprite = lightImage;
            tabPage.spriteState = tabPageSpriteState;

            //解析按钮文字
            if (normalNode.ChildrenNodes.Count < 2)
            {
                return;
            }

            var t2d = instance.GetComponentInChildren<TextMeshProUGUI>(true);
            var t2dNode = normalNode.ChildrenNodes[1];
            Json2PrefabFactory.SetTMPContent(t2d, t2dNode as PsdNodeText);
        }
    }

    // 弹窗页签定制化解析
    public class Json2PrefabTabPagePopupPanel : Json2PrefabTabPageBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "BigPopupPanelPages", "MiddlePopupPanelPages"
        };

        public Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.XY;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            ParseTabPage(node, instance, 1);
        }

        public void ParseTabPage(PsdNodeBase node, GameObject instance, int startIndex = 0)
        {
            var children = node.ChildrenNodes;
            if (children is not { Count: > 0 })
                return;

            int childCount = instance.transform.childCount;
            var firstObj = instance.transform.GetChild(0);
            int index = 0;
            GameObject obj = null;
            foreach (var child in children)
            {
                if (startIndex > index)
                {
                    index++;
                    continue;
                }

                if (index >= (childCount - 1))
                {
                    // 生成新的子节点
                    obj = GameObject.Instantiate(firstObj.gameObject, instance.transform);
                    obj.name = firstObj.name;
                }
                else
                {
                    obj = instance.transform.GetChild(index).gameObject;
                }

                ParseTabPageButton(child, obj);
                index++;
            }
        }

        //解析TabPage按钮节点信息 第一个子节点下为高亮选中信息 第二个节点为常态信息
        protected override void ParseTabPageButton(PsdNodeBase node, GameObject instance)
        {
            // 解析高亮选中信息
            PsdNodeBase lightNode = node.ChildrenNodes[1];
            var lightIconNode = lightNode.ChildrenNodes[2] as PsdNodeImage;
            lightIconNode.assetPath = Json2PrefabParseTool.ParseImgPath(lightIconNode, lightNode);
            var lightImage = AssetDatabase.LoadAssetAtPath<Sprite>(lightIconNode.assetPath);

            // 解析常态信息
            PsdNodeBase normalNode = node.ChildrenNodes[0];
            var normalIconNode = normalNode.ChildrenNodes[1] as PsdNodeImage;
            normalIconNode.assetPath = Json2PrefabParseTool.ParseImgPath(normalIconNode, normalNode);
            var normalImage = AssetDatabase.LoadAssetAtPath<Sprite>(normalIconNode.assetPath);

            //tab组件不同状态显示图片
            var tabPage = instance.GetComponentInChildren<Selectable>(true);
            tabPage.transition = Selectable.Transition.SpriteSwap;
            var tabPageSpriteState = tabPage.spriteState;
            tabPageSpriteState.disabledSprite = normalImage;
            tabPageSpriteState.highlightedSprite = lightImage;
            tabPageSpriteState.selectedSprite = lightImage;
            tabPageSpriteState.pressedSprite = lightImage;
            tabPage.spriteState = tabPageSpriteState;

            //设置按钮图片
            var icon = tabPage.targetGraphic.GetComponent<Image>();
            icon.sprite = normalImage;

            //解析按钮文字
            if (normalNode.ChildrenNodes.Count < 2)
            {
                return;
            }

            var t2d = instance.GetComponentInChildren<TextMeshProUGUI>(true);
            var t2dNode = normalNode.ChildrenNodes[0];
            Json2PrefabFactory.SetTMPContent(t2d, t2dNode as PsdNodeText);
        }
    }
}