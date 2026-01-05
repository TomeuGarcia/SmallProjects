using UnityEngine;



namespace HeadChopping
{

    public class ChoppingPlayer : MonoBehaviour, PhysicsMovement.IInputSource
    {
        [SerializeField] private PhysicsMovement _physicsMovement;
        [SerializeField] private Camera _camera;
        [SerializeField, Min(1.0f)] private float _sensitivity = 1.0f;
        [SerializeField, Range(0.0f, 89.0f)] private float _maxPitchDown = 45.0f;
        [SerializeField, Range(0.0f, 89.0f)] private float _maxPitchUp = 80.0f;


        private void Awake()
        {
            _physicsMovement.AwakeConfigure(this, autoUpdate: false);
        }

        private void Update()
        {
            UpdateCamera();
            _physicsMovement.DoUpdate();
        }

        private float _accumulatedYaw = 0;
        private float _accumulatedPitch = 0;
        private void UpdateCamera()
        {
            float deltaSensitivity = _sensitivity * Time.deltaTime;
            float yawAnglesToAdd = Input.GetAxis("Mouse X") * deltaSensitivity;
            float pitchAnglesToAdd = Input.GetAxis("Mouse Y") * -deltaSensitivity;

            Vector3 cameraForward = _camera.transform.forward;
            Vector3 cameraRight = _camera.transform.right;

            Vector3 flattenedCameraForward = cameraForward.WithY(0).normalized;
            Quaternion flattenedCameraRotation = Quaternion.LookRotation(flattenedCameraForward, Vector3.up);
            float pitchAngle = Vector3.SignedAngle(flattenedCameraForward, cameraForward, axis: cameraRight);

            _accumulatedYaw += yawAnglesToAdd;
            _accumulatedPitch = Mathf.Clamp(_accumulatedPitch + pitchAnglesToAdd, -_maxPitchUp, _maxPitchDown);

            Quaternion cameraRotation = 
                Quaternion.AngleAxis(_accumulatedYaw, Vector3.up) * 
                Quaternion.AngleAxis(_accumulatedPitch, Vector3.right);

            _camera.transform.rotation = cameraRotation;


            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Confined;
            }
            //if (Input.GetKeyDown(KeyCode.Escape))
            //{
            //    Cursor.visible = true;
            //    Cursor.lockState = CursorLockMode.None;
            //}
        }


        void PhysicsMovement.IInputSource.GetInput(out Vector2 movementInput, out bool desiredJump)
        {
            float forward = Input.GetAxis("Vertical");
            float right = Input.GetAxis("Horizontal");

            Vector3 forwardDirection = _camera.transform.forward;
            forwardDirection.y = 0f;
            forwardDirection.Normalize();
            Vector3 rightDirection = Vector3.Cross(Vector3.up, forwardDirection).normalized;

            Vector3 moveDirection = (forwardDirection * forward) +
                                    (rightDirection * right);

            movementInput.x = moveDirection.x;
            movementInput.y = moveDirection.z;
            movementInput = Vector2.ClampMagnitude(movementInput, 1f);

            desiredJump = Input.GetButtonDown("Jump");
        }
        void PhysicsMovement.IInputSource.OnJumpInputConsumed()
        {
        }
    }


}