using System.Collections.Generic;
namespace Framework.View.Navigation.Editor
{
    public interface INavigationGUIObjectPool
    {
        void Reset();
    }

    public class NavigationGUIObjectPool<TNavigationGUIObjectPool>
        where TNavigationGUIObjectPool : INavigationGUIObjectPool, new()
    {
        private readonly Queue<TNavigationGUIObjectPool> _pool = new Queue<TNavigationGUIObjectPool>();

        public TNavigationGUIObjectPool Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Dequeue();
            }

            return new TNavigationGUIObjectPool();
        }

        public void Put(TNavigationGUIObjectPool item)
        {
            item.Reset();
            _pool.Enqueue(item);
        }
    }
}