using UnityEngine;

namespace Project.Shared.Scripts.Input
{
    public class MouseCursorPositionTracker : ICursorPositionTracker
    {
        private readonly Camera _camera;

        public MouseCursorPositionTracker(Camera camera)
        {
            _camera = camera;
        }
        
        public Vector3 GetWorldPosition()
        {
            return _camera.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
        }
    }
}