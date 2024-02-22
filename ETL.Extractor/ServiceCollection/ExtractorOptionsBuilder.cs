namespace ETL.Extractor.ServiceCollection;

public class ExtractorOptionsBuilder
{
    public ExtractorOptions Options { get; }

    public ExtractorOptionsBuilder()
    {
        Options = new ExtractorOptions();
    }

    public ExtractorOptionsBuilder(ExtractorOptions options)
    {
        Options = options;
    }
}

public class ExtractorOptions
{
    public ExternalApiConfig? Config { get; set; }
}