using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Data;


public class BlogDbContextRepository<T>(BlogDbContext dbContext) : DbContextRepository<T>(dbContext)
    where T : class;
