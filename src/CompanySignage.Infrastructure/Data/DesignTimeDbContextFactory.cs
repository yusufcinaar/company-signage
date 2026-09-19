using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CompanySignage.Infrastructure.Data;

/// <summary>
/// EF Core migration komutlarının (dotnet ef migrations add vb.) çalışabilmesi için
/// tasarım zamanında DbContext oluşturan factory.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SignageDbContext>
{
    public SignageDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SignageDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\MSSQLLocalDB;Database=CompanySignagePublic;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");
        return new SignageDbContext(optionsBuilder.Options);
    }
}
