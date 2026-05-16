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

            var animator = source != null ? source.GetComponentInChildren<Animator>() : null;
            if (animator != null)
            {
                int combatLayer = animator.GetLayerIndex("Combat");
                if (combatLayer >= 0)
                    Debug.Log($"[Hitbox] Active during: {GetStateName(animator, combatLayer)}");
                else
                    Debug.Log("[Hitbox] Active (no Combat layer)");
            }
            else
            {
                Debug.Log("[Hitbox] Active (no animator)");
            }
        }

        private string GetStateName(Animator anim, int layer)
        {
            var info = anim.GetCurrentAnimatorStateInfo(layer);
            if (info.IsName("LightCombo01_A")) return "A";
            if (info.IsName("LightCombo01_B")) return "B";
            if (info.IsName("LightCombo01_C")) return "C";
            if (info.IsName("Empty")) return "Empty";
            return $"Unknown(hash:{info.shortNameHash})";
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
                    var info = new DamageInfo
                    {
                        damage = damage,
                        hitPoint = transform.position,
                        hitDirection = (hit.transform.position - source.transform.position).normalized,
                        source = source
                    };
                    damageable.TakeDamage(info);
                    _hitThisSwing.Add(hit);
                    Debug.Log($"[Hitbox] Hit: {hit.name} for {damage} damage");
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
