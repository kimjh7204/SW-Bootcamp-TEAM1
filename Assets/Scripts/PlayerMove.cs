using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PlayerInputControl))]
public class PlayerMove : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 2.0f;

    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 5.335f;

    [Tooltip("Roll speed of the character in m/s")]
    public float RollSpeed = 10f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    //public AudioClip LandingAudioClip;
    //public AudioClip[] FootstepAudioClips;
    //[Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Space(10)]
    [Tooltip("The amount of force applied when player jump"), Range(5f, 20f)]
    public float JumpForce = 10f;

    //[Space(10)]
    //[Tooltip("The amount of force applied when player roll"), Range(5f, 10f)]
    //public float RollForce = 10f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Tooltip("Time required to pass before being able to roll again.")]
    public float RollTimeout = 1f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;

    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;

    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // player
    private float _speed;
    private float _speedSmoothVelocity;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    //private float _terminalVelocity = 53.0f;
    private float _camYawVelocity;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;
    private float _rollTimeoutDelta;

    // animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;
    private int _animIDRoll;

    private PlayerInputControl _playerInput;
    private Animator _animator;
    private Rigidbody _rigidBody;

    private PlayerAnimationEvent _animationEvent;
    private GameObject _mainCamera;

    private const float _threshold = 0.01f;

    private bool _hasAnimator = true;

    private bool isRolling;
    private Coroutine rolling;

    private void Awake()
    {
        // get a reference to our main camera
        if(_mainCamera is null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _animator = GetComponentInChildren<Animator>();
        //_controller = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInputControl>();
        _rigidBody = GetComponent<Rigidbody>();

        _animationEvent = GetComponentInChildren<PlayerAnimationEvent>();
        _animationEvent.onRollFinish += RollFinish;

        AssignAnimationIDs();

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
        _rollTimeoutDelta = -1f;
    }
    private void Update()
    {
        if (_playerInput.jump && _jumpTimeoutDelta <= 0.0f && Grounded)
        {
            Jump();
        }
        if (_playerInput.roll && _rollTimeoutDelta <= 0f && Grounded && !isRolling)
        {
            Roll();
        }
        GroundedCheck();
    }


    private void FixedUpdate()
    {
        VerticalMovement();
        HorizontalMovement();
        CameraRotation();
    }
    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");

        _animIDRoll = Animator.StringToHash("Roll");
    }
    private void Jump()
    {
        // add force to rigid body
        _rigidBody.AddForce(JumpForce * Vector3.up, ForceMode.Impulse);

        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDJump, true);
        }
    }
    private void Roll()
    {
        if(rolling is not null) StopCoroutine(rolling);
        rolling = StartCoroutine(Rolling());
        //_rigidBody.AddForce(RollForce * transform.forward, ForceMode.Impulse);
    }
    private void VerticalMovement()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.fixedDeltaTime;
            }

        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.fixedDeltaTime;
            }
            else
            {
                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }
        }
    }
    private void Rotate()
    {
        #region Rotation
        // input direction
        Vector3 inputDirection = new(_playerInput.moveInput.x, 0f, _playerInput.moveInput.y);

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (_playerInput.moveInput != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
        #endregion
    }
    private void HorizontalMovement()
    {
        if (isRolling)
        {
            _rigidBody.velocity = transform.forward * RollSpeed;
            return;
        }

        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = _playerInput.sprint ? SprintSpeed : MoveSpeed;

        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is no input, set the target speed to 0
        float inputMagnitude = _playerInput.moveInput.magnitude;
        if (inputMagnitude < 0.01f) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(_rigidBody.velocity.x, 0.0f, _rigidBody.velocity.z).magnitude;

        _speed = Mathf.SmoothDamp(currentHorizontalSpeed, targetSpeed * inputMagnitude, ref _speedSmoothVelocity, Time.fixedDeltaTime);
        // round speed to 3 decimal places
        //_speed = Mathf.Round(_speed * 1000f) / 1000f;

        //_animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.fixedDeltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        Rotate();

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        // move the player
        Vector3 targetVelocity = targetDirection.normalized * _speed + Vector3.up * _rigidBody.velocity.y;
        _rigidBody.velocity = targetVelocity;
        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            //_animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }
    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }
    private void CameraRotation()
    {
        // if there is an input and camera position is not fixed
        if (_playerInput.lookInput.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            //Don't multiply mouse input by Time.deltaTime;
            _cinemachineTargetYaw += _playerInput.lookInput.x;
            _cinemachineTargetPitch += _playerInput.lookInput.y;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
    public void OnFootstep(AnimationEvent animationEvent)
    {
        print("foot step!");
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            //if (FootstepAudioClips.Length > 0)
            //{
            //    var index = Random.Range(0, FootstepAudioClips.Length);
            //    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            //}
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            //AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
        }
    }
    private IEnumerator Rolling()
    {
        _rollTimeoutDelta = RollTimeout;
        isRolling = true;
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDRoll, true);
        }
        while (_rollTimeoutDelta > 0f)
        {
            _rollTimeoutDelta -= Time.deltaTime;

            yield return null;
        }
    }
    private void RollFinish()
    {
        print("roll finish!");
        isRolling = false;
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDRoll, false);
        }
    }

}