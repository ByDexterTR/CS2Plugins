# JBDoors

Haritadaki tüm kapıları tek komutla açar veya kapatır. Jailbreak sunucularında hücre kapıları için kullanılır.

## Özellikler

- Tek komutla tüm kapı türlerini açma: `func_door`, `func_movelinear`, `func_door_rotating`, `prop_door_rotating`
- Açma komutu ayrıca `func_breakable` entity'lerini kırar (kırılabilir hücre kapıları için)
- Kapatma komutu aynı kapı türlerini kapatır
- İşlemi yapan oyuncunun adı tüm sunucuya duyurulur
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `JBDoors` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/JBDoors/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load JBDoors` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_kapiac` | Tüm kapıları açar, kırılabilirleri kırar | `@css/generic` **veya** `@jailbreak/warden` |
| `css_kapikapat` | Tüm kapıları kapatır | `@css/generic` **veya** `@jailbreak/warden` |

## Yapılandırma

Config dosyası yoktur. Mesajlar `lang/tr.json` / `lang/en.json` üzerinden düzenlenebilir.

## Kullanım Örneği

```
!kapiac    → [ByDexter] WardenAdı tüm kapıları açtı!
!kapikapat → [ByDexter] WardenAdı tüm kapıları kapattı!
```

## Notlar

- Kırılan `func_breakable` entity'leri raunt yenilenene kadar geri gelmez (harita davranışı).
- Komut haritadaki **tüm** eşleşen entity'leri hedefler; belirli kapıları ayırt etmez.