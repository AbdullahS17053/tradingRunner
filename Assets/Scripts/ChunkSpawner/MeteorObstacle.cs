using UnityEngine;

public class MeteorObstacle : MonoBehaviour
{
    [Header("Meteor Visual")]
    public Transform visualMesh;
    public Vector3 rotationSpeed = Vector3.zero;
    public float incomingSpeed = 0f;
    public float activationDistance = 0f;
    public float maxTravelDistance = 0f;

    // Meteors stay stationary at their exact aligned chunk positions
}
