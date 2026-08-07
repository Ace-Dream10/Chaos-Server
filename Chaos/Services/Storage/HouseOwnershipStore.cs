#region
using Chaos.Collections;
using Chaos.IO.FileSystem;
using Chaos.NLog.Logging.Definitions;
using Chaos.NLog.Logging.Extensions;
using Chaos.Services.Storage.Abstractions;
using Chaos.Services.Storage.Options;
using Chaos.Storage.Abstractions;
using Microsoft.Extensions.Options;
#endregion

namespace Chaos.Services.Storage;

/// <summary>
///     A store for <see cref="HouseOwnership" />, one JSON file per owner name. Modeled directly on
///     <see cref="AscensionFloorStore" /> - <see cref="HouseOwnership" /> is a flat POCO with no domain logic, so
///     it's saved/loaded directly rather than through a separate schema/mapping type.
/// </summary>
public class HouseOwnershipStore : PeriodicSaveStoreBase<HouseOwnership, HouseOwnershipStoreOptions>
{
    /// <inheritdoc />
    public HouseOwnershipStore(
        IEntityRepository entityRepository,
        IOptions<HouseOwnershipStoreOptions> options,
        ILogger<HouseOwnershipStore> logger)
        : base(entityRepository, options, logger) { }

    /// <inheritdoc />
    protected override HouseOwnership LoadFromFile(string dir, string key)
    {
        Logger.WithTopics(Topics.Entities.House, Topics.Actions.Load)
              .LogDebug("Loading new {@TypeName} entry with key {@Key}", nameof(HouseOwnership), key);

        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"Directory {dir} does not exist");

        var path = Path.Combine(dir, "house.json");

        return EntityRepository.Load<HouseOwnership>(path);
    }

    /// <inheritdoc />
    public override void Save(HouseOwnership obj)
    {
        Logger.WithTopics(Topics.Entities.House, Topics.Actions.Save)
              .WithProperty(obj)
              .LogDebug("Saving {@TypeName} entry with key {@Key}", nameof(HouseOwnership), obj.Owner);

        try
        {
            var directory = Path.Combine(Options.Directory, obj.Owner.ToLower());

            directory.SafeExecute(dir =>
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var path = Path.Combine(dir, "house.json");

                EntityRepository.Save(obj, path);
                Directory.SetLastWriteTimeUtc(directory, DateTime.UtcNow);
            });
        } catch (Exception e)
        {
            Logger.WithTopics(Topics.Entities.House, Topics.Actions.Save)
                  .WithProperty(obj)
                  .LogError(e, "Failed to save {@TypeName} entry with key {@Key}", nameof(HouseOwnership), obj.Owner);
        }
    }

    /// <inheritdoc />
    public override async Task SaveAsync(HouseOwnership obj)
    {
        try
        {
            var directory = Path.Combine(Options.Directory, obj.Owner.ToLower());

            await directory.SafeExecuteAsync(async dir =>
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var path = Path.Combine(dir, "house.json");

                await EntityRepository.SaveAsync(obj, path);
                Directory.SetLastWriteTimeUtc(directory, DateTime.UtcNow);
            });
        } catch (Exception e)
        {
            Logger.WithTopics(Topics.Entities.House, Topics.Actions.Save)
                  .WithProperty(obj)
                  .LogError(e, "Failed to save {@TypeName} entry with key {@Key}", nameof(HouseOwnership), obj.Owner);
        }
    }
}
