using Microsoft.Extensions.DependencyInjection;

namespace ETL.Extractor.ServiceCollection;

public static class ExtractorServiceCollectionExtensions
{
    public static IServiceCollection AddExtractor(this IServiceCollection serviceCollection,
        Action<ExtractorOptionsBuilder>? optionsAction = null)
    {
        var options = CreateExtractorOptions(optionsAction);
        var config = options.Config ?? new ExternalApiConfig();
        serviceCollection.AddHttpClient<IExtractor, Extractor>(client =>
        {
            if (config.Host != null)
                client.BaseAddress = new Uri(config.Host);
        });
        serviceCollection.AddSingleton(config);
        serviceCollection.AddScoped<IExtractor, Extractor>();

        return serviceCollection;
    }

    private static ExtractorOptions CreateExtractorOptions(Action<ExtractorOptionsBuilder>? optionsAction)
    {
        var builder = new ExtractorOptionsBuilder(new ExtractorOptions());

        optionsAction?.Invoke(builder);

        return builder.Options;
    }
}