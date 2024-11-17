using UnityEngine;

namespace Project.Shared.Scripts.Motion
{
    public class AcceleratedMotion2D
    {
        private Vector2 _currentVelocity;
        public Vector2 CurrentVelocity => _currentVelocity;

        
        public void Update(float deltaTime, Vector2 moveDirection, float maxSpeed, float acceleration)
        {
            Vector2 desiredVelocity = moveDirection * maxSpeed;
            
            float maxSpeedChange = acceleration * deltaTime;
            
            Vector2 xAxis = Vector2.right;
            float currentX = Vector2.Dot(_currentVelocity, xAxis);
            float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
            
            Vector2 yAxis = Vector2.up;
            float currentY = Vector2.Dot(_currentVelocity, yAxis);
            float newY = Mathf.MoveTowards(currentY, desiredVelocity.y, maxSpeedChange);

            _currentVelocity += xAxis * (newX - currentX);
            _currentVelocity += yAxis * (newY - currentY);
        }
        
        
    }
}