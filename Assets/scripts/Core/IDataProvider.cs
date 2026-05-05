using UnityEngine;
/// <summary>
/// Интерфейс, который должны реализовывать все системы, предоставляющие данные для сохранения.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDataProvider<T> where T : class
{
    /// <summary>
    /// Возвращает текущее состояние системы в виде сериализуемого объекта
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T GetSaveData();
    /// <summary>
    /// Восстанавливает состояние системы из загруженных данных
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    void RestoreFromSaveData(T data);
}