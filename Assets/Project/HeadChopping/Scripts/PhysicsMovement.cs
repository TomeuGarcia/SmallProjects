using UnityEngine;
using static UnityEngine.LightAnchor;



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
            [Space(5)]
            [SerializeField, Min(0.0f)] public float maxAcceleration = 80.0f;
            [SerializeField, Min(0.0f)] public float maxDeceleration = 80.0f;
            [SerializeField, Min(0.0f)] public float maxAirAcceleration = 30.0f;
            [SerializeField, Min(0.0f)] public float maxAirDeceleration = 30.0f;
            [Space(5)]
            [SerializeField, Min(1.0f)] public float jumpDistance = 3.0f;
            [SerializeField, Min(1.0f)] public float wallJumpDistance = 2.0f;
            [SerializeField] public bool canWallJump = true;
            [SerializeField, Min(1.0f)] public float airJumpDistance = 2.0f;
            [SerializeField, Range(0, 5)] public int maxAirJumps = 0;
            [Space(5)]
            [SerializeField, Min(0.0f)] public float gravityMultiplier = 1.0f;
            [SerializeField, Min(0.0f)] public float coyoteTime = 0.2f;
            [SerializeField] public bool alwaysJumpStraightUpOnGround = false;
            [SerializeField] public bool clearVerticalSpeedOnJump = false;
            [Space(5)]
            [SerializeField, Range(0, 90)] private float _maxGroundAngle = 50.0f;
            [SerializeField, Min(0.0f)] public float speedThresholdToIgnoreSnap = 100.0f;
            [SerializeField, Min(0.0f)] public float groundProbeExtraDistance = 1.0f;
            [SerializeField] public LayerMask probeMask = -1;
            [Space(5)]
            [SerializeField, Min(0.0f)] public float maxStairSlopeHeight = 0.3f;

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

        private Vector2 _movementInput;
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
        private float _timeSinceLastGrounded;
        private Vector3 _gravityAcceleration;
        private Vector3 _gravityVelocity;

        public Configuration Config => _configuration;
        private bool OnGround => _groundContactCount > 0;
        private bool OnSteep => _steepContactCount > 0;
        private bool _previousOnGround;
        private bool _wasSnappedToGround;

        private bool _climbingStairSlopes = false;
        private Vector3 _targetPositionOnSlopeTip;
        private Vector3 _initialDirectionToSlopeTip;
        private Vector2 _movementInputInitialSlopeClimb;



        private void OnValidate()
        {
            _configuration.Validate();
        }

        private void Awake()
        {
            _configuration.Validate();
            _groundContactCount = 0;
            _jumpPhase = 0;

            _stepsSinceLastJump = 0;
            _stepsSinceLastGrounded = 0;
            _timeSinceLastGrounded = 0.0f;

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
            _inputSource.GetInput(out _movementInput, out bool desiredJump);

            _desiredVelocity = new Vector3(_movementInput.x, 0f, _movementInput.y) * Config.maxSpeed;
            _desiredJump |= desiredJump;
        }

        private void FixedUpdate()
        {
            UpdateState();
            CheckStartClimbStairSlopes();
            AdjustVelocity();
            ApplyClimbStairSlopesVelocity();

            if (_desiredJump)
            {
                _desiredJump = false;
                Jump();
            }

            //Debug.Log($"OnGround {OnGround}     ContactNormal {_contactNormal}");

            ApplyGravity();
            RemoveGroundPenetrationSpeed();     

            _rigidbody.linearVelocity = _currentVelocity;
            ClearState();
        }



        public void TeleportToPosition(Vector3 position, bool clearVelocity)
        {
            _climbingStairSlopes = false;
            _rigidbody.MovePosition(position);
            if (clearVelocity)
            {
                _currentVelocity = Vector3.zero;
            }
        }

        public void GetGroundedState(out bool onGround, out bool onSteep, out bool climbingStairSlopes)
        {
            onGround = OnGround || _wasSnappedToGround;
            onSteep = OnSteep;
            climbingStairSlopes = _climbingStairSlopes;
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
            float distance = GetColliderRadius() + Mathf.Max(0.1f, (_desiredVelocity.magnitude * Time.deltaTime) );
            Vector3 direction = _desiredVelocity.normalized;
            Vector3 position = GetPosition();
            position.y -= GetBodyOffsetToFeet() - 0.1f;

            if (Physics.Raycast(position, direction, out RaycastHit hit, distance, Config.probeMask, QueryTriggerInteraction.Ignore))
            {
                hitNormal = hit.normal;
                hitDistanceForContact = Mathf.Max(0.0f, hit.distance - GetColliderRadius());
                return true;
            }

            hitNormal = Vector3.zero;
            hitDistanceForContact = 0.0f;
            return false;
        }



        private void UpdateState()
        {
            _timeSinceLastGrounded += Time.deltaTime;
            _stepsSinceLastGrounded += 1;
            _stepsSinceLastJump += 1;
            _currentVelocity = _rigidbody.linearVelocity;

            _gravityAcceleration = Config.gravityMultiplier * Physics.gravity;
            _gravityVelocity = _gravityAcceleration * Time.deltaTime;

            bool onGround = OnGround;
            _wasSnappedToGround = SnapToGround();
            bool onSteep = CheckSteepContacts();
            if (onGround || _wasSnappedToGround || onSteep)
            {
                //Debug.Log($"OnGround {onGround},    _wasSnappedToGround {_wasSnappedToGround},    onSteep {onSteep}");

                _timeSinceLastGrounded = 0.0f;
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

            _connectionWorldPosition = GetPosition();
            _connectionLocalPosition = _connectedRigidbody.transform.InverseTransformPoint(_connectionWorldPosition);
        }


        private void AdjustVelocity()
        {
            //// (Tomeu)    Repulsion velocity to prevent climbing up steeps
            //if (!IsClimbingStairSlope && EvaluateCollisionAhead(out Vector3 hitNormal, out float hitDistanceForContact))
            //{
            //    if (hitNormal.y < Config.MinGroundDotProduct)
            //    {
            //        Vector3 counterDirection = ProjectOnContactPlane(hitNormal).normalized;
            //        Vector3 repulsionVelocity = -Vector3.Dot(counterDirection, _desiredVelocity) * counterDirection;
            //        _desiredVelocity += repulsionVelocity;
            //    }
            //}
            ////

            Vector3 xAxis = Vector3.zero;
            Vector3 zAxis = Vector3.zero;

            if (Config.alwaysJumpStraightUpOnGround && _stepsSinceLastJump <= 2) // To prevent direction correction when jump starts
            {
                xAxis = Vector3.right;
                zAxis = Vector3.forward;
            }
            else
            {
                xAxis = ProjectOnContactPlane(Vector3.right).normalized;
                zAxis = ProjectOnContactPlane(Vector3.forward).normalized;
            }

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
            Vector3 jumpDirection = Vector3.zero;
            bool onWall = false;

            if (OnGround || _timeSinceLastGrounded < Config.coyoteTime)
            {
                jumpDirection = Config.alwaysJumpStraightUpOnGround ? Vector3.up : _contactNormal;
            }
            else if (OnSteep && Config.canWallJump)
            {
                jumpDirection = _steepNormal;
                onWall = true;
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

            _inputSource.OnJumpInputConsumed();
            _stepsSinceLastJump = 0;
            _jumpPhase += 1;

            jumpDirection = (jumpDirection + Vector3.up).normalized; // To allow interactions like: wall jumping chain, etc.

            float jumpHeight = _jumpPhase > 1 ? Config.airJumpDistance : (onWall ? Config.wallJumpDistance : Config.jumpDistance);
            float jumpSpeed = ComputeJumpSpeed(jumpHeight, jumpDirection);

            if (Config.clearVerticalSpeedOnJump)
            {
                _currentVelocity.y = 0.0f;
            }

            _currentVelocity += jumpDirection * jumpSpeed;
        }
        private float ComputeJumpSpeed(float jumpHeight, Vector3 jumpDirection)
        {
            float gravity = _gravityAcceleration.y;
            float jumpSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);
            float alignedSpeed = Vector3.Dot(_currentVelocity, jumpDirection);

            if (alignedSpeed > 0.0f)
            {
                jumpSpeed = Mathf.Max(0.0f, jumpSpeed - alignedSpeed);
            }

            return jumpSpeed;
        }


        private void ApplyGravity()
        {
            // GRAVITY  (Tomeu) 
            _currentVelocity.y += _gravityVelocity.y;

            if (OnGround) // To avoid sliding down in slopes
            {
                Vector3 slopesGravityPush = ProjectOnContactPlane(_gravityVelocity);
                _currentVelocity -= slopesGravityPush;
            }
        }

        private void RemoveGroundPenetrationSpeed()
        {
            // (Tomeu) 
            if (_currentVelocity.y < _gravityVelocity.y) // < because it is negative
            {
                float colliderHeightOffset = GetBodyOffsetToFeet();
                float distance = colliderHeightOffset + (-_currentVelocity.y * Time.deltaTime);
                if (Physics.Raycast(GetPosition(), Vector3.down, out RaycastHit hit, distance, Config.probeMask, QueryTriggerInteraction.Ignore))
                {
                    float penetrationDistance = distance - hit.distance;
                    float excessSpeedY = penetrationDistance / Time.deltaTime;
                    _currentVelocity.y += excessSpeedY;
                }
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

            float probeDistance = GetBodyOffsetToFeet() + Config.groundProbeExtraDistance;
            if (!Physics.Raycast(GetPosition(), Vector3.down, out RaycastHit hit, probeDistance, Config.probeMask, QueryTriggerInteraction.Ignore))
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


        private Vector3 GetPosition()
        {
            return _rigidbody.position;
        }
        private float GetBodyOffsetToFeet()
        {
            return (_capsuleCollider.height / 2) - _capsuleCollider.center.y;
        }
        private float GetColliderRadius()
        {            
            return _capsuleCollider.radius;
        }


        private Vector3 ProjectOnContactPlane(Vector3 vector)
        {
            return vector - _contactNormal * Vector3.Dot(vector, _contactNormal);
        }


        public void PreventSnapToGround()
        {
            _stepsSinceLastJump -= 1;
        }



        private void CheckStartClimbStairSlopes()
        {
            float desiredSpeed = _desiredVelocity.magnitude;
            if (desiredSpeed < 0.01f)
            {
                return;
            }

            Vector3 currentPosition = GetPosition();
            float bodyOffsetToFeet = GetBodyOffsetToFeet();
            float colliderRadius = GetColliderRadius();

            float initialForward_ProbeDistance = Config.maxStairSlopeHeight + colliderRadius;
            Vector3 initialForward_ProbeDirection = _desiredVelocity / desiredSpeed;
            Vector3 initialForward_ProbePosition = currentPosition;
            initialForward_ProbePosition.y -= bodyOffsetToFeet - 0.01f;

            if (!Physics.Raycast(initialForward_ProbePosition, initialForward_ProbeDirection, out RaycastHit initialForward_Hit, initialForward_ProbeDistance,
                Config.probeMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            float initialDownwards_ProbeDistance = Config.maxStairSlopeHeight;
            Vector3 initialDownwards_ProbeDirection = Vector3.down;
            Vector3 initialDownwards_ProbePosition = initialForward_Hit.point + (initialForward_ProbeDirection * 0.01f);
            initialDownwards_ProbePosition.y += Config.maxStairSlopeHeight;
            if (!Physics.Raycast(initialDownwards_ProbePosition, initialDownwards_ProbeDirection, out RaycastHit initialDownwards_Hit, initialDownwards_ProbeDistance,
                Config.probeMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            bool isSteep = Vector3.Dot(initialForward_Hit.normal, initialDownwards_Hit.normal) > 0.9f;
            if (isSteep)
            {
                return;
            }

            float realSlopeHeight = initialDownwards_Hit.point.y - (currentPosition.y - bodyOffsetToFeet);
            if (realSlopeHeight < 0.0f)
            {
                return;
            }

            float airDistanceToWall = initialForward_Hit.distance - colliderRadius;
            float excessProbeDistance = Config.maxStairSlopeHeight - realSlopeHeight - colliderRadius;
            if (airDistanceToWall + excessProbeDistance > realSlopeHeight + colliderRadius)
            {
                return;
            }

            //Debug.Log($"Detecting slope with height: {realSlopeHeight}");

            Vector3 targetPositionOnSlopeTip = initialForward_Hit.point;
            targetPositionOnSlopeTip += initialForward_ProbeDirection * 0.01f;
            targetPositionOnSlopeTip.y = initialDownwards_Hit.point.y + bodyOffsetToFeet + 0.01f;
            
            _climbingStairSlopes = true;
            _targetPositionOnSlopeTip = targetPositionOnSlopeTip;
            _initialDirectionToSlopeTip = (_targetPositionOnSlopeTip - currentPosition).normalized;
            _movementInputInitialSlopeClimb = _movementInput.normalized;
        }

        public void ApplyClimbStairSlopesVelocity()
        {
            if (!_climbingStairSlopes)
            {
                return;                
            }

            if (_stepsSinceLastJump < 1 || Vector3.Dot(_movementInputInitialSlopeClimb, _movementInput.normalized) < 0.9f)
            {
                _climbingStairSlopes = false;
                return;
            }

            Vector3 currentPosition = GetPosition();
            Vector3 toSlopeTip = _targetPositionOnSlopeTip - currentPosition;

            bool reachedSlopeTip = Vector3.Dot(_initialDirectionToSlopeTip, toSlopeTip) < 0;
            if (reachedSlopeTip)
            {
                //Debug.Log("Reached slope tip");
                _climbingStairSlopes = false;
                return;
            }

            _currentVelocity = toSlopeTip.normalized * Config.maxSpeed;
        }


    }




}

