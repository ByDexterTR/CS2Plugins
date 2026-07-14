# Lazer

Ölü oyunculara ve izleyicilere, canlı oyuncuların o an baktığı yönü lazer ışını olarak gösterir. Işın, oyuncunun gözünden bakış yönünde ilk engele (duvar, zemin, oyuncu) kadar uzanır; canlı oyuncular ışınları göremez.

## Özellikler

- Her canlı oyuncunun bakış yönü gerçek zamanlı (her tick) lazerle çizilir
- Native ray-trace ile ışın ilk engelde biter (duvar arkasına taşmaz)
- Işınlar yalnızca lazeri açık olan ölü oyunculara/izleyicilere gönderilir (`CheckTransmit`); canlı oyuncular ve GOTV hiçbir şekilde görmez
- `css_lazer` ile oyuncu bazında aç/kapat; varsayılan durum config'ten ayarlanabilir
- Takım bazlı ışın rengi (T / CT ayrı, config'ten `R G B` veya `#RRGGBB`)
- Işın kalınlığı ve maksimum mesafe ayarlanabilir
- Ölüm, ayrılma, raunt başı/sonu ve eklenti kapanışında ışınlar güvenle temizlenir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `Lazer` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/Lazer/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load Lazer` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_lazer` / `css_laser` | Lazer görünürlüğünü açar/kapatır (tercih ölüyken uygulanır) | Yok |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/Lazer/Lazer.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `lazer_cmd` | string | `"css_lazer,css_laser"` | Virgülle ayrılmış komut adları |
| `lazer_default_on` | bool | `true` | Yeni bağlanan oyuncular için lazer başlangıç durumu |
| `lazer_width` | float | `0.4` | Işın kalınlığı (minimum 0.1) |
| `lazer_max_distance` | float | `8192` | Işının maksimum uzunluğu (minimum 256) |
| `lazer_t_color` | string | `"234 210 139"` | T takımı ışın rengi (`R G B` veya `#RRGGBB`) |
| `lazer_ct_color` | string | `"182 212 238"` | CT takımı ışın rengi (`R G B` veya `#RRGGBB`) |

### Örnek Config

```json
{
  "lazer_cmd": "css_lazer,css_laser",
  "lazer_default_on": true,
  "lazer_width": 0.4,
  "lazer_max_distance": 8192,
  "lazer_t_color": "234 210 139",
  "lazer_ct_color": "182 212 238"
}
```

## Notlar

- Işın kesme, oyun motorunun trace fonksiyonuna imza taramasıyla bağlanır (`CNavPhysicsInterface`). Oyun güncellemesi sonrası imza kırılırsa eklenti çalışmaya devam eder ancak ışınlar engelde kesilmek yerine maksimum mesafeye uzanır (konsola hata yazılır).
- Lazeri açık ölü oyuncu yoksa hiç ışın oluşturulmaz ve trace çağrılmaz; performans maliyeti sıfıra iner.
- Oyuncu takım değiştirdiğinde ışın rengi bir sonraki canlanışta otomatik güncellenir.
- Komut adı değişikliği (`lazer_cmd`) sunucu/eklenti yeniden başlatıldığında etkinleşir.
