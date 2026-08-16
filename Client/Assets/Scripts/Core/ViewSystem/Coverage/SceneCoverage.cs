using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Framework.Coverage;
using Framework.Log;
using Framework.View;
using Framework.View.Navigation;
using UnityEngine;

namespace Core.ViewSystem
{
    /// <summary>
    /// 场景遮挡组件（对应参考项目 SceneCoverage）。
    /// 当场景被上层全屏界面遮挡时，自动隐藏场景（渲染优化）。
    /// </summary>
    [RequireComponent(typeof(BaseScene))]
    public class SceneCoverage : BaseCoverage
    {
        /// <summary>
        /// 控制显隐策略类型
        /// </summary>
        public enum VisibleStrategy
        {
            CameraCoverage = 1, // 通过相机上的 CameraCoverage 来控制
            RootGameObjectsEnable = 2, // 改变场景所有根节点的 Enable
        }

        #region private fields

        [SerializeField, Header("场景被遮挡的显隐策略")]
        private VisibleStrategy _visibleStrategy = VisibleStrategy.CameraCoverage;

        private BaseScene _scene;
        private IntRect[] _showRects;
        private IntRect[] _coverRects;
        private RectSide[] _verticalSides;

        private bool _visibleStateExcept;
        private bool _hasRegisterListener;
        private IntRect _lastRange;

        #endregion

        #region public properties

        /// <summary>
        /// 场景显隐控制器：当场景被遮挡时隐藏场景
        /// </summary>
        public VisibleController SceneCoverageVisibleController { get; private set; }

        /// <summary>
        /// 场景被遮挡的显隐控制策略
        /// </summary>
        public VisibleStrategy SceneVisibleStrategy => _visibleStrategy;

        public override int CoverageIdx => -1;

        protected override bool ActualRendering => _scene.Rendering;

        public override IEnumerable<IntRect> ShowRectList => _showRects;
        public override IEnumerable<IntRect> CoverRectList => _coverRects;
        public override IList<RectSide> VerticalSideList => _verticalSides;
        public override IList<RectSide> HorizontalSideList => throw new Exception("暂时只用竖直方向的边");

        #endregion

        protected override bool RegisterOnStart => false;

        protected override bool Init()
        {
            // 场景有且只有一个显示区域，即为 UI 设计尺寸，没有遮挡区域
            _showRects = new[] { Holder.Range };
            _coverRects = Array.Empty<IntRect>();
            _verticalSides = Array.Empty<RectSide>();
            _lastRange = Holder.Range;

            IVisibleStrategy strategy = _visibleStrategy switch
            {
                // 当通过 CameraCoverage 控制显隐时，显隐完全交由 CameraCoverage 组件，这里不需要做任何事情
                VisibleStrategy.CameraCoverage => null,
                VisibleStrategy.RootGameObjectsEnable => SceneVisibleStrategyByRootGameObjects.Shared,
                _ => throw new ArgumentOutOfRangeException()
            };
            SceneCoverageVisibleController = new VisibleController(nameof(SceneCoverage), strategy);
            return true;
        }

        protected override void DoSetVisible(bool visible)
        {
            _scene.SetVisibleState(SceneCoverageVisibleController, visible).Forget();
        }

        private void ReInitIfRangeChanged()
        {
            if (_lastRange != Holder.Range)
                ReInit();
        }

        public override string DebugInfo()
        {
            var sceneName = gameObject.scene.name;
            var coverageState = _scene.GetVisibleState(SceneCoverageVisibleController);
            return $"场景[{sceneName}]->  屏蔽渲染状态:{coverageState}  可见状态:{ActiveAndRendering}";
        }

        protected override void Awake()
        {
            base.Awake();
            _scene = GetComponent<BaseScene>();
            _scene.InstanceLifeCycleEvents.PostShow.Add(OnScenePostShow);
        }

        private void OnVisibleControllerChanged(ViewBase scene, VisibleController controller,
            VisibleControllerState state)
        {
            if (controller == SceneCoverageVisibleController) return;

            var newState = _scene.IsVisibleExcept(SceneCoverageVisibleController);
            if (_visibleStateExcept != newState)
            {
                _visibleStateExcept = newState;
                InvokeNeedAdjustEvt();
            }
        }

        private void OnScenePostShow()
        {
            ReInitIfRangeChanged();

            if (_hasRegisterListener) return;

            _hasRegisterListener = true;
            var result = Holder.Register(this);
            GameLog.Debug($"Register '{name}' -> {result}, _scene:{_scene}", module: nameof(SceneCoverage));

            if (!result)
            {
                GameLog.Error($"SceneCoverage Register failed! scene:{_scene}, coverage:{this}", module: nameof(SceneCoverage));
                return;
            }

            _visibleStateExcept = _scene.IsVisibleExcept(SceneCoverageVisibleController);
            _scene.VisibleControllerChanged += OnVisibleControllerChanged;
            _scene.InstanceLifeCycleEvents.PostClose.Add(OnScenePostClose);
        }

        private void OnScenePostClose()
        {
            if (!_hasRegisterListener) return;

            _scene.VisibleControllerChanged -= OnVisibleControllerChanged;
            _scene.InstanceLifeCycleEvents.PostClose.Remove(OnScenePostClose);
            _hasRegisterListener = false;

            var result = Holder.UnRegister(this);
            GameLog.Debug($"UnRegister '{name}' -> {result}, _scene:{_scene}", module: nameof(SceneCoverage));
            if (!result)
            {
                GameLog.Error($"SceneCoverage UnRegister failed! scene:{_scene}, coverage:{this}", module: nameof(SceneCoverage));
            }
        }

        protected override void OnEnable()
        {
        }

        protected override void OnDisable()
        {
        }
    }
}
