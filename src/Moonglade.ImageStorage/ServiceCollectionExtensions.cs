using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoongladePure.ImageStorage.Providers;

namespace MoongladePure.ImageStorage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImageStorage(
        this IServiceCollection services, 
        IConfiguration configuration,
        bool isTest)
    {
        var section = configuration.GetSection(nameof(ImageStorage));
        var settings = section.Get<ImageStorageSettings>();
        services.Configure<ImageStorageSettings>(section);

        if (string.IsNullOrWhiteSpace(settings.FileSystemPath))
        {
            throw new ArgumentNullException(nameof(settings.FileSystemPath), "FileSystemPath can not be null or empty.");
        }

        if (isTest)
        {
            settings.FileSystemPath = Path.GetTempPath();
        }

        services.AddFileSystemStorage(settings.FileSystemPath);

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