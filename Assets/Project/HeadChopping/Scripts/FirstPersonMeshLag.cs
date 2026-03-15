using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;


namespace HeadChopping
{

    public class FirstPersonMeshLag : MonoBehaviour
    {
        [System.Serializable]
        private class Configuration
        {
            [System.Serializable]
            public class PositionConfiguration
            {
                [SerializeField, Min(0)] public float dampSpeed = 3000.0f;                
                [SerializeField, Min(0)] public float sqrDistanceToDetectMovement = 0.00001f;                
            }

            [System.Serializable]
            public class RotationConfiguration
            {
                [SerializeField, Min(0)] public float dampSpeed = 10.0f;
                [SerializeField, Min(0)] public float deadZoneAngleStart = 5.0f;
                [SerializeField, Min(0)] public float deadZoneAngleFinish = 1.0f;
                [SerializeField, Min(0)] public float exitDeadZoneTimeForFullSpeed = 0.25f;
            }

            [System.Serializable]
            public class TiltConfiguration
            {
                [SerializeField] public Vector3 localRotationAxis = Vector3.back;
                [SerializeField, Min(0)] public float dampSpeed = 10.0f;
                [SerializeField, Min(0)] public float angleChangeSpeed = 200.0f;
                [SerializeField, Min(0)] public float baseAngleMultiplier = 0.02f;
                [SerializeField, Min(0)] public float extraAngleMultiplierMaxTime = 0.1f;
                [SerializeField, Min(0)] public float maxTime = 1.0f;
                [Space(5)]
                [SerializeField, Min(0)] public float sidewaysDisplacementMultiplier = 100.0f;
                [SerializeField, Min(0)] public float sidewaysDisplacementChangeMaxSpeed = 80.0f;
                [SerializeField, Min(0)] public float upwardsDisplacementMultiplier = 1.0f;
                [Space(5)]
                [SerializeField, Min(0)] public float rotationChangeMaxSpeed = 200.0f;
            }

            [System.Serializable]
            public class DisableConfiguration
            {
                [SerializeField, Min(0)] public float dampSpeedRotation = 300.0f;
                [SerializeField, Min(0)] public float dampSpeedTilt = 300.0f;
            }


            [SerializeField] public PositionConfiguration position;
            [SerializeField] public RotationConfiguration rotation;
            [SerializeField] public TiltConfiguration tilt;
            [SerializeField] public DisableConfiguration disable;

            public void Validate()
            {
                rotation.deadZoneAngleStart = Mathf.Max(rotation.deadZoneAngleStart, rotation.deadZoneAngleFinish + 0.1f);
                tilt.localRotationAxis.Normalize();
            }
        }


        [Header("COMPONENTS")]
        [SerializeField] private CinemachineCamera _cinemachineCameraToTrack;
        [SerializeField] private Transform _transformToUpdate;
        [SerializeField] private Transform _transformToTilt;
        [SerializeField, Tooltip("Noise, smoothing, etc.")] private bool _withCorrections;
        [SerializeField, Min(1)] private int _testingFrameRate = 30;
        [SerializeField, Range(1, 4)] private int _testingVSync = 4;

        [Header("CONFIGURATION")]
        [SerializeField] private Configuration _configuration;

        private bool _exitedDeadZone;
        private float _timestamp_exitedDeadZone;

        private bool _isMoving;
        private int _framesIsMoving;
        private float _timestamp_startedMoving;

        private float _currentTiltAngle;


        private ICinemachineCamera CinemachineCameraToTrack => _cinemachineCameraToTrack;


        private void OnValidate()
        {
            _configuration.Validate();
        }

        private void OnEnable()
        {            
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
        }
        private void OnDisable()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
        }

        private void Start()
        {
            _exitedDeadZone = false;
            _timestamp_exitedDeadZone = Time.timeSinceLevelLoad;

            _isMoving = false;
            _framesIsMoving = 0;
            _timestamp_startedMoving = Time.timeSinceLevelLoad;

            _currentTiltAngle = 0.0f;
        }

        private bool IsRotationDisabled()
        {
            return false;
        }
        private bool IsTiltDisabled()
        {
            return false;
        }


        private void OnCameraUpdated(CinemachineBrain cinemachineBrain)
        {
            if (cinemachineBrain.ActiveVirtualCamera != CinemachineCameraToTrack)
            {
                return;
            }


            CameraState cameraState = cinemachineBrain.ActiveVirtualCamera.State;

            Vector3 targetPosition = cameraState.RawPosition;
            Quaternion targetRotation = cameraState.RawOrientation;

            if (_withCorrections)
            {
                targetPosition += cameraState.PositionCorrection;
                targetRotation *= cameraState.OrientationCorrection;
            }


            UpdateTransform(targetPosition, targetRotation, Time.deltaTime);            
        }


        private void UpdateTransform(Vector3 targetPosition, Quaternion targetRotation, float deltaTime)
        {
            Application.targetFrameRate = _testingFrameRate;
            QualitySettings.vSyncCount = _testingVSync;

            UpdateTransform_Position(targetPosition, deltaTime, out Vector3 newPosition, out bool moved);
            UpdateTransform_Rotation(targetRotation, deltaTime, moved, out Quaternion newRotation);
            UpdateTilit(_transformToUpdate.position, newPosition, _transformToUpdate.rotation, newRotation, deltaTime, out Quaternion newTiltRotation);

            _transformToUpdate.position = newPosition;
            _transformToUpdate.rotation = newRotation;
            _transformToTilt.localRotation = newTiltRotation;
        }


        private void UpdateTransform_Position(Vector3 targetPosition, float deltaTime, out Vector3 newPosition, out bool moved)
        {
            Vector3 currentPosition = _transformToUpdate.position;

            float positionT = deltaTime * _configuration.position.dampSpeed;
            newPosition = Vector3.Lerp(currentPosition, targetPosition, positionT);

            float sqrDistance = (currentPosition - newPosition).sqrMagnitude;

            moved = sqrDistance > _configuration.position.sqrDistanceToDetectMovement;
            if (!moved)
            {
                newPosition = targetPosition;

                if (_isMoving)
                {
                    _isMoving = false;
                }
                _framesIsMoving = 0;
            }
            else
            {
                if (!_isMoving)
                {
                    _isMoving = true;
                    _timestamp_startedMoving = Time.timeSinceLevelLoad;
                }
                _framesIsMoving += 1;
            }
            
        }

        private void UpdateTransform_Rotation(Quaternion targetRotation, float deltaTime, bool moved, out Quaternion newRotation)
        {
            Quaternion currentRotation = _transformToUpdate.rotation;

            if (IsRotationDisabled())
            {
                newRotation = Quaternion.Slerp(currentRotation, targetRotation, deltaTime * _configuration.disable.dampSpeedRotation);
                return;
            }


            float angleToTarget = Quaternion.Angle(currentRotation, targetRotation);
            
            if (_exitedDeadZone && angleToTarget < _configuration.rotation.deadZoneAngleFinish)
            {
                _exitedDeadZone = false;
                newRotation = currentRotation;
                return;
            }
            if (!_exitedDeadZone)
            {
                if (angleToTarget > _configuration.rotation.deadZoneAngleStart || _framesIsMoving == 1)
                {
                    _exitedDeadZone = true;
                    _timestamp_exitedDeadZone = Time.timeSinceLevelLoad;
                }
                else
                {
                    newRotation = currentRotation;
                    return;
                }
            }


            // Ease In multiplier
            float timeMult = Mathf.Clamp01((Time.timeSinceLevelLoad - _timestamp_exitedDeadZone) / _configuration.rotation.exitDeadZoneTimeForFullSpeed);

            float rotationT = deltaTime * _configuration.rotation.dampSpeed;
            rotationT *= timeMult;
            newRotation = Quaternion.Slerp(currentRotation, targetRotation, rotationT);                      
        }


        private void UpdateTilit(Vector3 currentPosition, Vector3 newPosition, Quaternion currentRotation, Quaternion newRotation, 
            float deltaTime, out Quaternion newTiltRotation)
        {
            Quaternion currentTiltRotation = _transformToTilt.localRotation;

            if (IsTiltDisabled())
            {
                newTiltRotation = Quaternion.Slerp(currentTiltRotation, Quaternion.identity, deltaTime * _configuration.disable.dampSpeedTilt);
                return;
            }


            float rotationSign = Mathf.Sign(Vector3.SignedAngle(currentRotation * Vector3.forward, newRotation * Vector3.forward, Vector3.up));
            float rotationAngle = Quaternion.Angle(currentRotation, newRotation);
            float rotationSpeed = rotationAngle / deltaTime;

            float angleMultiplier = _configuration.tilt.baseAngleMultiplier;
            float timeMult = Mathf.Clamp01((Time.timeSinceLevelLoad - _timestamp_exitedDeadZone) / _configuration.tilt.maxTime);
            angleMultiplier += timeMult * _configuration.tilt.extraAngleMultiplierMaxTime;

            float tiltAngle = Mathf.Min(rotationSpeed, _configuration.tilt.rotationChangeMaxSpeed);
            tiltAngle *= angleMultiplier * rotationSign;


            Vector3 displacement = (newPosition - currentPosition);
            float sidewaysDisplacement = Vector3.Dot(displacement, _transformToUpdate.right);
            float sidewaysSpeed = sidewaysDisplacement / deltaTime;

            if (Mathf.Abs(tiltAngle) < 0.0000001f  && Mathf.Abs(sidewaysSpeed) > 0.1f) // Use position change instead
            {
                angleMultiplier = _configuration.tilt.baseAngleMultiplier;
                timeMult = Mathf.Clamp01((Time.timeSinceLevelLoad - _timestamp_startedMoving) / _configuration.tilt.maxTime);
                angleMultiplier += timeMult * _configuration.tilt.extraAngleMultiplierMaxTime;

                tiltAngle = sidewaysSpeed * _configuration.tilt.sidewaysDisplacementMultiplier;
                tiltAngle = Mathf.Clamp(tiltAngle, -_configuration.tilt.sidewaysDisplacementChangeMaxSpeed, _configuration.tilt.sidewaysDisplacementChangeMaxSpeed);
                tiltAngle *= angleMultiplier;
            }


            _currentTiltAngle = Mathf.MoveTowards(_currentTiltAngle, tiltAngle, deltaTime * _configuration.tilt.angleChangeSpeed);
            Quaternion targetTiltRotation = Quaternion.AngleAxis(_currentTiltAngle, _configuration.tilt.localRotationAxis);


            float upwardsDisplacement = Vector3.Dot(displacement, _transformToUpdate.up);
            float upwardsSpeed = upwardsDisplacement / deltaTime;
            if (Mathf.Abs(upwardsSpeed) > 0.1f)
            {
                upwardsSpeed *= _configuration.tilt.upwardsDisplacementMultiplier;
                targetTiltRotation *= Quaternion.AngleAxis(-upwardsSpeed, _transformToUpdate.right);
            }


            float tiltT = deltaTime * _configuration.tilt.dampSpeed;
            newTiltRotation = Quaternion.Slerp(currentTiltRotation, targetTiltRotation, tiltT);
        }


    }


}