using System;
using System.Collections;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
namespace Framework.View.Navigation.Editor
{
    public static class NavigationViewKit
    {
        /// <summary>
        /// 获取状态描述
        /// </summary>
        /// <param name="stateType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string GetStateDescribe(NavigationStateType stateType)
        {
            return stateType switch
            {
                NavigationStateType.None => "空",
                NavigationStateType.Open => "打开",
                NavigationStateType.Close => "关闭",
                NavigationStateType.Clear => "清理",
                _ => throw new ArgumentOutOfRangeException(nameof(stateType), stateType, null)
            };
        }

        /// <summary>
        /// 获取锁描述
        /// </summary>
        /// <param name="lockType"></param>
        /// <param name="describe"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string GetLockDescribe(NavigationLockType lockType, string describe = "")
        {
            //直接判断锁类型
            if (lockType == NavigationLockType.None)
                return "无锁";
            if (lockType == NavigationLockType.All)
                return "全部锁";
            if (lockType == NavigationLockType.AllExceptOpen)
                return "可打开全部锁";

            //通过计算是否包含某个锁类型来判断
            if (lockType.HasFlag(NavigationLockType.Open))
            {
                lockType &= ~NavigationLockType.Open;
                return GetLockDescribe(lockType, $"{describe}|打开锁");
            }

            if (lockType.HasFlag(NavigationLockType.Close))
            {
                lockType &= ~NavigationLockType.Close;
                return GetLockDescribe(lockType, $"{describe}|关闭锁");
            }

            if (lockType.HasFlag(NavigationLockType.Clear))
            {
                lockType &= ~NavigationLockType.Clear;
                return GetLockDescribe(lockType, $"{describe}|清理锁");
            }

            return describe;
        }

        /// <summary>
        /// 获取缓存状态描述
        /// </summary>
        /// <param name="clearType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string GetCacheDescribe(NavigationClearType clearType)
        {
            return clearType switch
            {
                NavigationClearType.Complete => "完整状态",
                NavigationClearType.ClearMemory => "清理内存",
                NavigationClearType.AllRecover => "全部还原",
                NavigationClearType.EntranceRecover => "入口还原",
                NavigationClearType.NoRecover => "不还原",
                _ => throw new ArgumentOutOfRangeException(nameof(clearType), clearType, null)
            };
        }

        /// <summary>
        /// 连接父节点和子节点
        /// </summary>
        /// <param name="parentView"></param>
        /// <param name="childView"></param>
        /// <param name="graphView"></param>
        public static void LineNode(Port parentView, Port childView, GraphView graphView)
        {
            //创建连线
            Edge edge = new Edge { output = parentView, input = childView };
            //添加连线
            edge.input.Connect(edge);
            edge.output.Connect(edge);
            graphView.AddElement(edge);
        }

        /// <summary>
        /// 向上遍历NavigationNodeView节点
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="action">遍历节点回调</param>
        public static void UpwardsTraverse(NavigationNodeView root, Action<NavigationNodeView> action)
        {
            if (root == null) return;
            foreach (var child in root.Child)
            {
                UpwardsTraverse(child, action);
            }

            action(root);
        }

        /// <summary>
        /// 延迟向上遍历NavigationNodeView节点
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="action">遍历节点回调</param>
        /// <param name="delayTime">单个遍历延迟(秒)</param>
        /// <returns></returns>
        public static IEnumerator DelayUpwardsTraverse(NavigationNodeView root,
            Action<NavigationNodeView> action, float delayTime)
        {
            WaitForSeconds wait = new WaitForSeconds(delayTime);
            if (root == null) yield return wait;
            foreach (var child in root.Child)
            {
                UpwardsTraverse(child, action);
            }

            action(root);
            yield return wait;
        }

        /// <summary>
        /// 获取同小于层级的最后一个节点
        /// </summary>
        /// <param name="root"></param>
        /// <param name="find"></param>
        /// <returns></returns>
        public static NavigationNodeView GetLastGreaterLayerNode(NavigationNodeView root, NavigationNodeView find)
        {
            NavigationNodeView retNode = null;
            bool isFind = false;
            UpwardsTraverse(root, node =>
            {
                if (node == find)
                    isFind = true;
                if (!isFind && node.CurLayer >= find.CurLayer)
                    retNode = node;
            });
            return retNode;
        }

        /// <summary>
        /// 获取NavigationBehaviour类型
        /// </summary>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        public static string GetBehaviourDes(NavigationBehaviour behaviour)
        {
            return behaviour switch
            {
                EditorNavigateContainer or NavigateContainer => "导航容器",
                EditorNavigationSceneLoader or NavigationSceneLoader => "场景",
                EditorNavigationFormLoader or NavigationFormLoader => "界面",
                _ => ""
            };
        }

        /// <summary>
        /// 获取NavigationBehaviour类型
        /// </summary>
        /// <param name="behaviourType"></param>
        /// <returns></returns>
        public static string GetBehaviourDes(Type behaviourType)
        {
            if (behaviourType == typeof(EditorNavigateContainer) || behaviourType == typeof(NavigateContainer))
                return "导航组";
            if (behaviourType == typeof(EditorNavigationSceneLoader) || behaviourType == typeof(NavigationSceneLoader))
                return "场景";
            if (behaviourType == typeof(EditorNavigationFormLoader) || behaviourType == typeof(NavigationFormLoader))
                return "界面";
            return "";
        }


        /// <summary>
        /// 创建一个纯色的Texture2D
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        /// <summary>
        /// 获取带颜色的BoxStyle
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static GUIStyle GetColoredBoxStyle(Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            style.normal.background = texture;
            return style;
        }

        /// <summary>
        /// 创建圆点
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static Texture2D CreateDot(int width, int height, Color col)
        {
            Texture2D dot = new Texture2D(width, height);

            for (int y = 0; y < dot.height; ++y)
            {
                for (int x = 0; x < dot.width; ++x)
                {
                    Color color =
                        ((x - dot.width / 2) * (x - dot.width / 2) + (y - dot.height / 2) * (y - dot.height / 2)) <
                        dot.width / 2 * dot.width / 2
                            ? col
                            : Color.clear;
                    dot.SetPixel(x, y, color);
                }
            }

            dot.Apply();
            return dot;
        }

        /// <summary>
        /// 绘制带边框的矩形
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="borderColor"></param>
        /// <param name="borderWidth"></param>
        public static void DrawBorderedRect(Rect rect, Color borderColor, int borderWidth)
        {
            // 绘制上、下、左、右边框
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, borderWidth), borderColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - borderWidth, rect.width, borderWidth), borderColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, borderWidth, rect.height), borderColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - borderWidth, rect.y, borderWidth, rect.height), borderColor);
        }

        /// <summary>
        /// 绘制搜索框
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <param name="space"></param>
        /// <returns></returns>
        public static string DrawSearch(string searchQuery, float space = 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(space);

            // 绘制放大镜图标
            GUIContent searchIcon = EditorGUIUtility.IconContent("Search Icon");
            GUILayout.Label(searchIcon, GUILayout.Width(20), GUILayout.Height(20));

            // 绘制搜索框
            searchQuery = EditorGUILayout.TextField(searchQuery, GUILayout.ExpandWidth(true));

            GUILayout.EndHorizontal();
            return searchQuery;
        }

        /// <summary>
        /// 绘制背景
        /// </summary>
        /// <param name="area"></param>
        /// <param name="color"></param>
        public static void DrawColoredBackgroundPanel(Rect area, Color color)
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            EditorGUI.DrawRect(area, color);
            GUI.backgroundColor = originalColor;
        }

        //打开样式
        private static readonly GUIContent _openGUIContent =
            new GUIContent(EditorGUIUtility.IconContent("d_greenLight"));

        //缓存样式
        private static readonly GUIContent
            _clearGUIContent = new GUIContent(EditorGUIUtility.IconContent("d_lightRim"));

        //异常样式
        private static readonly GUIContent _errorGUIContent =
            new GUIContent(EditorGUIUtility.IconContent("console.erroricon"));

        /// <summary>
        /// 获取状态GUIContent
        /// </summary>
        /// <param name="stateType"></param>
        /// <returns></returns>
        public static GUIContent GetStateGUIContent(NavigationStateType stateType)
        {
            return stateType switch
            {
                NavigationStateType.Open => _openGUIContent,
                NavigationStateType.Clear => _clearGUIContent,
                _ => _errorGUIContent
            };
        }

        /// <summary>
        /// 选择加载器
        /// </summary>
        /// <param name="loader"></param>
        public static void SelectLoader(NavigationLoader loader)
        {
            Selection.activeObject = loader.View;
        }

        /// <summary>
        /// 获取左对齐按钮样式
        /// </summary>
        /// <returns></returns>
        public static GUIStyle GetLeftAlignedButtonStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft
            };
            return style;
        }
    }
}
