using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SistLubri.Infrastructure.Persistence;

public sealed class SistLubriDbContextFactory : IDesignTimeDbContextFactory<SistLubriDbContext>
{
    public SistLubriDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SistLubriDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SistLubriDesignTime;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True")
            .Options;

        return new SistLubriDbContext(options);
    }
}
