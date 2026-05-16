using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkNautica.Gameplay.Player
{
    public class PlayerHUDBinder : MonoBehaviour
    {
        [Header("Player Reference")]
        [SerializeField] private PlayerHealth _playerHealth;

        [Header("HUD Bars")]
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Slider _staminaSlider;
        [SerializeField] private Slider _manaSlider;

        [Header("HUD Labels")]
        [SerializeField] private TMP_Text _levelLabel;

        [Header("Initial State")]
        [SerializeField] private int _displayedLevel = 1;

        void OnEnable()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged += HandleHealthChanged;
                HandleHealthChanged(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
            }
            if (_levelLabel != null) _levelLabel.text = _displayedLevel.ToString();
            if (_staminaSlider != null) _staminaSlider.value = 1f;
            if (_manaSlider != null) _manaSlider.value = 1f;
        }

        void OnDisable()
        {
            if (_playerHealth != null)
                _playerHealth.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (_healthSlider != null && max > 0)
                _healthSlider.value = current / max;
        }
    }
}
