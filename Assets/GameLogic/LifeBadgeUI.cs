using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace GameLogic
{
    public class LifeBadgeUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("Referências")]
        public Text lifeText;
        public Image icon;

        [Header("Configuração")]
        public Color healColor = new Color(0.3f, 1f, 0.3f);
        public Color damageColor = new Color(1f, 0.3f, 0.3f);
        [Tooltip("Amplitude do pulso contínuo.")]
        public float pulseAmplitude = 0.08f;
        [Tooltip("Velocidade do pulso (ciclos por segundo).")]
        public float pulseSpeed = 4f;

        Color originalIconColor;
        Vector3 badgeOriginalScale;
        Vector3 iconBaseScale = Vector3.one;
        Coroutine pulseCoroutine;
        int currentLife = 1;
        float currentIntensity = 1f;

        void Awake()
        {
            EnsureReferences();
            if (icon != null)
            {
                originalIconColor = icon.color;
                iconBaseScale = icon.rectTransform.localScale;
            }
            badgeOriginalScale = transform.localScale;
            if (spellTargetManager == null)
                spellTargetManager = FindFirstObjectByType<SpellTargetSelectionManager>();
            ApplyPulseState();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!Application.isPlaying)
                EnsureReferences();
        }
#endif

        void EnsureReferences()
        {
            if (lifeText == null)
                lifeText = GetComponentInChildren<Text>(true);

            if (icon == null)
            {
                var images = GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (lifeText != null && img.gameObject == lifeText.gameObject)
                        continue;

                    icon = img;
                    break;
                }

                if (icon == null && images.Length > 0)
                    icon = images[0];
            }
        }

        public void UpdateLife(int newLife)
        {
            currentLife = newLife;
            if (lifeText != null)
                lifeText.text = newLife.ToString();

            ApplyPulseState();
            ApplyIntensity();
        }

        public void PlayDamageAnimation()
        {
            StopAllCoroutines();
            pulseCoroutine = null;
            StartCoroutine(DamageAnim());
        }

        public void PlayHealAnimation()
        {
            StopAllCoroutines();
            pulseCoroutine = null;
            StartCoroutine(HealAnim());
        }

        void ApplyPulseState()
        {
            bool shouldPulse = currentLife > 0;

            if (shouldPulse)
            {
                if (pulseCoroutine == null)
                    pulseCoroutine = StartCoroutine(PulseIcon());
            }
            else
            {
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                    pulseCoroutine = null;
                }

                if (icon != null)
                    icon.rectTransform.localScale = iconBaseScale;
            }
        }

        IEnumerator DamageAnim()
        {
            float t = 0f;

            if (icon != null)
                icon.color = damageColor;

            while (t < 0.2f)
            {
                float shake = Mathf.Sin(Time.time * 50f) * 5f;
                transform.localRotation = Quaternion.Euler(0f, 0f, shake);
                t += Time.deltaTime;
                yield return null;
            }

            transform.localRotation = Quaternion.identity;
            if (icon != null)
                icon.color = originalIconColor;
            ApplyPulseState();
        }

        IEnumerator HealAnim()
        {
            float t = 0f;
            if (icon != null)
                icon.color = healColor;

            while (t < 0.25f)
            {
                float scale = 1f + Mathf.Sin(t * 20f) * 0.08f;
                transform.localScale = badgeOriginalScale * scale;
                t += Time.deltaTime;
                yield return null;
            }

            transform.localScale = badgeOriginalScale;
            if (icon != null)
                icon.color = originalIconColor;
            ApplyPulseState();
        }

        IEnumerator PulseIcon()
        {
            float phase = 0f;
            while (true)
            {
                if (icon != null)
                {
                    phase += Time.deltaTime * pulseSpeed;
                    float scale = 1f + Mathf.Sin(phase) * pulseAmplitude;
                    icon.rectTransform.localScale = iconBaseScale * scale;
                }

                yield return null;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (spellTargetManager != null && spellTargetManager.TrySelectHero(owner))
                eventData.Use();
        }

        [Header("Alvo de Magia")]
        public TurnManager.TurnOwner owner = TurnManager.TurnOwner.Player;
        public SpellTargetSelectionManager spellTargetManager;

        public void SetIntensity(float normalized)
        {
            currentIntensity = Mathf.Clamp01(normalized);
            ApplyIntensity();
        }

        void ApplyIntensity()
        {
            if (icon == null)
                return;

            Color target = Color.Lerp(Color.red, originalIconColor, currentIntensity);
            icon.color = target;
        }
    }
}

