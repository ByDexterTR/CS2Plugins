# JBDoors

*Bu dosyanın [İngilizcesi / English](README.md).*

Haritadaki tüm kapıları tek komutla açar veya kapatır. Jailbreak sunucularında hücre kapıları için kullanılır.

## Özellikler

- Tek komutla haritadaki her türden kapıyı açar (sürgülü, döner, kayar)
- Açma komutu kırılabilir hücre kapılarını da kırar
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

Mesajlar `lang/tr.json` / `lang/en.json` üzerinden düzenlenebilir.

## Kullanım Örneği

```
!kapiac    → [ByDexter] WardenAdı tüm kapıları açtı!
!kapikapat → [ByDexter] WardenAdı tüm kapıları kapattı!
```

## Notlar

- Kırılan kapılar raunt yenilenene kadar geri gelmez; bu haritanın kendi davranışıdır.
- Komut haritadaki **bütün** kapıları etkiler; tek tek kapı seçilemez.