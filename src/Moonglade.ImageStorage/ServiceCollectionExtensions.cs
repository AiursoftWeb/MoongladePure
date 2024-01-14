using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoongladePure.ImageStorage.Providers;

namespace MoongladePure.ImageStorage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImageStorage(
        this IServiceCollection services, 
        IConfigurationSection section,
        bool isTest)
    {
        var settings = section.Get<ImageStorageSettings>();
        services.Configure<ImageStorageSettings>(section);

        if (string.IsNullOrWhiteSpace(settings.Path))
        {
            throw new ArgumentNullException(nameof(settings.Path), "Path can not be null or empty.");
        }

        if (isTest)
        {
            settings.Path = Path.GetTempPath();
        }

        services.AddFileSystemStorage(settings.Path);

        return services;
    }

    private static void AddFileSystemStorage(this IServiceCollection services, string fileSystemPath)
    {
        var fullPath = FileSystemImageStorage.ResolveImageStoragePath(fileSystemPath);
        services.AddSingleton(_ => new FileSystemImageConfiguration(fullPath))
                .AddSingleton<IBlogImageStorage, FileSystemImageStorage>()
                .AddScoped<IFileNameGenerator>(_ => new GuidFileNameGenerator(Guid.NewGuid()));
    }
}