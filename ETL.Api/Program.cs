using ETL.DataAccess.PostgreSQL;
using ETL.Domain;
using ETL.Extractor;
using ETL.Extractor.ServiceCollection;
using Microsoft.AspNetCore.Mvc;
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
builder.Services.AddExtractor(optionsBuilder =>
{
    optionsBuilder.Options.Config = externalApiConfig;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UniversityDbContext>();
    db.Database.EnsureCreated();
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

if (app.Environment.IsDevelopment())
{
    app.MapPost("/extract", async (
                [FromServices] IExtractor extractor,
                [FromKeyedServices("pg_repo")] IUniversityRepository universityRepository,
                List<string> countries,
                int threadLimit,
                CancellationToken cancellationToken) =>
            await extractor.DoExtract(countries, threadLimit, universityRepository, cancellationToken))
        .WithName("Extract")
        .WithOpenApi();
}

app.Run();