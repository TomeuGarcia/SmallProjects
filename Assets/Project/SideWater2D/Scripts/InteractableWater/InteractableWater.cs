using System;
using System.Collections;
using UnityEngine;

namespace Project.SideWater2D.Scripts.InteractableWater
{
    public class InteractableWater : MonoBehaviour
    {
        [Header("COMPONENTS")]
        [SerializeField] private Renderer _topWaterRenderer;
        [SerializeField] private Transform _bottomWater;
        
        [Header("SIZES")]
        [SerializeField, Min(0)] private float _width = 20f;
        [SerializeField, Min(0)] private float _depth = 7f;
        [SerializeField, Min(0)] private float _maxWaveAmplitude = 1.5f;

        private Material _waterSharedMaterial;

        public Transform TopWaterTransform => _topWaterRenderer.transform;


        private void OnValidate()
        {
            ApplySizeCorrections();
        }

        private void Awake()
        {
            ApplySizeCorrections();
            Init();
        }

        private void ApplySizeCorrections()
        {
            Vector3 topWaterScale = new Vector3(_width, _maxWaveAmplitude * 2);
            Vector3 topWaterPosition = new Vector3(0f, topWaterScale.y / 2f);
            TopWaterTransform.localScale = topWaterScale;
            TopWaterTransform.localPosition = topWaterPosition;
            

            Vector3 bottomWaterScale = new Vector3(_width, _depth - _maxWaveAmplitude);
            Vector3 bottomWaterPosition = new Vector3(0f, -bottomWaterScale.y / 2f);
            _bottomWater.localScale = bottomWaterScale;
            _bottomWater.localPosition = bottomWaterPosition;
        }

        private void Init()
        {
            _waterSharedMaterial = _topWaterRenderer.sharedMaterial;
            _waterSharedMaterial.SetFloat( "_CurrentRippleTime", 0f);
        }

        
        
        public void StartEnterInteraction(IInteractableWaterUser user)
        {
            user.CurrentlyUnderWater = true;
            
            _waterSharedMaterial.SetVector( "_InteractPosition", user.WaterInteractPosition);
            _waterSharedMaterial.SetFloat( "_IsEntering", 1f);
            
            StopAllCoroutines();
            StartCoroutine(PlayRipple());
        }

        public void StartExitInteraction(IInteractableWaterUser user)
        {
            user.CurrentlyUnderWater = false;
            
            _waterSharedMaterial.SetVector( "_InteractPosition", user.WaterInteractPosition);
            _waterSharedMaterial.SetFloat( "_IsEntering", 0f);

            StopAllCoroutines();
            StartCoroutine(PlayRipple());
        }


        private IEnumerator PlayRipple()
        {
            float rippleDuration = _waterSharedMaterial.GetFloat("_RippleDuration");
            float rippleTime = 0f;
            
            while (rippleTime < rippleDuration)
            {
                _waterSharedMaterial.SetFloat( "_CurrentRippleTime", rippleTime);

                rippleTime += Time.deltaTime;
                yield return null;
            }
            
            _waterSharedMaterial.SetFloat( "_CurrentRippleTime", rippleDuration);
        }
        
        
    }
}