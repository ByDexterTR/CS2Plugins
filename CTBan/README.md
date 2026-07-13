# CTBan

Oyuncuların CT (gardiyan) takımına geçişini süreli olarak yasaklar. Jailbreak sunucularının vazgeçilmez moderasyon aracıdır.

## Özellikler

- Süreli CT yasağı (saniyeden yıla kadar esnek süre birimleri)
- Yasaklı oyuncu CT'ye geçmeye çalıştığında otomatik T takımına gönderilir (takım değişimi, spawn ve `jointeam` komutunun üçü de kontrol edilir)
- Sunucuda olmayan oyunculara SteamID64 ile yasak ekleme
- Yasak listesi kalıcıdır — `CTBanList.json` dosyasında saklanır, sunucu yeniden başlasa da korunur
- Süresi dolan yasaklar otomatik temizlenir
- Yasak sebebini ve yasaklayan admini kaydeder
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `CTBan` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/CTBan/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load CTBan` komutunu çalıştırın.
3. `CTBanList.json` ilk yüklemede otomatik oluşturulur.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_ctban <hedef> <süre> [sebep]` | Sunucudaki oyuncuya CT yasağı verir (CT'deyse T'ye atılır) | `@css/ban` |
| `css_ctunban <hedef>` | Sunucudaki oyuncunun CT yasağını kaldırır | `@css/ban` |
| `css_ctaddban <steamid64> <süre> [sebep]` | SteamID64 ile (çevrimdışı) yasak ekler | `@css/ban` |
| `css_ctbanlist` | Aktif CT yasaklarını listeler | — (herkes) |

- `<hedef>`: oyuncu adı veya `#userid`
- `<süre>` birimleri: `s` saniye, `m` dakika (varsayılan), `h` saat, `d` gün, `w` hafta, `mo` ay, `y` yıl

## Veri Dosyası

Yasaklar eklenti klasöründeki `CTBanList.json` dosyasında tutulur:

```json
{
  "BannedPlayers": {
    "76561198000000000": {
      "Nickname": "OyuncuAdı",
      "BanTime": 1751630000,
      "Reason": "Kural ihlali",
      "Admin": "76561198000000001"
    }
  }
}
```

> `BanTime`, yasağın **bitiş** zamanıdır (Unix timestamp, saniye).

## Kullanım Örnekleri

```
!ctban Oyuncu 30m Serbest gün kuralı ihlali
!ctban #42 2h
!ctaddban 76561198000000000 1d Mikrofon yok
!ctunban Oyuncu
!ctbanlist
```

## Notlar

- Yasak süresi dolduğunda oyuncu bağlandığı, takım değiştirdiği veya spawn olduğu anda kayıt otomatik silinir.
- Dosya yazma işlemleri arka planda (async) yapılır, oyun akışını bloklamaz.