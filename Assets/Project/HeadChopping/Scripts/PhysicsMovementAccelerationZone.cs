using UnityEngine;



namespace HeadChopping
{

    public class PhysicsMovementAccelerationZone : MonoBehaviour
    {
        [SerializeField, Min(0.0f)] private float _speed = 5.0f;
        [SerializeField, Min(0.0f), Tooltip("If set to 0, _speed is applied instantly")] private float _acceleration = 10.0f;
        [SerializeField] private bool _preventFalling = true;


        private void OnTriggerEnter(Collider other)
        {
            Rigidbody rigidbody = other.attachedRigidbody;
            if (rigidbody)
            {
                Accelerate(rigidbody);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            Rigidbody rigidbody = other.attachedRigidbody;
            if (rigidbody)
            {
                Accelerate(rigidbody);
            }
        }


        private void Accelerate(Rigidbody rigidbody)
        {
            Vector3 velocity = transform.InverseTransformDirection(rigidbody.linearVelocity); // Acceelerate along the current move direction
            float rigidbodySpeed = _preventFalling ? velocity.y : Mathf.Abs(velocity.y);
            if (rigidbodySpeed >= _speed)
            {
                return;
            }

            if (_acceleration > 0.01f)
            {
                velocity.y = Mathf.MoveTowards(velocity.y, _speed, _acceleration * Time.deltaTime);
            }
            else
            {
                velocity.y = _speed;
            }

            rigidbody.linearVelocity = transform.TransformDirection(velocity);

            if (rigidbody.TryGetComponent(out PhysicsMovement physicsMovement))
            {
                physicsMovement.PreventSnapToGround();
            }
        }

    }

}
