using ETL.Domain;
using ETL.Extractor.ServiceCollection;
using Microsoft.Extensions.DependencyInjection;
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
    /// <param name="threadLimit">Кол-во потоков при параллельной загрузке</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DoExtract(List<string> countries, int threadLimit, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class Extractor : IExtractor
{
    private readonly ILogger<Extractor> _logger;
    private readonly IUniversityRepository _universityRepository;
    private readonly HttpClient _httpClient;
    private readonly ExternalApiConfig _apiConfig;

    public Extractor(
        ILogger<Extractor> logger,
        [FromKeyedServices("pg_repo")] IUniversityRepository universityRepository,
        HttpClient httpClient,
        ExternalApiConfig apiConfig)
    {
        _logger = logger;
        _universityRepository = universityRepository;
        _httpClient = httpClient;
        _apiConfig = apiConfig;
    }

    /// <inheritdoc />
    public async Task DoExtract(List<string> countries, int threadLimit, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await GetUniversities(countries, threadLimit, cancellationToken);

            if (data is null)
            {
                _logger.Log(LogLevel.Information, "Не удалось найти данные");
                return;
            }

            foreach (var entries in data)
            {
                if (entries == null) continue;
                var universities = entries.Select(entry => new UniversityModel
                {
                    CountryName = entry.Country,
                    Name = entry.Name,
                    Sites = entry.WebPages?.ToList()
                });

                await _universityRepository.PutAsync(universities, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex, "Ошибка при выгрузке данных");
        }
    }

    private async Task<List<IEnumerable<UniversityEntry>?>?> GetUniversities(List<string> countries, int threadLimit, CancellationToken cancellationToken)
    {
        if (_apiConfig.Host is null || _apiConfig.SearchRoute is null)
            throw new ArgumentException("Неверно заполнен ExternalApi в конфигурационном файле!");

        var result = new List<IEnumerable<UniversityEntry>?>();

        try
        {
            var tasks = new List<Task<IEnumerable<UniversityEntry>?>>();
            var semaphore = new SemaphoreSlim(threadLimit);

            foreach (var country in countries)
            {
                await semaphore.WaitAsync(cancellationToken);

                tasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            return await GetUniversities(country, cancellationToken);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken));
            }

            result = (await Task.WhenAll(tasks)).ToList();
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex, "Ошибка при попытка получения данных");
        }

        return result;
    }

    private async Task<IEnumerable<UniversityEntry>?> GetUniversities(string country, CancellationToken cancellationToken)
    {
        var universities = new List<UniversityEntry>();

        try
        {
            if (_apiConfig.Host is null || _apiConfig.SearchRoute is null)
                throw new ArgumentException("Неверно заполнен ExternalApi в конфигурационном файле!");

            var response = await _httpClient.GetAsync(_apiConfig.Host + _apiConfig.SearchRoute + $"/{country}", cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            universities = JsonConvert.DeserializeObject<List<UniversityEntry>>(json);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex, "Ошибка при попытка получения данных");
        }

        return universities;
    }
}