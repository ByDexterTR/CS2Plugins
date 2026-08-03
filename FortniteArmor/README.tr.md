# FortniteArmor

Alınan hasarı Fortnite'taki gibi önce zırhtan düşürür: zırh yettiği sürece can hiç azalmaz, zırh bitince kalan hasar cana geçer. Örneğin 50 hasarlık bir vuruşta zırhınız 40 ise zırhınız 0'a iner ve canınızdan yalnızca 10 eksilir.

## Özellikler

- Zırh varken tüm hasar önce zırhtan düşer; can ancak zırh tükenince azalmaya başlar
- Vanilla kevlar oranı (kısmi emilim) devre dışı — zırh hasarı 1'e 1 emer
- Yetki bayrağıyla sınırlandırılabilir; bayrak boşsa herkeste çalışır
- Düşme hasarı varsayılan olarak zırhtan düşmez (config ile açılabilir)
- Hasar uygulanmadan **önce** müdahale eder (`OnEntityTakeDamagePre`) — ölüm/can hesabı her zaman doğrudur
- Mermi, HE, molotof, bıçak dahil tüm hasar türlerinde çalışır

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `FortniteArmor` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/FortniteArmor/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load FortniteArmor` komutunu çalıştırın.
3. İlk yüklemede config dosyası otomatik oluşturulur.

## Yapılandırma

```
csgo/addons/counterstrikesharp/configs/plugins/FortniteArmor/FortniteArmor.json
```

| Ayar | Tip | Açıklama |
| --- | --- | --- |
| `armor_flag` | string | Virgülle ayrılmış yetki bayrakları; boş bırakılırsa herkes yararlanır (varsayılan `""`) |
| `absorb_fall_damage` | bool | `true` ise düşme hasarı da zırhtan düşer (varsayılan `false`) |

### Örnek Config

```json
{
  "armor_flag": "@css/vip,@css/root,@css/ban",
  "absorb_fall_damage": false
}
```

## Notlar

- `player_hurt` eventi hasar uygulandıktan sonra yandığı için bu iş orada yapılamaz; oyuncu vanilla hesapla ölmüşse event anında iş işten geçmiştir. Bu yüzden hasar öncesi hook kullanılır.
- Hasar tamamen zırh tarafından emildiğinde motor 0 hasar gördüğü için vuruş geri bildirimi (aim punch, `player_hurt` eventi) oluşmayabilir.
- Kask ayrıca takip edilmez; zırh 0'a inince vanilla davranış (korumasız) geçerlidir.
- `armor_flag` dolu olduğunda yetkisi olmayan oyuncularda vanilla zırh davranışı geçerlidir. `@css/root` bayrağı her zaman geçerli sayılır.
