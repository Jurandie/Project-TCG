using System.Collections;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Handles a Hearthstone-style attack dash animation for hero portraits.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class HeroAttackAnimator : MonoBehaviour
    {
        [Header("Configuração")]
        public float dashDistance = 120f;
        public float dashDuration = 0.18f;
        public float returnDuration = 0.25f;
        public float tiltAngle = 18f;
        public float impactShake = 12f;
        public float shakeDuration = 0.2f;

        [Header("Feedback opcional")]
        public AudioSource attackAudio;
        public ParticleSystem attackEffect;

        RectTransform rect;
        Vector2 origin;
        Quaternion originRotation;
        Coroutine currentRoutine;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            origin = rect.anchoredPosition;
            originRotation = rect.localRotation;
        }

        public void Play()
        {
            if (!isActiveAndEnabled)
                return;

            if (currentRoutine != null)
                StopCoroutine(currentRoutine);

            currentRoutine = StartCoroutine(AttackRoutine());
        }

        IEnumerator AttackRoutine()
        {
            if (attackAudio != null)
                attackAudio.Play();
            if (attackEffect != null)
                attackEffect.Play();

            Vector2 targetOffset = new Vector2(0f, dashDistance);
            Vector2 start = rect.anchoredPosition;
            Quaternion startRot = originRotation;
            Quaternion targetRot = Quaternion.Euler(0f, 0f, -tiltAngle);

            float t = 0f;
            while (t < dashDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dashDuration);
                u = EaseOutQuad(u);
                rect.anchoredPosition = Vector2.Lerp(start, start + targetOffset, u);
                rect.localRotation = Quaternion.Slerp(startRot, targetRot, u);
                yield return null;
            }

            yield return StartCoroutine(ReturnRoutine(start));
            currentRoutine = null;
        }

        IEnumerator ReturnRoutine(Vector2 startAnchor)
        {
            Vector2 attackPos = rect.anchoredPosition;
            Quaternion attackRot = rect.localRotation;

            float t = 0f;
            while (t < returnDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / returnDuration);
                u = EaseOutQuad(u);
                rect.anchoredPosition = Vector2.Lerp(attackPos, origin, u);
                rect.localRotation = Quaternion.Slerp(attackRot, originRotation, u);
                yield return null;
            }

            rect.anchoredPosition = origin;
            rect.localRotation = originRotation;

            if (impactShake > 0f)
                yield return StartCoroutine(ShakeRoutine());
        }

        IEnumerator ShakeRoutine()
        {
            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float damping = 1f - (elapsed / shakeDuration);
                float offsetX = Mathf.Sin(elapsed * 30f) * impactShake * damping;
                rect.anchoredPosition = origin + new Vector2(offsetX, 0f);
                yield return null;
            }

            rect.anchoredPosition = origin;
        }

        float EaseOutQuad(float x)
        {
            return 1f - (1f - x) * (1f - x);
        }
    }
}
