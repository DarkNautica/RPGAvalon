using UnityEngine;

namespace DarkNautica.Core.Combat
{
    public struct DamageInfo
    {
        public float damage;
        public Vector3 hitPoint;
        public Vector3 hitDirection;
        public GameObject source;
    }

    public interface IDamageable
    {
        void TakeDamage(DamageInfo info);
        bool IsDead { get; }
    }
}
