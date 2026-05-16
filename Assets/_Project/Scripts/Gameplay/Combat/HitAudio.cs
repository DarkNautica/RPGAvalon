using UnityEngine;

namespace DarkNautica.Gameplay.Combat
{
    public class HitAudio : MonoBehaviour
    {
        public static HitAudio Instance { get; private set; }
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip[] _hitClips;
        [SerializeField] private float _pitchVariance = 0.1f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void PlayHit()
        {
            if (_audioSource == null || _hitClips == null || _hitClips.Length == 0) return;
            var clip = _hitClips[Random.Range(0, _hitClips.Length)];
            _audioSource.pitch = 1f + Random.Range(-_pitchVariance, _pitchVariance);
            _audioSource.PlayOneShot(clip);
        }
    }
}
