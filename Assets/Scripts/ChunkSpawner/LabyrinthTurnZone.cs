using UnityEngine;

public class LabyrinthTurnZone : MonoBehaviour
{
    [Tooltip("The transform marking the center of the new corridor after the 90-degree turn")]
    public Transform turnPivot;

    [Tooltip("-90 for Left turn, +90 for Right turn")]
    public float turnAngle = -90f;

    private bool hasTriggered = false;

    private void OnEnable()
    {
        hasTriggered = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (hasTriggered || !other.CompareTag("Player")) return;

        RunnerController runner = other.GetComponent<RunnerController>();
        if (runner == null) runner = RunnerController.Instance;
        if (runner == null || !runner.enableLabyrinthTurns || turnPivot == null) return;

        Vector3 currentForward = runner.PathForward;
        Vector3 toPivot = turnPivot.position - runner.transform.position;
        float forwardDistToPivot = Vector3.Dot(toPivot, currentForward);

        if (forwardDistToPivot <= 0.35f)
        {
            hasTriggered = true;
            runner.ExecuteCornerTurn(turnPivot.position, turnPivot.rotation, turnAngle);
        }
    }
}


