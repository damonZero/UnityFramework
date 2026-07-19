using System;
using System.Collections.Generic;

namespace Framework.Aop
{
    public sealed class InMemoryAopCollector : IAopCollector
    {
        private readonly object _gate = new object();
        private List<AopEvent> _events;
        private readonly int _capacity;
        private int _droppedCount;
        private int _totalRecorded;

        public InMemoryAopCollector(int capacity = 4096)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _capacity = capacity;
            _events = new List<AopEvent>(Math.Min(capacity, 256));
        }

        public int DroppedCount
        {
            get
            {
                lock (_gate)
                    return _droppedCount;
            }
        }

        public int TotalRecorded
        {
            get
            {
                lock (_gate)
                    return _totalRecorded;
            }
        }

        public void Record(AopEvent spanEvent)
        {
            if (spanEvent == null)
                return;

            lock (_gate)
            {
                if (_events.Count >= _capacity)
                {
                    _droppedCount++;
                    return;
                }

                _events.Add(spanEvent);
                _totalRecorded++;
            }
        }

        /// <summary>
        /// 获取当前已采集事件的快照。
        /// 使用 swap 模式最小化锁持有时间，调用方获得独立副本。
        /// </summary>
        public List<AopEvent> Snapshot()
        {
            List<AopEvent> result;
            lock (_gate)
            {
                result = _events;
                _events = new List<AopEvent>(Math.Min(_capacity, 256));
                _totalRecorded = 0;
            }

            return result;
        }
    }
}
