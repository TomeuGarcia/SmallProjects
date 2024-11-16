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
        }
        
        
        public void StartInteraction(Vector2 position, bool isEntering)
        {
            _waterMaterial.SetVector( _propertyInteractPosition, position);
            _waterMaterial.SetFloat( _propertyIsEntering, isEntering ? 1f : 0f);
            PlayRipple();
        }
        
        private async void PlayRipple()
        {
            float rippleDuration = _waterMaterial.GetFloat(_propertyRippleDuration);
            float rippleTime = 0f;
            
            while (rippleTime < rippleDuration)
            {
                _waterMaterial.SetFloat( _propertyCurrentRippleTime, rippleTime);

                rippleTime += Time.deltaTime;
                await Task.Yield();
            }
            
            _waterMaterial.SetFloat( _propertyCurrentRippleTime, rippleDuration);
        }
        
    }
}