using System;
using UnityEngine;

namespace Project.Shared.Scripts.CollisionNotifiers
{
    public class TriggerEnterNotifier2DPair : MonoBehaviour
    {
        [SerializeField] private TriggerEnterNotifier2D _triggerA;
        [SerializeField] private TriggerEnterNotifier2D _triggerB;

        public Action<Collider2D> OnEnterA;
        public Action<Collider2D> OnEnterB;

        private void OnEnable()
        {
            _triggerA.OnEnter += OnTriggerAEnter;
            _triggerB.OnEnter += OnTriggerBEnter;
        }
        private void OnDisable()
        {
            _triggerA.OnEnter -= OnTriggerAEnter;
            _triggerB.OnEnter -= OnTriggerBEnter;
        }


        private void OnTriggerAEnter(Collider2D other)
        {
            OnEnterA?.Invoke(other);
        }
        private void OnTriggerBEnter(Collider2D other)
        {
            OnEnterA?.Invoke(other);
        }
    }
}