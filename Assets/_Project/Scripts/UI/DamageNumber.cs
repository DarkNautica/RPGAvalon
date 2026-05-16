using TMPro;
using UnityEngine;

namespace DarkNautica.UI
{
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _text;
        [SerializeField] private float _lifetime = 0.8f;
        [SerializeField] private float _floatSpeed = 2f;
        [SerializeField] private float _driftAmount = 0.4f;
        [SerializeField] private float _detachAtNormalizedTime = 0.5f;
        [SerializeField] private AnimationCurve _scaleCurve = new AnimationCurve(
            new Keyframe(0f, 1.5f),
            new Keyframe(0.15f, 1.8f),
            new Keyframe(1f, 0.7f)
        );
        [SerializeField] private AnimationCurve _alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        private float _elapsed;
        private Camera _cam;
        private Vector3 _initialPos;
        private Vector3 _driftDirection;
        private Transform _attachTarget;
        private Vector3 _attachOffset;

        public void Init(float damage, Vector3 worldPos, Transform attachTo = null)
        {
            _text.text = Mathf.RoundToInt(damage).ToString();

            Vector2 randomCircle = Random.insideUnitCircle * 0.3f;
            Vector3 offsetWorldPos = worldPos + new Vector3(randomCircle.x, 0, randomCircle.y);

            transform.position = offsetWorldPos;
            _initialPos = offsetWorldPos;
            _cam = Camera.main;

            float driftAngle = Random.Range(0f, 360f);
            _driftDirection = new Vector3(Mathf.Cos(driftAngle * Mathf.Deg2Rad), 0, Mathf.Sin(driftAngle * Mathf.Deg2Rad));

            if (attachTo != null)
            {
                _attachTarget = attachTo;
                _attachOffset = offsetWorldPos - attachTo.position;
            }
        }

        void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            float t = _elapsed / _lifetime;

            Vector3 anchor = _initialPos;
            if (_attachTarget != null && t < _detachAtNormalizedTime)
            {
                anchor = _attachTarget.position + _attachOffset;
                _initialPos = anchor;
            }

            Vector3 floatOffset = Vector3.up * (_floatSpeed * _elapsed);
            Vector3 driftOffset = _driftDirection * (_driftAmount * t);
            transform.position = anchor + floatOffset + driftOffset;

            transform.localScale = Vector3.one * _scaleCurve.Evaluate(t);

            var c = _text.color;
            c.a = _alphaCurve.Evaluate(t);
            _text.color = c;

            if (_cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);

            if (t >= 1f) Destroy(gameObject);
        }
    }
}
