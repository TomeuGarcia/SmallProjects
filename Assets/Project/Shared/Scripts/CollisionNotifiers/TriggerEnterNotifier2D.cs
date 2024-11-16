using System;
using UnityEngine;

namespace Project.Shared.Scripts.CollisionNotifiers
{
    public class TriggerEnterNotifier2D : MonoBehaviour
    {
        public Action<Collider2D> OnEnter;
        
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            OnEnter?.Invoke(other);
        }
        
    }
}