using ETL.DataAccess.PostgreSQL;
using ETL.Domain;
using ETL.Extractor;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<UniversityDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("NpgUniversitiesDbConnection"));
});
builder.Services.AddKeyedScoped<IUniversityRepository, UniversityRepository>("pg_repo");

var externalApiConfig = new ExternalApiConfig
{
    Host = builder.Configuration.GetValue<string>("ExternalApi:Host"),
    SearchRoute = builder.Configuration.GetValue<string>("ExternalApi:SearchRoute")
};
builder.Services.AddHttpClient<IExtractor, Extractor>(client =>
{
    if (externalApiConfig.Host != null)
        client.BaseAddress = new Uri(externalApiConfig.Host);
});
builder.Services.AddSingleton(externalApiConfig);
builder.Services.AddScoped<IExtractor, Extractor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/universities", async (
        [FromKeyedServices("pg_repo")] IUniversityRepository universityRepository,
        string? countryName,
        string? name,
        CancellationToken cancellationToken) =>
    {
        var filter = new UniversityFilter
        {
            CountryName = countryName,
            Name = name
        };
        return await universityRepository.GetAsync(filter, cancellationToken);
    })
    .WithName("GetUniversities")
    .WithOpenApi();

app.Run();