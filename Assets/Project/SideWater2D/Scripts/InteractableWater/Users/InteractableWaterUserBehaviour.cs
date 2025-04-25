using System;
using UnityEngine;

namespace Project.SideWater2D.Scripts.InteractableWater
{
    public class InteractableWaterUserBehaviour : MonoBehaviour, IInteractableWaterUser
    {
        public Vector3 WaterInteractPosition => transform.position;

        private bool _currentlyUnderWater = false;
        public bool CurrentlyUnderWater
        {
            get => _currentlyUnderWater;
            set
            {
                _currentlyUnderWater = value;
                if (value) OnEnterWater?.Invoke();
                else OnExitWater?.Invoke();
            }
        }

        public Action OnEnterWater;
        public Action OnExitWater;
    }
}