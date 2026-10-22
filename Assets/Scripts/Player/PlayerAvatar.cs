using UnityEngine;

namespace MyGame.Player
{
    /// <summary>Makes the avatar visual face its movement direction and adds a soft walk bob, lean, and idle float.
    /// Lives on the visual child so the root can keep rotating with the camera.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("MyGame/Player/Player Avatar")]
    public class PlayerAvatar : MonoBehaviour
    {
        [SerializeField, Tooltip("Assigned automatically from the parent if empty.")]
        private CharacterController _controller;

        [Header("Turning")]
        [SerializeField, Min(0f)] private float _turnSpeed = 720f;
        [SerializeField, Min(0f)] private float _minTurnSpeed = 0.3f;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float _bobHeight = 0.05f;
        [SerializeField, Min(0f), Tooltip("Bob cycles per meter travelled.")]
        private float _bobFrequency = 0.55f;
        [SerializeField, Range(0f, 20f)] private float _maxLeanAngle = 7f;
        [SerializeField, Min(0.1f)] private float _leanFullSpeed = 8f;
        [SerializeField, Min(0f)] private float _idleFloatHeight = 0.015f;
        [SerializeField, Min(0f)] private float _idleFloatSpeed = 1.6f;
        [SerializeField, Min(0f)] private float _smoothing = 12f;

        private Vector3 _baseLocalPosition;
        private Quaternion _facing;
        private float _bobPhase;
        private float _currentLean;
        private float _currentBob;

        private void Awake()
        {
            if (_controller == null) _controller = GetComponentInParent<CharacterController>();
            _baseLocalPosition = transform.localPosition;
            _facing = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        private void LateUpdate()
        {
            if (_controller == null || _controller.gameObject == gameObject) return;

            Vector3 velocity = _controller.velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;
            float blend = 1f - Mathf.Exp(-_smoothing * Time.deltaTime);

            Transform lookRoot = transform.parent != null ? transform.parent : transform;
            Vector3 faceDirection = speed > _minTurnSpeed ? velocity / speed : lookRoot.forward;
            Quaternion targetFacing = Quaternion.LookRotation(faceDirection, Vector3.up);
            _facing = Quaternion.RotateTowards(_facing, targetFacing, _turnSpeed * Time.deltaTime);

            bool moving = _controller.isGrounded && speed > _minTurnSpeed;
            _bobPhase += speed * _bobFrequency * Time.deltaTime * Mathf.PI * 2f;
            float targetBob = moving ? Mathf.Abs(Mathf.Sin(_bobPhase)) * _bobHeight
                : Mathf.Sin(Time.time * _idleFloatSpeed) * _idleFloatHeight;
            _currentBob = Mathf.Lerp(_currentBob, targetBob, blend);

            float targetLean = Mathf.Clamp01(speed / _leanFullSpeed) * _maxLeanAngle;
            _currentLean = Mathf.Lerp(_currentLean, targetLean, blend);

            transform.rotation = _facing * Quaternion.Euler(_currentLean, 0f, 0f);
            transform.localPosition = _baseLocalPosition + Vector3.up * _currentBob;
        }
    }
}
