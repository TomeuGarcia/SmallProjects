using System;
using System.Collections;
using UnityEngine;

namespace Project.SideWater2D.Scripts.InteractableWater
{
    public class InteractableWater : MonoBehaviour, IInteractableWater
    {
        [Header("COMPONENTS")]
        [SerializeField] private Renderer _topWaterRenderer;
        [SerializeField] private Transform _bottomWater;
        
        [Header("SIZES")]
        [SerializeField, Min(0)] private float _width = 20f;
        [SerializeField, Min(0)] private float _depth = 7f;
        [SerializeField, Min(0)] private float _maxWaveAmplitude = 1.5f;

        private IWaterInteractionsController _interactionsController;
        private Material _waterSharedMaterial;

        private Transform TopWaterTransform => _topWaterRenderer.transform;
        public Transform WaterSurface => TopWaterTransform;



        private void OnValidate()
        {
            ApplySizeCorrections();
        }

        private void OnDestroy()
        {
            _interactionsController.Clear();
        }

        private void ApplySizeCorrections()
        {
            Vector3 topWaterScale = new Vector3(_width, _maxWaveAmplitude * 2);
            Vector3 topWaterPosition = new Vector3(0f, topWaterScale.y / 2f);
            TopWaterTransform.localScale = topWaterScale;
            TopWaterTransform.localPosition = topWaterPosition;
            TopWaterTransform.localRotation = Quaternion.identity;
            

            Vector3 bottomWaterScale = new Vector3(_width, _depth - _maxWaveAmplitude);
            Vector3 bottomWaterPosition = new Vector3(0f, -bottomWaterScale.y / 2f);
            _bottomWater.localScale = bottomWaterScale;
            _bottomWater.localPosition = bottomWaterPosition;
            _bottomWater.localRotation = Quaternion.identity;
        }

        public void Init(IWaterInteractionsController interactionsController, Material waterMaterial)
        {
            _interactionsController = interactionsController;
            ApplySizeCorrections();

            _topWaterRenderer.material = waterMaterial;
        }


        public void StartEnterInteraction(IInteractableWaterUser user)
        {
            user.CurrentlyUnderWater = true;
            _interactionsController.StartInteraction(user.WaterInteractPosition, true);
        }

        public void StartExitInteraction(IInteractableWaterUser user)
        {
            user.CurrentlyUnderWater = false;
            _interactionsController.StartInteraction(user.WaterInteractPosition, false);
        }

        
    }
}