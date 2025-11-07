using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LFramework
{
    public static class Pool
    {
        private static readonly Dictionary<GameObject, ObjectPool> prefab2ObjectPoolDic =
            new Dictionary<GameObject, ObjectPool>();

        private static readonly Dictionary<GameObject, ObjectPool> clone2ObjectPoolDic =
            new Dictionary<GameObject, ObjectPool>();

        private static readonly Dictionary<Type, IClassPool> type2ClassPoolDic = new Dictionary<Type, IClassPool>();

        private static readonly Dictionary<System.Object, IClassPool> instance2ClassPoolDic =
            new Dictionary<System.Object, IClassPool>();

        private static Transform poolParent;

        public static void SetPoolParent(Transform parent)
        {
            poolParent = parent;
        }

        public static GameObject Spawn(GameObject prefab)
        {
            if (!prefab2ObjectPoolDic.TryGetValue(prefab, out ObjectPool pool))
            {
                pool = new ObjectPool(prefab);
                prefab2ObjectPoolDic.Add(prefab, pool);
            }

            var clone = pool.Spawn();
            clone2ObjectPoolDic.Add(clone, pool);
            return clone;
        }

        public static T Spawn<T>() where T : class, new()
        {
            if (!type2ClassPoolDic.TryGetValue(typeof(T), out IClassPool pool))
            {
                pool = new ClassPool<T>();
                type2ClassPoolDic.Add(typeof(T), pool);
            }

            var realClassPool = pool as ClassPool<T>;

            var instance = realClassPool!.Spawn();
            instance2ClassPoolDic.Add(instance, pool);
            return instance;
        }

        public static void Despawn(GameObject clone)
        {
            if (clone2ObjectPoolDic.TryGetValue(clone, out ObjectPool pool))
            {
                clone2ObjectPoolDic.Remove(clone);
                pool.Despawn(clone);
                return;
            }

            Object.Destroy(clone);
        }

        public static void Despawn(Component clone)
        {
            Despawn(clone.gameObject);
        }

        public static void Despawn<T>(T instance) where T : class, new()
        {
            if (!instance2ClassPoolDic.TryGetValue(typeof(T), out IClassPool pool))
            {
                return;
            }

            var realClassPool = pool as ClassPool<T>;
            instance2ClassPoolDic.Remove(instance);
            realClassPool!.Despawn(instance);
        }
    }
}