using UnityEngine;


namespace HeadChopping
{

    public class TriggerTeleporter : MonoBehaviour
    {
        public interface ITarget
        {
            public void RequestTeleport(Vector3 teleportDestinationPosition);
        }


        [Header("DESTINATION")]
        [SerializeField] private Transform _teleportDestination;


        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out ITarget target))
            {
                target.RequestTeleport(_teleportDestination.position);
            }
        }
    }


}