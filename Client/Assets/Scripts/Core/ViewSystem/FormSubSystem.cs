using System.Collections.Generic;
using System.Threading;
using Core.UI;
using Cysharp.Threading.Tasks;
using Framework.Asset;
using Framework.Log;
using Framework.View;
using MessagePipe;
using UnityEngine;

namespace Core.ViewSystem
{
    /// <summary>
    /// ViewSystem 中的界面子系统：界面的开启、缓存与层级管理。
    /// 继承 Framework.View.FormManager，补上资源加载注入点（InstantiateForm），
    /// 并把 Form 生命周期事件转发为全局 GameEvent（对应参考项目 EventManager 的 EventKey 转发）。
    /// </summary>
    public sealed class FormSubSystem : FormManager
    {
        public static FormSubSystem Instance => ViewSystem.FormSubSystem;

        private IAssetSystem _assetSystem;
        private IPublisher<FormLifecycleEvent> _publisher;

        public void Init(RectTransform uiRoot, IAssetSystem assetSystem, IPublisher<FormLifecycleEvent> publisher)
        {
            _assetSystem = assetSystem;
            _publisher = publisher;
            base.Init(uiRoot);

            if (_publisher != null)
            {
                BindLifecycleEvents();
            }
        }

        /// <summary>
        /// 把 FormManager 的 14 个 C# 生命周期事件转发为全局 FormLifecycleEvent。
        /// 事件在 FormManager.Shutdown 时被整体置空，无需在此显式解绑。
        /// </summary>
        private void BindLifecycleEvents()
        {
            FormPreAwake += f => _publisher.Publish(new FormLifecycleEvent(FormLifecyclePhase.PreAwake, f));
            FormPostAwake += f => _publisher.Publish(new FormLifecycleEvent(FormLifecyclePhase.PostAwake, f));
            FormPreDestroy += f => _publisher.Publish(new FormLifecycleEvent(FormLifecyclePhase.PreDestroy, f));
            FormPostDestroy += f => _publisher.Publish(new FormLifecycleEvent(FormLifecyclePhase.PostDestroy, f));

            FormPreOpen += f => _publisher.Publish(new FormLifecycleEvent(FormLifecyclePhase.PreOpen, f));
            FormPostOpen += f => _publisher.Publish(new FormLifecycleEvent(FormLifecyclePhase.PostOpen, f));
            FormPreShow += f => _publisher.Publish(new FormLifecycleEvent(FormLifecyclePhase.PreShow, f));
            FormPostShow += f => _publisher.Publish(new FormLifecycleEvent(FormLifecyclePhase.PostShow, f));
            FormPreHide += f => _publisher.Publish(new FormLifecycleEvent(FormLifecyclePhase.PreHide, f));
            FormPostHide += f => _publisher.Publish(new FormLifecycleEvent(FormLifecyclePhase.PostHide, f));
            FormPreClose += f => _publisher.Publish(new FormLifecycleEvent(FormLifecyclePhase.PreClose, f));
            FormPostClose += f => _publisher.Publish(new FormLifecycleEvent(FormLifecyclePhase.PostClose, f));

            FormLayerChanged += (f, oldLayer, newLayer) =>
                _publisher.Publish(new FormLifecycleEvent(f, oldLayer, newLayer));
            FormRenderingChanged += f =>
                _publisher.Publish(new FormLifecycleEvent(FormLifecyclePhase.RenderingChanged, f));
        }

        /// <summary>
        /// 异步关闭所有运行中的界面（KJ 的 ISystem.Shutdown 是同步的，此方法暂未接入，保留备用）。
        /// </summary>
        public async UniTask PreShutdownAsync()
        {
            var closeTasks = new List<UniTask>();
            foreach (var form in FindForms<BaseForm>())
            {
                if (form != null && form.Running)
                {
                    var args = new LifeCycleArgs(LifeCycleCause.Close);
                    closeTasks.Add(form.LifeCycleExecutor.LifeCycleExecuteClose(form, args));
                }
            }

            if (closeTasks.Count > 0)
            {
                await UniTask.WhenAll(closeTasks);
            }
        }

        protected override async UniTask<GameObject> InstantiateForm(
            string assetName, Transform parent, CancellationToken cancellationToken)
        {
            // KJ 的 IAssetSystem 无取消支持，取消语义由 FormManager.LoadForm 的二次检查兜底。
            _ = cancellationToken;

            var prefab = await _assetSystem.LoadAssetAsync<GameObject>(assetName);
            if (prefab == null)
            {
                GameLog.Error($"InstantiateForm 资源加载失败: {assetName}", module: "Core.ViewSystem");
                return null;
            }

            var go = Object.Instantiate(prefab, parent);

            // 分辨率适配：归一化界面 RectTransform（对应参考项目 FormSubSystem.InstantiateForm）。
            ScreenHelper.AdaptResolution(go.transform as RectTransform);

            return go;
        }
    }
}
