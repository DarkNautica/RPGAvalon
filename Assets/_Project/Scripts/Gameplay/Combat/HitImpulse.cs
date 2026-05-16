using Unity.Cinemachine;
using UnityEngine;

namespace DarkNautica.Gameplay.Combat
{
    public class HitImpulse : MonoBehaviour
    {
        public static HitImpulse Instance { get; private set; }
        [SerializeField] private CinemachineImpulseSource _impulseSource;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Pulse(float strength = 0.15f)
        {
            if (_impulseSource != null)
                _impulseSource.GenerateImpulseWithForce(strength);
        }
    }
}
