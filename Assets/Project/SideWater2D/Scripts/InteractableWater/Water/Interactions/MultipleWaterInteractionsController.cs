using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Project.SideWater2D.Scripts.InteractableWater
{
    public class MultipleWaterInteractionsController : IWaterInteractionsController
    {
        private readonly SingleWaterInteractionController[] _subInteractionControllers;
        private int _currentAvailableIndex;


        public MultipleWaterInteractionsController(SingleWaterInteractionController[] subInteractionControllers)
        {
            _subInteractionControllers = subInteractionControllers;
            _currentAvailableIndex = 0;
        }
        
        
        public void StartInteraction(Vector2 position, bool isEntering)
        {
            GetNextAvailable().StartInteraction(position, isEntering);
        }

        public void Clear()
        {
            foreach (SingleWaterInteractionController subInteractionController in _subInteractionControllers)
            {
                subInteractionController.Clear();
            }
        }


        private SingleWaterInteractionController GetNextAvailable()
        {
            SingleWaterInteractionController interactionController = _subInteractionControllers[_currentAvailableIndex];
            _currentAvailableIndex = (_currentAvailableIndex + 1) % _subInteractionControllers.Length;
            
            return interactionController;
        }
        
        
    }
}