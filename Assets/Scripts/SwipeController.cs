using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeController : MonoBehaviour
{
    [Header("Swipe Settings")]
    public float minimumSwipeDistance = 50f;

    private Vector2 touchStartPos;
    private Vector2 touchEndPos;

    private bool hasSwiped = false;
    private bool touchStartedOnUI = false;

    private void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.isGameOver || GameManager.Instance.isTradingChallengeActive))
        {
            return;
        }

        if (Time.timeScale == 0f || (RunnerController.Instance != null && !RunnerController.Instance.enabled))
        {
            return;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStartedOnUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId);
                touchStartPos = touch.position;
                hasSwiped = false;
            }
            else if (touch.phase == TouchPhase.Moved && !hasSwiped && !touchStartedOnUI)
            {
                touchEndPos = touch.position;
                CheckSwipe();
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                hasSwiped = false;
                touchStartedOnUI = false;
            }
        }
        else if (!Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0))
        {
            // Safety net: if a touch/mouse release was missed (app focus loss,
            // cancel outside the window) do not leave the gesture flag stuck.
            hasSwiped = false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            touchStartedOnUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            touchStartPos = Input.mousePosition;
            hasSwiped = false;
        }
        else if (Input.GetMouseButton(0) && !hasSwiped && !touchStartedOnUI)
        {
            touchEndPos = Input.mousePosition;
            CheckSwipe();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            hasSwiped = false;
            touchStartedOnUI = false;
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
