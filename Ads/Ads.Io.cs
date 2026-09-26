using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace Ads;

public partial class Ads
{
  private readonly object _queueLock = new();
  private Task _queue = Task.CompletedTask;
  private int _loadVersion;
  private volatile bool _unloaded;

  private static bool IsRemote(IAdsStorage storage) => storage is AdsMySqlStorage;

  private void Background(Action work)
  {
    lock (_queueLock)
    {
      _queue = _queue.ContinueWith(_ =>
      {
        try
        {
          work();
        }
        catch (Exception ex)
        {
          Logger.LogError("Arka plan islemi basarisiz: {message}", ex.Message);
        }
      }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }
  }

  private void Main(Action work)
  {
    Server.NextFrame(() =>
    {
      if (!_unloaded)
        work();
    });
  }

  private void Store(Action<IAdsStorage> write, CCSPlayerController? player)
  {
    var storage = _storage;

    if (!IsRemote(storage))
    {
      try
      {
        write(storage);
      }
      catch (Exception ex)
      {
        Reply(player, Localizer["ads.save_error", ex.Message]);
      }

      return;
    }

    int userId = Util.UserId(player);

    Background(() =>
    {
      try
      {
        write(storage);
      }
      catch (Exception ex)
      {
        Main(() =>
        {
          Logger.LogError("Kayit basarisiz: {message}", ex.Message);
          var target = Util.FromUserId(userId);
          if (target != null)
            Reply(target, Localizer["ads.save_error", ex.Message]);
        });
      }
    });
  }

  private void Fetch<T>(Func<IAdsStorage, T> read, Action<T> apply, Action<Exception> fail)
  {
    var storage = _storage;

    if (!IsRemote(storage))
    {
      T value;
      try
      {
        value = read(storage);
      }
      catch (Exception ex)
      {
        fail(ex);
        return;
      }

      apply(value);
      return;
    }

    Background(() =>
    {
      T value;
      try
      {
        value = read(storage);
      }
      catch (Exception ex)
      {
        Main(() => fail(ex));
        return;
      }

      Main(() => apply(value));
    });
  }

  private void SaveMaps(CCSPlayerController? player)
  {
    var snapshot = new MapsData { Props = _mapsData.Props.Select(p => p.Clone()).ToList() };
    Store(storage => storage.SaveMaps(snapshot), player);
  }
}
