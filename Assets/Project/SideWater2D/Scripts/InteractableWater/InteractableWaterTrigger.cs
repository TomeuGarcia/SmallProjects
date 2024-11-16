using System;
using Project.Shared.Scripts.CollisionNotifiers;
using UnityEngine;

namespace Project.SideWater2D.Scripts.InteractableWater
{
    public class InteractableWaterTrigger : MonoBehaviour
    {
        [Header("COMPONENTS")] 
        [SerializeField] private InteractableWater _interactableWater;

        private Vector3 WaterNormal => _interactableWater.TopWaterTransform.up;
        private Vector3 WaterSurfaceOrigin => _interactableWater.TopWaterTransform.position;


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

            if (CheckUserComingFromOutside(user))
            {
                _interactableWater.StartEnterInteraction(user);
            }
        }
        
        private void CheckUSerExitsWater(IInteractableWaterUser user)
        {
            if (!user.CurrentlyUnderWater) return;

            if (CheckUserComingFromOutside(user))
            {
                _interactableWater.StartExitInteraction(user);
            }
        }


        private bool CheckUserComingFromOutside(IInteractableWaterUser user)
        {
            Vector3 surfaceOriginToUserDirection = (user.WaterInteractPosition - WaterSurfaceOrigin).normalized;
            return Vector3.Dot(surfaceOriginToUserDirection, WaterNormal) > 0;
        }
    }
}