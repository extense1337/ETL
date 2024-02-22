namespace ETL.Domain;

/// <summary>
/// Репозиторий учебных заведений
/// </summary>
public interface IUniversityRepository
{
    /// <summary>
    /// Получить список учебных заведений
    /// </summary>
    /// <param name="filter">Фильтр</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns></returns>
    Task<IEnumerable<UniversityModel>> GetAsync(UniversityFilter? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавить список учебных заведений
    /// </summary>
    /// <param name="universities">Перечисление учебных заведений</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task PutAsync(IEnumerable<UniversityModel> universities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохранить изменения
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Число добавленых записей</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}