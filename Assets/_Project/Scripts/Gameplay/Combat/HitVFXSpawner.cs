using UnityEngine;

namespace DarkNautica.Gameplay.Combat
{
    public class HitVFXSpawner : MonoBehaviour
    {
        public static HitVFXSpawner Instance { get; private set; }
        [SerializeField] private GameObject _hitSparksPrefab;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Spawn(Vector3 worldPos)
        {
            if (_hitSparksPrefab == null) return;
            Instantiate(_hitSparksPrefab, worldPos, Quaternion.identity);
        }
    }
}
