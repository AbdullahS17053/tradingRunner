using UnityEngine;

public enum TurnDirectionMode
{
    LeftOnly,
    RightOnly,
    Both
}

public class LabyrinthTurnZone : MonoBehaviour
{
    [Header("Branch Availability")]
    public TurnDirectionMode turnMode = TurnDirectionMode.Both;

    [Header("Turn Pivots")]
    [Tooltip("Pivot used for Left Turn (-90)")]
    public Transform leftPivot;
    [Tooltip("Pivot used for Right Turn (+90)")]
    public Transform rightPivot;

    [Tooltip("Fallback pivot")]
    public Transform turnPivot;
    public float turnAngle = -90f;

    [Header("Indicators & Visuals")]
    public GameObject leftArrowIndicator;
    public GameObject rightArrowIndicator;

    private bool hasTriggered = false;
    private RunnerController activeRunner;

    public bool CanTurnLeft => (turnMode == TurnDirectionMode.LeftOnly || turnMode == TurnDirectionMode.Both);
    public bool CanTurnRight => (turnMode == TurnDirectionMode.RightOnly || turnMode == TurnDirectionMode.Both);
    public bool HasTriggered => hasTriggered;

    private void OnEnable()
    {
        hasTriggered = false;
        activeRunner = null;
        SetupDefaultPivots();
        UpdateVisuals();
    }

    private void Start()
    {
        SetupDefaultPivots();
        CreateProceduralIndicatorsIfMissing();
        UpdateVisuals();
    }

    private void Update()
    {
        if (hasTriggered || activeRunner == null) return;

        Transform pivot = (turnAngle < 0f && leftPivot != null) ? leftPivot : (turnAngle > 0f && rightPivot != null ? rightPivot : turnPivot);
        if (pivot != null)
        {
            Vector3 toPlayer = activeRunner.transform.position - pivot.position;
            float forwardDot = Vector3.Dot(toPlayer, transform.forward);
            // When runner reaches apex of corner, safely execute corner turn
            if (forwardDot >= -0.3f)
            {
                ExecuteTurn(activeRunner, turnAngle);
            }
        }
    }

    private void SetupDefaultPivots()
    {
        if (turnPivot != null)
        {
            if (turnAngle <= -45f && leftPivot == null) leftPivot = turnPivot;
            if (turnAngle >= 45f && rightPivot == null) rightPivot = turnPivot;
        }

        if (turnMode == TurnDirectionMode.Both)
        {
            if (leftPivot == null && rightPivot != null)
            {
                GameObject lp = new GameObject("GeneratedLeftPivot");
                lp.transform.SetParent(transform.parent != null ? transform.parent : transform, false);
                lp.transform.localPosition = rightPivot.localPosition;
                lp.transform.localRotation = Quaternion.Euler(0f, 270f, 0f);
                leftPivot = lp.transform;
            }
            else if (rightPivot == null && leftPivot != null)
            {
                GameObject rp = new GameObject("GeneratedRightPivot");
                rp.transform.SetParent(transform.parent != null ? transform.parent : transform, false);
                rp.transform.localPosition = leftPivot.localPosition;
                rp.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                rightPivot = rp.transform;
            }
        }
    }

    private void CreateProceduralIndicatorsIfMissing()
    {
        if (leftArrowIndicator == null && CanTurnLeft)
        {
            leftArrowIndicator = CreateFloorArrow("LeftFloorArrow", new Vector3(-1.8f, 0.06f, 15f), Quaternion.Euler(90f, 270f, 0f), new Color(0.1f, 0.85f, 1f));
        }

        if (rightArrowIndicator == null && CanTurnRight)
        {
            rightArrowIndicator = CreateFloorArrow("RightFloorArrow", new Vector3(1.8f, 0.06f, 15f), Quaternion.Euler(90f, 90f, 0f), new Color(1f, 0.55f, 0.1f));
        }
    }

    private GameObject CreateFloorArrow(string name, Vector3 localPos, Quaternion localRot, Color neonColor)
    {
        GameObject arrowObj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        arrowObj.transform.SetParent(transform.parent != null ? transform.parent : transform, false);
        arrowObj.transform.localPosition = localPos;
        arrowObj.transform.localRotation = localRot;
        arrowObj.transform.localScale = new Vector3(1.6f, 2.2f, 0.05f);

        // Quad mesh for glowing indicator
        MeshFilter mf = arrowObj.GetComponent<MeshFilter>();
        GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mf.sharedMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
        Destroy(tempQuad);

        MeshRenderer mr = arrowObj.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = neonColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", neonColor * 2.2f);
        mr.material = mat;

        return arrowObj;
    }

    public void UpdateVisuals()
    {
        if (leftArrowIndicator != null) leftArrowIndicator.SetActive(CanTurnLeft);
        if (rightArrowIndicator != null) rightArrowIndicator.SetActive(CanTurnRight);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || !other.CompareTag("Player")) return;

        RunnerController runner = other.GetComponent<RunnerController>();
        if (runner == null) runner = RunnerController.Instance;
        if (runner == null || !runner.enableLabyrinthTurns) return;

        activeRunner = runner;
        runner.RegisterActiveTurnZone(this);
    }

    private void OnTriggerStay(Collider other)
    {
        if (hasTriggered || !other.CompareTag("Player")) return;

        if (activeRunner == null)
        {
            activeRunner = other.GetComponent<RunnerController>();
            if (activeRunner == null) activeRunner = RunnerController.Instance;
            if (activeRunner != null && activeRunner.enableLabyrinthTurns)
            {
                activeRunner.RegisterActiveTurnZone(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && activeRunner != null)
        {
            activeRunner.UnregisterActiveTurnZone(this);
            activeRunner = null;
        }
    }

    public bool ExecuteTurn(RunnerController runner, float requestedAngle)
    {
        if (hasTriggered) return false;

        Transform chosenPivot = null;
        float finalAngle = requestedAngle;

        if (requestedAngle < 0f && CanTurnLeft)
        {
            chosenPivot = leftPivot != null ? leftPivot : turnPivot;
            finalAngle = -90f;
        }
        else if (requestedAngle > 0f && CanTurnRight)
        {
            chosenPivot = rightPivot != null ? rightPivot : turnPivot;
            finalAngle = 90f;
        }

        if (chosenPivot == null) return false;

        hasTriggered = true;
        runner.UnregisterActiveTurnZone(this);

        // Execute corner turn on runner
        runner.ExecuteCornerTurn(chosenPivot.position, chosenPivot.rotation, finalAngle);

        // Play corner turning sound
        if (SoundManager.Instance != null && SoundManager.Instance.correctStrategySound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.correctStrategySound);
        }

        // Notify ChunkSpawner so future chunks align to the chosen branch
        var spawner = FindFirstObjectByType<ChunkSpawner>();
        if (spawner != null)
        {
            spawner.OnLabyrinthBranchChosen(finalAngle, chosenPivot);
        }

        return true;
    }
}
