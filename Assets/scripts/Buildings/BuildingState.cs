/// <summary>
/// Состояние постройки.
/// </summary>
public enum BuildingState
{
    Planned,    // Только размечено для постройки
    Constructing, // В процессе постройки
    Operational, // Полностью функционирует
    Damaged,     // Повреждено
    Destroyed    // Полностью уничтожено
}