using UnityEngine;


namespace HeadChopping
{
    public class Timer
    {
        private float _duration;
        private float _counter;

        public float Time => _counter;
        public float Duration => _duration;
        public float RemainingTime => _duration - _counter;
        public float NormalizedTime => Mathf.Clamp01(_counter / _duration);

        public Timer(float duration)
        {
            SetDuration(duration);
            Clear();
        }
        public Timer(float duration, float startTime)
        {
            SetDuration(duration);
            Update(startTime);
        }

        public void SetDuration(float newDuration)
        {
            _duration = Mathf.Max(newDuration, 0f);
        }

        public void Clear()
        {
            _counter = 0;
        }
        public void SetFinished()
        {
            _counter = _duration;
        }

        public void Update(float deltaTime)
        {
            _counter += deltaTime;
            _counter = Mathf.Clamp(_counter, 0, _duration);
        }

        public bool HasFinished()
        {
            return _counter >= _duration;
        }




    }
}