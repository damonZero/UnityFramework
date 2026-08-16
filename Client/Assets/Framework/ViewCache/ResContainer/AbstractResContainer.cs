using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.ViewCache
{
    public abstract class AbstractResContainer<T> : AbstractCommonContainer<string, T>
    {
        protected readonly Transform _cacheRoot;

        protected AbstractResContainer(Transform cacheRootParent)
        {
            if (cacheRootParent == null)
            {
                throw new ArgumentNullException($"{nameof(cacheRootParent)}");
            }


            var root = new GameObject(nameof(AbstractResContainer<T>));
            root.SetActive(false);

            _cacheRoot = root.transform;
            _cacheRoot.SetParent(cacheRootParent, false);
        }

        protected override void OnPutInContainer(T instance)
        {
            var transform = GetTransform(instance);
            transform.SetParent(_cacheRoot, false);
        }


        protected abstract Transform GetTransform(T instance);
    }
}
