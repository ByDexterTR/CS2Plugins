# Redbull

Komut kullanan oyuncuya kısa süreli hız artışı verir ("Redbull kanatlandırır"). Süre, hız, takım kısıtı, raunt limiti ve bekleme süresi config'ten yönetilir.

## Özellikler

- Ayarlanabilir hız çarpanı ve süre
- Efekt aktifken oyuncu config'teki renge boyanır; süre bitince normale döner
- Takım filtresi: yalnızca T, yalnızca CT veya herkes
- Raunt başına kullanım limiti
- Kullanımlar arası bekleme süresi (cooldown)
- Raunt başında tüm limit/cooldown/efektler sıfırlanır
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `Redbull` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/Redbull/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load Redbull` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_redbull` | Hız efektini başlatır | — (takım filtresine ve hayatta olmaya bağlı) |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/Redbull/Redbull.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `speed` | float | `2.0` | Hız çarpanı (1.0 = normal) |
| `duration` | int | `10` | Efekt süresi (saniye) |
| `filter_team` | string | `"T"` | `"T"`, `"CT"` veya `"Both"` |
| `player_color` | int[3] | `[248, 123, 27]` | Efekt aktifken oyuncu rengi (RGB) |
| `round_limiter` | int | `2` | Raunt başına kullanım limiti (`0` = sınırsız) |
| `cooldown` | int | `15` | Kullanımlar arası bekleme (saniye, `0` = yok) |

### Örnek Config

```json
{
  "speed": 2.0,
  "duration": 10,
  "filter_team": "T",
  "player_color": [248, 123, 27],
  "round_limiter": 2,
  "cooldown": 15
}
```

## Kullanım Örneği

```
!redbull → 10 saniye boyunca 2x hız + turuncu renk
```

## Notlar

- Efekt sırasında başka bir eklenti hızı düşürürse Redbull hızı tekrar uygular (`VelocityModifier` her tick kontrol edilir).
- Efekt yalnızca hayattaki oyuncularda çalışır; oyuncu ölürse efekt kendiliğinden sonlanır.