using System.Collections.Generic;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// Generic component object pool (spec section 9: never Instantiate/Destroy mid-raid).
    /// WebGL-safe: avoids GC churn from spawning units, projectiles, and VFX during big battles.
    /// </summary>
    /// <typeparam name="T">The pooled MonoBehaviour component.</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _available = new();

        public ObjectPool(T prefab, Transform parent, int prewarm = 0)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < prewarm; i++)
            {
                T inst = CreateNew();
                inst.gameObject.SetActive(false);
                _available.Enqueue(inst);
            }
        }

        private T CreateNew() => Object.Instantiate(_prefab, _parent);

        public T Get(Vector3 position, Quaternion rotation)
        {
            T inst = _available.Count > 0 ? _available.Dequeue() : CreateNew();
            Transform t = inst.transform;
            t.SetPositionAndRotation(position, rotation);
            inst.gameObject.SetActive(true);
            return inst;
        }

        public void Release(T instance)
        {
            if (instance == null) return;
            instance.gameObject.SetActive(false);
            _available.Enqueue(instance);
        }
    }
}
