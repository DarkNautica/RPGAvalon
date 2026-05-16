// Combo flow:
// Input → set ComboStep = 1, trigger AttackTrigger, start LightCombo01_A
// During swing: OnAttackHitFrameStart/End fires (hitbox logic later)
// Near end of swing: OnAttackBufferOpen — input now buffers
// End of recovery: OnAttackBufferClose — either advance combo or reset

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using PlayerInputActions = @PlayerInput;

namespace DarkNautica.Gameplay
{
    [RequireComponent(typeof(Animator))]
    public class PlayerCombat : MonoBehaviour
    {
        private Animator _animator;
        private PlayerInputActions _input;
        private int _combatLayerIndex = -1;

        private int _currentComboStep;
        private bool _isAttacking;
        private bool _canBufferNext;
        private bool _bufferedAttack;
        private bool _pendingEarlyBuffer;
        private bool _pendingFreshStart;
        private float _pendingFreshStartTime;

        // Read-only accessors for debug logger
        public int CurrentComboStep => _currentComboStep;
        public bool BufferedAttack => _bufferedAttack;
        public bool CanBufferNext => _canBufferNext;

        // Ring buffer for events
        private Queue<string> _eventBuffer = new Queue<string>();
        private const int MaxEventBuffer = 300;

        private static readonly int AnimAttackTrigger = Animator.StringToHash("AttackTrigger");
        private static readonly int AnimComboStep = Animator.StringToHash("ComboStep");
        private static readonly int AnimIsAttacking = Animator.StringToHash("IsAttacking");

        public string[] GetEventBuffer()
        {
            return _eventBuffer.ToArray();
        }

        private void LogEvent(string msg)
        {
            _eventBuffer.Enqueue(msg);
            if (_eventBuffer.Count > MaxEventBuffer)
                _eventBuffer.Dequeue();
        }

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

        private void Update()
        {
            if (_pendingFreshStart)
            {
                // Timeout safety
                if (Time.time - _pendingFreshStartTime > 0.5f)
                {
                    _pendingFreshStart = false;
                    LogEvent("[F:" + Time.frameCount + "] PENDING FRESH START TIMEOUT (0.5s elapsed)");
                    return;
                }

                // Wait for Empty state and not attacking
                if (!_isAttacking && GetCombatStateName() == "Empty")
                {
                    _pendingFreshStart = false;
                    _currentComboStep = 1;
                    _isAttacking = true;
                    _animator.SetInteger(AnimComboStep, _currentComboStep);
                    _animator.SetBool(AnimIsAttacking, true);
                    _animator.SetTrigger(AnimAttackTrigger);
                    LogEvent("[F:" + Time.frameCount + "] FRESH START EXECUTED FROM PENDING");
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
            if (info.IsName("LightCombo01_ReturnToIdle")) return "ReturnToIdle";
            return "h:" + info.shortNameHash;
        }

        private float GetCombatNormTime()
        {
            if (_combatLayerIndex < 0) return -1;
            return _animator.GetCurrentAnimatorStateInfo(_combatLayerIndex).normalizedTime;
        }

        private void OnAttack(InputAction.CallbackContext ctx)
        {
            string currentState = GetCombatStateName();
            float normTime = GetCombatNormTime();
            LogEvent("[F:" + Time.frameCount + "] >>> ATTACK PRESSED | state=" + currentState + "@" + normTime.ToString("F3")
                + " step=" + _currentComboStep + " attacking=" + _isAttacking
                + " canBuf=" + _canBufferNext + " buffered=" + _bufferedAttack);

            if (_currentComboStep == 0 && !_isAttacking)
            {
                string state = GetCombatStateName();
                bool inTrans = _combatLayerIndex >= 0 && _animator.IsInTransition(_combatLayerIndex);

                // If animator is in ReturnToIdle: ignore the input entirely.
                // The combo is over — player must wait for idle before starting a new one.
                if (state == "ReturnToIdle")
                {
                    _pendingEarlyBuffer = false;
                    _pendingFreshStart = false;
                    LogEvent("[F:" + Time.frameCount + "] PATH: IGNORED (in ReturnToIdle, combo is over)");
                }
                // If transitioning (e.g. ReturnToIdle → Empty): defer until Empty arrives
                else if (inTrans && state != "Empty")
                {
                    _pendingFreshStart = true;
                    _pendingFreshStartTime = Time.time;
                    LogEvent("[F:" + Time.frameCount + "] PATH: PENDING FRESH START (currentState=" + state + " inTrans=" + inTrans + ")");
                }
                else
                {
                    _currentComboStep = 1;
                    _isAttacking = true;
                    _animator.SetInteger(AnimComboStep, _currentComboStep);
                    LogEvent("[F:" + Time.frameCount + "] SetInteger(ComboStep, 1)");
                    _animator.SetBool(AnimIsAttacking, true);
                    LogEvent("[F:" + Time.frameCount + "] SetBool(IsAttacking, true)");
                    _animator.SetTrigger(AnimAttackTrigger);
                    LogEvent("[F:" + Time.frameCount + "] SetTrigger(AttackTrigger) | PATH: FRESH START");
                }
            }
            else if (_isAttacking && _canBufferNext)
            {
                _bufferedAttack = true;
                _pendingEarlyBuffer = false;
                LogEvent("[F:" + Time.frameCount + "] PATH: BUFFERED (bufferedAttack=true)");
            }
            else if (_isAttacking && !_canBufferNext)
            {
                // Early buffer: player clicked before buffer window opened
                // Only honor if we're in an active combo state (A, B, or C)
                string state = GetCombatStateName();
                if (state == "A" || state == "B" || state == "C")
                {
                    _pendingEarlyBuffer = true;
                    LogEvent("[F:" + Time.frameCount + "] PATH: EARLY BUFFER (pending, state=" + state + ")");
                }
                else
                {
                    LogEvent("[F:" + Time.frameCount + "] PATH: IGNORED (state=" + state + ", not in combo)");
                }
            }
            else
            {
                LogEvent("[F:" + Time.frameCount + "] PATH: IGNORED (attacking=" + _isAttacking + " canBuf=" + _canBufferNext + ")");
            }
        }

        public void OnAttackHitFrameStart()
        {
            LogEvent("[F:" + Time.frameCount + "] HitFrameStart | state=" + GetCombatStateName() + "@" + GetCombatNormTime().ToString("F3") + " step=" + _currentComboStep);
        }

        public void OnAttackHitFrameEnd()
        {
            LogEvent("[F:" + Time.frameCount + "] HitFrameEnd | state=" + GetCombatStateName() + "@" + GetCombatNormTime().ToString("F3") + " step=" + _currentComboStep);
        }

        public void OnAttackBufferOpen()
        {
            string currentState = GetCombatStateName();
            float normTime = GetCombatNormTime();
            _canBufferNext = true;

            // Honor early buffer: player clicked before window opened
            if (_pendingEarlyBuffer)
            {
                _bufferedAttack = true;
                _pendingEarlyBuffer = false;
                LogEvent("[F:" + Time.frameCount + "] BufferOpen FIRED | EARLY BUFFER HONORED | state=" + currentState + "@" + normTime.ToString("F3")
                    + " step=" + _currentComboStep);
            }
            else
            {
                LogEvent("[F:" + Time.frameCount + "] BufferOpen FIRED | state=" + currentState + "@" + normTime.ToString("F3")
                    + " step=" + _currentComboStep + " attacking=" + _isAttacking + " buffered=" + _bufferedAttack);
            }
        }

        public void OnAttackBufferClose()
        {
            string currentState = GetCombatStateName();
            float normTime = GetCombatNormTime();
            LogEvent("[F:" + Time.frameCount + "] BufferClose FIRED | state=" + currentState + "@" + normTime.ToString("F3")
                + " step=" + _currentComboStep + " attacking=" + _isAttacking
                + " buffered=" + _bufferedAttack + " canBuf=" + _canBufferNext);

            if (_bufferedAttack && _currentComboStep < 3)
            {
                int from = _currentComboStep;
                _currentComboStep++;
                _animator.SetInteger(AnimComboStep, _currentComboStep);
                LogEvent("[F:" + Time.frameCount + "] SetInteger(ComboStep, " + _currentComboStep + ")");
                _animator.SetTrigger(AnimAttackTrigger);
                LogEvent("[F:" + Time.frameCount + "] SetTrigger(AttackTrigger)");
                _bufferedAttack = false;
                _canBufferNext = false;
                _pendingEarlyBuffer = false;
                LogEvent("[F:" + Time.frameCount + "] ADVANCE | from=" + from + " to=" + _currentComboStep);

                // Auto-dump on every combo advance for debugging
                var logger = GetComponent<CombatStateLogger>();
                if (logger != null) logger.DumpBuffer();
            }
            else
            {
                _currentComboStep = 0;
                _isAttacking = false;
                _canBufferNext = false;
                _bufferedAttack = false;
                _pendingEarlyBuffer = false;
                _animator.SetInteger(AnimComboStep, 0);
                LogEvent("[F:" + Time.frameCount + "] SetInteger(ComboStep, 0)");
                _animator.SetBool(AnimIsAttacking, false);
                LogEvent("[F:" + Time.frameCount + "] SetBool(IsAttacking, false)");
                LogEvent("[F:" + Time.frameCount + "] RESET | step->0");
            }
        }
    }
}
