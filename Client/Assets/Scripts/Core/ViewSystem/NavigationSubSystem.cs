using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Framework.Coverage;
using Framework.Touch;
using Framework.View;
using Framework.View.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.ViewSystem
{
    /// <summary>
    /// ViewSystem 中的导航子系统，继承 Framework.View.Navigation.NavigationManager，
    /// 管理全局导航容器对象。
    /// </summary>
    public class NavigationSubSystem : NavigationManager
    {
        public NavigationMemory NavMemory { get; private set; }
        public NavigationExceptionMgr NavError { get; private set; }

        public NavigateContainer GetLastContainer()
        {
            return Root.GetLastContainer();
        }

        public NavigateContainer FindContainer(string containerName)
        {
            foreach (var container in Root.ForeachContainers())
            {
                if (container.Name == containerName)
                {
                    return container;
                }
            }

            return null;
        }

        public NavigationLoader FindLoader(string loaderName)
        {
            return Root.FindLoader(loaderName, true, true);
        }

        public void SetLimitMemory(int memory)
        {
            NavMemory.LimitMemory = memory;
            NavMemory.ClearGroupMemory(Root.GetLastContainer(), Root);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append($"\n导航信息:{NavMemory} 当前帧:{Time.frameCount}\n");
            builder.Append("\n");
            foreach (var group in Root.ForeachContainers(TraversalOrder.Forward, includeSelf: false))
            {
                builder.Append(group);
            }

            return builder.ToString();
        }

        protected override void SetEventSystemEnable(bool enable)
        {
            // 统一触摸开关（对应参考项目 Package.Touch.TouchUtil.DisableEventSystem）。
            TouchUtil.DisableEventSystem(!enable);
        }

        public void Init(FormSubSystem formSubSystem, SceneSubSystem sceneSubSystem,
            Func<ITransition> defaultTransitionFactory = null)
        {
            base.Init(formSubSystem, sceneSubSystem, defaultTransitionFactory);

            NavMemory = new NavigationMemory();
            NavError = new NavigationExceptionMgr(Root);

            NavigateUtils.FormFullScreenJudge = FormFullScreenJudge;

            InitResidentContainers();
        }

        public void Update(float elapsed)
        {
            NavError.Update();
        }

        /// <summary>
        /// 异步关闭所有导航容器（KJ 的 ISystem.Shutdown 是同步的，此方法暂未接入，保留备用）。
        /// </summary>
        public async UniTask PreShutdownAsync()
        {
            foreach (var container in Root.ForeachContainers(TraversalOrder.Reverse, includeSelf: false))
            {
                container.LockType = NavigationLockType.None;
                container.EffectOther = false;
                await container.Close();
            }
        }

        public void Shutdown()
        {
            BeforeContainerStateChange.Clear();
            AfterContainerStateChange.Clear();
            BeforeLoaderStateChange.Clear();
            AfterLoaderStateChange.Clear();

            NavigationFactory.Instance.Release();

            ClearResidentContainers();

            base.ShutDown();
        }

        #region 常驻导航容器

        private const string RESIDENT_PARENT = "Resident";
        private const string CORE_RESIDENT = "Resident.Core";
        private const string GENERAL_RESIDENT = "Resident.General";
        private const string PROJECT_RESIDENT = "Resident.Project";

        public static NavigateContainer ResidentParent { get; private set; }
        public static NavigateContainer CoreResident { get; private set; }
        public static NavigateContainer GeneralResident { get; private set; }
        public static NavigateContainer ProjectResident { get; private set; }

        private void InitResidentContainers()
        {
            ResidentParent = CreateContainer(RESIDENT_PARENT);
            ResidentParent.LockType = NavigationLockType.All | NavigationLockType.Single;

            CoreResident = CreateContainer(CORE_RESIDENT);
            CoreResident.LockType = NavigationLockType.All | NavigationLockType.Single;

            GeneralResident = CreateContainer(GENERAL_RESIDENT);
            GeneralResident.LockType = NavigationLockType.All | NavigationLockType.Single;

            ProjectResident = CreateContainer(PROJECT_RESIDENT);
            ProjectResident.LockType = (NavigationLockType.All | NavigationLockType.Single) ^ NavigationLockType.Open;

            Root.AddChildContainer(ResidentParent);
            ResidentParent.AddChildContainer(CoreResident);
            ResidentParent.AddChildContainer(GeneralResident);
            ResidentParent.AddChildContainer(ProjectResident);
        }

        private void ClearResidentContainers()
        {
            ResidentParent = null;
            CoreResident = null;
            GeneralResident = null;
            ProjectResident = null;
        }

        #endregion

        /// <summary>
        /// 全屏界面判断（对应参考项目 FormFullScreenJudge）。
        /// 通过界面上的 BaseCoverage 判断是否铺满全屏，供导航渲染优化使用。
        /// </summary>
        public static bool FormFullScreenJudge(BaseForm form)
        {
            var coverage = form != null ? form.gameObject.GetComponent<BaseCoverage>() : null;
            return coverage != null && coverage.IsFull();
        }
    }
}
