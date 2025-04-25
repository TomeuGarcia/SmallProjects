using UnityEngine;

namespace Project.SideWater2D.Scripts.InteractableWater
{
    public class InteractableWaterTrigger : MonoBehaviour
    {
        private IInteractableWater _interactableWater;
        private Vector3 WaterNormal => _interactableWater.WaterSurface.up;
        private Vector3 WaterSurfaceOrigin => _interactableWater.WaterSurface.position;


        public void Init(IInteractableWater interactableWater)
        {
            _interactableWater = interactableWater;
        }


        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out IInteractableWaterUser user))
            {
                CheckUserEntersWater(user);
            }
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out IInteractableWaterUser user))
            {
                CheckUSerExitsWater(user);
            }
        }
        
        
        private void CheckUserEntersWater(IInteractableWaterUser user)
        {
            if (user.CurrentlyUnderWater) return;

            if (CheckUserIsOutsideWater(user))
            {
                _interactableWater.StartEnterInteraction(user);
            }
        }
        
        private void CheckUSerExitsWater(IInteractableWaterUser user)
        {
            if (!user.CurrentlyUnderWater) return;

            if (CheckUserIsOutsideWater(user))
            {
                _interactableWater.StartExitInteraction(user);
            }
        }


        private bool CheckUserIsOutsideWater(IInteractableWaterUser user)
        {
            Vector3 surfaceOriginToUserDirection = (user.WaterInteractPosition - WaterSurfaceOrigin).normalized;
            return Vector3.Dot(surfaceOriginToUserDirection, WaterNormal) > 0;
        }
    }
}