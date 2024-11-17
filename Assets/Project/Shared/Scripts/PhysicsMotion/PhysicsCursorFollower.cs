using System;
using Project.Shared.Scripts.Input;
using UnityEngine;

namespace Project.Shared.Scripts.PhysicsMotion
{
    public class PhysicsCursorFollower : MonoBehaviour
    {
        [Header("CONFIGURATION")]
        [SerializeField] private GravitylessMotionFollower2D.Config _motionConfig;
        [SerializeField] private KeyCode _requiredKey = KeyCode.Mouse0;
        
        [Header("COMPONENTS")]
        [SerializeField] private Rigidbody2D _rigidbody;
        
        
        private ICursorPositionTracker _cursorPositionTracker;
        private GravitylessMotionFollower2D _motionFollower;
        
        private void Awake()
        {
            _cursorPositionTracker = new MouseCursorPositionTracker(Camera.main);
            _motionFollower = new GravitylessMotionFollower2D(_motionConfig, _rigidbody);
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(_requiredKey))
            {
                _motionFollower.StartMoving();
            }
            else if (UnityEngine.Input.GetKeyUp(_requiredKey))
            {
                _motionFollower.StopMoving();
            }        
        }

        private void FixedUpdate()
        {
            _motionFollower.Update(Time.fixedDeltaTime, _cursorPositionTracker.GetWorldPosition());
        }
        
    }
}