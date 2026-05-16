using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace DarkNautica.Gameplay
{
    public class CombatStateLogger : MonoBehaviour
    {
        [SerializeField] private PlayerCombat _combat;

        private Animator _animator;
        private int _combatLayerIndex = -1;
        private int _baseLayerIndex = 0;
        private string _lastCombatState = "";
        private Queue<string> _buffer = new Queue<string>();
        private const int MaxBufferSize = 300;
        private static string DumpPath => Application.dataPath + "/../combat_dump.txt";

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_combat == null)
                _combat = GetComponent<PlayerCombat>();
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
            if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
                DumpBuffer();
        }

        private void LateUpdate()
        {
            if (_combatLayerIndex < 0) return;

            var stateInfo = _animator.GetCurrentAnimatorStateInfo(_combatLayerIndex);
            string stateName = GetStateName(stateInfo);
            float normTime = stateInfo.normalizedTime;
            bool inTransition = _animator.IsInTransition(_combatLayerIndex);

            string transInfo = "-";
            if (inTransition)
            {
                var nextInfo = _animator.GetNextAnimatorStateInfo(_combatLayerIndex);
                var transitionInfo = _animator.GetAnimatorTransitionInfo(_combatLayerIndex);
                string nextName = GetStateName(nextInfo);
                transInfo = nextName + "@" + transitionInfo.normalizedTime.ToString("F2") + "/dur" + transitionInfo.duration.ToString("F2");
            }

            var baseInfo = _animator.GetCurrentAnimatorStateInfo(_baseLayerIndex);
            string baseStateName = GetBaseStateName(baseInfo);

            int comboStep = _animator.GetInteger("ComboStep");
            bool isAtk = _animator.GetBool("IsAttacking");

            int scriptStep = _combat != null ? _combat.CurrentComboStep : -1;
            bool buffered = _combat != null && _combat.BufferedAttack;
            bool canBuf = _combat != null && _combat.CanBufferNext;

            string changeMarker = "";
            if (stateName != _lastCombatState)
            {
                changeMarker = " <<< STATE CHANGE from=" + _lastCombatState;
                _lastCombatState = stateName;
            }

            string line = "[F:" + Time.frameCount + "] State=" + stateName + "@" + normTime.ToString("F3")
                + " InTrans=" + inTransition + " Next=" + transInfo
                + " | ComboStep=" + comboStep + " IsAtk=" + isAtk
                + " Buf=" + buffered + " CanBuf=" + canBuf + " ScriptStep=" + scriptStep
                + " | Base=" + baseStateName + "@" + baseInfo.normalizedTime.ToString("F2")
                + changeMarker;

            _buffer.Enqueue(line);
            if (_buffer.Count > MaxBufferSize)
                _buffer.Dequeue();
        }

        public void DumpBuffer()
        {
            string fullPath = System.IO.Path.GetFullPath(DumpPath);
            var lines = new List<string>();
            lines.Add("=== COMBAT STATE LOG ===");
            lines.Add("Dumped at: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            lines.Add("Frame: " + Time.frameCount);
            lines.Add("");
            lines.AddRange(_buffer);

            // Append combat events from PlayerCombat
            if (_combat != null)
            {
                lines.Add("");
                lines.Add("=== PLAYER COMBAT EVENTS ===");
                lines.AddRange(_combat.GetEventBuffer());
            }

            System.IO.File.WriteAllLines(fullPath, lines.ToArray());
            Debug.Log("Combat dump written to: " + fullPath);
        }

        private string GetStateName(AnimatorStateInfo info)
        {
            if (info.IsName("Empty")) return "Empty";
            if (info.IsName("LightCombo01_A")) return "A";
            if (info.IsName("LightCombo01_B")) return "B";
            if (info.IsName("LightCombo01_C")) return "C";
            if (info.IsName("ReturnToIdle_A")) return "RetA";
            if (info.IsName("ReturnToIdle_B")) return "RetB";
            if (info.IsName("ReturnToIdle_C")) return "RetC";
            if (info.IsName("LightCombo01_ReturnToIdle")) return "ReturnToIdle";
            return "h:" + info.shortNameHash;
        }

        private string GetBaseStateName(AnimatorStateInfo info)
        {
            if (info.IsName("Idle_Standing")) return "Idle";
            if (info.IsName("LocomotionBlendTree")) return "Loco";
            if (info.IsName("Fall")) return "Fall";
            if (info.IsName("LandingSoft")) return "LandSoft";
            if (info.IsName("LandingDefault")) return "LandDef";
            return "h:" + info.shortNameHash;
        }
    }
}
