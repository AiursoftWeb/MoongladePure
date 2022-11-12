using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.ImageStorage.Providers;

namespace Moonglade.ImageStorage;

public class ImageStorageOptions
{
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
}

public static class ServiceCollectionExtensions
{
    private static readonly ImageStorageOptions Options = new();

    public static IServiceCollection AddImageStorage(
        this IServiceCollection services, IConfiguration configuration, Action<ImageStorageOptions> options)
    {
        options(Options);

        var section = configuration.GetSection(nameof(ImageStorage));
        var settings = section.Get<ImageStorageSettings>();
        services.Configure<ImageStorageSettings>(section);

        if (string.IsNullOrWhiteSpace(settings.FileSystemPath))
        {
            throw new ArgumentNullException(nameof(settings.FileSystemPath), "FileSystemPath can not be null or empty.");
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