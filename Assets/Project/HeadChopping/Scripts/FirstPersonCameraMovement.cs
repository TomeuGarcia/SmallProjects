using UnityEngine;


namespace HeadChopping
{


    public class FirstPersonCameraMovement : MonoBehaviour
    {
        [SerializeField] private Transform _cameraTransform;
        [SerializeField, Min(1.0f)] private float _sensitivity = 1000.0f;
        [SerializeField, Range(0.0f, 89.0f)] private float _maxPitchDown = 45.0f;
        [SerializeField, Range(0.0f, 89.0f)] private float _maxPitchUp = 80.0f;

        private float _accumulatedYaw;
        private float _accumulatedPitch;
        const float Clamp = 0.5f;

        private bool _canMoveCamera;

        public Transform CameraTransform => _cameraTransform;


        private void Awake()
        {
            _accumulatedYaw = 0.0f;
            _accumulatedPitch = 0.0f;
            _canMoveCamera = true;
        }

        private void Start()
        {
            HideCursor();
        }

        public void DoUpdate()
        {
            if (_canMoveCamera)
            {
                UpdateCameraRotation();
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                HideCursor();
                _canMoveCamera = true;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _canMoveCamera = false;
            }
        }

        private void UpdateCameraRotation()
        {
            float deltaSensitivity = _sensitivity * Time.deltaTime;
            float yawAnglesToAdd = Mathf.Clamp(Input.GetAxis("Mouse X"), -Clamp, Clamp) * deltaSensitivity;
            float pitchAnglesToAdd = Mathf.Clamp(Input.GetAxis("Mouse Y"), -Clamp, Clamp) * -deltaSensitivity;

            Vector3 cameraForward = _cameraTransform.forward;
            Vector3 cameraRight = _cameraTransform.right;

            Vector3 flattenedCameraForward = cameraForward.WithY(0).normalized;
            Quaternion flattenedCameraRotation = Quaternion.LookRotation(flattenedCameraForward, Vector3.up);
            float pitchAngle = Vector3.SignedAngle(flattenedCameraForward, cameraForward, axis: cameraRight);

            _accumulatedYaw += yawAnglesToAdd;
            _accumulatedPitch = Mathf.Clamp(_accumulatedPitch + pitchAnglesToAdd, -_maxPitchUp, _maxPitchDown);

            Quaternion cameraRotation =
                Quaternion.AngleAxis(_accumulatedYaw, Vector3.up) *
                Quaternion.AngleAxis(_accumulatedPitch, Vector3.right);

            _cameraTransform.rotation = cameraRotation;
        }

        private void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }

}