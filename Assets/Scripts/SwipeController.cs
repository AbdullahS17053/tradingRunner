using UnityEngine;

public class SwipeController : MonoBehaviour
{
    [Header("Swipe Settings")]
    public float minimumSwipeDistance = 50f;

    private Vector2 touchStartPos;
    private Vector2 touchEndPos;

    private bool hasSwiped = false;

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
                hasSwiped = false; // Unlock on new touch
            }
            else if (touch.phase == TouchPhase.Moved && !hasSwiped)
            {
                touchEndPos = touch.position;
                CheckSwipe();
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                hasSwiped = false;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
            hasSwiped = false; // Unlock on new click
        }
        else if (Input.GetMouseButton(0) && !hasSwiped)
        {
            touchEndPos = Input.mousePosition;
            CheckSwipe();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            hasSwiped = false;
        }
    }

    private void CheckSwipe()
    {
        float distance = Vector2.Distance(touchStartPos, touchEndPos);

        if (distance > minimumSwipeDistance)
        {
            Vector2 direction = touchEndPos - touchStartPos;

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                if (direction.x > 0)
                    SwipeRight();
                else
                    SwipeLeft();
            }
            else
            {
                if (direction.y > 0)
                    SwipeUp();
                else
                    SwipeDown();
            }

            hasSwiped = true;
        }
    }

    private void SwipeLeft()
    {
        if (RunnerController.Instance != null)
            RunnerController.Instance.MoveLane(-1);
    }

    private void SwipeRight()
    {
        if (RunnerController.Instance != null)
            RunnerController.Instance.MoveLane(1);
    }

    private void SwipeUp()
    {
        if (RunnerController.Instance != null)
            RunnerController.Instance.TriggerJump();
    }

    private void SwipeDown()
    {
        if (RunnerController.Instance != null)
            RunnerController.Instance.TriggerSlide();
    }
}
