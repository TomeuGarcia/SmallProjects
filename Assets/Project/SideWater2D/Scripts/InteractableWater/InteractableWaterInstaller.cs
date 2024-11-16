using System;
using UnityEngine;

namespace Project.SideWater2D.Scripts.InteractableWater
{
    public class InteractableWaterInstaller : MonoBehaviour
    {
        [Header("CONFIGURATION")]
        [SerializeField] private Material _waterSharedMaterial;
        [SerializeField] private bool _singleRippleWave = true;
        
        [Header("REFERENCES")]
        [SerializeField] private InteractableWater _interactableWater;
        [SerializeField] private InteractableWaterTrigger[] _triggers;
        

        private const string PropertyCurrentRippleTime = "_CurrentRippleTime";
        private const string PropertyInteractPosition = "_InteractPosition";
        private const string PropertyIsEntering = "_IsEntering";
        private const string PropertyRippleDuration = "_RippleDuration";
        private const string PropertyBufferCount = "_BUFFER_COUNT";
        private const string BufferPrefix = "_Buffer";
        
        private void Awake()
        {
            Material waterMaterial = _waterSharedMaterial;
            
            IWaterInteractionsController interactionsController = _singleRippleWave
                ? MakeSingleWaterInteractionController(waterMaterial)
                : MakeMultipleWaterInteractionController(waterMaterial);

            _interactableWater.Init(interactionsController, waterMaterial);

            foreach (InteractableWaterTrigger trigger in _triggers)
            {
                trigger.Init(_interactableWater);
            }
        }
        

        private SingleWaterInteractionController MakeSingleWaterInteractionController(Material waterMaterial, string propertiesPrefix = "")
        {
            return new SingleWaterInteractionController(
                waterMaterial,
                propertiesPrefix + PropertyCurrentRippleTime,
                propertiesPrefix + PropertyInteractPosition,
                propertiesPrefix + PropertyIsEntering,
                PropertyRippleDuration
            );
        }
        
        private MultipleWaterInteractionsController MakeMultipleWaterInteractionController(Material waterMaterial)
        {
            SingleWaterInteractionController[] subInteractionControllers = 
                new SingleWaterInteractionController[waterMaterial.GetInt(PropertyBufferCount)];

            for (int i = 0; i < subInteractionControllers.Length; ++i)
            {
                string propertiesPrefix = BufferPrefix + i; 
                subInteractionControllers[i] = MakeSingleWaterInteractionController(waterMaterial, propertiesPrefix);
            }
            
            return new MultipleWaterInteractionsController(subInteractionControllers);
        } 
        
    }
}