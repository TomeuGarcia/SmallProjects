using UnityEngine;



namespace HeadChopping
{

    public class ChoppingPlayerInputs : FirstPersonCameraMovement.IInputSource, PhysicsMovement.IInputSource
    {
        private Transform _cameraTransform;
        private Timer _jumpInputBufferTimer;
        private float _movementInputForward;
        private float _movementInputRight;

        private bool _canMoveCamera;

        public ChoppingPlayerInputs(Transform cameraTransform)
        {
            _canMoveCamera = true;
            _cameraTransform = cameraTransform;
            _jumpInputBufferTimer = new Timer(duration: 0.15f);
            _jumpInputBufferTimer.SetFinished();
            HideCursor();
        }


        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1))
            {
                HideCursor();
                _canMoveCamera = true;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _canMoveCamera = false;
            }


            _jumpInputBufferTimer.Update(Time.deltaTime);
            if (Input.GetButtonDown("Jump"))
            {
                _jumpInputBufferTimer.Clear();
            }

            _movementInputForward = Input.GetKey(KeyCode.W) ? 1 : (Input.GetKey(KeyCode.S) ? -1 : 0);
            _movementInputRight = Input.GetKey(KeyCode.D) ? 1 : (Input.GetKey(KeyCode.A) ? -1 : 0);
        }


        private void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }




        void FirstPersonCameraMovement.IInputSource.GetInput(out float yawInput, out float pitchInput)
        {
            yawInput =   _canMoveCamera ? Input.GetAxis("Mouse X") : 0.0f;
            pitchInput = _canMoveCamera ? Input.GetAxis("Mouse Y") : 0.0f;
        }



        void PhysicsMovement.IInputSource.GetInput(out Vector2 movementInput, out bool desiredJump)
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

            desiredJump = !_jumpInputBufferTimer.HasFinished();
        }
        void PhysicsMovement.IInputSource.OnJumpInputConsumed()
        {
            _jumpInputBufferTimer.SetFinished();
        }


    }

}