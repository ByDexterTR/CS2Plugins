# Thirdperson

Oyuncunun kamerasını üçüncü şahıs (omuz arkası) görünüme alan bağımsız eklenti. Komut adları, yetki, kamera mesafesi ve duvar engelleme davranışı config'ten yönetilir.

## Özellikler

- Komutla aç/kapat; komut adları config'ten değiştirilebilir
- Yetki (flag) kontrolü — boş bırakılırsa herkes kullanabilir
- Ayarlanabilir kamera mesafesi
- **Duvar engelleme (`thirdperson_blockwall`)** — açıkken kamera duvarların arkasına geçemez; native ray-trace ile duvara çarptığı noktada oyuncuya yaklaştırılır (duvar arkasını görme/wallhack istismarını engeller)
- Kamera her tick oyuncunun bakışını takip eder (görünmez `prop_dynamic` + `ViewEntity`)
- **Raunt başında ve raunt sonunda** tüm üçüncü şahıs kameralar zorla kapatılır
- Ölüm, ayrılma ve eklenti kapanışında (unload) kamera güvenle eski hâline döner
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `Thirdperson` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/Thirdperson/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load Thirdperson` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_tp` / `css_thirdperson` | Üçüncü şahıs görünümü açar/kapatır (hayattayken) | `thirdperson_flag` (varsayılan `@css/thirdperson`) |

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/Thirdperson/Thirdperson.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `thirdperson_cmd` | string | `"css_tp,css_thirdperson"` | Virgülle ayrılmış komut adları |
| `thirdperson_flag` | string | `"@css/thirdperson"` | Gerekli yetki; boş string = herkes kullanabilir |
| `thirdperson_distance` | float | `110` | Kameranın oyuncuya uzaklığı (minimum 20) |
| `thirdperson_blockwall` | bool | `true` | `true`: duvarlar kamerayı engeller; `false`: kamera duvarların arkasına geçebilir |

### Örnek Config

```json
{
  "thirdperson_cmd": "css_tp,css_thirdperson",
  "thirdperson_flag": "@css/thirdperson",
  "thirdperson_distance": 110,
  "thirdperson_blockwall": true
}
```

## Notlar

- Duvar engelleme, oyun motorunun trace fonksiyonuna imza taramasıyla bağlanır (`CNavPhysicsInterface`). Oyun güncellemesi sonrası imza kırılırsa eklenti çalışmaya devam eder ancak kamera duvar kısıtlaması devre dışı kalır (konsola hata yazılır).
- Duvar engelleme açıkken kamera, göz ile hedef nokta arasındaki ilk engelde (16 birim pay bırakarak) durur; engel çok yakınsa kamera göz hizasına çekilir.
- Komut adı değişikliği (`thirdperson_cmd`) sunucu/eklenti yeniden başlatıldığında etkinleşir.
- VIP üyelerinize grup bazlı üçüncü şahıs özelliği vermek istiyorsanız [VIPCore](../VIPCore)'daki `Thirdperson` modülünü kullanın; iki sistemi aynı oyuncuda aynı anda kullanmayın.
