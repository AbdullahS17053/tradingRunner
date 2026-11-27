using UnityEngine;

public class TrackChunk : MonoBehaviour
{
    public Transform connectionPoint;
    public Transform coinsParent;

    [Header("Branching (T-Junction)")]
    public bool isBranchingChunk = false;
    public Transform leftConnectionPoint;
    public Transform rightConnectionPoint;

    private void Awake()
    {
        EnsureBranchingSetup();
    }

    public void EnsureBranchingSetup()
    {
        if (!isBranchingChunk)
        {
            LabyrinthTurnZone ltz = GetComponentInChildren<LabyrinthTurnZone>();
            if (ltz != null && ltz.turnMode == TurnDirectionMode.Both)
            {
                isBranchingChunk = true;
            }
        }

        if (!isBranchingChunk) return;

        // Ensure LeftTurnWing and RightTurnWing exist
        Transform leftWing = FindChildByName("LeftTurnWing");
        Transform rightWing = FindChildByName("RightTurnWing");

        if (leftWing == null && rightWing != null)
        {
            GameObject mirroredLeft = Instantiate(rightWing.gameObject, transform);
            mirroredLeft.name = "LeftTurnWing";
            mirroredLeft.transform.localPosition = new Vector3(0f, 0f, 19.5f);
            mirroredLeft.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            leftWing = mirroredLeft.transform;
        }
        else if (rightWing == null && leftWing != null)
        {
            GameObject mirroredRight = Instantiate(leftWing.gameObject, transform);
            mirroredRight.name = "RightTurnWing";
            mirroredRight.transform.localPosition = new Vector3(0f, 0f, 19.5f);
            mirroredRight.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            rightWing = mirroredRight.transform;
        }

        // Ensure LabyrinthTurnZone is set to Both
        LabyrinthTurnZone zone = GetComponentInChildren<LabyrinthTurnZone>();
        if (zone != null)
        {
            zone.turnMode = TurnDirectionMode.Both;
            zone.UpdateVisuals();
        }

        // Connection Points
        if (leftConnectionPoint == null)
        {
            Transform foundLeft = FindChildByName("ConnectionPoint_Left") ?? FindChildByName("ConnectionPointLeft");
            if (foundLeft != null)
            {
                leftConnectionPoint = foundLeft;
            }
            else
            {
                GameObject lcp = new GameObject("ConnectionPoint_Left");
                lcp.transform.SetParent(transform, false);
                Vector3 rightPosition = rightConnectionPoint != null
                    ? rightConnectionPoint.localPosition
                    : new Vector3(30.85f, 0f, 19.5f);
                lcp.transform.localPosition = new Vector3(-Mathf.Abs(rightPosition.x), rightPosition.y, rightPosition.z);
                lcp.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                leftConnectionPoint = lcp.transform;
            }
        }

        if (rightConnectionPoint == null)
        {
            Transform foundRight = FindChildByName("ConnectionPoint_Right") ?? FindChildByName("ConnectionPointRight");
            if (foundRight != null)
            {
                rightConnectionPoint = foundRight;
            }
            else
            {
                GameObject rcp = new GameObject("ConnectionPoint_Right");
                rcp.transform.SetParent(transform, false);
                rcp.transform.localPosition = new Vector3(32.5f, 0f, 19.5f);
                rcp.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                rightConnectionPoint = rcp.transform;
            }
        }

        NormalizeBranchConnectionPoint(leftConnectionPoint, false);
        NormalizeBranchConnectionPoint(rightConnectionPoint, true);
    }

    public Transform GetConnectionPoint(float turnAngle)
    {
        EnsureBranchingSetup();

        if (isBranchingChunk)
        {
            if (turnAngle < 0f)
            {
                if (leftConnectionPoint != null) return leftConnectionPoint;
                Transform foundLeft = FindChildByName("ConnectionPoint_Left") ?? FindChildByName("ConnectionPointLeft");
                if (foundLeft != null) { leftConnectionPoint = foundLeft; return foundLeft; }
            }
            else if (turnAngle > 0f)
            {
                if (rightConnectionPoint != null) return rightConnectionPoint;
                Transform foundRight = FindChildByName("ConnectionPoint_Right") ?? FindChildByName("ConnectionPointRight");
                if (foundRight != null) { rightConnectionPoint = foundRight; return foundRight; }
            }
        }
        return connectionPoint != null ? connectionPoint : transform;
    }

    private Transform FindChildByName(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child != transform && child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private void NormalizeBranchConnectionPoint(Transform point, bool rightSide)
    {
        if (point == null) return;

        float side = rightSide ? 1f : -1f;
        float branchDistance = Mathf.Max(1f, Mathf.Abs(point.localPosition.x));

        point.SetParent(transform, false);
        point.localPosition = new Vector3(side * branchDistance, 0f, 19.5f);
        point.localRotation = Quaternion.Euler(0f, rightSide ? 90f : -90f, 0f);
    }

    // Optional: Add references to obstacles, coins, or enemies here 
    // if you want the spawner to reset them when the chunk is recycled.

    private void OnEnable()
    {
        ResetCollectibles();
    }

    private void ResetCollectibles()
    {
        if (coinsParent == null) return;

        foreach (Transform coin in coinsParent)
        {
            coin.gameObject.SetActive(true);
        }
    }

    private void OnDisable()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<PowerUpPickup>() != null || child.GetComponent<PortalTrigger>() != null || child.GetComponent<FomoBot>() != null)
            {
                Destroy(child.gameObject);
            }
        }
    }
}