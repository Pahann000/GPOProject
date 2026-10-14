/// <summary>
/// Реализуют системы, которые умеют отдавать свои данные для сохранения
/// и восстанавливаться из них.
/// </summary>
/// <remarks>Перенос из ветки features/13_save_system (коммит 30816d2).</remarks>
/// <typeparam name="T">Тип сериализуемого контейнера данных.</typeparam>
public interface IDataProvider<T> where T : class
{
    /// <summary>Возвращает текущее состояние системы для записи в сохранение.</summary>
    T GetSaveData();

    /// <summary>Восстанавливает состояние системы из загруженных данных.</summary>
    void RestoreFromSaveData(T data);
}
