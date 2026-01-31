using UnityEngine;



namespace HeadChopping
{

    public class ChoppingPlayer : MonoBehaviour
    {
        [SerializeField] private FirstPersonCameraMovement _firstPersonCameraMovement;
        [SerializeField] private PhysicsMovement _physicsMovement;
        private ChoppingPlayerInputs _inputs;

        [Header("TESTING")]
        [SerializeField] private ParticleSystem _bloodParticlesPrefab;
        [SerializeField] private LayerMask _spawnBloodMask = -1;


        private void Awake()
        {
            _inputs = new ChoppingPlayerInputs(_firstPersonCameraMovement.CameraTransform);
            _firstPersonCameraMovement.AwakeConfigure(_inputs);
            _physicsMovement.AwakeConfigure(_inputs, autoUpdate: false);
        }

        private void Update()
        {
            _inputs.Update();
            _firstPersonCameraMovement.DoUpdate();
            _physicsMovement.DoUpdate();

            if (_inputs.AttackRequested())
            {
                SpawnBlood();
            }
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
    }


}