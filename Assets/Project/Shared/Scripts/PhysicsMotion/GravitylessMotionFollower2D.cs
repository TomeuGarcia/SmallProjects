using Project.Shared.Scripts.Motion;
using UnityEngine;

namespace Project.Shared.Scripts.PhysicsMotion
{
    public class GravitylessMotionFollower2D
    {
        [System.Serializable]
        public class Config
        {
            [SerializeField, Min(0)] private float _maxSpeed = 5f;
            [SerializeField, Min(0)] private float _acceleration = 300f;
            [SerializeField, Min(0)] private float _startDeceleratingDistance = 1f;
            [SerializeField, Min(0)] private float _stopMovingDistance = 0.01f;
            

            public float MaxSpeed => _maxSpeed;
            public float Acceleration => _acceleration;
            public float StartDeceleratingDistance => _startDeceleratingDistance;
            public float StopMovingDistance => _stopMovingDistance;
        }
        
        private readonly Rigidbody2D _rigidbody;
        private readonly Config _config;
        private readonly AcceleratedMotion2D _acceleratedMotion;

        private bool _isMoving;

        public GravitylessMotionFollower2D(Config config, Rigidbody2D rigidbody)
        {
            _config = config;
            _rigidbody = rigidbody;
            _rigidbody.gravityScale = 0f;
            _isMoving = false;

            _acceleratedMotion = new AcceleratedMotion2D();
        }

        public void Update(float deltaTime, Vector2 targetPosition)
        {
            Vector2 toTarget = targetPosition - _rigidbody.position;
            float toTargetDistance = toTarget.magnitude;

            bool withinStopMovingDistance = toTargetDistance < _config.StopMovingDistance;
            if (withinStopMovingDistance)
            {
                _rigidbody.velocity = Vector2.zero;
                return;
            }
            
            float decelerateDirectionMultiplier = 
                Mathf.Min(1f, Mathf.Pow((toTargetDistance / _config.StartDeceleratingDistance), 2));
            decelerateDirectionMultiplier *= _isMoving ? 1f : 0f;
            
            Vector2 toTargetDirection = toTarget / toTargetDistance;
            Vector2 direction = toTargetDirection * decelerateDirectionMultiplier;
            
            
            _acceleratedMotion.Update(deltaTime, direction, _config.MaxSpeed, _config.Acceleration);
            _rigidbody.velocity = _acceleratedMotion.CurrentVelocity;
        }

        
        public void StartMoving()
        {
            _isMoving = true;
        }
        public void StopMoving()
        {
            _isMoving = false;
        }
        
    }
}