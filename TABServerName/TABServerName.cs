using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using static CounterStrikeSharp.API.Core.Listeners;

namespace TABServerName;

public class TABServerNameConfig : BasePluginConfig
{
  [JsonPropertyName("servername_text")]
  public string ServerNameText { get; set; } = "{MAP} | {HOSTNAME} | github.com/ByDexterTR/CS2Plugins";
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
delegate void CUtlStringSetDelegate(IntPtr thisPtr, string value);

public class TABServerName : BasePlugin, IPluginConfig<TABServerNameConfig>
{
  public override string ModuleName => "TABServerName";
  public override string ModuleVersion => "1.0.0";
  public override string ModuleAuthor => "ByDexter";
  public override string ModuleDescription => "https://github.com/ByDexterTR/CS2Plugins";

  public TABServerNameConfig Config { get; set; } = new();

  private const string GetIGameServerKey = "INetworkServerService_GetIGameServer";
  private const string MapNameOffsetKey = "CNetworkGameServer_MapName";
  private const string UtlStringSetKey = "CUtlString_Set";
  private const string Tier0ModuleWindows = "tier0.dll";
  private const string Tier0ModuleLinux = "libtier0.so";

  [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
  private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

  [DllImport("libdl.so.2")]
  private static extern IntPtr dlopen(string filename, int flags);

  [DllImport("libdl.so.2")]
  private static extern IntPtr dlsym(IntPtr handle, string symbol);

  private const int RTLD_NOW = 2;
  private const int RTLD_NOLOAD = 4;

  private VirtualFunctionWithReturn<IntPtr, IntPtr>? _getIGameServer;
  private CUtlStringSetDelegate? _utlStringSet;
  private int? _mapNameOffset;
  private bool _disabled;

  public void OnConfigParsed(TABServerNameConfig config)
  {
    Config = config;
  }

  public override void Load(bool hotReload)
  {
    RegisterListener<OnMapStart>(OnMapStartHandler);
  }

  private void OnMapStartHandler(string mapName)
  {
    if (_disabled || string.IsNullOrWhiteSpace(Config.ServerNameText))
      return;

    var utlStringSet = GetUtlStringSetDelegate();
    if (utlStringSet == null)
      return;

    IntPtr gameServerPtr = GetGameServerPtr();
    if (gameServerPtr == IntPtr.Zero)
      return;

    int? mapNameOffset = GetMapNameOffset();
    if (mapNameOffset == null)
      return;

    string newValue = Config.ServerNameText
      .Replace("{MAP}", mapName)
      .Replace("{HOSTNAME}", ConVar.Find("hostname")?.StringValue ?? "")
      .Replace("{IP}", ConVar.Find("ip")?.StringValue ?? "")
      .Replace("{PORT}", ConVar.Find("hostport")?.GetPrimitiveValue<int>().ToString() ?? "27015");

    IntPtr fieldAddr = IntPtr.Add(gameServerPtr, mapNameOffset.Value);
    utlStringSet(fieldAddr, newValue);
  }

  private IntPtr GetGameServerPtr()
  {
    IntPtr networkServerServicePtr = ValveInterface.NetworkServerService.Pointer;
    if (networkServerServicePtr == IntPtr.Zero)
    {
      SelfDisable("NetworkServerService_001 arayuzu bulunamadi");
      return IntPtr.Zero;
    }

    if (_getIGameServer == null)
    {
      int offset;
      try
      {
        offset = GameData.GetOffset(GetIGameServerKey);
      }
      catch (Exception ex)
      {
        SelfDisable($"gamedata '{GetIGameServerKey}' okunamadi: {ex.Message}");
        return IntPtr.Zero;
      }

      _getIGameServer = new VirtualFunctionWithReturn<IntPtr, IntPtr>(networkServerServicePtr, offset);
    }

    return _getIGameServer.Invoke(networkServerServicePtr);
  }

  private int? GetMapNameOffset()
  {
    if (_mapNameOffset != null)
      return _mapNameOffset;

    try
    {
      _mapNameOffset = GameData.GetOffset(MapNameOffsetKey);
    }
    catch (Exception ex)
    {
      SelfDisable($"gamedata '{MapNameOffsetKey}' okunamadi: {ex.Message}");
      return null;
    }

    return _mapNameOffset;
  }

  private CUtlStringSetDelegate? GetUtlStringSetDelegate()
  {
    if (_utlStringSet != null)
      return _utlStringSet;

    IntPtr setFnPtr = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
      ? FindTier0ExportLinux()
      : FindTier0ExportWindows();

    if (setFnPtr == IntPtr.Zero)
      return null;

    _utlStringSet = Marshal.GetDelegateForFunctionPointer<CUtlStringSetDelegate>(setFnPtr);
    return _utlStringSet;
  }

  private IntPtr FindTier0ExportWindows()
  {
    ProcessModule? mod = FindModule(Tier0ModuleWindows);
    if (mod == null)
    {
      SelfDisable($"{Tier0ModuleWindows} bulunamadi");
      return IntPtr.Zero;
    }

    string exportName;
    try
    {
      exportName = GameData.GetSignature(UtlStringSetKey);
    }
    catch (Exception ex)
    {
      SelfDisable($"gamedata '{UtlStringSetKey}' (windows) okunamadi: {ex.Message}");
      return IntPtr.Zero;
    }

    IntPtr fn = GetProcAddress(mod.BaseAddress, exportName);
    if (fn == IntPtr.Zero)
      SelfDisable($"{exportName} export'u bulunamadi ({Tier0ModuleWindows} guncellenmis olabilir)");

    return fn;
  }

  private IntPtr FindTier0ExportLinux()
  {
    ProcessModule? mod = FindModule(Tier0ModuleLinux);
    if (mod == null)
    {
      SelfDisable($"{Tier0ModuleLinux} bulunamadi");
      return IntPtr.Zero;
    }

    string exportName;
    try
    {
      exportName = GameData.GetSignature(UtlStringSetKey);
    }
    catch (Exception ex)
    {
      SelfDisable($"gamedata '{UtlStringSetKey}' (linux) okunamadi: {ex.Message}");
      return IntPtr.Zero;
    }

    IntPtr handle = dlopen(mod.FileName, RTLD_NOW | RTLD_NOLOAD);
    if (handle == IntPtr.Zero)
    {
      SelfDisable($"{Tier0ModuleLinux} icin dlopen basarisiz");
      return IntPtr.Zero;
    }

    IntPtr fn = dlsym(handle, exportName);
    if (fn == IntPtr.Zero)
      SelfDisable($"{exportName} sembolu bulunamadi ({Tier0ModuleLinux} guncellenmis olabilir)");

    return fn;
  }

  private static ProcessModule? FindModule(string name)
  {
    foreach (ProcessModule m in Process.GetCurrentProcess().Modules)
    {
      if (m.ModuleName.Equals(name, StringComparison.OrdinalIgnoreCase))
        return m;
    }
    return null;
  }

  private void SelfDisable(string reason)
  {
    _disabled = true;
    Console.WriteLine($"[TABServerName] DEVRE DISI: {reason}");
  }
}
