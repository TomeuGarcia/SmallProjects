using UnityEngine;



namespace HeadChopping
{

    public class TransformRotator : MonoBehaviour
    {
        [SerializeField] private Vector3 _axis = Vector3.up;
        [SerializeField] private float _rotationsPerSecond = 0.25f;
        private float _frequency;

        private void OnValidate()
        {

            if (_axis.sqrMagnitude > 0.01f)
            {
                _axis.Normalize();
            }
            else
            {
                _axis = Vector3.up;
            }

            _frequency = _rotationsPerSecond * 360.0f;
        }

        private void LateUpdate()
        {
            Quaternion rotateAmount = Quaternion.AngleAxis(_frequency * Time.deltaTime, _axis);
            transform.localRotation *= rotateAmount;
        }
    }


}