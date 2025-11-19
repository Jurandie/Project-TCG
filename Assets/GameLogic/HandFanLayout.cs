using UnityEngine;

namespace GameLogic
{
    [ExecuteAlways]
    public class HandFanLayout : MonoBehaviour
    {
        [Tooltip("Raio do arco (maior = cartas mais afastadas).")]
        public float radius = 350f;

        [Tooltip("Ângulo total do leque.")]
        public float angleRange = 50f;

        [Tooltip("Offset vertical aplicado às cartas.")]
        public float yOffset = -60f;

        [Tooltip("Rotação adicional aplicada para simular tilt.")]
        public float tiltIntensity = 0.6f;

        [Tooltip("Mantém as cartas dentro do retângulo do container.")]
        public bool clampWithinParent = true;

        void LateUpdate()
        {
            int count = transform.childCount;
            if (count == 0) return;

            float step = count > 1 ? angleRange / (count - 1) : 0f;
            float startAngle = -angleRange * 0.5f;
            RectTransform parentRect = transform as RectTransform;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + step * i;
                float rad = angle * Mathf.Deg2Rad;

                RectTransform rect = transform.GetChild(i) as RectTransform;
                if (rect == null) continue;

                Vector2 basePos = new Vector2(
                    Mathf.Sin(rad) * radius,
                    yOffset + (1f - Mathf.Cos(rad)) * radius);

                if (clampWithinParent && parentRect != null)
                {
                    float halfWidth = parentRect.rect.width * 0.5f;
                    float halfHeight = parentRect.rect.height * 0.5f;
                    basePos.x = Mathf.Clamp(basePos.x, -halfWidth, halfWidth);
                    basePos.y = Mathf.Clamp(basePos.y, -halfHeight, halfHeight);
                }

                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = basePos;
                rect.localScale = Vector3.one; // garante escala original
                float tilt = angle * tiltIntensity;
                float clampTilt = Mathf.Clamp(tilt, -45f, 45f);
                rect.localRotation = Quaternion.Euler(0f, 0f, clampTilt);
                rect.SetSiblingIndex(i);
            }
        }
    }
}
