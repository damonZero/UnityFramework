using System.Collections.Generic;

namespace Framework.ViewCache
{
    public class LoadTimeStatistics : AbstractCacheStatistics
    {
        //FIXME by liangc:long类型 int类型?
        private Dictionary<string, long> _dictionary = new Dictionary<string, long>();

        public override void BeforeTake(string key)
        {
            if (_dictionary.TryGetValue(key, out var num))
            {
                _dictionary[key] = num + 1;
            }
            else
            {
                _dictionary.Add(key, 1);
            }
        }

        public override void AfterTake(string key)
        {
        }

        public override long GetScore(string key)
        {
            if (!_dictionary.TryGetValue(key, out var value))
            {
                value = 0;
            }

            return value;
        }

        public override string ToString()
        {
            var str = $"\n{GetType().FullName} (\n ";
            foreach (var pair in _dictionary)
            {
                str += $"key = {pair.Key}, value = {pair.Value} \n";
            }
            return str + ")\n";
        }
    }
}
