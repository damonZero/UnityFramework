// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

using Framework.Log;
using System;
using UnityEngine;
namespace Framework.View
{
    [RequireComponent(typeof(Canvas))]
    public class BaseForm : ViewBase
    {
        #region public: 属性

        /// <summary>
        /// 此界面的层级序号
        /// </summary>
        public int Layer
        {
            get => _layer;
            set
            {
                Debug.Assert(value >= 0, $"{name}: Layer must be >= 0, got {value}");
                if (_layer == value) return;

                var oldLayer = _layer;
                _layer = value;

                if (oldLayer < 0) return;

                try
                {
                    LayerChanged?.Invoke(this, oldLayer, _layer);
                }
                catch (Exception e)
                {
                    GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                }
            }
        }

        /// <summary>
        /// 渲染排序 canvas.sortingOrder
        /// </summary>
        public int SortingOrder
        {
            get => Canvas.sortingOrder;
            set
            {
                if (Canvas.sortingOrder == value) return;
                Canvas.overrideSorting = true;
                Canvas.sortingOrder = value;

                try
                {
                    SortingOrderChanged?.Invoke(this);
                }
                catch (Exception e)
                {
                    GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                }
            }
        }

        // ReSharper disable once IdentifierTypo
        public Canvas Canvas { get; private set; }

        /// <summary>
        /// 界面的逻辑可见性控制器
        /// </summary>
        public static VisibleController FormLogicalVisibleController { get; } = new (
            nameof(FormLogicalVisibleController), FormVisibleStrategyByCanvas.Shared);

        public override VisibleController LogicalVisibleController => FormLogicalVisibleController;

        #endregion

        #region public: 属性变化事件


        /// <summary>
        /// 触发时机：Layer属性改变之后
        /// </summary>
        public event Action<BaseForm, int, int> LayerChanged;

        /// <summary>
        /// 触发时机：canvas的sortingOrder改变过后
        /// </summary>
        public event Action<BaseForm> SortingOrderChanged;

        #endregion


        #region protected: 生命周期方法

        protected override void OnViewAwake()
        {
            Canvas = GetComponent<Canvas>();
            if (Canvas == null)
            {
                GameLog.Error($"{name}: Missing required Canvas component!", module: "Framework.View");
                return;
            }
            Canvas.overrideSorting = true;

            OnFormAwake();
        }

        protected virtual void OnFormAwake()
        {
        }

        protected override void OnViewDestroy()
        {
            OnFormDestroy();
        }

        protected virtual void OnFormDestroy()
        {
        }

        #endregion


        #region protected：字段

        private int _layer = -1; // 此界面的层级序号

        #endregion

        #region internal：方法

        /// <summary>
        /// 销毁自身GameObject（只应该由界面管理相关模块调用，其他系统、业务不应该调用）
        /// </summary>
        internal void DestroySelf()
        {
            Destroy(gameObject);
        }

        #endregion


        #region 其它

        protected override IVisibleStrategy CreateDefaultVisibleStrategy()
        {
            return FormVisibleStrategyByCanvas.Shared;
        }

        public override string ToString()
        {
            return $"{GetType().Name}({AssetName}, CurrentPhase:{CurrentPhase}, PendingPhase:{PendingPhase}, Rendering:{Rendering}, " +
                   $"Layer:{Layer}, SortingOrder:{SortingOrder})";
        }

        #endregion
    }
}
