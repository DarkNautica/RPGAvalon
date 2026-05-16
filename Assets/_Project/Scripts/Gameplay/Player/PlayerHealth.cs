using DarkNautica.Core.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DarkNautica.Gameplay.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 100f;
        private float _currentHealth;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float HealthNormalized => _currentHealth / _maxHealth;
        public bool IsDead => _currentHealth <= 0;

        public event System.Action<float, float> OnHealthChanged;
        public event System.Action OnDied;

        void Awake()
        {
            _currentHealth = _maxHealth;
        }

        void Start()
        {
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                TakeDamage(new DamageInfo { damage = 15f, hitPoint = transform.position, source = gameObject });
            }
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                Heal(20f);
            }
        }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;
            _currentHealth = Mathf.Max(0, _currentHealth - info.damage);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            Debug.Log($"Player took {info.damage} damage. HP: {_currentHealth}/{_maxHealth}");
            if (IsDead)
            {
                OnDied?.Invoke();
                Debug.Log("Player died");
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void SetMaxHealth(float newMax, bool restoreToFull = false)
        {
            _maxHealth = newMax;
            if (restoreToFull) _currentHealth = _maxHealth;
            else _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }
}
