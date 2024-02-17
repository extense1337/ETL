using ETL.Domain;
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
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DoExtract(CancellationToken cancellationToken = default);
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
    public async Task DoExtract(CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await GetExternalData(cancellationToken);

            if (data is null)
            {
                _logger.Log(LogLevel.Information, "Не удалось найти данные");
                return;
            }

            var universities = data.Select(entry => new UniversityModel
            {
                CountryName = entry.Country,
                Name = entry.Name,
                Sites = entry.WebPages.ToList()
            });

            await _universityRepository.PutAsync(universities, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex, "Ошибка при выгрузке данных");
        }
    }

    private async Task<IEnumerable<UniversityEntry>?> GetExternalData(CancellationToken cancellationToken)
    {
        IEnumerable<UniversityEntry>? result = null;

        try
        {
            if (_apiConfig.Host is null || _apiConfig.SearchRoute is null)
                throw new ArgumentException("Неверно заполнен ExternalApi в конфигурационном файле!");

            var response = await _httpClient.GetAsync(_apiConfig.Host + _apiConfig.SearchRoute, cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            result = JsonConvert.DeserializeObject<IEnumerable<UniversityEntry>>(json);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex, "Ошибка при попытка получения данных");
        }

        return result;
    }
}