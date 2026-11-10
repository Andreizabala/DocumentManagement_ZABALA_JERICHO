using DocumentManagement.Application.Interfaces;
using DocumentManagement.Infrastructure.Persistence;
using DocumentManagement.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DocumentsDatabase")
            ?? throw new InvalidOperationException("Connection string 'DocumentsDatabase' is required.");
        var serverVersion = new MySqlServerVersion(new Version(8, 0));
        services.AddDbContext<DocumentsDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        return services;
    }
}

