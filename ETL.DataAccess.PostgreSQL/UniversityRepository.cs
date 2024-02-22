using ETL.Domain;
using Microsoft.EntityFrameworkCore;

namespace ETL.DataAccess.PostgreSQL;

public class UniversityRepository : IUniversityRepository
{
    private readonly UniversityDbContext _universityDbContext;

    public UniversityRepository(UniversityDbContext universityDbContext)
    {
        _universityDbContext = universityDbContext;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UniversityModel>> GetAsync(UniversityFilter? filter, CancellationToken cancellationToken = default)
    {
        IQueryable<UniversityEntity> universityEntities;

        if (filter != null)
        {
            universityEntities = _universityDbContext.Universities
                .Where(entity =>
                    (filter.CountryName == null || entity.CountryName == filter.CountryName) &&
                    (filter.Name == null || entity.Name == filter.Name));
        }
        else
        {
            universityEntities = _universityDbContext.Universities.AsQueryable();
        }

        var universities = await universityEntities.Select(entity => new UniversityModel
        {
            CountryName = entity.CountryName,
            Name = entity.Name,
            Sites = entity.Sites.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        }).ToListAsync(cancellationToken).ConfigureAwait(false);

        return universities;
    }

    /// <inheritdoc />
    public async Task PutAsync(IEnumerable<UniversityModel> universities, CancellationToken cancellationToken = default)
    {
        var entities = universities.Select(university => new UniversityEntity
        {
            CountryName = university.CountryName,
            Name = university.Name,
            Sites = university.Sites != null ? string.Join(',', university.Sites) : string.Empty
        });

        await _universityDbContext.Universities.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _universityDbContext.SaveChangesAsync(cancellationToken);
    }
}