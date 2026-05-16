using UnityEngine;

namespace DarkNautica.Gameplay.Combat
{
    public class AutoDestroyParticle : MonoBehaviour
    {
        [SerializeField] private float _lifetime = 1f;
        void Start() => Destroy(gameObject, _lifetime);
    }
}
