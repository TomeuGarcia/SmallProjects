using UnityEngine;

namespace Project.SideWater2D.Scripts.InteractableWater
{
    public interface IInteractableWaterUser
    {
        Vector3 WaterInteractPosition { get; }
        bool CurrentlyUnderWater { get; set; }
    }
}