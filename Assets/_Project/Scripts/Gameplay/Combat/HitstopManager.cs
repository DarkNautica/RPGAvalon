using System.Collections;
using UnityEngine;

namespace DarkNautica.Gameplay.Combat
{
    public class HitstopManager : MonoBehaviour
    {
        public static HitstopManager Instance { get; private set; }

        private Coroutine _activeHitstop;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Freeze(float duration)
        {
            if (_activeHitstop != null) StopCoroutine(_activeHitstop);
            _activeHitstop = StartCoroutine(DoFreeze(duration));
        }

        private IEnumerator DoFreeze(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            _activeHitstop = null;
        }
    }
}
