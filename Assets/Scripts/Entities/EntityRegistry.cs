using System;
using System.Collections.Generic;

namespace GhostArena
{
    public sealed class EntityRegistry<T>
    {
        private readonly List<T> _items = new List<T>();
        private readonly IReadOnlyList<T> _readOnlyItems;

        public EntityRegistry()
        {
            _readOnlyItems = _items.AsReadOnly();
        }

        public event Action<T> Added;
        public event Action<T> Removed;

        public int Count => _items.Count;

        public IReadOnlyList<T> Items => _readOnlyItems;

        public bool Add(T item)
        {
            if (ReferenceEquals(item, null) || _items.Contains(item))
            {
                return false;
            }

            _items.Add(item);
            Added?.Invoke(item);
            return true;
        }

        public bool Remove(T item)
        {
            if (ReferenceEquals(item, null) || _items.Remove(item) == false)
            {
                return false;
            }

            Removed?.Invoke(item);
            return true;
        }

        public void Clear()
        {
            T[] snapshot = _items.ToArray();

            foreach (T item in snapshot)
            {
                Remove(item);
            }
        }
    }
}
