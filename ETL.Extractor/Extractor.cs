using System.Web;
using ETL.Domain;
using ETL.Extractor.ServiceCollection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ETL.Extractor;

/// <summary>
///
/// </summary>
public interface IExtractor
{
    /// <summary>
    /// Выполнить загрузку данных
    /// </summary>
    /// <param name="countries">Список стран</param>
    /// <param name="threadLimit">Кол-во потоков при п/араллельной загрузке</param>
    /// <param name="universityRepository">Репозиторий учебных заведений</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DoExtract(List<string> countries, int threadLimit, IUniversityRepository universityRepository, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class Extractor : IExtractor
{
    private readonly ILogger<Extractor> _logger;
    private readonly HttpClient _httpClient;
    private readonly ExternalApiConfig _apiConfig;

    public Extractor(
        ILogger<Extractor> logger,
        HttpClient httpClient,
        ExternalApiConfig apiConfig)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiConfig = apiConfig;

        if (_apiConfig.Host != null && !_apiConfig.Host.EndsWith('/'))
            _apiConfig.Host += "/";
    }

    /// <inheritdoc />
    public async Task DoExtract(List<string> countries, int threadLimit, IUniversityRepository universityRepository, CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(threadLimit);

            foreach (var country in countries)
            {
                await semaphore.WaitAsync(cancellationToken);

                tasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            var entries = await GetUniversities(country, cancellationToken);
                            if (entries is not { Count: > 0 }) return;
                            var universities = entries.Select(entry => new UniversityModel
                            {
                                CountryName = entry.Country,
                                Name = entry.Name,
                                Sites = entry.WebPages?.ToList()
                            });

                            await universityRepository.PutAsync(universities, cancellationToken);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken));
            }

            await Task.WhenAll(tasks);
            var added = await universityRepository.SaveChangesAsync(cancellationToken);
            _logger.Log(LogLevel.Information, "Добавлено организаций - {Added}", added);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex, "Ошибка при выгрузке данных");
        }
    }

    private async Task<List<UniversityEntry>?> GetUniversities(string country, CancellationToken cancellationToken)
    {
        var universities = new List<UniversityEntry>();

        try
        {
            if (_apiConfig.Host is null || _apiConfig.SearchRoute is null)
                throw new ArgumentException("Неверно заполнен ExternalApi в конфигурационном файле!");

            var url = _apiConfig.Host + _apiConfig.SearchRoute + $"?country={HttpUtility.UrlEncode(country)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            universities = JsonConvert.DeserializeObject<List<UniversityEntry>>(json);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex, "Ошибка при попытке получения данных");
        }

        return universities;
    }
}