using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class RunnerController : MonoBehaviour
{
    [Header("Lane Settings")]
    [Tooltip("Total number of lanes. Match this with your chunk visualizers.")]
    public int numberOfLanes = 3;
    public float laneSpacing = 2.5f;

    [Header("Movement Settings")]
    public float forwardSpeed = 15f;
    public float laneSwitchSpeed = 12f;
    
    [Header("Jump Settings")]
    public float jumpForce = 8f;
    public float gravity = -20f;

    private CharacterController controller;
    private int currentLane;
    private Vector3 velocity;
    private float targetXPosition;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        
        currentLane = numberOfLanes / 2;
        targetXPosition = GetLaneXPosition(currentLane);
        
        Vector3 startPos = transform.position;
        startPos.x = targetXPosition;
        transform.position = startPos;
    }

    private void Update()
    {
        HandleInput();
        MovePlayer();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            currentLane--;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            currentLane++;
        }

        currentLane = Mathf.Clamp(currentLane, 0, numberOfLanes - 1);
        targetXPosition = GetLaneXPosition(currentLane);

        if (controller.isGrounded)
        {
            velocity.y = -2f; 
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            {
                velocity.y = jumpForce;
            }
        }
    }

    private void MovePlayer()
    {
        velocity.y += gravity * Time.deltaTime;
        Vector3 moveVector = new Vector3(0, velocity.y, forwardSpeed) * Time.deltaTime;

        float currentX = transform.position.x;
        float moveX = Mathf.Lerp(currentX, targetXPosition, laneSwitchSpeed * Time.deltaTime) - currentX;
        moveVector.x = moveX;

        controller.Move(moveVector);
    }

    private float GetLaneXPosition(int laneIndex)
    {
        float leftMostOffset = -(numberOfLanes - 1) * laneSpacing / 2f;
        return leftMostOffset + (laneIndex * laneSpacing);
    }
}