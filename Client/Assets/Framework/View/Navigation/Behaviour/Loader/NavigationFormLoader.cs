//**************************************************************************************
//Create By Liangc on 2023/11/15
//导航界面的加载器
//@Description 负责实现导航生命周期，以及与Form的交互
//**************************************************************************************

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace Framework.View.Navigation
{
    public class NavigationFormLoader : NavigationLoader
    {
        /// <summary>
        /// 界面的排序层级
        /// </summary>
        private int _layer;

        /// <summary>
        /// 实现 <see cref="NavigationLoader.Layer"/>：返回界面的排序层级。
        /// </summary>
        public override int Layer => _layer;

        /// <summary>
        /// 管理界面的Form对象
        /// </summary>
        public BaseForm Form => View as BaseForm;

        /// <summary>
        /// 界面的加载和开启参数
        /// </summary>
        public NavigateFormOptions FormOptions { get; set; }

        /// <summary>
        /// 实现 INavigateOptions.ViewOptions
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public override INavigateOptions ViewOptions
        {
            get => FormOptions;
            internal set {
                if (value is NavigateFormOptions options)
                {
                    FormOptions = options;
                }
                else
                {
                    throw new ArgumentException($"{nameof(ViewOptions)} must be {nameof(NavigateFormOptions)}, but got {value}");
                }
            }
        }

        /// <summary>
        /// 界面占用内存
        /// </summary>
        public override int Memory { get; protected set; } = 10;


        /// <summary>
        /// 是否逻辑可见
        /// </summary>
        public override bool LogicalVisible => Form is { LogicalVisible: true };

        /// <summary>
        /// 是否渲染
        /// </summary>
        public override bool Rendering => Form is { Rendering: true };

        //是否为全屏界面(-1为未判断,0为false,1为true)
        private short _isFullScreen = -1;

        /// <summary>
        /// 是否为全屏
        /// </summary>
        /// <returns></returns>
        public override bool IsFullScreen()
        {
            if (Form == null) return false;
            if (_isFullScreen == -1)
                _isFullScreen = (short)(NavigateUtils.FormFullScreenJudge(Form) ? 1 : 0);
            var isFullScreen = _isFullScreen == 1;
            return isFullScreen;
        }

        /// <summary>
        /// 是否全屏且逻辑可见
        /// </summary>
        public override bool FullScreenAndLogicalVisible()
        {
            return LogicalVisible && IsFullScreen();
        }

        /// <summary>
        /// 是否全屏且正在渲染
        /// </summary>
        public override bool FullScreenAndRendering()
        {
            return Rendering && IsFullScreen();
        }

        /// <summary>
        /// INavigationLoader：打开界面
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override async UniTask<ViewBase> DoOpenOnly(CancellationToken cancellationToken = default)
        {
            var view = await base.DoOpenOnly(cancellationToken);

            if (view is BaseForm form)
            {
                _layer = form.Layer; // FIXME by fred 不在这里赋値Layer，仅在清理时赋値，避免界面Layer变化后不同步
            }

            return view;
        }

        /// <summary>
        /// INavigationLoader：重置
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _layer = 0;
            _isFullScreen = -1;
        }

        /// <summary>
        /// 还原之前的设置
        /// </summary>
        internal override void BeforeSetRecover()
        {
            base.BeforeSetRecover();

            //还原时使用之前的Layer,保证层级正确性
            _layer = -_layer;
        }

        public override string ToString()
        {
            if (Form == null)
                return base.ToString();
            return $"{base.ToString()}, Layer:{Form.Layer}, Rendering={Form.Rendering}, " +
                   $"LogicalVisible:{Form.LogicalVisible} ,Entrance:{Entrance}";
        }
    }
}
