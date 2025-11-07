using System;
using System.Collections.Generic;

namespace LFramework
{
    public class ClassPool<T> : IClassPool where T : class, new()
    {
        private readonly List<T> cached;

        public ClassPool()
        {
            cached = new List<T>();
        }

        public T Spawn()
        {
            var count = cached.Count;
            T instance;
            if (count <= 0)
            {
                instance = new T();
            }
            else
            {
                var index = count - 1;
                instance = cached[index];
                cached.RemoveAt(index);
            }

            if (instance is IPoolable poolable)
            {
                poolable.OnSpawn();
            }

            return instance;
        }

        public void Despawn(T instance)
        {
            if (instance == null)
            {
                return;
            }

            if (instance is IPoolable poolable)
            {
                poolable.OnDespawn();
            }

            cached.Add(instance);
        }
    }
}