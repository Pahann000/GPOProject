using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class SounndPlay : MonoBehaviour
{
    public AudioClip soundClip;
    public float maxVolume = 0.8f;
    public float maxDistance = 20f;

    // Сглаживание
    public float smoothTime = 0.3f;

    private AudioSource audioSource;
    private UnityEngine.Transform cameraTransform;
    private float smoothVelocity;

    /// <summary>
    /// Инициализация компонента при запуске сцены
    /// </summary>
    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = soundClip;
        audioSource.loop = true;
        audioSource.volume = 0f;

        // Звук запускается здесь, при старте сцены
        audioSource.Play();

        Debug.Log("Динамический звук запущен. Источник: " + gameObject.name);

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("Основная камера не найдена!");
            enabled = false;
        }
    }

    /// <summary>
    /// Обновление громкости звука в каждом кадре
    /// Вычисляет расстояние до камеры и плавно регулирует громкость
    /// </summary>
    void Update()
    {
        if (cameraTransform == null) return;

        // Вычисляем дистанцию до камеры
        float distance = Vector3.Distance(transform.position, cameraTransform.position);

        // Вычисляем целевую громкость через кривую
        float targetVolume = CalculateVolumeByDistance(distance);

        audioSource.volume = Mathf.SmoothDamp(
            audioSource.volume,
            targetVolume,
            ref smoothVelocity,
            smoothTime
        );
    }

    /// <summary>
    /// Вычисляет громкость звука на основе расстояния до камеры
    /// Используется линейная зависимость: ближе - громче, дальше - тише
    /// </summary>
    /// <param name="distance">Расстояние от источника звука до камеры</param>
    /// <returns></returns>
    float CalculateVolumeByDistance(float distance)
    {
        if (distance > maxDistance)
            return 0f;

        float normalizedDistance = distance / maxDistance;

        return normalizedDistance * maxVolume;
    }
}
