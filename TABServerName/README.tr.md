# TABServerName

Harita başında `CNetworkGameServer::m_MapName`'i (motorun kendi `tier0` allocator'ı üzerinden, `CUtlString::Set` ile) config'teki metinle değiştirerek TAB skor tablosundaki `{mod} | {harita}` etiketinin harita kısmını özelleştirir.

## Özellikler

- Her `OnMapStart`'ta otomatik uygulanır, oyuncular bağlanmadan önce çalışır (client bu değeri sadece bağlantı anında bir kez okur)
- `{MAP}` placeholder'ı config metninde gerçek harita adıyla değiştirilir
- Mod ismine (Rekabetçi/Basit Eğlence/vb.) hiç dokunmaz, native olarak doğru kalır
- Motorun kendi bellek yöneticisini kullanır (`tier0` export), canlı harita değişiminde çökmez
- Tüm offset ve export isimleri koda gömülü değil, `gamedata` dosyasında tutulur (CounterStrikeSharp'ın kendi native gamedata sistemi); CS2/CounterStrikeSharp güncellemesi bu değerleri kaydırırsa yeni eklenti sürümü beklemeden dosya düzeltilebilir

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `TABServerName` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/TABServerName/
   ```
2. `gamedata/TABServerName.gamedata.json` dosyasını, CounterStrikeSharp'ın paylaşılan gamedata klasörüne kopyalayın:
   ```
   csgo/addons/counterstrikesharp/gamedata/TABServerName.gamedata.json
   ```
3. Sunucuyu yeniden başlatın veya `css_plugins load TABServerName` komutunu çalıştırın.

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/TABServerName/TABServerName.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `servername_text` | string | `"{MAP} \| github.com/ByDexterTR/CS2Plugins"` | TAB'da harita kısmında görünecek metin; placeholder'lar gerçek değerle değiştirilir |

### Placeholder'lar

| Placeholder | Karşılık |
| --- | --- |
| `{MAP}` | Harita ismi |
| `{HOSTNAME}` | `hostname` convar'ı (sunucu ismi) |
| `{IP}` | `ip` convar'ı (sunucu ip adresi) |
| `{PORT}` | `hostport` convar'ı (sunucu port adresi) |

### Örnek Config

```json
{
  "servername_text": "{MAP} | {HOSTNAME} | {IP}:{PORT}"
}
```

## Notlar

- `gamedata/TABServerName.gamedata.json` içindeki `offsets` (`INetworkServerService_GetIGameServer`, `CNetworkGameServer_MapName`) ve `signatures` (`CUtlString_Set`, tier0'ın gerçek export ismi) değerleri ters mühendislikle bulunmuştur; bir CS2/CounterStrikeSharp/tier0 güncellemesi bunları bozarsa eklenti sessizce devre dışı kalır (konsola `[TABServerName] DEVRE DISI: ...` yazar), sunucuyu çökertmez.
- Değişiklik sadece TAB skor tablosunun üst etiketinde görünür; Steam sunucu listesi/A2S sorgusuna yansımaz (bu, ayrı bir veri yoludur).

## Bilinen Sorunlar

- `CNetworkGameServer::m_MapName` tek bir alan; sadece client TAB'ının okuduğu yer değil, CounterStrikeSharp'ın kendi `Server.MapName` / `NativeAPI.GetMapName()` çağrısı da (ve buna dayanan başka eklentiler) aynı alanı okuyor olabilir. TABServerName bu alanı kendi metniyle (`{MAP} | ...`) değiştirdiği için, harita adını dosya/klasör ismi gibi bir yerde kullanan başka bir eklenti artık gerçek harita adı yerine bu spoof edilmiş metni alır.
- Geçici çözüm: `servername_text` değerinde dosya sistemi için sorunlu karakterlerden (`/`, `\`, `:`, `|`) kaçının VE aynı sunucuda harita adını dosya/API için okuyan başka eklentiler varsa bunları test edin. Kalıcı çözüm henüz yok; bu, `CNetworkGameServer::m_MapName`'i doğrudan yazan yöntemin doğasında olan bir sınırlama.
