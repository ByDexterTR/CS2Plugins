# Çekiliş (Cekilis)

Sunucudaki oyuncular arasından filtreli rastgele çekiliş yapar. Jailbreak etkinlikleri, ödül dağıtımı vb. için idealdir.

## Özellikler

- 9 farklı filtre: herkes, canlı, ölü, takım ve takım+durum kombinasyonları
- Kazanan tüm sunucuya duyurulur (çekilişi yapan admin ve kategori bilgisiyle)
- Botlar çekilişe dahil edilmez
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `Cekilis` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/Cekilis/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load Cekilis` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_cek <filtre>` | Belirtilen havuzdan rastgele bir oyuncu seçer | `@css/chat` |
| `css_cek` | Kullanılabilir filtrelerin listesini gösterir | `@css/chat` |

### Filtreler

| Filtre | Havuz |
| --- | --- |
| `all` | Tüm oyuncular |
| `live` | Hayatta olan tüm oyuncular |
| `dead` | Ölü olan tüm oyuncular |
| `t` | Tüm T takımı |
| `ct` | Tüm CT takımı |
| `tlive` | Hayattaki T'ler |
| `tdead` | Ölü T'ler |
| `ctlive` | Hayattaki CT'ler |
| `ctdead` | Ölü CT'ler |

## Yapılandırma

Config dosyası yoktur. Mesajlar ve sohbet ön eki `lang/tr.json` / `lang/en.json` üzerinden düzenlenebilir.

## Kullanım Örneği

```
!cek tdead
```

> `[ByDexter] AdminAdı, TDEAD kategorisinde çekiliş yaptı: Kazanan → OyuncuAdı`