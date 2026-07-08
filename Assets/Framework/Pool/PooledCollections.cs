using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Pool
{
    // A-D5：防御可变 struct 值拷贝导致的「同一集合两次归还共享池」。
    // 一旦对 PooledX 做值拷贝（var b = a;）后两个副本都 Dispose，会把同一集合归还两次、
    // 损坏共享池。理想防护是让拷贝在编译期报错。但本工程编译上下文的 UnityEngine 命名空间
    // 下没有 NonCopyable，也未引用 com.unity.collections（其
    // Unity.Collections.LowLevel.Unsafe.NonCopyable 才是真正编译期阻止拷贝的特性），
    // 因此此处定义「本地标记特性」以通过编译并明确契约：禁止值拷贝，必须 ref / using 使用。
    // 如需真编译期保护：在 manifest 引入 com.unity.collections，并将下方标记替换为
    // `using Unity.Collections.LowLevel.Unsafe;` + `[NonCopyable]`。

    /// <summary>
    /// 本地 NonCopyable 标记特性。仅作契约声明与文档化，不提供编译期拷贝阻止。
    /// 真编译期保护请改用 Unity.Collections.LowLevel.Unsafe.NonCopyable（需 com.unity.collections）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class NonCopyableAttribute : Attribute { }

    [NonCopyable]
    public struct PooledList<T> : IDisposable
    {
        private List<T> _value;

        public PooledList(List<T> value) => _value = value;
        public List<T> Value => _value;

        public void Dispose()
        {
            if (_value == null)
                return;

            CollectionPool.Return(_value);
            _value = null;
        }
    }

    [NonCopyable]
    public struct PooledHashSet<T> : IDisposable
    {
        private HashSet<T> _value;

        public PooledHashSet(HashSet<T> value) => _value = value;
        public HashSet<T> Value => _value;

        public void Dispose()
        {
            if (_value == null)
                return;

            CollectionPool.Return(_value);
            _value = null;
        }
    }

    [NonCopyable]
    public struct PooledQueue<T> : IDisposable
    {
        private Queue<T> _value;

        public PooledQueue(Queue<T> value) => _value = value;
        public Queue<T> Value => _value;

        public void Dispose()
        {
            if (_value == null)
                return;

            CollectionPool.Return(_value);
            _value = null;
        }
    }

    [NonCopyable]
    public struct PooledStack<T> : IDisposable
    {
        private Stack<T> _value;

        public PooledStack(Stack<T> value) => _value = value;
        public Stack<T> Value => _value;

        public void Dispose()
        {
            if (_value == null)
                return;

            CollectionPool.Return(_value);
            _value = null;
        }
    }

    [NonCopyable]
    public struct PooledDictionary<TKey, TValue> : IDisposable
        where TKey : notnull
    {
        private Dictionary<TKey, TValue> _value;

        public PooledDictionary(Dictionary<TKey, TValue> value) => _value = value;
        public Dictionary<TKey, TValue> Value => _value;

        public void Dispose()
        {
            if (_value == null)
                return;

            CollectionPool.Return(_value);
            _value = null;
        }
    }
}
