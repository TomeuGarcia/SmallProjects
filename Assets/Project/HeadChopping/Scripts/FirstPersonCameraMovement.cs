using UnityEngine;


namespace HeadChopping
{


    public class FirstPersonCameraMovement : MonoBehaviour
    {
        public interface IInputSource
        {
            void GetInput(out float yawInput, out float pitchInput);
        }

        private class DefaultInputSource : IInputSource
        {
            void IInputSource.GetInput(out float yawInput, out float pitchInput)
            {
                yawInput = Input.GetAxis("Mouse X");
                pitchInput = Input.GetAxis("Mouse Y");
            }
        }


        [SerializeField] private Transform _cameraTransform;
        [SerializeField, Min(1.0f)] private float _sensitivity = 1000.0f;
        [SerializeField, Range(0.0f, 89.0f)] private float _maxPitchDown = 45.0f;
        [SerializeField, Range(0.0f, 89.0f)] private float _maxPitchUp = 80.0f;

        const float Clamp = 0.5f;
        private float _accumulatedYaw;
        private float _accumulatedPitch;
        private float _timeSinceLastInputChange;

        private IInputSource _inputSource = null;

        public Transform CameraTransform => _cameraTransform;


        private void Awake()
        {
            _accumulatedYaw = 0.0f;
            _accumulatedPitch = 0.0f;
            _timeSinceLastInputChange = 0.0f;
        }

        public void AwakeConfigure(IInputSource inputSource)
        {
            _inputSource = inputSource;
        }

        private void Start()
        {
            if (_inputSource == null) _inputSource = new DefaultInputSource();
        }

        public void DoUpdate(float deltaTime)
        {
            _inputSource.GetInput(out float yawInput, out float pitchInput);
            bool inputChanged = !(yawInput + pitchInput < 0.001f);
            if (inputChanged)
            {
                _timeSinceLastInputChange = 0.0f;
            }
            else
            {
                _timeSinceLastInputChange += deltaTime;
            }

            float deltaSensitivity = _sensitivity * deltaTime;
            float yawAnglesToAdd =   Mathf.Clamp(yawInput,   -Clamp, Clamp) * deltaSensitivity;
            float pitchAnglesToAdd = Mathf.Clamp(pitchInput, -Clamp, Clamp) * -deltaSensitivity;

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

        public float GetTimeSinceLastInputChange()
        {
            return _timeSinceLastInputChange;
        }

    }

}