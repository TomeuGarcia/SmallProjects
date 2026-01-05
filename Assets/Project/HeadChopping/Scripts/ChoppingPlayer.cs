using UnityEngine;



namespace HeadChopping
{

    public class ChoppingPlayer : MonoBehaviour
    {
        public class PlayerInput : PhysicsMovement.IInputSource
        {
            private Transform _cameraTransform;
            private Timer _jumpInputBufferTimer;
            private float _movementInputForward;
            private float _movementInputRight;

            public PlayerInput(Transform cameraTransform)
            {
                _cameraTransform = cameraTransform;
                _jumpInputBufferTimer = new Timer(duration: 0.15f);
                _jumpInputBufferTimer.SetFinished();
            }

            public void Update()
            {
                _jumpInputBufferTimer.Update(Time.deltaTime);
                if (Input.GetButtonDown("Jump"))
                {
                    _jumpInputBufferTimer.Clear();
                }

                _movementInputForward = Input.GetKey(KeyCode.W) ? 1 : (Input.GetKey(KeyCode.S) ? -1 : 0);
                _movementInputRight =   Input.GetKey(KeyCode.D) ? 1 : (Input.GetKey(KeyCode.A) ? -1 : 0);
            }

            void PhysicsMovement.IInputSource.GetInput(out Vector2 movementInput, out bool desiredJump)
            {
                Vector3 forwardDirection = _cameraTransform.forward;
                forwardDirection.y = 0f;
                forwardDirection.Normalize();
                Vector3 rightDirection = Vector3.Cross(Vector3.up, forwardDirection).normalized;

                Vector3 moveDirection = (forwardDirection * _movementInputForward) +
                                        (rightDirection * _movementInputRight);

                Debug.Log($"moveDirection {moveDirection}");
                movementInput.x = moveDirection.x;
                movementInput.y = moveDirection.z;
                movementInput = Vector2.ClampMagnitude(movementInput, 1f);

                desiredJump = !_jumpInputBufferTimer.HasFinished();
            }
            void PhysicsMovement.IInputSource.OnJumpInputConsumed()
            {
                _jumpInputBufferTimer.SetFinished();
            }
        }



        [SerializeField] private FirstPersonCameraMovement _firstPersonCameraMovement;
        [SerializeField] private PhysicsMovement _physicsMovement;
        private PlayerInput _playerInput;


        private void Awake()
        {
            _playerInput = new PlayerInput(_firstPersonCameraMovement.CameraTransform);
            _physicsMovement.AwakeConfigure(_playerInput, autoUpdate: false);
        }

        private void Update()
        {
            _playerInput.Update();
            _firstPersonCameraMovement.DoUpdate();
            _physicsMovement.DoUpdate();
        }


    }


}