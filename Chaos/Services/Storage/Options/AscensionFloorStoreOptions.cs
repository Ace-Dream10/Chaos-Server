using Chaos.Common.Abstractions;
using Chaos.Services.Storage.Abstractions;

namespace Chaos.Services.Storage.Options;

/// <summary>
///     No backup support (unlike <see cref="GuildStoreOptions" />/<see cref="MailStoreOptions" />) - this store's
///     content is small and easily regenerated (worst case, a floor's first-clearer record resets), so the extra
///     <see cref="Chaos.IO.FileSystem.DirectoryBackupService{TOptions}" /> wiring isn't warranted for it.
/// </summary>
public sealed class AscensionFloorStoreOptions : IPeriodicSaveStoreOptions
{
    public string Directory { get; set; } = null!;
    public int SaveIntervalMins { get; set; }

    /// <inheritdoc />
    public void UseBaseDirectory(string baseDirectory) => Directory = Path.Combine(baseDirectory, Directory);
}
