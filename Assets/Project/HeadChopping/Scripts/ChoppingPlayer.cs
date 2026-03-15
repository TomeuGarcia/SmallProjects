using UnityEngine;



namespace HeadChopping
{

    public class ChoppingPlayer : MonoBehaviour, TriggerTeleporter.ITarget
    {
        [SerializeField] private FirstPersonCameraMovement _firstPersonCameraMovement;
        [SerializeField] private PhysicsMovement _physicsMovement;
        private ChoppingPlayerInputs _inputs;

        [Header("TESTING")]
        [SerializeField] private ParticleSystem _bloodParticlesPrefab;
        [SerializeField] private LayerMask _spawnBloodMask = -1;

        private float _timeSinceLastMovement;
        private bool _previousOnGround = true;

        private void Awake()
        {
            _inputs = new ChoppingPlayerInputs(_firstPersonCameraMovement.CameraTransform);
            _firstPersonCameraMovement.AwakeConfigure(_inputs);
            _physicsMovement.AwakeConfigure(_inputs, autoUpdate: false);
            _timeSinceLastMovement = 0.0f;
        }

        private void OnDestroy()
        {
            _inputs.Cleanup();
        }

        private void Update()
        {
            _inputs.Update();
            _physicsMovement.DoUpdate();
            UpdateTimeSinceLastMovement();

            if (_inputs.AttackRequested())
            {
                SpawnBlood();
            }

            DEBUG_TestGroundDetection();
        }

        private void LateUpdate()
        {
            _firstPersonCameraMovement.DoUpdate(Time.deltaTime);

            float timeSinceLastInputChange = Mathf.Min(_firstPersonCameraMovement.GetTimeSinceLastInputChange(), _timeSinceLastMovement);
        }

        private void FixedUpdate()
        {
            _physicsMovement.DoFixedUpdate();
        }


        private void SpawnBlood()
        {
            Vector3 position = _firstPersonCameraMovement.CameraTransform.position;
            Vector3 direction = _firstPersonCameraMovement.CameraTransform.forward;
            if (Physics.Raycast(position, direction, out RaycastHit hit, maxDistance: 50.0f, _spawnBloodMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 spawnPosition = hit.point + (hit.normal * 0.2f);
                Quaternion spawnRotation = Quaternion.LookRotation(hit.normal);
                Instantiate(_bloodParticlesPrefab, spawnPosition, spawnRotation);
            }
        }

        private void UpdateTimeSinceLastMovement()
        {
            _physicsMovement.GetVelocity(out Vector3 relativeVelocity);
            bool moved = (relativeVelocity.x + relativeVelocity.y + relativeVelocity.z) > 0.0f;
            if (moved)
            {
                _timeSinceLastMovement = 0.0f;
            }
            else
            {
                _timeSinceLastMovement += Time.deltaTime;
            }
        }

        private void DEBUG_TestGroundDetection()
        {
            return;
            _physicsMovement.GetVelocity(out Vector3 relativeVelocity);
            Debug.Log(relativeVelocity);

            return;
            _physicsMovement.GetAirState(out bool isGroundJumpRising, out bool isWallJumpRising, out bool isAirJumpRising, out bool isFalling);
            if (isGroundJumpRising)  Debug.Log("Ground Jumping");
            if (isWallJumpRising)  Debug.Log("Wall Jumping");
            if (isAirJumpRising)  Debug.Log("Air Jumping");
            if (isFalling)  Debug.Log("Falling");

            return;
            _physicsMovement.GetGroundedState(out bool onGround, out bool onSteep, out bool climbingStairSlopes, out bool isStandingOnColliderEdge);
            if (!_previousOnGround && (onGround || climbingStairSlopes  /*|| onSteep*/))
            {
                _previousOnGround = true;
                Debug.Log($"[PLAYER] Start Grounded:   onGround {onGround}   onSteep {onSteep}   climbingStairSlopes {climbingStairSlopes}   isStandingOnColliderEdge {isStandingOnColliderEdge}");
            }
            if (_previousOnGround && (!onGround && !climbingStairSlopes /*&& !onSteep*/))
            {
                _previousOnGround = false;
                Debug.Log($"[PLAYER] Start Air:   onGround {(onGround)}   onSteep {(onSteep)}   climbingStairSlopes {climbingStairSlopes}   isStandingOnColliderEdge {isStandingOnColliderEdge}");
            }
        }



        // Explicit Interfaces

        void TriggerTeleporter.ITarget.RequestTeleport(Vector3 teleportDestinationPosition)
        {
            _physicsMovement.TeleportToPosition(teleportDestinationPosition, clearVelocity: true);
        }
    }


}