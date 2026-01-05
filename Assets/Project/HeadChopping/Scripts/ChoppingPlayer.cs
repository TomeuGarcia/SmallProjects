using UnityEngine;



namespace HeadChopping
{

    public class ChoppingPlayer : MonoBehaviour
    {
        [SerializeField] private FirstPersonCameraMovement _firstPersonCameraMovement;
        [SerializeField] private PhysicsMovement _physicsMovement;
        private ChoppingPlayerInputs _inputs;


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
        }


    }


}