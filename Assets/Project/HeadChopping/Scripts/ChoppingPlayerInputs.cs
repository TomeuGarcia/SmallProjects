using UnityEngine;
using UnityEngine.InputSystem;



namespace HeadChopping
{

    public class ChoppingPlayerInputs : FirstPersonCameraMovement.IInputSource, PhysicsMovement.IInputSource
    {
        private InputActions_HeadChopping _inputActions;

        private Transform _cameraTransform;
        private Timer _jumpInputBufferTimer;
        private bool _jumpInputHeld;
        private float _movementInputForward;
        private float _movementInputRight;
        private bool _attackRequested;

        private bool _canMoveCamera;

        public ChoppingPlayerInputs(Transform cameraTransform)
        {
            _canMoveCamera = true;
            _cameraTransform = cameraTransform;
            _jumpInputBufferTimer = new Timer(duration: 0.15f);
            _jumpInputBufferTimer.SetFinished();
            _jumpInputHeld = false;
            HideCursor();

            _inputActions = new InputActions_HeadChopping();
            _inputActions.Enable();
        }

        public void Cleanup()
        {
            _inputActions.Disable();
        }


        public void Update()
        {
            if (_canMoveCamera)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _canMoveCamera = false;
                    return;
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1))
                {
                    HideCursor();
                    _canMoveCamera = true;
                    return;
                }
            }

            
            _jumpInputBufferTimer.Update(Time.deltaTime);
            _jumpInputHeld = _inputActions.Player.Jump.IsPressed(); //Input.GetButton("Jump");
            if (_inputActions.Player.Jump.WasPressedThisFrame() /*Input.GetButtonDown("Jump")*/)
            {
                _jumpInputBufferTimer.Clear();
            }

            
            Vector2 movementInput = _inputActions.Player.Movement.ReadValue<Vector2>();
            _movementInputForward = Mathf.Round(movementInput.y);// Input.GetKey(KeyCode.W) ? 1 : (Input.GetKey(KeyCode.S) ? -1 : 0);
            _movementInputRight = Mathf.Round(movementInput.x);// Input.GetKey(KeyCode.D) ? 1 : (Input.GetKey(KeyCode.A) ? -1 : 0);

            _attackRequested = _inputActions.Player.Attack.WasPressedThisFrame();// Input.GetKeyDown(KeyCode.Mouse0);
        }


        private void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }



        public bool AttackRequested()
        {
            return _attackRequested;
        }



        void FirstPersonCameraMovement.IInputSource.GetInput(out float yawInput, out float pitchInput)
        {
            yawInput = 0.0f;
            pitchInput = 0.0f;
            if (!_canMoveCamera)
            {
                return;
            }

            Vector2 lookInput = _inputActions.Player.Look.ReadValue<Vector2>();
            if (lookInput.sqrMagnitude > 0.01f)
            {
                yawInput += lookInput.x;
                pitchInput += lookInput.y;
            }
            else
            {
                // Using old "input system" for Mouse because it feels better
                yawInput += Input.GetAxis("Mouse X");   
                pitchInput += Input.GetAxis("Mouse Y");
            }
        }



        void PhysicsMovement.IInputSource.GetInput(out Vector2 movementInput, out bool desireStartJumping, out bool desireKeepJumping)
        {
            Vector3 forwardDirection = _cameraTransform.forward;
            forwardDirection.y = 0f;
            forwardDirection.Normalize();
            Vector3 rightDirection = Vector3.Cross(Vector3.up, forwardDirection).normalized;

            Vector3 moveDirection = (forwardDirection * _movementInputForward) +
                                    (rightDirection * _movementInputRight);

            movementInput.x = moveDirection.x;
            movementInput.y = moveDirection.z;
            movementInput = Vector2.ClampMagnitude(movementInput, 1f);

            desireStartJumping = !_jumpInputBufferTimer.HasFinished();
            desireKeepJumping = _jumpInputHeld;
        }
        void PhysicsMovement.IInputSource.OnJumpInputConsumed()
        {
            _jumpInputBufferTimer.SetFinished();
        }


    }

}