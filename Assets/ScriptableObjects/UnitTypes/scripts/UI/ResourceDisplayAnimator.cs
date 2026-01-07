using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Вспомогательный класс для анимаций UI
/// </summary>
public class ResourceDisplayAnimator : MonoBehaviour
{
    public static IEnumerator PulseAnimation(Transform target, float duration = 0.5f, float scale = 1.2f)
    {
        Vector3 originalScale = target.localScale;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            float pulse = Mathf.PingPong(progress * 2f, 1f);

            target.localScale = originalScale * (1f + (scale - 1f) * pulse);
            yield return null;
        }

        target.localScale = originalScale;
    }

    public static IEnumerator ColorFlashAnimation(Graphic graphic, Color flashColor, float duration = 0.3f)
    {
        Color originalColor = graphic.color;
        graphic.color = flashColor;

        yield return new WaitForSeconds(duration);

        graphic.color = originalColor;
    }

    public static IEnumerator ShakeAnimation(Transform target, float intensity = 5f, float duration = 0.5f)
    {
        Vector3 originalPosition = target.localPosition;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float shakeX = Mathf.Sin(Time.time * 30f) * intensity;
            float shakeY = Mathf.Cos(Time.time * 25f) * intensity;

            target.localPosition = originalPosition + new Vector3(shakeX, shakeY, 0) * (1f - timer / duration);
            yield return null;
        }

        target.localPosition = originalPosition;
    }
}