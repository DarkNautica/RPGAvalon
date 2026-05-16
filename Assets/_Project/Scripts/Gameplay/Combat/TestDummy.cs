using DarkNautica.Core.Combat;
using UnityEngine;

namespace DarkNautica.Gameplay.Combat
{
    public class TestDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 100f;
        private float _currentHealth;

        public bool IsDead => _currentHealth <= 0;

        private void Awake() => _currentHealth = _maxHealth;

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;
            _currentHealth -= info.damage;
            Debug.Log($"{name} took {info.damage} damage. HP: {_currentHealth}/{_maxHealth}");
            if (IsDead)
            {
                Debug.Log($"{name} died");
                Destroy(gameObject, 0.5f);
            }
        }
    }
}
