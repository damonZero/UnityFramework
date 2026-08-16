using UnityEngine;
using System;
using System.Collections.Generic;
namespace Framework.View
{

    [Serializable]
    public class SerializationDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys;
        [SerializeField]
        private List<TValue> values;

        public void OnBeforeSerialize()
        {
            keys = new List<TKey>(this.Keys);
            values = new List<TValue>(this.Values);
        }

        public void OnAfterDeserialize()
        {
            this.Clear();
            var count = Math.Min(keys.Count, values.Count);
            for (var i = 0; i < count; ++i)
            {
                this.Add(keys[i], values[i]);
            }
        }
    }
}
