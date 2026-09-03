using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace CommandMaker;

public readonly struct ActionContext
{
  public readonly CommandMaker Plugin;
  public readonly CCSPlayerController? Player;
  public readonly CCSPlayerController Target;
  public readonly string Value;

  public ActionContext(CommandMaker plugin, CCSPlayerController? player, CCSPlayerController target, string value)
  {
    Plugin = plugin;
    Player = player;
    Target = target;
    Value = value;
  }

  public CBasePlayerPawn? Pawn
  {
    get
    {
      var pawn = Target.PlayerPawn.Value;
      return pawn != null && pawn.IsValid ? pawn : null;
    }
  }

  public CCSPlayerPawn? CsPawn => Pawn as CCSPlayerPawn;

  public CBasePlayerWeapon? ActiveWeapon => CsPawn?.WeaponServices?.ActiveWeapon?.Value as CBasePlayerWeapon;

  public bool Int(out int value) =>
    int.TryParse(Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

  public bool Float(out float value) =>
    float.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

  public bool Bool() => Value.Equals("true", StringComparison.OrdinalIgnoreCase) || Value == "1";

  public bool Triple(out int a, out int b, out int c)
  {
    a = b = c = 0;
    var parts = Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    return parts.Length >= 3
        && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out a)
        && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out b)
        && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out c);
  }

  public bool Triple(out float a, out float b, out float c)
  {
    a = b = c = 0f;
    var parts = Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    return parts.Length >= 3
        && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out a)
        && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out b)
        && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out c);
  }
}

public partial class CommandMaker
{
  private static readonly string[] TargetPrefixes = { "[TARGET]", "[PLAYER/TARGET]", "[PLAYER]" };

  private static readonly (Func<CommandDefinition, string?> Get, Action<ActionContext> Run)[] ActionTable =
  {
    (c => c.SetHealth, ctx =>
    {
      if (ctx.Pawn is not { } pawn || !ctx.Int(out int health))
        return;

      pawn.Health = health;
      Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }),

    (c => c.SetFreeze, ctx =>
    {
      if (ctx.Pawn is { } pawn)
        SetMoveTypeHelper(pawn, ctx.Bool() ? MoveType_t.MOVETYPE_INVALID : MoveType_t.MOVETYPE_WALK);
    }),

    (c => c.GiveWeapon, ctx =>
    {
      var weaponName = ctx.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
      if (string.IsNullOrEmpty(weaponName))
        return;

      if (!weaponName.StartsWith("weapon_") && !weaponName.StartsWith("item_"))
        weaponName = "weapon_" + weaponName;

      ctx.Target.GiveNamedItem(weaponName);
    }),

    (c => c.SetNoclip, ctx =>
    {
      if (ctx.Pawn is not { } pawn)
        return;

      SetMoveTypeHelper(pawn, pawn.MoveType == MoveType_t.MOVETYPE_NOCLIP
        ? MoveType_t.MOVETYPE_WALK
        : MoveType_t.MOVETYPE_NOCLIP);
    }),

    (c => c.Kill, ctx =>
    {
      ctx.Pawn?.CommitSuicide(false, true);
    }),

    (c => c.SetName, ctx =>
    {
      var name = SanitizePlayerName(ctx.Value);
      if (name.Length == 0)
        return;

      ctx.Target.PlayerName = name;
      Utilities.SetStateChanged(ctx.Target, "CBasePlayerController", "m_iszPlayerName");
    }),

    (c => c.SetArmor, ctx =>
    {
      if (ctx.CsPawn is not { } pawn || !ctx.Int(out int armor))
        return;

      pawn.ArmorValue = armor;
      Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
    }),

    (c => c.SetMaxHealth, ctx =>
    {
      if (ctx.Pawn is not { } pawn || !ctx.Int(out int maxHealth))
        return;

      pawn.MaxHealth = maxHealth;
      Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
    }),

    (c => c.SetClip, ctx =>
    {
      if (ctx.ActiveWeapon is not { } weapon || !ctx.Int(out int clip))
        return;

      weapon.Clip1 = clip;
      Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
    }),

    (c => c.SetAmmo, ctx =>
    {
      if (ctx.ActiveWeapon is not { } weapon || !ctx.Int(out int ammo))
        return;

      weapon.ReserveAmmo[0] = ammo;
      Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
    }),

    (c => c.Teleport, ctx =>
    {
      if (ctx.CsPawn is { } pawn && ctx.Triple(out float x, out float y, out float z))
        pawn.Teleport(new Vector(x, y, z), null, new Vector(0f, 0f, 0f));
    }),

    (c => c.SetAngle, ctx =>
    {
      if (ctx.CsPawn is { } pawn && ctx.Triple(out float pitch, out float yaw, out float _))
        pawn.Teleport(null, new QAngle(pitch, yaw, 0f), null);
    }),

    (c => c.SetPlayerColor, ctx =>
    {
      if (ctx.Pawn is not { } pawn || !ctx.Triple(out int r, out int g, out int b))
        return;

      pawn.Render = System.Drawing.Color.FromArgb(255, r, g, b);
      Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }),

    (c => c.SlapDamage, ctx =>
    {
      if (ctx.Pawn is { } pawn && ctx.Int(out int damage))
        PerformSlap(pawn, damage);
    }),

    (c => c.SetMoney, ctx =>
    {
      if (ctx.Int(out int money))
        SetMoney(ctx.Target, money);
    }),

    (c => c.AddHealth, ctx =>
    {
      if (ctx.Pawn is not { } pawn || !ctx.Int(out int delta))
        return;

      pawn.Health = Math.Max(0, pawn.Health + delta);
      Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

      if (pawn.Health <= 0)
        pawn.CommitSuicide(true, true);
    }),

    (c => c.AddArmor, ctx =>
    {
      if (ctx.CsPawn is not { } pawn || !ctx.Int(out int delta))
        return;

      pawn.ArmorValue = Math.Max(0, pawn.ArmorValue + delta);
      Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
    }),

    (c => c.AddMoney, ctx =>
    {
      if (!ctx.Int(out int delta))
        return;

      int current = ctx.Target.InGameMoneyServices?.Account ?? 0;
      SetMoney(ctx.Target, Math.Max(0, current + delta));
    }),

    (c => c.ChangeTeam, ctx =>
    {
      if (ctx.Int(out int team) && team >= 0 && team <= 3)
        ctx.Target.ChangeTeam((CsTeam)team);
    }),

    (c => c.SetClanTag, ctx =>
    {
      ctx.Target.Clan = SanitizePlayerName(ctx.Value);
      Utilities.SetStateChanged(ctx.Target, "CCSPlayerController", "m_szClan");
      new EventNextlevelChanged(false).FireEvent(false);
    }),

    (c => c.SetSpeed, ctx =>
    {
      if (!ctx.Float(out float speed))
        return;

      var state = ctx.Plugin.State(Util.UserId(ctx.Target));
      state.Speed = Math.Clamp(speed, 0f, 10f);
      state.HasSpeed = true;
    }),

    (c => c.SetGravity, ctx =>
    {
      if (!ctx.Float(out float gravity))
        return;

      var state = ctx.Plugin.State(Util.UserId(ctx.Target));
      state.Gravity = Math.Clamp(gravity, 0f, 10f);
      state.HasGravity = true;
    }),

    (c => c.Respawn, ctx =>
    {
      if (ctx.Pawn is { } pawn && pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        ctx.Target.Respawn();
    }),

    (c => c.SetHelmet, ctx =>
    {
      if (ctx.Value.Length == 0 || ctx.Bool())
      {
        ctx.Target.GiveNamedItem("item_assaultsuit");
        return;
      }

      var itemServices = ctx.CsPawn?.ItemServices;
      if (itemServices == null)
        return;

      Schema.SetSchemaValue(itemServices.Handle, "CCSPlayer_ItemServices", "m_bHasHelmet", false);
      Utilities.SetStateChanged(ctx.CsPawn!, "CCSPlayerPawn", "m_pItemServices");
    }),

    (c => c.SetGodmode, ctx =>
    {
      int userId = Util.UserId(ctx.Target);
      if (userId < 0)
        return;

      ctx.Plugin.SetGodmode(userId, ctx.Bool());
    }),

    (c => c.SetMoveType, ctx =>
    {
      if (ctx.Pawn is { } pawn && ctx.Int(out int moveType))
        SetMoveTypeHelper(pawn, (MoveType_t)moveType);
    }),

    (c => c.StripWeapons, ctx =>
    {
      ctx.Target.RemoveWeapons();
    }),

    (c => c.DropWeapon, ctx =>
    {
      if (ctx.ActiveWeapon != null && ctx.CsPawn?.ItemServices != null)
        ctx.Target.DropActiveWeapon();
    }),

    (c => c.SetModel, ctx =>
    {
      if (ctx.Pawn is not { } pawn || ctx.Value.Length == 0)
        return;

      string model = ctx.Value;

      Server.NextFrame(() =>
      {
        if (pawn.IsValid)
          pawn.SetModel(model);
      });
    }),

    (c => c.ScreenColor, ctx =>
    {
      var parts = ctx.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length < 3)
        return;

      var rgb = Util.ParseColor(string.Join(' ', parts[..3]), System.Drawing.Color.White);
      int alpha = parts.Length > 3 && int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int a)
        ? Math.Clamp(a, 0, 255)
        : 90;
      float fade = parts.Length > 4 && float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 0.35f;
      float hold = parts.Length > 5 && float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float h) ? h : 0.05f;

      ScreenFade.Apply(ctx.Target, System.Drawing.Color.FromArgb(alpha, rgb.R, rgb.G, rgb.B), fade, hold);
    }),

    (c => c.PlaySound, ctx =>
    {
      var sound = SanitizeCommandValue(ctx.Value);
      if (sound.Length > 0)
        ctx.Target.ExecuteClientCommand($"play {sound}");
    }),

    (c => c.EmitSound, ctx =>
    {
      var parts = ctx.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0)
        return;

      float volume = parts.Length > 1 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 1f;
      SoundUtil.EmitToPlayer(ctx.Target, parts[0], volume);
    })
  };

  private static string ActionValue(string raw)
  {
    var value = raw.TrimStart();

    foreach (var prefix in TargetPrefixes)
    {
      if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        return value[prefix.Length..].Trim();
    }

    return value.Trim();
  }

  private void CompileActions(CommandDefinition cmd)
  {
    cmd.Compiled.Clear();

    foreach (var (get, run) in ActionTable)
    {
      var raw = get(cmd);
      if (string.IsNullOrEmpty(raw))
        continue;

      string value = ActionValue(raw);
      cmd.Compiled.Add(new CompiledAction(value, run));
      PrecompileTemplate(value);
    }
  }

  private void ApplyCommandActions(CCSPlayerController? player, CCSPlayerController target, CommandDefinition cmd, string? arg1, string? arg2, string? arg3)
  {
    foreach (var action in cmd.Compiled)
    {
      string value = FormatMessage(action.Value, player, target, arg1, arg2, arg3);
      action.Run(new ActionContext(this, player, target, value));
    }
  }

  private static void PerformSlap(CBasePlayerPawn pawn, int damage = 0)
  {
    if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
      return;

    var vel = new Vector(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z);

    vel.X += (Random.Shared.Next(180) + 50) * (Random.Shared.Next(2) == 1 ? -1 : 1);
    vel.Y += (Random.Shared.Next(180) + 50) * (Random.Shared.Next(2) == 1 ? -1 : 1);
    vel.Z += Random.Shared.Next(200) + 100;

    pawn.AbsVelocity.X = vel.X;
    pawn.AbsVelocity.Y = vel.Y;
    pawn.AbsVelocity.Z = vel.Z;

    if (damage <= 0)
      return;

    pawn.Health -= damage;
    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

    if (pawn.Health <= 0)
      pawn.CommitSuicide(true, true);
  }

  private static void SetMoney(CCSPlayerController? controller, int money)
  {
    var moneyServices = controller?.InGameMoneyServices;
    if (moneyServices == null || controller == null)
      return;

    moneyServices.Account = money;
    Utilities.SetStateChanged(controller, "CCSPlayerController", "m_pInGameMoneyServices");
  }

  private static void SetMoveTypeHelper(CBasePlayerPawn pawn, MoveType_t moveType)
  {
    pawn.MoveType = moveType;
    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
    Schema.GetRef<MoveType_t>(pawn.Handle, "CBaseEntity", "m_nActualMoveType") = moveType;
  }
}
