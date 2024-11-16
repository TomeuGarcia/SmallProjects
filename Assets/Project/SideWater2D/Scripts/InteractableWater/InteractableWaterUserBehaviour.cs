using UnityEngine;

namespace Project.SideWater2D.Scripts.InteractableWater
{
    public class InteractableWaterUserBehaviour : MonoBehaviour, IInteractableWaterUser
    {
        public Vector3 WaterInteractPosition => transform.position;
        public bool CurrentlyUnderWater { get; set; } = false;
    }
}