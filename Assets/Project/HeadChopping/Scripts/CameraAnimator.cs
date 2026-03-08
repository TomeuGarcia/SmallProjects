using Unity.Cinemachine;
using UnityEngine;



namespace HeadChopping
{

    public class CameraAnimator : MonoBehaviour
    {
        [System.Serializable]
        public class Configuration
        {
            [System.Serializable]
            public class Idle
            {
                [SerializeField, Min(0)] public float waitTimeToEnter = 0.5f;
                [SerializeField, Min(0)] public float transitionTimeToEnter = 0.5f;
                [SerializeField, Min(0)] public float transitionTimeToExit = 0.2f;
                [Space(5)]
                [SerializeField, Min(0)] public float amplitudeGain = 1.0f;
                [SerializeField, Min(0)] public float frequencyGain = 1.0f;
            }

            [SerializeField] public Idle idle;
        }

        [Header("CONFIGURATION")]
        [SerializeField] private Configuration _configuration;

        [Header("COMPONENTS")]
        [SerializeField] private CinemachineBasicMultiChannelPerlin _cinemachineChannelPerlin;

        private float _idleTimeEnter;
        private float _idleTimeExit;


        private void Awake()
        {
            _idleTimeEnter = _idleTimeExit = 0.0f;
            SetAmplitudeAndFrequency(amplitudeGain: 0.0f, frequencyGain: 0.0f);
        }


        public void DoUpdate(float deltaTime, float timeSinceLastInputChange)
        {
            UpdateIdle(deltaTime, timeSinceLastInputChange);
        }


        private void UpdateIdle(float deltaTime, float timeSinceLastCameraInputChange)
        {
            if (timeSinceLastCameraInputChange > _configuration.idle.waitTimeToEnter)
            {
                float maxTransitionTime = _configuration.idle.transitionTimeToEnter;
                _idleTimeEnter = Mathf.Min(_idleTimeEnter + deltaTime, maxTransitionTime);
                float t = _idleTimeEnter / maxTransitionTime;

                float amplitudeGain = Mathf.LerpUnclamped(0.0f, _configuration.idle.amplitudeGain, t);
                float frequencyGain = Mathf.LerpUnclamped(0.0f, _configuration.idle.frequencyGain, t);
                SetAmplitudeAndFrequency(amplitudeGain, frequencyGain);

                _idleTimeExit = t * _configuration.idle.transitionTimeToExit;
            }
            else
            {
                float maxTransitionTime = _configuration.idle.transitionTimeToExit;
                _idleTimeExit = Mathf.Min(_idleTimeExit, maxTransitionTime);
                _idleTimeExit = Mathf.Max(_idleTimeExit - deltaTime, 0.0f);
                float t = _idleTimeExit / maxTransitionTime;

                float amplitudeGain = Mathf.LerpUnclamped(0.0f, _configuration.idle.amplitudeGain, t);
                float frequencyGain = Mathf.LerpUnclamped(0.0f, _configuration.idle.frequencyGain, t);
                SetAmplitudeAndFrequency(amplitudeGain, frequencyGain);

                _idleTimeEnter = 0.0f;
            }
        }

        private void SetAmplitudeAndFrequency(float amplitudeGain, float frequencyGain)
        {
            _cinemachineChannelPerlin.AmplitudeGain = amplitudeGain;
            _cinemachineChannelPerlin.FrequencyGain = frequencyGain;
        }
    }


}