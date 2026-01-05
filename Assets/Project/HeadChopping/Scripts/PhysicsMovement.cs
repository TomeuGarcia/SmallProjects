using UnityEngine;
using static HeadChopping.PhysicsMovement;



namespace HeadChopping
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PhysicsMovement : MonoBehaviour
    {
        public interface IInputSource
        {
            void GetInput(out Vector2 movementInput, out bool desiredJump);
            void OnJumpInputConsumed();
        }

        private class DefaultInputSource : IInputSource
        {
            void IInputSource.GetInput(out Vector2 movementInput, out bool desiredJump)
            {
                movementInput.x = Input.GetAxis("Horizontal");
                movementInput.y = Input.GetAxis("Vertical");
                movementInput = Vector2.ClampMagnitude(movementInput, 1f);

                desiredJump = Input.GetButtonDown("Jump");
            }

            void IInputSource.OnJumpInputConsumed()
            {
            }
        }


        [System.Serializable]
        public class Configuration
        {
            [SerializeField, Min(0.0f)] public float maxSpeed = 10.0f;
            [Space(4)]
            [SerializeField, Min(0.0f)] public float maxAcceleration = 80.0f;
            [SerializeField, Min(0.0f)] public float maxDeceleration = 80.0f;
            [SerializeField, Min(0.0f)] public float maxAirAcceleration = 30.0f;
            [SerializeField, Min(0.0f)] public float maxAirDeceleration = 30.0f;
            [Space(4)]
            [SerializeField, Min(1.0f)] public float jumpHeight = 2.0f;
            [SerializeField, Min(1.0f)] public float airJumpHeight = 2.0f;
            [SerializeField, Min(0.0f)] public float gravityMultiplier = 1.0f;
            [SerializeField, Range(0, 5)] public int maxAirJumps = 0;
            [SerializeField] public bool alwaysJumpStraightUpOnGround = false;
            [SerializeField] public bool clearVerticalSpeedOnJump = false;
            [Space(4)]
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


        private IInputSource _inputSource;
        private bool _autoUpdate = true;

        private Rigidbody _connectedRigidbody;
        private Rigidbody _previousConnectedRigidbody;

        private Vector3 _currentVelocity;
        private Vector3 _desiredVelocity;
        private Vector3 _connectionVelocity;
        private Vector3 _connectionWorldPosition;
        private Vector3 _connectionLocalPosition;
        private bool _desiredJump;
        private int _jumpPhase;
        private Vector3 _contactNormal;
        private int _groundContactCount;
        private Vector3 _steepNormal;
        private int _steepContactCount;
        private int _stepsSinceLastGrounded;
        private int _stepsSinceLastJump;
        private int _stepsSinceLastSnapToGround;

        public Configuration Config => _configuration;
        private bool OnGround => _groundContactCount > 0;
        private bool OnSteep => _steepContactCount > 0;
        private bool OnAir => !OnGround && !OnSteep;
        private bool _previousOnGround;


        private void OnValidate()
        {
            _configuration.Validate();
        }

        private void Awake()
        {
            _configuration.Validate();
            _groundContactCount = 0;
            _jumpPhase = 0;

            _stepsSinceLastGrounded = 0;
            _stepsSinceLastJump = 0;
            _stepsSinceLastSnapToGround = 0;

            _previousOnGround = false;
            _previousConnectedRigidbody = null;

            _rigidbody.useGravity = false; // Rigidbody's gravity must be DISABED   (Tomeu)
        }

        public void AwakeConfigure(IInputSource inputSource, bool autoUpdate)
        {
            _inputSource = inputSource;
            _autoUpdate = autoUpdate;
        }

        private void Start()
        {
            if (_inputSource == null) _inputSource = new DefaultInputSource();
        }
        

        private void Update()
        {
            if (_autoUpdate)
            {
                DoUpdate();
            }
        }
        public void DoUpdate()
        {
            _inputSource.GetInput(out Vector2 movementInput, out bool desiredJump);

            _desiredVelocity = new Vector3(movementInput.x, 0f, movementInput.y) * Config.maxSpeed;
            _desiredJump |= desiredJump;
        }

        private void FixedUpdate()
        {
            UpdateState();
            AdjustVelocity();

            if (_desiredJump)
            {
                _desiredJump = false;
                _inputSource.OnJumpInputConsumed();
                Jump();
            }

            //Debug.Log($"OnGround {OnGround}     ContactNormal {_contactNormal}");

            ApplyLandFromFall();
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
                    _connectedRigidbody = collision.rigidbody;
                }
                else if (normal.y > -0.01f)
                {
                    _steepContactCount += 1;
                    _steepNormal += normal;
                    if (_groundContactCount == 0)
                    {
                        _connectedRigidbody = collision.rigidbody;
                    }
                }
            }
        }

        private bool EvaluateCollisionAhead(out Vector3 hitNormal, out float hitDistanceForContact)
        {
            float distance = _capsuleCollider.radius + Mathf.Max(0.1f, (_desiredVelocity.magnitude * Time.deltaTime) );
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
            _stepsSinceLastSnapToGround += 1;
            _currentVelocity = _rigidbody.linearVelocity;

            bool onGround = OnGround;
            bool snapToGround = SnapToGround();
            bool onSteep = CheckSteepContacts();
            if (onGround || snapToGround || onSteep)
            {
                //Debug.Log($"OnGround {onGround},    snapToGround {snapToGround},    onSteep {onSteep}");
                
                _stepsSinceLastGrounded = 0;
                if (_stepsSinceLastJump > 1)
                {
                    _jumpPhase = 0;
                }
                if (_groundContactCount > 1)
                {
                    _contactNormal.Normalize();
                }
                if (snapToGround)
                {
                    _stepsSinceLastSnapToGround = 0;
                }
            }
            else
            {
                _contactNormal = Vector3.up;
            }

            if (_connectedRigidbody)
            {
                if (_connectedRigidbody.isKinematic || _connectedRigidbody.mass >= _rigidbody.mass)
                {
                    UpdateConnectionState();
                }
            }
        }

        private void UpdateConnectionState()
        {
            if (_connectedRigidbody == _previousConnectedRigidbody)
            {
                Vector3 connectionMovement = _connectedRigidbody.transform.TransformPoint(_connectionLocalPosition) - _connectionWorldPosition;
                _connectionVelocity = connectionMovement / Time.deltaTime;
            }            

            _connectionWorldPosition = _rigidbody.position;
            _connectionLocalPosition = _connectedRigidbody.transform.InverseTransformPoint(_connectionWorldPosition);
        }


        private void AdjustVelocity()
        {
            //// (Tomeu)    Repulsion velocity to prevent climbing up steeps
            if (EvaluateCollisionAhead(out Vector3 hitNormal, out float hitDistanceForContact))
            {
                if (hitNormal.y < Config.MinGroundDotProduct)
                {
                    Vector3 counterDirection = ProjectOnContactPlane(hitNormal).normalized;
                    Vector3 repulsionVelocity = -Vector3.Dot(counterDirection, _desiredVelocity) * counterDirection;
                    _desiredVelocity += repulsionVelocity;
                }
            }
            ////

            Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
            Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

            Vector3 relativeVelocity = _currentVelocity - _connectionVelocity;
            float currentX = Vector3.Dot(relativeVelocity, xAxis);
            float currentZ = Vector3.Dot(relativeVelocity, zAxis);

            float currentSpeed = Mathf.Sqrt(Mathf.Pow(currentX, 2) + Mathf.Pow(currentZ, 2));
            float desiredSpeed = _desiredVelocity.magnitude;
            bool accelerating = desiredSpeed > (currentSpeed - 0.1f);

            float acceleration = OnGround 
                ? (accelerating ? Config.maxAcceleration : Config.maxDeceleration)
                : (accelerating ? Config.maxAirAcceleration : Config.maxAirDeceleration);
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
                jumpDirection = Config.alwaysJumpStraightUpOnGround ? Vector3.up : _contactNormal;
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

            float jumpHeight = _jumpPhase > 1 ? Config.airJumpHeight : Config.jumpHeight;
            float gravity = Config.gravityMultiplier * Physics.gravity.y;
            float jumpSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);
            float alignedSpeed = Vector3.Dot(_currentVelocity, jumpDirection);

            if (alignedSpeed > 0.0f)
            {
                jumpSpeed = Mathf.Max(0.0f, jumpSpeed - alignedSpeed);
            }

            if (Config.clearVerticalSpeedOnJump)
            {
                _currentVelocity.y = 0.0f;
            }

            _currentVelocity += jumpDirection * jumpSpeed;
        }

        private void ApplyLandFromFall()
        {
            // LAND FROM FALL  (Tomeu) 
            bool justLandedFromFall = !OnSteep && OnGround && _stepsSinceLastSnapToGround > 1;
            if (justLandedFromFall)
            {
                Vector3 projectedDesiredVelocity = ProjectOnContactPlane(_desiredVelocity);
                _currentVelocity = projectedDesiredVelocity;
            }
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
            _previousOnGround = OnGround;
            _groundContactCount = _steepContactCount = 0;
            _contactNormal = _steepNormal = _connectionVelocity = Vector3.zero;

            _previousConnectedRigidbody = _connectedRigidbody;
            _connectedRigidbody = null;
        }


        private bool SnapToGround()
        {
            if (OnGround && !_previousOnGround) // Tomeu
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
            _connectedRigidbody = hit.rigidbody;


            float dot = Vector3.Dot(_currentVelocity, hit.normal);
            if (dot > 0.01f)
            {
                //Debug.Log($"Snap _currentVelocity {_currentVelocity}    dot {dot}");
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



        public void PreventSnapToGround()
        {
            _stepsSinceLastJump -= 1;
        }

    }




}

