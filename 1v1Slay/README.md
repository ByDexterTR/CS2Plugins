# 1v1Slay

Rauntta her iki takımdan da yalnızca **1'er canlı oyuncu** kaldığında otomatik geri sayım başlatan ve süre dolduğunda kalan oyuncuları öldürür. Raundun 1v1 durumunda kilitlenmesini engeller.

## Özellikler

- 1 T vs 1 CT durumunu otomatik algılar ve geri sayımı başlatır
- Geri sayım sohbet mesajı ve/veya CenterHtml HUD üzerinde gösterilir
- Sayaç; raunt başında, raunt sonunda veya 1v1 durumu bozulduğunda otomatik iptal edilir
- Minimum oyuncu sayısı şartı (varsayılan: 3) — az kişiyle sayaç devreye girmez
- Süre dolduğunda hayatta kalan oyuncular slay edilir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `1v1Slay` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/1v1Slay/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load 1v1Slay` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_stopslay` | Aktif 1v1 geri sayımını durdurur | `@css/generic` **veya** `@css/slay` |

## Yapılandırma

Config dosyası ilk yüklemede otomatik oluşturulur:

```
csgo/addons/counterstrikesharp/configs/plugins/1v1Slay/1v1Slay.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `min_players` | int | `3` | Sayaç için gereken minimum oyuncu sayısı (T + CT + izleyici) |
| `countdown_time` | int | `30` | Geri sayım süresi (saniye) |
| `enable_chat_announce` | bool | `true` | Sohbet duyurularını aç/kapat |
| `enable_hud_announce` | bool | `true` | HUD (CenterHtml) sayacını aç/kapat |

### Örnek Config

```json
{
  "min_players": 3,
  "countdown_time": 30,
  "enable_chat_announce": true,
  "enable_hud_announce": true
}
```

## Kullanım Örneği

1. Rauntta yalnızca 1 T ve 1 CT hayatta kalır → 30 saniyelik sayaç başlar.
2. Sohbete her 5 saniyede bir (son 5 saniyede her saniye) uyarı düşer; HUD'da kırmızı ölüm sayacı görünür.
3. Süre dolarsa iki oyuncu da öldürülür; biri diğerini öldürürse sayaç kendiliğinden durur.
4. Admin dilerse `!stopslay` ile sayacı iptal edebilir.

## Notlar

- Sohbet ön eki (`chat_prefix`) ve tüm mesajlar `lang/tr.json` / `lang/en.json` dosyalarından düzenlenebilir.
- HUD sayacındaki ikon bu repodaki [`img/skull.png`](../img/skull.png) dosyasından yüklenir.