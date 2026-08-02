using System;
using UnityEngine;

namespace Framework.TestKit.Fixtures
{
    public sealed class TestGameObjectRoot : IDisposable
    {
        public TestGameObjectRoot(string name = "TestRoot")
        {
            GameObject = new GameObject(name);
            Transform = GameObject.transform;
        }

        public GameObject GameObject { get; }
        public Transform Transform { get; }

        public GameObject CreateChild(string name = "TestChild")
        {
            var child = new GameObject(name);
            child.transform.SetParent(Transform, false);
            return child;
        }

        public void Dispose()
        {
            if (GameObject == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(GameObject);
            else
                UnityEngine.Object.DestroyImmediate(GameObject);
        }
    }
}
