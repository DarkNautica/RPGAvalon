// Combo flow:
// Input → set ComboStep = 1, trigger AttackTrigger, start LightCombo01_A
// During swing: OnAttackHitFrameStart/End fires (hitbox logic later)
// Near end of swing: OnAttackBufferOpen — input now buffers
// End of recovery: OnAttackBufferClose — either advance combo or reset

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using DarkNautica.Gameplay.Combat;
using PlayerInputActions = @PlayerInput;

namespace DarkNautica.Gameplay
{
    [RequireComponent(typeof(Animator))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private Hitbox _swordHitbox;
        [SerializeField] private float _hitboxActiveDuration = 0.35f;

        private Animator _animator;
        private Coroutine _hitboxCoroutine;
        private PlayerInputActions _input;
        private int _combatLayerIndex = -1;

        private int _currentComboStep;
        private bool _isAttacking;
        private bool _canBufferNext;
        private bool _bufferedAttack;
        private bool _pendingEarlyBuffer;
        private bool _pendingFreshStart;
        private float _pendingFreshStartTime;

        private static readonly int AnimAttackTrigger = Animator.StringToHash("AttackTrigger");
        private static readonly int AnimComboStep = Animator.StringToHash("ComboStep");
        private static readonly int AnimIsAttacking = Animator.StringToHash("IsAttacking");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _input = new PlayerInputActions();
        }

        private void Start()
        {
            for (int i = 0; i < _animator.layerCount; i++)
            {
                if (_animator.GetLayerName(i) == "Combat")
                {
                    _combatLayerIndex = i;
                    break;
                }
            }
        }

        private void OnEnable()
        {
            _input.Player.Enable();
            _input.Player.Attack.performed += OnAttack;
        }

        private void OnDisable()
        {
            _input.Player.Attack.performed -= OnAttack;
            _input.Player.Disable();
        }

        private void OnDestroy()
        {
            _input?.Dispose();
        }

        private void Update()
        {
            if (_pendingFreshStart)
            {
                if (Time.time - _pendingFreshStartTime > 0.5f)
                {
                    _pendingFreshStart = false;
                    return;
                }

                if (!_isAttacking && GetCombatStateName() == "Empty")
                {
                    _pendingFreshStart = false;
                    _currentComboStep = 1;
                    _isAttacking = true;
                    _animator.SetInteger(AnimComboStep, _currentComboStep);
                    _animator.SetBool(AnimIsAttacking, true);
                    _animator.SetTrigger(AnimAttackTrigger);
                }
            }
        }

        private string GetCombatStateName()
        {
            if (_combatLayerIndex < 0) return "?";
            var info = _animator.GetCurrentAnimatorStateInfo(_combatLayerIndex);
            if (info.IsName("Empty")) return "Empty";
            if (info.IsName("LightCombo01_A")) return "A";
            if (info.IsName("LightCombo01_B")) return "B";
            if (info.IsName("LightCombo01_C")) return "C";
            if (info.IsName("ReturnToIdle_A")) return "ReturnToIdle";
            if (info.IsName("ReturnToIdle_B")) return "ReturnToIdle";
            if (info.IsName("ReturnToIdle_C")) return "ReturnToIdle";
            return "h:" + info.shortNameHash;
        }

        private void OnAttack(InputAction.CallbackContext ctx)
        {
            if (_currentComboStep == 0 && !_isAttacking)
            {
                string state = GetCombatStateName();
                bool inTrans = _combatLayerIndex >= 0 && _animator.IsInTransition(_combatLayerIndex);

                if (state == "ReturnToIdle")
                {
                    _pendingEarlyBuffer = false;
                    _pendingFreshStart = false;
                }
                else if (inTrans && state != "Empty")
                {
                    _pendingFreshStart = true;
                    _pendingFreshStartTime = Time.time;
                }
                else
                {
                    _currentComboStep = 1;
                    _isAttacking = true;
                    _animator.SetInteger(AnimComboStep, _currentComboStep);
                    _animator.SetBool(AnimIsAttacking, true);
                    _animator.SetTrigger(AnimAttackTrigger);
                }
            }
            else if (_isAttacking && _canBufferNext)
            {
                _bufferedAttack = true;
                _pendingEarlyBuffer = false;
            }
            else if (_isAttacking && !_canBufferNext)
            {
                string state = GetCombatStateName();
                if (state == "A" || state == "B" || state == "C")
                    _pendingEarlyBuffer = true;
            }
        }

        public void OnAttackHitFrameStart()
        {
            Debug.Log("[Combat] Hit frame open");
            if (_swordHitbox != null)
            {
                _swordHitbox.Activate();
                if (_hitboxCoroutine != null) StopCoroutine(_hitboxCoroutine);
                _hitboxCoroutine = StartCoroutine(DeactivateHitboxAfterDelay());
            }
        }

        private IEnumerator DeactivateHitboxAfterDelay()
        {
            yield return new WaitForSeconds(_hitboxActiveDuration);
            if (_swordHitbox != null) _swordHitbox.Deactivate();
            Debug.Log("[Combat] Hit frame closed (timer)");
        }

        public void OnAttackHitFrameEnd()
        {
            // No longer used — hit window is now duration-based from OnAttackHitFrameStart.
            // Method kept to avoid "missing method" warnings from animation events.
        }

        public void OnAttackBufferOpen()
        {
            _canBufferNext = true;

            if (_pendingEarlyBuffer)
            {
                _bufferedAttack = true;
                _pendingEarlyBuffer = false;
            }
        }

        public void OnAttackBufferClose()
        {
            if (_bufferedAttack && _currentComboStep < 3)
            {
                int from = _currentComboStep;
                _currentComboStep++;
                _animator.SetInteger(AnimComboStep, _currentComboStep);
                _animator.SetTrigger(AnimAttackTrigger);
                _bufferedAttack = false;
                _canBufferNext = false;
                _pendingEarlyBuffer = false;
                Debug.Log("[Combat] ADVANCE " + from + " -> " + _currentComboStep);
            }
            else
            {
                _currentComboStep = 0;
                _isAttacking = false;
                _canBufferNext = false;
                _bufferedAttack = false;
                _pendingEarlyBuffer = false;
                _animator.SetInteger(AnimComboStep, 0);
                _animator.SetBool(AnimIsAttacking, false);
                Debug.Log("[Combat] RESET");
            }
        }
    }
}
