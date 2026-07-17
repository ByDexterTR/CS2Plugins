using System.Text.RegularExpressions;

namespace ByDexter.Shared;

public static class CC
{
  public static char Default => '\x01';
  public static char White => '\x01';
  public static char Red => '\x07';
  public static char LightRed => '\x0F';
  public static char DarkRed => '\x02';
  public static char BlueGrey => '\x0A';
  public static char Blue => '\x0B';
  public static char DarkBlue => '\x0C';
  public static char Purple => '\x0C';
  public static char Orchid => '\x0E';
  public static char Yellow => '\x09';
  public static char Gold => '\x10';
  public static char Orange => '\x10';
  public static char LightGreen => '\x05';
  public static char Green => '\x04';
  public static char Lime => '\x06';
  public static char Grey => '\x08';
  public static char Grey2 => '\x0D';

  private static readonly Dictionary<string, char> ColorMap = new(StringComparer.OrdinalIgnoreCase)
  {
    { "Default", '\x01' }, { "White", '\x01' },
    { "Red", '\x07' }, { "LightRed", '\x0F' }, { "DarkRed", '\x02' },
    { "BlueGrey", '\x0A' }, { "Blue", '\x0B' }, { "DarkBlue", '\x0C' },
    { "Purple", '\x0C' }, { "Orchid", '\x0E' },
    { "Yellow", '\x09' }, { "Gold", '\x10' }, { "Orange", '\x10' },
    { "LightGreen", '\x05' }, { "Green", '\x04' }, { "Lime", '\x06' },
    { "Grey", '\x08' }, { "Gray", '\x08' }, { "Grey2", '\x0D' }
  };

  public static string Parse(string message)
  {
    return Regex.Replace(message, @"\{(\w+)\}", match =>
    {
      var colorName = match.Groups[1].Value;
      return ColorMap.TryGetValue(colorName, out var color) ? color.ToString() : match.Value;
    });
  }
}
