using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LFramework
{
    public class ObjectPool
    {
        private List<GameObject> activeCloneList;
        private List<GameObject> inactiveCloneList;
        private GameObject refPrefab;

        public ObjectPool(GameObject prefab)
        {
            activeCloneList = new List<GameObject>();
            inactiveCloneList = new List<GameObject>();
            refPrefab = prefab;
        }

        public GameObject Spawn()
        {
            GameObject clone;
            if (inactiveCloneList.Count > 0)
            {
                clone = inactiveCloneList[0];
                inactiveCloneList.RemoveAt(0);
            }
            else
            {
                clone = Object.Instantiate(refPrefab);
            }

            activeCloneList.Add(clone);
            var poolableComp = clone.GetComponent<IPoolable>();
            if (poolableComp != null)
            {
                poolableComp.OnSpawn();
            }

            return clone;
        }

        public void Despawn(GameObject clone)
        {
            activeCloneList.Remove(clone);
            inactiveCloneList.Add(clone);
            var poolableComp = clone.GetComponent<IPoolable>();
            if (poolableComp != null)
            {
                poolableComp.OnDespawn();
            }
        }

        public void Despawn(Component clone)
        {
            Despawn(clone.gameObject);
        }
    }
}