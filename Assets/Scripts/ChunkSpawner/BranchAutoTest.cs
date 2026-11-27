using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Temporary diagnostics: auto-drives the Labyrinth, always choosing the right branch,
/// and logs the spawner state after every turn plus any spawn stalls.
/// </summary>
public class BranchAutoTest : MonoBehaviour
{
    public float maxRunSeconds = 90f;

    private float elapsed;
    private FieldInfo zoneField;
    private FieldInfo waitField;
    private FieldInfo activeField;
    private FieldInfo connField;
    private FieldInfo contField;
    private FieldInfo netField;
    private FieldInfo queueField;
    private FieldInfo totalField;
    private ChunkSpawner spawner;
    private int lastTotal = -1;
    private float stallTimer;
    private int junctions;

    private void Start()
    {
        spawner = Object.FindAnyObjectByType<ChunkSpawner>();
        zoneField = typeof(RunnerController).GetField("currentTurnZone", BindingFlags.NonPublic | BindingFlags.Instance);
        System.Type st = typeof(ChunkSpawner);
        waitField = st.GetField("waitingForBranchDecision", BindingFlags.NonPublic | BindingFlags.Instance);
        activeField = st.GetField("activeBranchingChunk", BindingFlags.NonPublic | BindingFlags.Instance);
        connField = st.GetField("currentConnectionPoint", BindingFlags.NonPublic | BindingFlags.Instance);
        contField = st.GetField("branchContinuationPending", BindingFlags.NonPublic | BindingFlags.Instance);
        netField = st.GetField("currentNetHeading", BindingFlags.NonPublic | BindingFlags.Instance);
        queueField = st.GetField("activeChunks", BindingFlags.NonPublic | BindingFlags.Instance);
        totalField = st.GetField("totalSpawnedChunks", BindingFlags.NonPublic | BindingFlags.Instance);
        Debug.Log("BOT start spawner=" + (spawner != null));
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed > maxRunSeconds)
        {
            Debug.Log("BOT end junctions=" + junctions);
            enabled = false;
            return;
        }

        RunnerController rc = RunnerController.Instance;
        if (rc == null || spawner == null) return;

        if (rc.IsDead)
        {
            Debug.Log("BOT dead at junctions=" + junctions + " pos=" + rc.transform.position);
            enabled = false;
            return;
        }

        LabyrinthTurnZone zone = zoneField.GetValue(rc) as LabyrinthTurnZone;
        if (zone != null && !zone.HasTriggered)
        {
            junctions++;
            int dir = zone.CanTurnRight ? 1 : -1;
            bool waitingBefore = (bool)waitField.GetValue(spawner);
            int totalBefore = (int)totalField.GetValue(spawner);

            rc.MoveLane(dir);

            Transform conn = (Transform)connField.GetValue(spawner);
            ICollection chunks = (ICollection)queueField.GetValue(spawner);
            Debug.Log("BOT turn#" + junctions + " mode=" + zone.turnMode + " dir=" + dir
                + " waitingBefore=" + waitingBefore + " totalBefore=" + totalBefore
                + " waitAfter=" + waitField.GetValue(spawner)
                + " cont=" + contField.GetValue(spawner)
                + " net=" + netField.GetValue(spawner)
                + " conn=" + (conn != null ? conn.position.ToString() : "null")
                + " chunks=" + (chunks != null ? chunks.Count : -1)
                + " playerPos=" + rc.transform.position.ToString("F1"));
        }

        int total = (int)totalField.GetValue(spawner);
        if (total != lastTotal)
        {
            lastTotal = total;
            stallTimer = 0f;
        }
        else
        {
            stallTimer += Time.deltaTime;
            if (stallTimer > 6f)
            {
                TrackChunk active = (TrackChunk)activeField.GetValue(spawner);
                Debug.Log("BOT STALL total=" + total
                    + " waiting=" + waitField.GetValue(spawner)
                    + " activeBranch=" + (active != null ? active.name : "null")
                    + " playerPos=" + rc.transform.position.ToString("F1")
                    + " chunks=" + ((ICollection)queueField.GetValue(spawner)).Count);
                stallTimer = 0f;
            }
        }
    }
}
