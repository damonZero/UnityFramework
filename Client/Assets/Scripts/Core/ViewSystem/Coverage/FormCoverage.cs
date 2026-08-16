//**************************************************************************************
//Create By szx on 2019/11/14
//
//@Description 界面显示对象,依赖于 BaseForm,用于处理界面之间的遮挡关系
//**************************************************************************************

using System.Text;
using Cysharp.Threading.Tasks;
using Framework.Coverage;
using Framework.View;
using UnityEngine;

namespace Core.ViewSystem
{
    /// <summary>
    /// 界面遮挡组件（对应参考项目 ScriptsC#/Core/ViewSystem/Coverage/FormCoverage）。
    /// 当界面被上层全屏界面遮挡时，自动隐藏本界面（渲染优化）。
    /// </summary>
    [RequireComponent(typeof(BaseForm))]
    public class FormCoverage : CanvasCoverage
    {
        private BaseForm _form;

        /// <summary>
        /// 界面显隐控制器：当界面被遮挡时隐藏界面
        /// </summary>
        public VisibleController FormCoverageVisibleController { get; } =
            new(nameof(FormCoverage), FormVisibleStrategyByCanvas.Shared);

        private bool _visibleStateExcept;
        private bool _hasRegisterListener;
        private IntRect _lastRange;
        [SerializeField] private bool _ignoreFullScreenCheck;

        public override int CoverageIdx => _form.Layer;

        protected override bool RegisterOnStart => false;

        /// <summary>是否忽略全屏检测</summary>
        public bool IgnoreFullScreenCheck
        {
            get => _ignoreFullScreenCheck;
            set => _ignoreFullScreenCheck = value;
        }

        protected override bool ActualRendering => _form.Rendering;

        protected override void Awake()
        {
            // 界面的可见性由多个 VisibleController 控制，所以只在界面 PostShow 和 PostHide 时刷新 CoverageChild
            // FormCoverage 的 _visible 属性改变时，不刷新 CoverageChild，避免 CoverageChild 状态不正确
            _refreshChildrenWhenVisibleChanged = false;

            base.Awake();
            _form = GetComponent<BaseForm>();
            _form.InstanceLifeCycleEvents.PostShow.Add(OnFormPostShow);
            _form.InstanceLifeCycleEvents.PostHide.Add(OnFormPostHide);
            _form.InstanceLifeCycleEvents.PreClose.Add(OnFormPreClose);
        }

        protected override bool Init()
        {
            if (base.Init())
            {
                _lastRange = Holder.Range;
                return true;
            }

            return false;
        }

        protected override void DoSetVisible(bool visible)
        {
            _form.SetVisibleState(FormCoverageVisibleController, visible).Forget(UnityEngine.Debug.LogException);
        }

        private void OnFormLayerChange(BaseForm form, int oldLayer, int newLayer)
        {
            InvokeLayerChangeEvt();
        }

        private void OnFormOrderChange(BaseForm form)
        {
            InvokeLayerChangeEvt();
        }

        private void OnVisibleControllerChanged(ViewBase form, VisibleController controller, VisibleControllerState state)
        {
            if (controller == FormCoverageVisibleController) return;

            var newState = _form.IsVisibleExcept(FormCoverageVisibleController);
            if (_visibleStateExcept != newState)
            {
                _visibleStateExcept = newState;
                InvokeNeedAdjustEvt();
            }
        }

        private void OnFormPostShow()
        {
            if (_lastRange != Holder.Range)
            {
                ReInit();
            }

            if (!_hasRegisterListener)
            {
                _hasRegisterListener = true;
                Holder.Register(this);
                _visibleStateExcept = _form.IsVisibleExcept(FormCoverageVisibleController);
                _form.LayerChanged += OnFormLayerChange;
                _form.VisibleControllerChanged += OnVisibleControllerChanged;
            }

            RefreshChildren(ActiveAndRendering);
        }

        private void OnFormPostHide()
        {
            RefreshChildren(ActiveAndRendering);
        }

        private void OnFormPreClose()
        {
            if (_hasRegisterListener)
            {
                Holder.UnRegister(this);
                _form.LayerChanged -= OnFormLayerChange;
                _form.VisibleControllerChanged -= OnVisibleControllerChanged;
                _hasRegisterListener = false;
            }
        }

        protected override void OnEnable()
        {
        }

        protected override void OnDisable()
        {
        }

        /// <summary>
        /// 调试信息
        /// </summary>
        public override string DebugInfo()
        {
            var goName = gameObject.name;
            if (goName.EndsWith("(Clone)"))
                goName = goName.Substring(0, goName.Length - 7);
            var sb = new StringBuilder("[");
            foreach (var rect in ShowRectList)
            {
                sb.Append(rect.ToSimpleString());
                sb.Append(", ");
            }

            sb.Append("]");

            var showListStr = sb.ToString();
            sb.Clear();
            sb.Append("[");
            foreach (var rect in CoverRectList)
            {
                sb.Append(rect.ToSimpleString());
                sb.Append(", ");
            }

            sb.Append("]");

            var coverListStr = sb.ToString();

            var coverageState = _form.GetVisibleState(FormCoverageVisibleController);
            return
                $"界面[{goName}]->  屏蔽渲染状态:{coverageState}  可见状态:{ActiveAndRendering}  " +
                $"显示区域:{showListStr}  遮挡区域:{coverListStr}";
        }
    }
}
