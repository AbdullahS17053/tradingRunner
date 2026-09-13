using UnityEngine;

namespace SciFiUI.Effects
{
    public class UIRotateEffect : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [Tooltip("Degrees per second. Positive = Counter-Clockwise, Negative = Clockwise.")]
        [SerializeField] private float rotationSpeed = -30f;
        [SerializeField] private bool useUnscaledTime = true;

        private void Update()
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.Rotate(0f, 0f, rotationSpeed * dt);
        }
    }
}
