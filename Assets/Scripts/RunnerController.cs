using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class RunnerController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    [Header("Lane Settings")]
    public int numberOfLanes = 3;
    public float laneSpacing = 2.5f;

    [Header("Movement Settings")]
    public float forwardSpeed = 15f;
    public float laneSwitchSpeed = 12f;
    
    [Header("Jump & Slide Settings")]
    public float jumpForce = 8f;
    public float gravity = -20f;
    public float slideDuration = 1.0f;
    [Tooltip("How fast the character slams down when sliding mid-air")]
    public float fastFallVelocity = -20f; 

    private CharacterController controller;
    private int currentLane;
    private Vector3 velocity;
    private float targetXPosition;

    // Collider management
    private float originalHeight;
    private Vector3 originalCenter;
    private bool isSliding = false;
    private float slideTimer;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        originalHeight = controller.height;
        originalCenter = controller.center;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        
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
        UpdateAnimations();
        HandleSlidingTimer();
    }

   private void HandleInput()
    {
        int desiredLane = currentLane;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            desiredLane--;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            desiredLane++;

        desiredLane = Mathf.Clamp(desiredLane, 0, numberOfLanes - 1);

        if (desiredLane != currentLane)
        {
            if (controller.isGrounded && !isSliding)
            {
                if (desiredLane < currentLane)
                {
                    animator.SetTrigger("DodgeLeft");
                }
                else
                {
                    animator.SetTrigger("DodgeRight");
                }
            }
            
            // The mathematical movement still happens regardless of animation state
            currentLane = desiredLane;
            targetXPosition = GetLaneXPosition(currentLane);
        }

        if (controller.isGrounded)
        {
            if (velocity.y < 0) velocity.y = -2f; 
            
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            {
                ExecuteJump();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                ExecuteSlide();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                velocity.y = fastFallVelocity; 
            }
        }
    }

    private void ExecuteJump()
    {
        if (isSliding) StopSliding();
        
        velocity.y = jumpForce;
        
        animator.ResetTrigger("Slide"); 
        animator.SetTrigger("Jump");
    }

    private void ExecuteSlide()
    {
        isSliding = true;
        slideTimer = slideDuration; // Reset the timer
        
        controller.height = originalHeight / 2f;
        controller.center = new Vector3(originalCenter.x, originalCenter.y / 2f, originalCenter.z);

        animator.ResetTrigger("Jump");
        animator.SetTrigger("Slide");
    }

    private void HandleSlidingTimer()
    {
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0)
            {
                StopSliding();
            }
        }
    }

    private void StopSliding()
    {
        isSliding = false;
        controller.height = originalHeight;
        controller.center = originalCenter;
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

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetBool("Grounded", controller.isGrounded);
        }
    }

    private float GetLaneXPosition(int laneIndex)
    {
        float leftMostOffset = -(numberOfLanes - 1) * laneSpacing / 2f;
        return leftMostOffset + (laneIndex * laneSpacing);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Hit an obstacle! Restarting level...");
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}