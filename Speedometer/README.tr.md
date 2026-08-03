# Speedometer

Oyuncunun anlık hızını (u/s) ekran ortasında gösterir. Hız arttıkça gösterge rengi beyazdan maviye, turuncuya ve kırmızıya geçer. Bhop / surf / kz sunucuları için tasarlanmıştır.

## Özellikler

- CenterHtml HUD üzerinde anlık yatay hız göstergesi (`u/s`)
- Hıza göre renk geçişi: 0 beyaz → 1000 mavi → 2000 turuncu → 3000+ kırmızı (ara değerler interpolasyonlu)
- İzleyici (spectator) modunda izlenen oyuncunun hızı gösterilir
- Tercih kalıcıdır — kapatan oyuncu için `Speedometer.json` dosyasına kaydedilir
- Menü açıkken gösterge otomatik gizlenir
- Slot bazlı takip; kimse kullanmıyorken tick maliyeti minimumdur
- Türkçe / İngilizce dil desteği (`lang/`)

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `Speedometer` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/Speedometer/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load Speedometer` komutunu çalıştırın.

## Komutlar

| Komut | Açıklama | Yetki |
| --- | --- | --- |
| `css_hiz` / `css_hız` | Hız göstergesini açar/kapatır | — (herkes) |

## Yapılandırma

Göstergeyi **kapatan** oyuncuların SteamID64 listesi eklenti klasöründeki `Speedometer.json` dosyasında tutulur (varsayılan davranış: herkes için açık).

## Notlar

- Hız hesabına dikey eksen dahil edilmez (yalnızca X/Y düzlemi).
- Gösterge ikonu repodaki [`img/speedometer.png`](../img/speedometer.png) dosyasından yüklenir.