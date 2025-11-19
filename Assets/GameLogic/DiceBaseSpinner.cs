using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Spins the DiceBase object around its local Z axis to provide visual feedback.
    /// </summary>
    public class DiceBaseSpinner : MonoBehaviour
    {
        [Header("Rotation")]
        public float spinSpeed = 60f;
        public bool reverse = false;
        public bool useUnscaledTime = false;

        Transform target;

        void Awake()
        {
            target = transform;
        }

        void Update()
        {
            float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float direction = reverse ? -1f : 1f;
            target.Rotate(0f, 0f, spinSpeed * direction * delta, Space.Self);
        }
    }
}
