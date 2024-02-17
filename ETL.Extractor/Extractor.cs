using ETL.Domain;
using Microsoft.Extensions.Logging;

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

public class Extractor : IExtractor
{
    private readonly ILogger<Extractor> _logger;
    private readonly IUniversityRepository _universityRepository;

    public Extractor(ILogger<Extractor> logger, IUniversityRepository universityRepository, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _universityRepository = universityRepository;
    }

    /// <inheritdoc />
    public Task DoExtract(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}