using Chaos.IO.FileSystem;
using Chaos.Services.Storage.Abstractions;
using Chaos.Storage.Abstractions;

namespace Chaos.Services.Storage.Options;

/// <summary>
///     Has backup support (unlike <see cref="AscensionFloorStoreOptions" />) - unlike a floor's first-clearer
///     record, a house ownership record represents a purchased asset (and, transitively via
///     <see cref="Models.World.Aisling.HouseStorage" />, whatever items a player has stored in their house), so
///     losing it isn't "easily regenerated."
/// </summary>
public sealed class HouseOwnershipStoreOptions : IDirectoryBackupOptions, IPeriodicSaveStoreOptions
{
    /// <inheritdoc />
    public string BackupDirectory { get; set; } = null!;

    /// <inheritdoc />
    public int BackupIntervalMins { get; set; }

    /// <inheritdoc />
    public int BackupRetentionDays { get; set; }

    public string Directory { get; set; } = null!;
    public int SaveIntervalMins { get; set; }

    /// <summary>
    ///     How many days of non-payment before rent is considered overdue. Checked on-demand at the door/key entry
    ///     point (see <see cref="Utilities.HouseAccessHelper" />), not via a background sweep.
    /// </summary>
    public int RentIntervalDays { get; set; } = 7;

    /// <inheritdoc />
    public void UseBaseDirectory(string baseDirectory)
    {
        Directory = Path.Combine(baseDirectory, Directory);
        BackupDirectory = Path.Combine(baseDirectory, BackupDirectory);

        if (PathEx.IsSubPathOf(BackupDirectory, Directory))
            throw new InvalidOperationException($"{nameof(BackupDirectory)} cannot be a subdirectory of {nameof(Directory)}");
    }
}
