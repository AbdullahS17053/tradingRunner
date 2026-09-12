using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class RunnerController : MonoBehaviour
{
    public static RunnerController Instance { get; private set; }

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

    private float originalHeight;
    private Vector3 originalCenter;
    private bool isSliding = false;
    private float slideTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

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
        HandleKeyboardInput();
        MovePlayer();
        UpdateAnimations();
        HandleSlidingTimer();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            MoveLane(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            MoveLane(1);

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            TriggerJump();
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            TriggerSlide();
    }


    public void MoveLane(int direction)
    {
        int desiredLane = Mathf.Clamp(currentLane + direction, 0, numberOfLanes - 1);

        if (desiredLane != currentLane)
        {
            if (controller.isGrounded && !isSliding)
            {
                if (desiredLane < currentLane)
                    animator.SetTrigger("DodgeLeft");
                else
                    animator.SetTrigger("DodgeRight");
            }

            currentLane = desiredLane;
            targetXPosition = GetLaneXPosition(currentLane);
        }
    }

    public void TriggerJump()
    {
        if (controller.isGrounded)
        {
            if (isSliding) StopSliding();

            velocity.y = jumpForce;
            animator.ResetTrigger("Slide");
            animator.SetTrigger("Jump");
        }
    }

    public void TriggerSlide()
    {
        if (controller.isGrounded)
        {
            isSliding = true;
            slideTimer = slideDuration;

            controller.height = originalHeight / 2f;
            controller.center = new Vector3(originalCenter.x, originalCenter.y / 2f, originalCenter.z);

            animator.ResetTrigger("Jump");
            animator.SetTrigger("Slide");
        }
        else
        {
            velocity.y = fastFallVelocity;
        }
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
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        float currentSpeedMultiplier = GameManager.Instance != null ? GameManager.Instance.currentSpeedMultiplier : 1f;
        float currentForwardSpeed = forwardSpeed * currentSpeedMultiplier;

        Vector3 moveVector = new Vector3(0, velocity.y, currentForwardSpeed) * Time.deltaTime;

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
        SoundManager.Instance.StopMusic();
        SoundManager.Instance.PlaySFX(SoundManager.Instance.crashSound);
        GameManager.Instance.TriggerGameOver();
    }
}
