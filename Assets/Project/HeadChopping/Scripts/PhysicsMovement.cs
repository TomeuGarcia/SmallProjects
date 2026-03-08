using UnityEngine;



namespace HeadChopping
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PhysicsMovement : MonoBehaviour
    {
        public interface IInputSource
        {
            void GetInput(out Vector2 movementInput, out bool desireStartJumping, out bool desireKeepJumping);
            void OnJumpInputConsumed();
        }

        private class DefaultInputSource : IInputSource
        {
            void IInputSource.GetInput(out Vector2 movementInput, out bool desireStartJumping, out bool desireKeepJumping)
            {
                movementInput.x = Input.GetAxis("Horizontal");
                movementInput.y = Input.GetAxis("Vertical");
                movementInput = Vector2.ClampMagnitude(movementInput, 1f);

                desireStartJumping = Input.GetButtonDown("Jump");
                desireKeepJumping = Input.GetButton("Jump");
            }

            void IInputSource.OnJumpInputConsumed()
            {
            }
        }


        [System.Serializable]
        public class Configuration
        {
            [SerializeField, Min(0.0f)] public float maxSpeed = 10.0f;
            [Space(10)]
            [SerializeField, Min(0.0f)] public float maxAcceleration = 80.0f;
            [SerializeField, Min(0.0f)] public float maxDeceleration = 80.0f;
            [SerializeField, Min(0.0f)] public float maxAirAcceleration = 30.0f;
            [SerializeField, Min(0.0f)] public float maxAirDeceleration = 30.0f;
            [Space(10)]
            [SerializeField, Min(0.1f)] public float jumpHeight = 3.0f;
            [SerializeField, Min(0.0f)] public float jumpHoldInputDuration = 0.5f;
            [Space(5)]
            [SerializeField] public bool canWallJump = true;
            [SerializeField, Min(0.1f)] public float wallJumpHeight = 2.0f;
            [SerializeField, Min(0.0f)] public float wallJumpHoldInputDuration = 0.0f;
            [Space(5)]
            [SerializeField, Range(0, 5)] public int maxAirJumps = 0;
            [SerializeField, Min(0.1f)] public float airJumpHeight = 2.0f;
            [SerializeField, Min(0.0f)] public float airJumpHoldInputDuration = 0.25f;
            [Space(10)]
            [SerializeField] public bool clearVerticalSpeedOnJump = false;
            [SerializeField] public bool alwaysJumpStraightUpOnGround = false;
            [SerializeField, Min(0.0f)] public float gravityMultiplier = 1.0f;
            [SerializeField, Min(0.0f)] public float coyoteTime = 0.2f;
            [Space(10)]
            [SerializeField, Tooltip("More expensive, but better results")] public bool preventFallingFromColliderEdge = true;
            [SerializeField, Range(0, 90)] private float _maxGroundAngle = 50.0f;
            [SerializeField, Min(0.0f)] public float speedThresholdToIgnoreSnap = 100.0f;
            [SerializeField, Min(0.0f)] public float groundProbeExtraDistance = 1.0f;
            [SerializeField] public LayerMask collisionProbeMask = -1;
            [Space(10)]
            [SerializeField, Min(0.0f)] public float maxStairSlopeHeight = 0.3f;
            [SerializeField, Tooltip("Kind of acts like a Ledge-Grab")] public bool considerWallLedgeAsStairSlopeWhenJumping = true;

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
        private bool _enabled = true;

        private Rigidbody _connectedRigidbody;
        private Rigidbody _previousConnectedRigidbody;

        private Vector2 _movementInput;
        private float _currentFullJumpHoldInputTime;

        private Vector3 _currentVelocity;
        private Vector3 _desiredVelocity;
        private Vector3 _connectionVelocity;
        private Vector3 _connectionWorldPosition;
        private Vector3 _connectionLocalPosition;
        private bool _desiredStartJumping;
        private bool _desiredKeepJumping;
        private bool _canKeepJumping;
        private int _jumpPhase;
        private Vector3 _contactNormal;
        private int _groundContactCount;
        private Vector3 _steepNormal;
        private int _steepContactCount;
        private int _stepsSinceLastGrounded;
        private int _stepsSinceLastJump;
        private float _timeSinceLastGrounded;
        private Vector3 _gravityAcceleration;
        private Vector3 _gravityAccelerationKeepJumping;
        private Vector3 _gravityVelocity;

        private Configuration Config => _configuration;
        private bool OnGround => _groundContactCount > 0;
        private bool OnSteep => _steepContactCount > 0;
        private bool _previousOnGround;
        private bool _wasSnappedToGround;
        private bool _isStandingOnColliderEdge;

        private bool _isClimbingStairSlopes;
        private Vector3 _targetPositionOnStairSlope;
        private Vector3 _initialDirectionToStairSlope;
        private Vector2 _movementInputInitialStairSlopeClimb;

        private Vector3 _jumpDirection;
        private bool _jumpOnWall;


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
            _isStandingOnColliderEdge = false;
            _isClimbingStairSlopes = false;

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
            if (!_enabled) return;

            _inputSource.GetInput(out _movementInput, out bool desireStartJumping, out bool desireKeepJumping);

            _desiredVelocity = new Vector3(_movementInput.x, 0f, _movementInput.y) * Config.maxSpeed;
            _desiredStartJumping |= desireStartJumping;
            _desiredKeepJumping = desireKeepJumping;
            if (!_desiredKeepJumping) _canKeepJumping = false;
        }

        private void FixedUpdate()
        {
            if (_autoUpdate)
            {
                DoFixedUpdate();
            }
        }
        public void DoFixedUpdate()
        {
            if (!_enabled) return;

            UpdateState();
            CheckStartClimbStairSlopes();
            AdjustVelocity();
            ApplyClimbStairSlopesVelocity();

            if (_desiredStartJumping)
            {
                _desiredStartJumping = false;
                StartJumping();
            }
            if (_desiredKeepJumping)
            {
                EvaluateKeepJumping();
            }

            ApplyGravity();
            RemoveGroundPenetrationSpeed();
            CancelKeepJumpingIfHitHead();

            _rigidbody.linearVelocity = _currentVelocity;
            ClearState();
        }


        public void SetEnabled(bool enabled)
        {
            if (_enabled == enabled) return;

            _enabled = enabled;
            if (!enabled)
            {
                ClearState();
                ClearVelocity();
            }
        }

        public Configuration GetConfiguration()
        {
            return _configuration;
        }

        public void TeleportToPosition(Vector3 position, bool clearVelocity)
        {
            _isClimbingStairSlopes = false;
            _rigidbody.MovePosition(position);
            if (clearVelocity)
            {
                ClearVelocity();
            }
        }

        public void GetGroundedState(out bool isOnGround, out bool isOnSteep, out bool isClimbingStairSlopes, out bool isStandingOnColliderEdge)
        {
            isOnGround = OnGround || _wasSnappedToGround;
            isOnSteep = OnSteep;
            isClimbingStairSlopes = _isClimbingStairSlopes;
            isStandingOnColliderEdge = _isStandingOnColliderEdge;
        }

        public void GetAirState(out bool isGroundJumpRising, out bool isWallJumpRising, out bool isAirJumpRising, out bool isFalling)
        {
            isFalling = isGroundJumpRising = isWallJumpRising = isAirJumpRising = false;
            bool isJumpRising = _jumpPhase > 0 && _currentVelocity.y > 0.0f;
            if (isJumpRising)
            {
                if (_jumpPhase > 1)
                {
                    isAirJumpRising = true;
                }
                else if (_jumpOnWall)
                {
                    isWallJumpRising = true;
                }
                else
                {
                    isGroundJumpRising = true;
                }
            }
            else
            {
                isFalling = !(OnGround || _wasSnappedToGround);
            }
        }

        public void GetNormals(out Vector3 contactNormal, out Vector3 wallNormal)
        {
            contactNormal = _contactNormal;
            wallNormal = _steepNormal;
        }

        public void GetVelocity(out Vector3 relativeVelocity)
        {
            relativeVelocity = _currentVelocity - _connectionVelocity;
        }

        public void PreventSnapToGround()
        {
            _stepsSinceLastJump -= 1;
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
            if (!_enabled) return;

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
            position.y -= GetOffsetCenterToFeet() - 0.1f;

            if (Physics.Raycast(position, direction, out RaycastHit hit, distance, Config.collisionProbeMask, QueryTriggerInteraction.Ignore))
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

            bool onGround = OnGround;
            _wasSnappedToGround = SnapToGround();
            bool onSteep = CheckSteepContacts();
            if (onGround || _wasSnappedToGround || onSteep)
            {
                //Debug.Log("GROUND");
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
                //Debug.Log("NO ground");
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
            if (EvaluateCollisionAhead(out Vector3 hitNormal, out float hitDistanceForContact))
            {
                if (hitNormal.y < Config.MinGroundDotProduct)
                {
                    Vector3 counterDirection = ProjectOnContactPlane(hitNormal).normalized;
                    Vector3 repulsionVelocity = -Vector3.Dot(counterDirection, _desiredVelocity) * counterDirection;
                    _desiredVelocity += repulsionVelocity;
                }
                else
                {
                    _contactNormal = hitNormal;
                }
            }
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

        private void StartJumping()
        {
            _jumpDirection = Vector3.zero;
            _jumpOnWall = false;

            if (OnGround || _timeSinceLastGrounded < Config.coyoteTime)
            {
                _jumpDirection = Config.alwaysJumpStraightUpOnGround ? Vector3.up : _contactNormal;
            }
            else if (OnSteep && Config.canWallJump)
            {
                _jumpDirection = _steepNormal;
                _jumpOnWall = true;
                _jumpPhase = 0;
            }
            else if (Config.maxAirJumps > 0 && _jumpPhase <= Config.maxAirJumps)
            {
                _jumpDirection = _contactNormal;
                if (_jumpPhase == 0) _jumpPhase = 1;
            }
            else
            {
                _canKeepJumping = false;
                return;
            }

            _canKeepJumping = true;

            _inputSource.OnJumpInputConsumed();
            _stepsSinceLastJump = 0;
            _jumpPhase += 1;

            _jumpDirection = (_jumpDirection + Vector3.up).normalized; // To allow interactions like: wall jumping chain, etc.
            _currentFullJumpHoldInputTime = 0.0f;

            if (Config.clearVerticalSpeedOnJump)
            {
                _currentVelocity.y = 0.0f;
            }

        }
        private void EvaluateKeepJumping()
        {
            if (!_canKeepJumping)
            {
                return;
            }

            float fullJumpDuration = 0.0f;
            float jumpHeight = 0.0f;
            if (_jumpPhase > 1)
            {
                fullJumpDuration = Config.airJumpHoldInputDuration;
                jumpHeight = Config.airJumpHeight;
            }
            else if (_jumpOnWall)
            {
                fullJumpDuration = Config.wallJumpHoldInputDuration;
                jumpHeight = Config.wallJumpHeight;
            }
            else
            {
                fullJumpDuration = Config.jumpHoldInputDuration;
                jumpHeight = Config.jumpHeight;
            }

            float _extraPermissiveHeight = Mathf.Min(0.25f, jumpHeight);
            jumpHeight += _extraPermissiveHeight;

            _currentFullJumpHoldInputTime = Mathf.Min(_currentFullJumpHoldInputTime + Time.deltaTime, fullJumpDuration);
            float jumpSpeed = ComputeJumpSpeed(jumpHeight, _jumpDirection, fullJumpDuration, _currentFullJumpHoldInputTime);
            _currentVelocity += _jumpDirection * jumpSpeed;

            if (_currentFullJumpHoldInputTime >= fullJumpDuration)
            {
                _canKeepJumping = false;
            }            
        }

        private float ComputeJumpSpeed(float jumpHeight, Vector3 jumpDirection, float fullJumpTime, float currentJumpTime)
        {
            // V = Vo + a*t
            // V^2 = Vo^2 + 2*a*(X-Xo)
            // X = Xo + V*t + 0.5*a*t^2
            // V = 0    Xo = 0      X = jumpHeight      t = fullJumpTime    a = ?   Vo = ?

            float jumpGravity = 0.0f;
            float jumpSpeed = 0.0f;

            bool jumpIsInstantaneous = fullJumpTime < 0.001f;
            if (jumpIsInstantaneous)
            {
                jumpGravity = _gravityAcceleration.y;

                // Vo^2 = V^2 - 2*a*(X-Xo) = sqrt(-2*a*X)
                jumpSpeed = Mathf.Sqrt(-2.0f * jumpGravity * jumpHeight);
            }
            else
            {
                // a = (X - Xo - V*t) / (0.5*t^2) = X / (0.5*t^2)
                jumpGravity = -jumpHeight / (0.5f * fullJumpTime * fullJumpTime);

                // Vo = V - a*t = -a*t
                float jumpStartSpeed = -jumpGravity * fullJumpTime;

                // V = Vo + a*t
                jumpSpeed = jumpStartSpeed + (jumpGravity * currentJumpTime);
            }

            _gravityAccelerationKeepJumping = new Vector3(0, jumpGravity, 0);

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
            Vector3 gravityAcceleration = _canKeepJumping ? _gravityAccelerationKeepJumping : _gravityAcceleration;
            _gravityVelocity = gravityAcceleration * Time.deltaTime;
            _currentVelocity += _gravityVelocity;

            if (OnGround) // To avoid sliding down in slopes
            {
                Vector3 slopesGravityPush = ProjectOnContactPlane(_gravityVelocity);
                _currentVelocity -= slopesGravityPush;
            }
        }

        private void RemoveGroundPenetrationSpeed()
        {
            _isStandingOnColliderEdge = false;

            // (Tomeu)                        
            const float Error = 0.001f;            
            if (_currentVelocity.y < _gravityVelocity.y - Error) // < because it is negative
            {
                if (DoRemoveGroundPenetrationSpeed(probeOffset: Vector3.zero))
                {
                    return;
                }
                if (Config.preventFallingFromColliderEdge && FindStandingOnColliderEdge(out Vector3 offsetToCollider))
                {
                    DoRemoveGroundPenetrationSpeed(probeOffset: offsetToCollider);
                    _isStandingOnColliderEdge = true;
                }
            }            
        }

        private bool DoRemoveGroundPenetrationSpeed(Vector3 probeOffset)
        {
            float offsetToFeet = GetOffsetCenterToFeet();
            float fallDistance = -_currentVelocity.y * Time.deltaTime;
            float castDistance = offsetToFeet + fallDistance;
            if (Physics.Raycast(GetPosition() + probeOffset, Vector3.down, out RaycastHit hit, castDistance, Config.collisionProbeMask, QueryTriggerInteraction.Ignore))
            {
                float penetrationDistance = castDistance - hit.distance;
                float excessSpeedY = penetrationDistance / Time.deltaTime;
                _currentVelocity.y = Mathf.Min(0, _currentVelocity.y + excessSpeedY); // Clamp to avoid positive Y
                return true;
            }
            return false;
        }

        private bool FindStandingOnColliderEdge(out Vector3 offsetToCollider)
        {
            Vector3 castOrigin = GetColliderCenterPosition();
            float halfColliderHeight = GetHalfColliderHeight();
            float colliderRadius = GetColliderRadius();
            Vector3 halfExtents = new Vector3(colliderRadius, halfColliderHeight, colliderRadius) * 0.5f;
            float remainingDistanceToFeet = halfColliderHeight * 0.5f;
            float castDistance = remainingDistanceToFeet + (-_currentVelocity.y * Time.deltaTime);

            RaycastHit[] hits = Physics.BoxCastAll(castOrigin, halfExtents, Vector3.down, _rigidbody.rotation, castDistance,
                Config.collisionProbeMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; ++i)
            {
                RaycastHit hit = hits[i];
                bool probeStartedInBox = hit.distance < 0.0001f;
                if (probeStartedInBox)
                {
                    continue;
                }

                Vector3 validationPosition = new Vector3(castOrigin.x, 0, castOrigin.z);
                Vector3 toHit = Vector3.ProjectOnPlane(hit.point, Vector3.up) - validationPosition;
                float toHitDistance = toHit.magnitude;
                bool hitIsOutsideColliderRadius = toHitDistance > colliderRadius;

                const float ConsiderSlopeDot = 0.95f;
                bool isSlope = hit.normal.y < ConsiderSlopeDot;

                if (hitIsOutsideColliderRadius)
                {
                    Vector3 toHitDirection = toHit / toHitDistance;
                    Vector3 toHitAtRadius = toHitDirection * colliderRadius;
                    Vector3 secondCastOrigin = new Vector3(toHitAtRadius.x, castOrigin.y, toHitAtRadius.z);

                    if (Physics.Raycast(secondCastOrigin, Vector3.down, out RaycastHit secondHit, castDistance,
                        Config.collisionProbeMask, QueryTriggerInteraction.Ignore))
                    {
                        isSlope = secondHit.normal.y < ConsiderSlopeDot;
                        if (isSlope)
                        {
                            continue;
                        }

                        toHit = Vector3.ProjectOnPlane(secondHit.point, Vector3.up) - validationPosition;
                        toHitDistance = toHit.magnitude;
                        hitIsOutsideColliderRadius = toHitDistance > colliderRadius;

                        if (!hitIsOutsideColliderRadius)
                        {
                            offsetToCollider = (toHit / toHitDistance) * colliderRadius;
                            return true;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (!isSlope)
                {
                    offsetToCollider = (toHit / toHitDistance) * colliderRadius;
                    return true;
                }
            }

            offsetToCollider = Vector3.zero;
            return false;
        }


        private void CancelKeepJumpingIfHitHead()
        {
            // (Tomeu) 
            const float Error = 0.001f;
            if (_canKeepJumping && _currentVelocity.y > _gravityVelocity.y + Error)
            {
                float offsetToHead = GetOffsetCenterToHead();
                float jumpDistance = _currentVelocity.y * Time.deltaTime;
                float castDistance = offsetToHead + jumpDistance;
                if (Physics.Raycast(GetPosition(), _jumpDirection, out RaycastHit hit, castDistance, Config.collisionProbeMask, QueryTriggerInteraction.Ignore))
                {
                    float penetrationDistance = castDistance - hit.distance;
                    float excessSpeed = penetrationDistance / Time.deltaTime;
                    _currentVelocity -= _jumpDirection * excessSpeed;

                    _canKeepJumping = false;
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

            if ((_stepsSinceLastGrounded > 1 || _stepsSinceLastJump <= 2) && _currentVelocity.y > -0.01f) // Added this negative velocity Y check
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

            float probeDistance = GetOffsetCenterToFeet() + Config.groundProbeExtraDistance;
            if (!Physics.Raycast(GetPosition(), Vector3.down, out RaycastHit hit, probeDistance, Config.collisionProbeMask, QueryTriggerInteraction.Ignore))
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
        private Vector3 GetColliderCenterPosition()
        {
            return _rigidbody.position + _capsuleCollider.center;
        }
        private float GetHalfColliderHeight()
        {
            return _capsuleCollider.height / 2;
        }
        private float GetOffsetCenterToFeet()
        {
            return GetHalfColliderHeight() - _capsuleCollider.center.y;
        }
        private float GetOffsetCenterToHead()
        {
            return GetHalfColliderHeight() + _capsuleCollider.center.y;
        }
        private float GetColliderRadius()
        {            
            return _capsuleCollider.radius;
        }


        private Vector3 ProjectOnContactPlane(Vector3 vector)
        {
            return vector - _contactNormal * Vector3.Dot(vector, _contactNormal);
        }


        private void ClearVelocity()
        {
            _currentVelocity = Vector3.zero;
        }



        private void CheckStartClimbStairSlopes()
        {
            if (!Config.considerWallLedgeAsStairSlopeWhenJumping && (_stepsSinceLastJump > 1 && _stepsSinceLastGrounded > 1))
            {
                _isClimbingStairSlopes = false;
                return;
            }

            float desiredSpeed = _desiredVelocity.magnitude;
            if (desiredSpeed < 0.01f)
            {
                return;
            }

            Vector3 currentPosition = GetPosition();
            float bodyOffsetToFeet = GetOffsetCenterToFeet();
            float colliderRadius = GetColliderRadius();

            float initialForward_ProbeDistance = Config.maxStairSlopeHeight + colliderRadius;
            Vector3 initialForward_ProbeDirection = _desiredVelocity / desiredSpeed;
            Vector3 initialForward_ProbePosition = currentPosition;
            initialForward_ProbePosition.y -= (bodyOffsetToFeet - 0.01f);

            if (!Physics.Raycast(initialForward_ProbePosition, initialForward_ProbeDirection, out RaycastHit initialForward_Hit, initialForward_ProbeDistance,
                Config.collisionProbeMask, QueryTriggerInteraction.Ignore))
            {
                DoCheckStartClimbDownStairSlopes(initialForward_ProbePosition, initialForward_ProbeDirection);
                return;
            }

            float initialDownwards_ProbeDistance = Config.maxStairSlopeHeight;
            Vector3 initialDownwards_ProbeDirection = Vector3.down;
            Vector3 initialDownwards_ProbePosition = initialForward_Hit.point + (initialForward_ProbeDirection * 0.01f);
            initialDownwards_ProbePosition.y += Config.maxStairSlopeHeight;

            if (Physics.CheckSphere(initialDownwards_ProbePosition, 0.01f, Config.collisionProbeMask, QueryTriggerInteraction.Ignore))
            {
                return; // probing inside another collider
            }

            if (!Physics.Raycast(initialDownwards_ProbePosition, initialDownwards_ProbeDirection, out RaycastHit initialDownwards_Hit, initialDownwards_ProbeDistance,
                Config.collisionProbeMask, QueryTriggerInteraction.Ignore))
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
            
            _isClimbingStairSlopes = true;
            _targetPositionOnStairSlope = targetPositionOnSlopeTip;
            _initialDirectionToStairSlope = (_targetPositionOnStairSlope - currentPosition).normalized;
            _movementInputInitialStairSlopeClimb = _movementInput.normalized;
        }

        private void DoCheckStartClimbDownStairSlopes(Vector3 initialForward_ProbePosition, Vector3 initialForward_ProbeDirection)
        {
            if (!OnGround || _stepsSinceLastGrounded > 1 || _stepsSinceLastJump > 0)
            {
                return;
            }

            Vector3 currentPosition = GetPosition();
            float bodyOffsetToFeet = GetOffsetCenterToFeet();
            float colliderRadius = GetColliderRadius();

            Vector3 slopeDown_ProbePosition = initialForward_ProbePosition + (initialForward_ProbeDirection * colliderRadius);

            if (Physics.Raycast(slopeDown_ProbePosition, Vector3.down, out RaycastHit slopeDown_Hit, Config.maxStairSlopeHeight,
                Config.collisionProbeMask, QueryTriggerInteraction.Ignore) && slopeDown_Hit.distance > 0.015f)
            {
                Vector3 targetPositionDownSlope = slopeDown_Hit.point;
                targetPositionDownSlope.y = slopeDown_Hit.point.y + bodyOffsetToFeet + 0.01f;

                _isClimbingStairSlopes = true;
                _targetPositionOnStairSlope = targetPositionDownSlope;
                _initialDirectionToStairSlope = (_targetPositionOnStairSlope - currentPosition).normalized;
                _movementInputInitialStairSlopeClimb = _movementInput.normalized;
            }
        }


        private void ApplyClimbStairSlopesVelocity()
        {
            if (!_isClimbingStairSlopes)
            {
                return;                
            }

            bool movementInputChanged = (_movementInput.x + _movementInput.y) > 0.01f &&
                                        Vector3.Dot(_movementInputInitialStairSlopeClimb, _movementInput.normalized) < 0.9f;
            if (_desiredStartJumping || _stepsSinceLastJump < 1 || movementInputChanged)
            {
                _isClimbingStairSlopes = false;
                return;
            }

            Vector3 currentPosition = GetPosition();
            Vector3 toSlopeTip = _targetPositionOnStairSlope - currentPosition;
            bool reachedSlopeTip = Vector3.Dot(_initialDirectionToStairSlope, toSlopeTip) < 0;
            if (reachedSlopeTip)
            {
                //Debug.Log("Reached slope");
                _isClimbingStairSlopes = false;
                return;
            }

            _currentVelocity = toSlopeTip.normalized * Config.maxSpeed;
        }


    }


}

