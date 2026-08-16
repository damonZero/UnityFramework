// ********************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// ********************************************************************

using System;
using Cysharp.Threading.Tasks;
using Framework.ViewCache;
using UnityEngine;

namespace Framework.View
{
    public class FormResContainer : AbstractResContainer<BaseForm>
    {
        public FormResContainer(Transform cacheRootParent) : base(cacheRootParent)
        {
        }

        protected override Transform GetTransform(BaseForm instance)
        {
            return instance.transform;
        }

        protected override BaseForm Instance(string assetName)
        {
            // throw new NotSupportedException($"不支持在缓存容器中直接实例化form，form的实例化由界面管理器实现！");
            return null;
        }

        public override UniTask<BaseForm> InstanceAsync(string assetName)
        {
            throw new NotSupportedException($"不支持在缓存容器中直接实例化form，form的实例化由界面管理器实现！");
        }

        public override void DestroyObj(BaseForm form)
        {
            form.DestroySelf();
        }
    }
}
