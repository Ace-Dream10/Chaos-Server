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
///     A store for <see cref="AscensionFloorState" />, one JSON file per floor number. Modeled on
///     <see cref="GuildStore" />/<see cref="MailStore" />, but simpler - <see cref="AscensionFloorState" /> is a flat
///     POCO with no domain logic, so it's saved/loaded directly rather than through a separate schema/mapping type.
/// </summary>
public class AscensionFloorStore : PeriodicSaveStoreBase<AscensionFloorState, AscensionFloorStoreOptions>
{
    /// <inheritdoc />
    public AscensionFloorStore(
        IEntityRepository entityRepository,
        IOptions<AscensionFloorStoreOptions> options,
        ILogger<AscensionFloorStore> logger)
        : base(entityRepository, options, logger) { }

    /// <inheritdoc />
    protected override AscensionFloorState LoadFromFile(string dir, string key)
    {
        Logger.WithTopics(Topics.Entities.AscensionFloor, Topics.Actions.Load)
              .LogDebug("Loading new {@TypeName} entry with key {@Key}", nameof(AscensionFloorState), key);

        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"Directory {dir} does not exist");

        var path = Path.Combine(dir, "floor.json");

        return EntityRepository.Load<AscensionFloorState>(path);
    }

    /// <inheritdoc />
    public override void Save(AscensionFloorState obj)
    {
        Logger.WithTopics(Topics.Entities.AscensionFloor, Topics.Actions.Save)
              .WithProperty(obj)
              .LogDebug("Saving {@TypeName} entry with key {@Key}", nameof(AscensionFloorState), obj.FloorNumber);

        try
        {
            var directory = Path.Combine(Options.Directory, obj.FloorNumber.ToString());

            directory.SafeExecute(dir =>
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var path = Path.Combine(dir, "floor.json");

                EntityRepository.Save(obj, path);
                Directory.SetLastWriteTimeUtc(directory, DateTime.UtcNow);
            });
        } catch (Exception e)
        {
            Logger.WithTopics(Topics.Entities.AscensionFloor, Topics.Actions.Save)
                  .WithProperty(obj)
                  .LogError(e, "Failed to save {@TypeName} entry with key {@Key}", nameof(AscensionFloorState), obj.FloorNumber);
        }
    }

    /// <inheritdoc />
    public override async Task SaveAsync(AscensionFloorState obj)
    {
        try
        {
            var directory = Path.Combine(Options.Directory, obj.FloorNumber.ToString());

            await directory.SafeExecuteAsync(async dir =>
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var path = Path.Combine(dir, "floor.json");

                await EntityRepository.SaveAsync(obj, path);
                Directory.SetLastWriteTimeUtc(directory, DateTime.UtcNow);
            });
        } catch (Exception e)
        {
            Logger.WithTopics(Topics.Entities.AscensionFloor, Topics.Actions.Save)
                  .WithProperty(obj)
                  .LogError(e, "Failed to save {@TypeName} entry with key {@Key}", nameof(AscensionFloorState), obj.FloorNumber);
        }
    }
}
