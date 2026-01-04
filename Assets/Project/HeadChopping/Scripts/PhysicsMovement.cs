using UnityEngine;



namespace HeadChopping
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PhysicsMovement : MonoBehaviour
    {
        [System.Serializable]
        public class Configuration
        {
            [SerializeField, Min(0.0f)] public float maxSpeed = 10.0f;
            [SerializeField, Min(0.0f)] public float maxAcceleration = 80.0f;
            [SerializeField, Min(0.0f)] public float maxAirAcceleration = 30.0f;
            [SerializeField, Min(1.0f)] public float jumpHeight = 2.0f;
            [SerializeField, Min(0.0f)] public float gravityMultiplier = 1.0f;
            [SerializeField, Range(0, 5)] public int maxAirJumps = 0;
            [SerializeField, Range(0, 90)] private float _maxGroundAngle = 50.0f;
            [SerializeField, Min(0.0f)] public float speedThresholdToIgnoreSnap = 100.0f;
            [SerializeField, Min(0.0f)] public float groundProbeExtraDistance = 1.0f;
            [SerializeField] public LayerMask probeMask = -1;

            public float MinGroundDotProduct { get; private set; } = 0.0f;

            public void Validate()
            {
                MinGroundDotProduct = Mathf.Cos(_maxGroundAngle * Mathf.Deg2Rad);
            }
        }


        [Header("CONFIGURATION")]
        [SerializeField] private Configuration _configuration;

        [Header("COMPONENTS")]
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private CapsuleCollider _capsuleCollider;


        private Vector3 _currentVelocity;
        private Vector3 _desiredVelocity;
        private bool _desiredJump;
        private int _jumpPhase;
        private Vector3 _contactNormal;
        private int _groundContactCount;
        private Vector3 _steepNormal;
        private int _steepContactCount;
        private int _stepsSinceLastGrounded;
        private int _stepsSinceLastJump;

        public Configuration Config => _configuration;
        private bool OnGround => _groundContactCount > 0;
        private bool OnSteep => _steepContactCount > 0;
        private bool OnAir => !OnGround && !OnSteep;
        private bool _wasOnGround;


        private void OnValidate()
        {
            _configuration.Validate();
        }

        private void Awake()
        {
            _configuration.Validate();
            _groundContactCount = 0;
            _jumpPhase = 0;

            _rigidbody.useGravity = false; // Rigidbody's gravity must be DISABED   (Tomeu)
        }


        private void Update()
        {
            Vector2 movementInput;
            movementInput.x = Input.GetAxis("Horizontal");
            movementInput.y = Input.GetAxis("Vertical");
            movementInput = Vector2.ClampMagnitude(movementInput, 1f);

            _desiredVelocity = new Vector3(movementInput.x, 0f, movementInput.y) * Config.maxSpeed;
            _desiredJump |= Input.GetButtonDown("Jump");
        }

        private void FixedUpdate()
        {
            UpdateState();
            AdjustVelocity();

            if (_desiredJump)
            {
                _desiredJump = false;
                Jump();
            }

            //Debug.Log($"OnGround {OnGround}     ContactNormal {_contactNormal}");


            ApplyGravity();


            _rigidbody.linearVelocity = _currentVelocity;
            ClearState();
        }


        private void OnCollisionEnter(Collision collision)
        {
            EvaluateCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            EvaluateCollision(collision);
        }

        private void EvaluateCollision(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                if (normal.y >= Config.MinGroundDotProduct)
                {
                    _groundContactCount += 1;
                    _contactNormal += normal;
                }
                else if (normal.y > -0.01f)
                {
                    _steepContactCount += 1;
                    _steepNormal += normal;
                }
            }
        }

        private bool EvaluateCollisionAhead(out Vector3 hitNormal, out float hitDistanceForContact)
        {
            float distance = _capsuleCollider.radius + (_desiredVelocity.magnitude * Time.deltaTime);
            Vector3 direction = _desiredVelocity.normalized;
            Vector3 position = _rigidbody.position;
            position.y -= (_capsuleCollider.height / 2) - 0.1f;

            if (Physics.Raycast(position, direction, out RaycastHit hit, distance))
            {
                hitNormal = hit.normal;
                hitDistanceForContact = Mathf.Max(0.0f, hit.distance - _capsuleCollider.radius);
                return true;
            }

            hitNormal = Vector3.zero;
            hitDistanceForContact = 0.0f;
            return false;
        }




        private void UpdateState()
        {
            _stepsSinceLastGrounded += 1;
            _stepsSinceLastJump += 1;
            _currentVelocity = _rigidbody.linearVelocity;

            bool snapToGround = SnapToGround();
            bool onSteep = CheckSteepContacts();
            if (OnGround || snapToGround || onSteep)
            {
                Debug.Log($"OnGround {OnGround},    snapToGround {snapToGround},    onSteepCrevasse {onSteep}");
                
                _stepsSinceLastGrounded = 0;
                if (_stepsSinceLastJump > 1)
                {
                    _jumpPhase = 0;
                }
                if (_groundContactCount > 1)
                {
                    _contactNormal.Normalize();
                }
            }
            else
            {
                _contactNormal = Vector3.up;
            }
        }

        private void AdjustVelocity()
        {
            if (EvaluateCollisionAhead(out Vector3 hitNormal, out float hitDistanceForContact))
            {
                if (hitNormal.y < Config.MinGroundDotProduct)
                {
                    Vector3 counterDirection = ProjectOnContactPlane(hitNormal).normalized;
                    Vector3 repulsionVelocity = -Vector3.Dot(counterDirection, _desiredVelocity) * counterDirection;
                    Debug.Log($"repulsionVelocity {repulsionVelocity}");
                    _desiredVelocity += repulsionVelocity;
                }
            }

            Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
            Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

            float currentX = Vector3.Dot(_currentVelocity, xAxis);
            float currentZ = Vector3.Dot(_currentVelocity, zAxis);

            float acceleration = OnGround ? Config.maxAcceleration : Config.maxAirAcceleration;
            float maxSpeedChange = acceleration * Time.deltaTime;

            float newX = Mathf.MoveTowards(currentX, _desiredVelocity.x, maxSpeedChange);
            float newZ = Mathf.MoveTowards(currentZ, _desiredVelocity.z, maxSpeedChange);

            _currentVelocity += xAxis * (newX - currentX) +
                                zAxis * (newZ - currentZ);
        }

        private void Jump()
        {
            Vector3 jumpDirection;

            if (OnGround)
            {
                jumpDirection = _contactNormal;
            }
            else if (OnSteep)
            {
                jumpDirection = _steepNormal;
                _jumpPhase = 0;
            }
            else if (Config.maxAirJumps > 0 && _jumpPhase <= Config.maxAirJumps)
            {
                jumpDirection = _contactNormal;
                if (_jumpPhase == 0) _jumpPhase = 1;
            }
            else
            {
                return;
            }

            _stepsSinceLastJump = 0;
            _jumpPhase += 1;

            jumpDirection = (jumpDirection + Vector3.up).normalized; // To allow interactions like: wall jumping chain, etc.

            float gravity = Config.gravityMultiplier * Physics.gravity.y;
            float jumpSpeed = Mathf.Sqrt(-2f * gravity * Config.jumpHeight);
            float alignedSpeed = Vector3.Dot(_currentVelocity, jumpDirection);

            if (alignedSpeed > 0.0f)
            {
                jumpSpeed = Mathf.Max(0.0f, jumpSpeed - alignedSpeed);
            }

            _currentVelocity += jumpDirection * jumpSpeed;
        }


        private void ApplyGravity()
        {
            // GRAVITY  (Tomeu) 
            float maxSpeedChangeGravity = (Config.gravityMultiplier) * Physics.gravity.y * Time.deltaTime;
            _currentVelocity.y += maxSpeedChangeGravity;

            if (OnGround) // To avoid sliding down in slopes
            {
                Vector3 slopesGravityPush = ProjectOnContactPlane((Config.gravityMultiplier * Time.deltaTime) * Physics.gravity);
                _currentVelocity -= slopesGravityPush;
            }
        }


        private void ClearState()
        {
            _wasOnGround = OnGround;
            _groundContactCount = _steepContactCount = 0;
            _contactNormal = _steepNormal = Vector3.zero;
        }


        private bool SnapToGround()
        {
            if (OnGround) // Tomeu
            {
                //Debug.Log("ALREADY ON GROUND");
                return false;
            }

            if (_stepsSinceLastGrounded > 1 || _stepsSinceLastJump <= 2)
            {
                //Debug.Log("STEPS");
                return false;
            }

            float speed = _currentVelocity.magnitude;
            if (speed > Config.speedThresholdToIgnoreSnap)
            {
                //Debug.Log("SPEED");
                return false;
            }

            float proveDistance = (_capsuleCollider.height / 2) + Config.groundProbeExtraDistance;
            if (!Physics.Raycast(_rigidbody.position, Vector3.down, out RaycastHit hit, proveDistance, Config.probeMask))
            {
                //Debug.Log("NO HIT");
                return false;
            }

            if (hit.normal.y < Config.MinGroundDotProduct)
            {
                //Debug.Log("NORMAL");
                return false;
            }

            _groundContactCount = 1;
            _contactNormal = hit.normal;

            
            float dot = Vector3.Dot(_currentVelocity, hit.normal);
            if (dot > 0.0f)
            {
                _currentVelocity = (_currentVelocity - (hit.normal * dot)).normalized * speed;
            }

            return true;
        }


        private bool CheckSteepContacts()
        {
            if (_steepContactCount > 1)
            {
                _steepNormal.Normalize();
                if (_steepNormal.y >= Config.MinGroundDotProduct)
                {
                    _groundContactCount = 1;
                    _contactNormal = _steepNormal;
                    return true;
                }
            }

            return false;
        }



        private Vector3 ProjectOnContactPlane(Vector3 vector)
        {
            return vector - _contactNormal * Vector3.Dot(vector, _contactNormal);
        }




    }




}

