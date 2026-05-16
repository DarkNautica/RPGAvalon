using UnityEngine;
using System.Collections.Generic;
using DarkNautica.Core.Combat;

namespace DarkNautica.Gameplay.Combat
{
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] private float damage = 25f;
        [SerializeField] private GameObject source;

        private bool _active;
        private HashSet<Collider> _hitThisSwing = new HashSet<Collider>();

        private void Awake()
        {
            if (source == null)
                source = transform.root.gameObject;
        }

        public void Activate()
        {
            _active = true;
            _hitThisSwing.Clear();
        }

        public void Deactivate()
        {
            _active = false;
        }

        private void FixedUpdate()
        {
            if (!_active) return;

            var boxCollider = GetComponent<BoxCollider>();
            Vector3 worldCenter = transform.TransformPoint(boxCollider.center);
            Vector3 worldHalfExtents = Vector3.Scale(boxCollider.size, transform.lossyScale) * 0.5f;
            Quaternion worldRotation = transform.rotation;

            Collider[] hits = Physics.OverlapBox(worldCenter, worldHalfExtents, worldRotation);

            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (_hitThisSwing.Contains(hit)) continue;
                if (hit.gameObject == source || hit.transform.IsChildOf(source.transform)) continue;

                var damageable = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    Vector3 impactPoint = hit.ClosestPoint(transform.position);

                    var info = new DamageInfo
                    {
                        damage = damage,
                        hitPoint = impactPoint,
                        hitDirection = (hit.transform.position - source.transform.position).normalized,
                        source = source
                    };
                    damageable.TakeDamage(info);
                    _hitThisSwing.Add(hit);

                    // Juice
                    HitstopManager.Instance?.Freeze(0.06f);
                    DamageNumberSpawner.Instance?.Spawn(damage, impactPoint + Vector3.up * 0.5f, hit.transform);
                    HitImpulse.Instance?.Pulse(0.15f);
                    HitVFXSpawner.Instance?.Spawn(impactPoint);
                    HitAudio.Instance?.PlayHit();
                }
            }
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider>();
            if (col == null) return;
            Gizmos.color = _active ? Color.red : new Color(1, 0, 0, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.center, col.size);
        }
    }
}
