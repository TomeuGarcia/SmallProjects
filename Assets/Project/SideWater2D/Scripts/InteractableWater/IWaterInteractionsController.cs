using UnityEngine;

namespace Project.SideWater2D.Scripts.InteractableWater
{
    public interface IWaterInteractionsController
    {
        void StartInteraction(Vector2 position, bool isEntering);
    }
}