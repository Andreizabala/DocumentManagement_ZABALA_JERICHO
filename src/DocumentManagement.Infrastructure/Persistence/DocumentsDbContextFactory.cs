using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DocumentManagement.Infrastructure.Persistence;

public sealed class DocumentsDbContextFactory : IDesignTimeDbContextFactory<DocumentsDbContext>
{
    public DocumentsDbContext CreateDbContext(string[] args)
    {
        var apiPath = FindApiPath();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("DocumentsDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'DocumentsDatabase' is required for migrations.");

        var options = new DbContextOptionsBuilder<DocumentsDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0)))
            .Options;
        return new DocumentsDbContext(options);
    }

    private static string FindApiPath()
    {
        var currentPath = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(currentPath, "src", "DocumentManagement.Api"),
            Path.Combine(currentPath, "DocumentManagement.Api"),
            Path.GetFullPath(Path.Combine(currentPath, "..", "DocumentManagement.Api"))
        };

        var apiPath = candidates.FirstOrDefault(candidate =>
            File.Exists(Path.Combine(candidate, "appsettings.json")));
        return apiPath ?? throw new DirectoryNotFoundException(
            "Could not locate DocumentManagement.Api/appsettings.json. Run the EF command from the repository root, src, or Infrastructure project directory.");
    }
}
