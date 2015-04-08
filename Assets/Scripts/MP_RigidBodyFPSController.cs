using System;
using InControl;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class MP_RigidBodyFPSController : MonoBehaviour
{
    [Serializable]
    public class MovementSettings
    {
        public float ForwardSpeed = 8.0f;   // Speed when walking forward
        public float BackwardSpeed = 4.0f;  // Speed when walking backwards
        public float StrafeSpeed = 4.0f;    // Speed when walking sideways
        public float RunMultiplier = 2.0f;   // Speed when sprinting
        public KeyCode RunKey = KeyCode.LeftShift;
        public float JumpForce = 30f;
        public AnimationCurve SlopeCurveModifier = new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(90.0f, 0.0f));
        [HideInInspector]
        public float CurrentTargetSpeed = 8f;

#if !MOBILE_INPUT
        private bool _mRunning;
#endif

        public void UpdateDesiredTargetSpeed(Vector2 input)
        {
            if (input == Vector2.zero) return;
            if (input.x > 0 || input.x < 0)
            {
                //strafe
                CurrentTargetSpeed = StrafeSpeed;
            }
            if (input.y < 0)
            {
                //backwards
                CurrentTargetSpeed = BackwardSpeed;
            }
            if (input.y > 0)
            {
                //forwards
                //handled last as if strafing and moving forward at the same time forwards speed should take precedence
                CurrentTargetSpeed = ForwardSpeed;
                
                
            }
#if !MOBILE_INPUT
            if (InputManager.ActiveDevice.LeftTrigger)
            {
                CurrentTargetSpeed *= RunMultiplier;
                _mRunning = true;
            }
            else
            {
                _mRunning = false;
            }
#endif
        }

#if !MOBILE_INPUT
        public bool Running
        {
            get { return _mRunning; }
        }
#endif
    }


    [Serializable]
    public class AdvancedSettings
    {
        public float GroundCheckDistance = 0.01f; // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
        public float StickToGroundHelperDistance = 0.5f; // stops the character
        public float SlowDownRate = 20f; // rate at which the controller comes to a stop when there is no input
        public bool AirControl; // can the user control the direction that is being moved in the air
    }

    public int PlayerIndex = 0;

    public Camera Cam;
    public MovementSettings movementSettings = new MovementSettings();
    public MP_MouseLook MouseLook = new MP_MouseLook();
    public AdvancedSettings advancedSettings = new AdvancedSettings();

    private InputDevice _activeDevice;

    private Rigidbody _mRigidBody;
    private CapsuleCollider _mCapsule;
    private Animator _mAnimator;
    private float _mYRotation;
    private Vector3 _mGroundContactNormal;
    private bool _mJump, _mPreviouslyGrounded, _mJumping, _mIsGrounded;


    public Vector3 Velocity
    {
        get { return _mRigidBody.velocity; }
    }

    public bool Grounded
    {
        get { return _mIsGrounded; }
    }

    public bool Jumping
    {
        get { return _mJumping; }
    }

    public bool Running
    {
        get
        {
#if !MOBILE_INPUT
            return movementSettings.Running;
#else
	        return false;
#endif
        }
    }


    private void Start()
    {
        _mRigidBody = GetComponent<Rigidbody>();
        _mCapsule = GetComponent<CapsuleCollider>();
        _mAnimator = transform.FindChild("MotusMan_v2").GetComponent<Animator>();
        MouseLook.Init(transform, Cam.transform, PlayerIndex);
        _activeDevice = (InputManager.Devices.Count > PlayerIndex) ? InputManager.Devices[PlayerIndex] : null;
    }


    private void Update()
    {
        RotateView();

        if (_activeDevice.Action1 && !_mJump)
        {
            _mJump = true;
        }
    }


    private void FixedUpdate()
    {
        GroundCheck();
        Vector2 input = GetInput();
        _mAnimator.SetFloat("Forward", input.y);

        if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) && (advancedSettings.AirControl || _mIsGrounded))
        {
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = Cam.transform.forward * input.y + Cam.transform.right * input.x;
            desiredMove = Vector3.ProjectOnPlane(desiredMove, _mGroundContactNormal).normalized;

            desiredMove.x = desiredMove.x * movementSettings.CurrentTargetSpeed;
            desiredMove.z = desiredMove.z * movementSettings.CurrentTargetSpeed;
            desiredMove.y = desiredMove.y * movementSettings.CurrentTargetSpeed;
            if (_mRigidBody.velocity.sqrMagnitude <
                (movementSettings.CurrentTargetSpeed * movementSettings.CurrentTargetSpeed))
            {
                _mRigidBody.AddForce(desiredMove * SlopeMultiplier(), ForceMode.Impulse);
            }
        }

        if (_mIsGrounded)
        {
            _mRigidBody.drag = 5f;

            if (_mJump)
            {
                _mRigidBody.drag = 0f;
                _mRigidBody.velocity = new Vector3(_mRigidBody.velocity.x, 0f, _mRigidBody.velocity.z);
                _mRigidBody.AddForce(new Vector3(0f, movementSettings.JumpForce, 0f), ForceMode.Impulse);
                _mJumping = true;
            }

            if (!_mJumping && Mathf.Abs(input.x) < float.Epsilon && Mathf.Abs(input.y) < float.Epsilon && _mRigidBody.velocity.magnitude < 1f)
            {
                _mRigidBody.Sleep();
            }
        }
        else
        {
            _mRigidBody.drag = 0f;
            if (_mPreviouslyGrounded && !_mJumping)
            {
                StickToGroundHelper();
            }
        }
        _mJump = false;
    }


    private float SlopeMultiplier()
    {
        float angle = Vector3.Angle(_mGroundContactNormal, Vector3.up);
        return movementSettings.SlopeCurveModifier.Evaluate(angle);
    }


    private void StickToGroundHelper()
    {
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, _mCapsule.radius, Vector3.down, out hitInfo,
                                ((_mCapsule.height / 2f) - _mCapsule.radius) +
                                advancedSettings.StickToGroundHelperDistance))
        {
            if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 85f)
            {
                _mRigidBody.velocity = Vector3.ProjectOnPlane(_mRigidBody.velocity, hitInfo.normal);
            }
        }
    }


    private Vector2 GetInput()
    {
        Vector2 input = new Vector2
        {
            x = _activeDevice.LeftStickX,
            y = _activeDevice.LeftStickY
        };
        movementSettings.UpdateDesiredTargetSpeed(input);
        return input;
    }


    private void RotateView()
    {
        //avoids the mouse looking if the game is effectively paused
        if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

        // get the rotation before it's changed
        float oldYRotation = transform.eulerAngles.y;

        MouseLook.LookRotation(transform, Cam.transform);

        if (_mIsGrounded || advancedSettings.AirControl)
        {
            // Rotate the rigidbody velocity to match the new direction that the character is looking
            Quaternion velRotation = Quaternion.AngleAxis(transform.eulerAngles.y - oldYRotation, Vector3.up);
            _mRigidBody.velocity = velRotation * _mRigidBody.velocity;
        }
    }


    /// sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
    private void GroundCheck()
    {
        _mPreviouslyGrounded = _mIsGrounded;
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, _mCapsule.radius, Vector3.down, out hitInfo,
                                ((_mCapsule.height / 2f) - _mCapsule.radius) + advancedSettings.GroundCheckDistance))
        {
            _mIsGrounded = true;
            _mGroundContactNormal = hitInfo.normal;
        }
        else
        {
            _mIsGrounded = false;
            _mGroundContactNormal = Vector3.up;
        }
        if (!_mPreviouslyGrounded && _mIsGrounded && _mJumping)
        {
            _mJumping = false;
        }
    }
}

