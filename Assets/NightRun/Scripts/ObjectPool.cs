using System;
using System.Collections.Generic;
using UnityEngine;
namespace NightRun
{
    /// <summary>Fixed-capacity pool. Factories run only at scene initialization.</summary>
    public sealed class ObjectPool<T> where T : Component
    {
        readonly Queue<T> available;
        readonly HashSet<T> leased = new HashSet<T>();
        public int Capacity { get; }
        public int ActiveCount => leased.Count;
        public ObjectPool(int capacity, Func<int, T> factory)
        {
            Capacity = capacity; available = new Queue<T>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                T item = factory(i); item.gameObject.SetActive(false); available.Enqueue(item);
            }
        }
        public T Rent()
        {
            if (available.Count == 0) return null;
            T item = available.Dequeue(); leased.Add(item); item.gameObject.SetActive(true); return item;
        }
        public void Return(T item)
        {
            if (!item || !leased.Remove(item)) return;
            if (item is Interactable interactable) interactable.OnDespawn();
            else item.gameObject.SetActive(false);
            available.Enqueue(item);
        }
    }
}
