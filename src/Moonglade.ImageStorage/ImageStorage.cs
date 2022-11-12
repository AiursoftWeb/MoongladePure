using Moonglade.ImageStorage.Providers;

namespace Moonglade.ImageStorage;

public class ImageStorageSettings
{
    public string[] AllowedExtensions { get; set; }

    public string FileSystemPath { get; set; }
}