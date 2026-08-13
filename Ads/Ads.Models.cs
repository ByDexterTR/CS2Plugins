using System.Text.Json.Serialization;

namespace Ads;

public static class JsonText
{
  public static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

public class PropAd
{
  [JsonIgnore] public string Map { get; set; } = "*";

  [JsonPropertyName("path")] public string Path { get; set; } = "";
  [JsonPropertyName("pos")] public string Pos { get; set; } = "0 0 0";
  [JsonPropertyName("angle")] public string Angle { get; set; } = "0 0 0";
  [JsonPropertyName("scale")] public float Scale { get; set; } = 1f;
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] [JsonPropertyName("skin")] public int Skin { get; set; }
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] [JsonPropertyName("solid")] public bool Solid { get; set; }
  [JsonPropertyName("flag")] public string? Flag { get; set; }
  [JsonPropertyName("ignoreflag")] public string? IgnoreFlag { get; set; }
}

public class PropModel
{
  [JsonIgnore] public string Name { get; set; } = "";

  [JsonPropertyName("path")] public string Path { get; set; } = "";
  [JsonPropertyName("scale")] public float Scale { get; set; } = 1f;
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] [JsonPropertyName("skin")] public int Skin { get; set; }
  [JsonPropertyName("skins")] public List<int>? Skins { get; set; }
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] [JsonPropertyName("solid")] public bool Solid { get; set; }
  [JsonPropertyName("flag")] public string? Flag { get; set; }
  [JsonPropertyName("ignoreflag")] public string? IgnoreFlag { get; set; }
}

public class PropsData
{
  public List<PropModel> Models { get; set; } = new();

  public Dictionary<string, PropModel> ToJson()
  {
    var result = new Dictionary<string, PropModel>();

    foreach (var model in Models)
    {
      string name = string.IsNullOrWhiteSpace(model.Name) ? model.Path : model.Name;
      result[name] = model;
    }

    return result;
  }

  public static PropsData FromJson(Dictionary<string, PropModel> raw)
  {
    var data = new PropsData();

    foreach (var (name, model) in raw)
    {
      model.Name = name;
      model.Flag = Blank(model.Flag);
      model.IgnoreFlag = Blank(model.IgnoreFlag);
      data.Models.Add(model);
    }

    return data;
  }

  private static string? Blank(string? value) => JsonText.Blank(value);

  public static PropsData Sample() => new()
  {
    Models = new()
    {
      new PropModel { Name = "Tavuk", Path = "models/chicken/chicken.vmdl", Skins = new() { 0 } },
      new PropModel { Name = "Otomat", Path = "models/props/cs_office/vending_machine.vmdl" },
      new PropModel { Name = "Tas heykel", Path = "models/generic/stone_statue_01/stone_statue_01.vmdl" },
      new PropModel { Name = "Sos sisesi (sadece VIP)", Path = "models/de_mirage/food/magixx_sauce_01a/magixx_sauce_bottle_01a.vmdl", Flag = "@css/vip" }
    }
  };
}

public class MapsData
{
  public List<PropAd> Props { get; set; } = new();

  public Dictionary<string, List<PropAd>> ToJson()
  {
    var result = new Dictionary<string, List<PropAd>>();

    foreach (var ad in Props)
    {
      string map = string.IsNullOrWhiteSpace(ad.Map) ? "*" : ad.Map;

      if (!result.TryGetValue(map, out var list))
        result[map] = list = new List<PropAd>();

      list.Add(ad);
    }

    return result;
  }

  public static MapsData FromJson(Dictionary<string, List<PropAd>> raw)
  {
    var data = new MapsData();

    foreach (var (map, list) in raw)
    {
      foreach (var ad in list)
      {
        ad.Map = map;
        ad.Flag = Blank(ad.Flag);
        ad.IgnoreFlag = Blank(ad.IgnoreFlag);
        data.Props.Add(ad);
      }
    }

    return data;
  }

  private static string? Blank(string? value) => JsonText.Blank(value);

  public static MapsData Sample() => new()
  {
    Props = new()
    {
      new PropAd { Map = "de_mirage", Path = "models/chicken/chicken.vmdl", Pos = "-1902 -1816 -240", Angle = "0 90 0" },
      new PropAd { Map = "de_mirage", Path = "models/props/cs_office/vending_machine.vmdl", Pos = "1376 -16 -144", Angle = "0 180 0" },
      new PropAd { Map = "de_mirage", Path = "models/generic/stone_statue_01/stone_statue_01.vmdl", Pos = "-1972 -1988 -264", Angle = "0 45 0" }
    }
  };
}

public class ScreenTextAd
{
  [JsonPropertyName("text")] public string Text { get; set; } = "";
  [JsonPropertyName("life")] public float Life { get; set; } = 8f;
  [JsonPropertyName("timer")] public float Timer { get; set; } = 30f;
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] [JsonPropertyName("x")] public float X { get; set; }
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] [JsonPropertyName("y")] public float Y { get; set; }
  [JsonPropertyName("size")] public float Size { get; set; } = 32f;
  [JsonPropertyName("color")] public string Color { get; set; } = "#FFFFFF";
  [JsonPropertyName("justify")] public string Justify { get; set; } = "left";
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] [JsonPropertyName("background")] public bool Background { get; set; }
  [JsonPropertyName("flag")] public string? Flag { get; set; }
  [JsonPropertyName("ignoreflag")] public string? IgnoreFlag { get; set; }
}

public class HudSayAd
{
  [JsonPropertyName("text")] public string Text { get; set; } = "";
  [JsonPropertyName("life")] public float Life { get; set; } = 6f;
  [JsonPropertyName("timer")] public float Timer { get; set; } = 45f;
  [JsonPropertyName("flag")] public string? Flag { get; set; }
  [JsonPropertyName("ignoreflag")] public string? IgnoreFlag { get; set; }
}

public class ChatSayAd
{
  [JsonPropertyName("text")] public string Text { get; set; } = "";
  [JsonPropertyName("timer")] public float Timer { get; set; } = 60f;
  [JsonPropertyName("flag")] public string? Flag { get; set; }
  [JsonPropertyName("ignoreflag")] public string? IgnoreFlag { get; set; }
}

public class EventAd
{
  [JsonPropertyName("event")] public string Event { get; set; } = "";
  [JsonPropertyName("target")] public string Target { get; set; } = "all";
  [JsonPropertyName("type")] public string Type { get; set; } = "chatsay";
  [JsonPropertyName("text")] public string Text { get; set; } = "";
  [JsonPropertyName("life")] public float Life { get; set; } = 4f;
  [JsonPropertyName("cooldown")] public float Cooldown { get; set; } = 10f;
  [JsonPropertyName("chance")] public int Chance { get; set; } = 100;
  [JsonPropertyName("flag")] public string? Flag { get; set; }
  [JsonPropertyName("ignoreflag")] public string? IgnoreFlag { get; set; }
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] [JsonPropertyName("x")] public float X { get; set; }
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] [JsonPropertyName("y")] public float Y { get; set; }
  [JsonPropertyName("size")] public float Size { get; set; } = 32f;
  [JsonPropertyName("color")] public string Color { get; set; } = "#FFFFFF";
  [JsonPropertyName("justify")] public string Justify { get; set; } = "left";
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] [JsonPropertyName("background")] public bool Background { get; set; }

  public ScreenTextAd ToScreenText() => new()
  {
    Text = Text,
    Life = Life,
    Timer = 0f,
    X = X,
    Y = Y,
    Size = Size,
    Color = Color,
    Justify = Justify,
    Background = Background
  };
}

public class AdsData
{
  [JsonIgnore] public List<PropAd> Props { get; set; } = new();

  [JsonPropertyName("screentexts")] public List<ScreenTextAd> ScreenTexts { get; set; } = new();
  [JsonPropertyName("hudsays")] public List<HudSayAd> HudSays { get; set; } = new();
  [JsonPropertyName("chatsays")] public List<ChatSayAd> ChatSays { get; set; } = new();
  [JsonPropertyName("events")] public List<EventAd> Events { get; set; } = new();

  public static AdsData Sample() => new()
  {
    ScreenTexts = new()
    {
      new ScreenTextAd
      {
        Text = "bydexter.net\nGitHub: github.com/ByDexterTR",
        Life = 8f, Timer = 30f, X = -6.4f, Y = 1.3f, Size = 32f, Color = "#FFFFFF", Justify = "left", Background = true
      },
      new ScreenTextAd
      {
        Text = "Sunucumuza <br> destek olun",
        Life = 6f, Timer = 20f, X = -6.4f, Y = 1.3f, Size = 28f, Color = "#7CFC00", Justify = "left",
        IgnoreFlag = "@css/vip"
      }
    },
    HudSays = new()
    {
      new HudSayAd
      {
        Text = "<font color='#7CFC00' class='fontSize-m'>bydexter.net</font><br>GitHub: github.com/ByDexterTR",
        Life = 6f, Timer = 45f
      },
      new HudSayAd
      {
        Text = "<font color='#FF6347' class='fontSize-m'>Kurallara uyun</font><br>Iyi oyunlar",
        Life = 5f, Timer = 60f
      }
    },
    ChatSays = new()
    {
      new ChatSayAd { Text = "{Orchid}[Reklam]{Default} Sunucumuza destek olmak icin {Lime}bydexter.net{Default} adresini ziyaret edin.", Timer = 60f },
      new ChatSayAd { Text = "{Orchid}[Reklam]{Default} GitHub: {Blue}github.com/ByDexterTR", Timer = 90f, IgnoreFlag = "@css/vip" },
      new ChatSayAd { Text = "{Orchid}[VIP]{Default} VIP komutlari icin {Lime}!vip{Default} yazin.", Timer = 120f, Flag = "@css/vip" }
    },
    Events = new()
    {
      new EventAd
      {
        Event = "player_death", Target = "attacker", Type = "hudsay",
        Text = "<font color='#7CFC00' class='fontSize-m'>{victim} oldurdun</font><br>bydexter.net",
        Life = 3f, Cooldown = 5f
      },
      new EventAd
      {
        Event = "player_hurt", Target = "attacker", Type = "screentext",
        Text = "-{damage} HP\nbydexter.net",
        Life = 2f, Cooldown = 1f, X = 0f, Y = -1.2f, Size = 24f, Color = "#FF6347", Justify = "center"
      },
      new EventAd
      {
        Event = "round_start", Target = "all", Type = "chatsay",
        Text = "{Orchid}[Reklam]{Default} Iyi raundlar! {Lime}bydexter.net",
        Cooldown = 0f, Chance = 35
      },
      new EventAd
      {
        Event = "round_end", Target = "all", Type = "hudsay",
        Text = "<font color='#FFD700' class='fontSize-m'>{winner} kazandi</font><br>bydexter.net",
        Life = 4f, Cooldown = 0f
      },
      new EventAd
      {
        Event = "bomb_planted", Target = "all", Type = "chatsay",
        Text = "{Orchid}[Reklam]{Default} Bomba {Red}{site}{Default} bolgesine kuruldu. {Lime}bydexter.net",
        Cooldown = 0f
      },
      new EventAd
      {
        Event = "bomb_defused", Target = "all", Type = "chatsay",
        Text = "{Orchid}[Reklam]{Default} {player} bombayi etkisiz hale getirdi. {Lime}bydexter.net",
        Cooldown = 0f
      },
      new EventAd
      {
        Event = "player_connect_full", Target = "player", Type = "chatsay",
        Text = "{Orchid}[Reklam]{Default} Hos geldin {Lime}{player}{Default}! GitHub: {Blue}github.com/ByDexterTR",
        Cooldown = 0f
      },
      new EventAd
      {
        Event = "player_team", Target = "player", Type = "chatsay",
        Text = "{Orchid}[Reklam]{Default} {team} takimina gectin. Iyi oyunlar! {Lime}bydexter.net",
        Cooldown = 30f
      }
    }
  };
}
