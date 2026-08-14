# TABServerName

*Bu dosyanın [İngilizcesi / English](README.md).*

TAB skor tablosunun üstünde yazan `{mod} | {harita}` etiketinin harita kısmını, config'te yazdığınız metinle değiştirir. Böylece oyuncular TAB'a bastığında harita adının yanında sunucu adınızı, IP'nizi veya istediğiniz herhangi bir yazıyı görürler.

```
Normalde:   Rekabetçi | Mirage
Bu eklentiyle:   Rekabetçi | de_mirage | bydexter.net | 5v5 RETAKE
```

## Özellikler

- Her harita başında otomatik uygulanır
- Metin içinde harita adı, sunucu adı, IP ve port placeholder'ları kullanılabilir
- Mod ismine (Rekabetçi / Basit Eğlence / vb.) dokunmaz, o kısım olduğu gibi kalır
- Canlı harita değişimlerinde sunucuyu etkilemez

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371
- `gamedata` dosyası: `addons/counterstrikesharp/gamedata/TABServerName.gamedata.json`

## Kurulum

1. Derlenmiş `TABServerName` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/TABServerName/
   ```
2. `gamedata/TABServerName.gamedata.json` dosyasını, CounterStrikeSharp'ın ortak gamedata klasörüne kopyalayın:
   ```
   csgo/addons/counterstrikesharp/gamedata/TABServerName.gamedata.json
   ```
3. Sunucuyu yeniden başlatın veya `css_plugins load TABServerName` komutunu çalıştırın.

> İkinci adım zorunludur. Bu dosya eklenti klasörüne değil, ortak `gamedata` klasörüne konulmalıdır.

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/TABServerName/TABServerName.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `servername_text` | string | `"{MAP} \| github.com/ByDexterTR/CS2Plugins"` | TAB'da harita kısmında görünecek metin |

### Placeholder'lar

| Placeholder | Karşılığı |
| --- | --- |
| `{MAP}` | Harita ismi |
| `{HOSTNAME}` | Sunucu ismi (`hostname`) |
| `{IP}` | Sunucu IP adresi |
| `{PORT}` | Sunucu portu |

### Örnek Config

```json
{
  "servername_text": "{MAP} | {HOSTNAME} | {IP}:{PORT}"
}
```

## Notlar

- Değişiklik yalnızca TAB skor tablosunda görünür. Steam sunucu listesindeki adınız veya sunucu sorgularına verilen cevaplar etkilenmez.
- Bir CS2 veya CounterStrikeSharp güncellemesinden sonra yazı değişmiyorsa, `gamedata` dosyasının güncellenmesi gerekiyor demektir. Eklenti bu durumda kendini kapatır ve sunucu konsoluna `[TABServerName] DEVRE DISI: ...` satırını yazar; sunucunuz çökmez.

## Bilinen Sorunlar

- Bu eklenti harita adının yazıldığı yeri değiştirdiği için, harita adını kullanan **başka eklentiler de** bu yeni metni okur. Örneğin harita başına dosya oluşturan bir eklenti, dosya adı olarak gerçek harita adı yerine sizin yazdığınız metni kullanmaya çalışır ve hata verebilir.
- Bu yüzden `servername_text` içinde `/`, `\`, `:` ve `|` gibi dosya adlarında sorun çıkaran karakterlerden kaçının. Sunucunuzda harita adını kullanan başka eklentiler varsa (istatistik, spawn kaydı, harita oylaması gibi) bu eklentiyi açtıktan sonra onları mutlaka test edin.
- Bunun kalıcı bir çözümü yok; kullandığımız yöntemin doğasında olan bir sınırlama.
