using UnityEngine;

namespace Project.SideWater2D.Scripts.InteractableWater
{
    public interface IInteractableWater
    {
        Transform WaterSurface { get; }
        void StartEnterInteraction(IInteractableWaterUser user);
        void StartExitInteraction(IInteractableWaterUser user);
    }
}