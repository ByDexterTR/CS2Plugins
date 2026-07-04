# ChatCleaner

Sohbet temizleme aracı. Oyuncular kendi ekranını, adminler tüm sunucunun sohbetini temizleyebilir.

## Özellikler

- Oyuncu bazlı sohbet temizleme (yalnızca kendi ekranı)
- Admin için tüm sunucu sohbetini temizleme (temizleyen adminin adı duyurulur)
- 500 boş satır basarak geçmişi ekrandan kaydırır
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.369+

## Kurulum

1. Derlenmiş `ChatCleaner` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/ChatCleaner/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load ChatCleaner` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_selfcc` | Yalnızca kendi sohbet ekranınızı temizler | — (herkes) |
| `css_cc` | Tüm sunucunun sohbetini temizler | `@css/chat` |

## Yapılandırma

Config dosyası yoktur. Mesajlar ve sohbet ön eki `lang/tr.json` / `lang/en.json` üzerinden düzenlenebilir.

## Kullanım Örneği

```
!selfcc   → Sohbetin temizlendi.
!cc       → Sohbet temizlendi. Temizleyen: AdminAdı
```