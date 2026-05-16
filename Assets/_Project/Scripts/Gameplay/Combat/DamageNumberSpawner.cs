using DarkNautica.UI;
using UnityEngine;

namespace DarkNautica.Gameplay.Combat
{
    public class DamageNumberSpawner : MonoBehaviour
    {
        public static DamageNumberSpawner Instance { get; private set; }
        [SerializeField] private DamageNumber _prefab;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Spawn(float damage, Vector3 worldPos, Transform attachTo = null)
        {
            if (_prefab == null) return;
            var instance = Instantiate(_prefab, worldPos, Quaternion.identity);
            instance.Init(damage, worldPos, attachTo);
        }
    }
}
