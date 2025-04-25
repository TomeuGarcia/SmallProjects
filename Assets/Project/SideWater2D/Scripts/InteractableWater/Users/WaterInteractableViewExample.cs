using System;
using UnityEngine;

namespace Project.SideWater2D.Scripts.InteractableWater
{
    public class WaterInteractableViewExample : MonoBehaviour
    {
        [Header("CONFIG")]
        [SerializeField] private Color _underWaterColor = Color.blue;

        [Header("COMPONENTS")] 
        [SerializeField] private InteractableWaterUserBehaviour _interactableWaterUser;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private Color _normalColor;


        private void Awake()
        {
            _normalColor = _spriteRenderer.color;
        }

        private void OnEnable()
        {
            _interactableWaterUser.OnEnterWater += OnEnterWater;
            _interactableWaterUser.OnExitWater += OnExitWater;
        }
        private void OnDisable()
        {
            _interactableWaterUser.OnEnterWater -= OnEnterWater;
            _interactableWaterUser.OnExitWater -= OnExitWater;
        }


        private void OnEnterWater()
        {
            SetColor(_underWaterColor);
        }
        
        private void OnExitWater()
        {
            SetColor(_normalColor);
        }


        private void SetColor(Color color)
        {
            _spriteRenderer.color = color;
        }
    }
}