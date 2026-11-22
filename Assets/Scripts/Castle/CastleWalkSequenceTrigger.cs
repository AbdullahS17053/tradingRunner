using UnityEngine;

namespace RunnerForex.Castle
{
    /// <summary>
    /// Attached to the user-created trigger collider:
    /// 'Trigger fow walk and get near so door openes and sequence continues for castel lavel '
    /// Uses OnTriggerEnter and OnTriggerStay to transition player to walk state and advance the sequence.
    /// </summary>
    public class CastleWalkSequenceTrigger : MonoBehaviour
    {
        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleTrigger(other);
        }

        private void OnTriggerStay(Collider other)
        {
            HandleTrigger(other);
        }

        private void HandleTrigger(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<RunnerController>() != null || other.GetComponentInParent<RunnerController>() != null || other is CharacterController)
            {
                var controller = CastleSequenceController.EnsureInstanceExists();
                if (controller != null)
                {
                    controller.OnPlayerInWalkTriggerStay();
                }
            }
        }
    }
}
