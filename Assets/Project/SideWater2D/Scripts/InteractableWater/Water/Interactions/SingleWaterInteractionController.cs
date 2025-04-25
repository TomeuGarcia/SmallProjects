using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Project.SideWater2D.Scripts.InteractableWater
{
    public class SingleWaterInteractionController : IWaterInteractionsController
    {
        private readonly Material _waterMaterial;
        private readonly int _propertyCurrentRippleTime;
        private readonly int _propertyInteractPosition;
        private readonly int _propertyIsEntering;
        private readonly int _propertyRippleDuration;

        private int _playCount;
        private bool Playing => _playCount > 0;
        
        public SingleWaterInteractionController(
            Material waterMaterial,
            string propertyCurrentRippleTime,
            string propertyInteractPosition,
            string propertyIsEntering,
            string propertyRippleDuration
        )
        {
            _waterMaterial = waterMaterial;
            _propertyCurrentRippleTime = Shader.PropertyToID(propertyCurrentRippleTime);
            _propertyInteractPosition = Shader.PropertyToID(propertyInteractPosition);
            _propertyIsEntering = Shader.PropertyToID(propertyIsEntering);
            _propertyRippleDuration = Shader.PropertyToID(propertyRippleDuration);

            _playCount = 0;
            SetRippleTime(0f);
        }

        
        public void StartInteraction(Vector2 position, bool isEntering)
        {
            _waterMaterial.SetVector( _propertyInteractPosition, position);
            _waterMaterial.SetFloat( _propertyIsEntering, isEntering ? 1f : 0f);
            PlayRipple();
        }

        public void Clear()
        {
            _playCount = 0;
        }
        
        private async void PlayRipple()
        {
            ++_playCount;
            float rippleDuration = _waterMaterial.GetFloat(_propertyRippleDuration);
            float rippleTime = 0f;
            
            while (rippleTime < rippleDuration && Playing)
            {
                SetRippleTime(rippleTime);

                rippleTime += Time.deltaTime;
                await Task.Yield();
            }

            if (!Playing)
            {
                return;
            }

            SetRippleTime(rippleDuration);

            --_playCount;
        }

        private void SetRippleTime(float rippleTime)
        {
            _waterMaterial.SetFloat(_propertyCurrentRippleTime, rippleTime);
        }
    }
}