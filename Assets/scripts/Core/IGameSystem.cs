using UnityEngine;

/// <summary>
/// Единый контракт для всех подсистем ядра.
/// Позволяет Ядру (GameKernel) централизованно управлять их жизненным циклом и обновлением.
/// </summary>
public interface IGameSystem
{
    /// <summary>
    /// Имя системы, используемое для вывода отладочной информации и логов.
    /// </summary>
    string SystemName { get; }

    /// <summary>
    /// Управляет состоянием системы (Kill Switch).
    /// Если false - метод Tick и FixedTick вызываться не будут.
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    /// Вызывается Ядром при старте игры.
    /// Здесь система должна получать ссылки на другие системы и подписываться на события.
    /// </summary>
    /// <param name="kernel">Ссылка на главное Ядро игры.</param>
    void Initialize(GameKernel kernel);

    /// <summary>
    /// Аналог Update. Вызывается Ядром каждый кадр, если IsActive == true.
    /// </summary>
    /// <param name="deltaTime">Время в секундах с прошлого кадра (Time.deltaTime).</param>
    void Tick(float deltaTime);

    /// <summary>
    /// Аналог FixedUpdate. Вызывается Ядром фиксированое количество раз в секунду.
    /// </summary>
    /// <param name="fixedDeltaTime">Фиксированный шаг времени (Time.fixedDeltaTime).</param>
    void FixedTick(float fixedDeltaTime);

    /// <summary>
    /// Вызывается при уничтожении Ядра или выходе из игры.
    /// Используется для отписки от событий и очистки памяти.
    /// </summary>
    void Shutdown();
}