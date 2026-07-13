# SpawnkillProtection

Spawn olan oyunculara yapılandırılabilir süreli hasar koruması verir. [CTSpawnKill](../CTSpawnKill)'in kapsamlı sürümüdür: takım bazlı **ve** yetki (flag) bazlı koruma, renkli görsel geri bildirim ve koruma süresince **yavaşça normale dönen renk geçişi** içerir.

## Özellikler

- **Flag bazlı koruma** takım korumasından **önceliklidir** — ör. VIP'lere daha uzun koruma
- T ve CT için ayrı ayrı açılıp kapatılabilen, süresi ve rengi özelleştirilebilen takım koruması
- Koruma süresince oyuncunun rengi koruma renginden **kademeli olarak normale döner** — rengin solması korumanın ne kadar kaldığını gösterir, bitişi herkes anlar
- Koruma boyunca tüm hasar sıfırlanır (`OnEntityTakeDamagePre`)
- Koruma başlangıcı ve bitişi oyuncuya sohbetten bildirilir
- Raunt başında ve oyuncu ayrıldığında durum güvenle temizlenir
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `SpawnkillProtection` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/SpawnkillProtection/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load SpawnkillProtection` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Komutlar

Bu eklentinin komutu yoktur; spawn'da otomatik devreye girer.

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/SpawnkillProtection/SpawnkillProtection.json
```

| Ayar | Tip | Açıklama |
| --- | --- | --- |
| `flag_protections` | liste | Yetki bazlı korumalar; **listedeki sıra öncelik sırasıdır** ve takım korumasını ezer |
| `flag_protections[].flag` | string | Gerekli yetki (ör. `@css/vip`) |
| `flag_protections[].seconds` | float | Koruma süresi (saniye) |
| `flag_protections[].color` | int[3] | Koruma rengi (RGB) |
| `team_t.enabled` | bool | T takımı koruması aktif mi |
| `team_t.seconds` | float | T koruma süresi |
| `team_t.color` | int[3] | T koruma rengi |
| `team_ct.enabled` / `seconds` / `color` | — | CT için aynı ayarlar |

### Örnek Config

```json
{
  "flag_protections": [
    { "flag": "@css/root", "seconds": 10, "color": [255, 0, 255] },
    { "flag": "@css/vip",  "seconds": 8,  "color": [255, 215, 0] }
  ],
  "team_t":  { "enabled": true, "seconds": 5, "color": [255, 64, 64] },
  "team_ct": { "enabled": true, "seconds": 5, "color": [64, 128, 255] }
}
```

Bu örnekte: root yetkili 10 sn mor, VIP 8 sn altın, diğer T'ler 5 sn kırmızı, diğer CT'ler 5 sn mavi korumayla doğar.

## Çalışma Mantığı

1. Oyuncu spawn olur → önce `flag_protections` listesi sırayla denenir; eşleşme yoksa takım ayarına bakılır.
2. Koruma boyunca oyuncuya gelen tüm hasar sıfırlanır.
3. Oyuncunun rengi koruma renginde başlar ve süre ilerledikçe **doğrusal olarak beyaza (normale) yaklaşır** — süre bittiğinde renk tamamen normaldir ve "koruma sona erdi" mesajı gönderilir.

## Notlar

- Takım koruması botlara da uygulanır; flag kontrolü yalnızca gerçek oyuncular için yapılır.
- `seconds: 0` veya `enabled: false` ile ilgili koruma tamamen kapatılabilir.
- Aynı işlevin yalnızca CT'ye sabit renkle uygulanan basit hâli için [CTSpawnKill](../CTSpawnKill) eklentisine bakın; iki eklentiyi **aynı anda kullanmayın**.
