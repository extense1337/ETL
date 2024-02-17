namespace ETL.Extractor;

public class UniversityEntry
{
    public string AlphaTwoCode { get; set; }

    public IReadOnlyCollection<string> WebPages { get; set; }

    public string Name { get; set; }

    public IReadOnlyCollection<string> Domains { get; set; }

    public string Country { get; set; }

    public string StateProvince { get; set; }
}