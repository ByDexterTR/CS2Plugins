# CTSpawnKill

CT oyuncularına spawn olduktan sonra kısa süreli hasar koruması verir. Jailbreak'te raunt başı gardiyan avlamayı (spawn kill) engeller.

## Özellikler

- CT spawn olduğunda belirlenen süre boyunca **tüm hasar sıfırlanır**
- Korumalı oyuncu turuncu renkle boyanır; süre bitince rengi normale döner
- Koruma başlangıcı ve bitişi oyuncuya sohbetten bildirilir
- Oyuncu ayrıldığında / harita değiştiğinde zamanlayıcılar güvenle temizlenir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.369+

## Kurulum

1. Derlenmiş `CTSpawnKill` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/CTSpawnKill/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load CTSpawnKill` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

Bu eklentinin komutu yoktur; CT spawn'ında otomatik devreye girer.

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/CTSpawnKill/CTSpawnKill.json
```

| Ayar | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `spawn_protect_seconds` | int | `5` | Spawn korumasının süresi (saniye, minimum 1) |

### Örnek Config

```json
{
  "spawn_protect_seconds": 5
}
```

## Notlar

- Koruma yalnızca **CT takımı** için geçerlidir.
- Hasar engelleme `OnEntityTakeDamagePre` üzerinden yapılır; her türlü hasar kaynağı (silah, bıçak, patlama) sıfırlanır.
- Flag bazlı koruma, T desteği ve renk geçişli kapsamlı sürüm için [SpawnkillProtection](../SpawnkillProtection) eklentisine bakın; iki eklentiyi aynı anda kullanmayın.

