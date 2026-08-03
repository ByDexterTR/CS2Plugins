# AntiTeamFlash

Takım arkadaşlarının attığı flash bombalarının kör etme etkisini iptal eder. Düşman flash'ları normal şekilde çalışmaya devam eder.

## Özellikler

- Takım arkadaşından gelen flash etkisini anında sıfırlar
- Düşmandan gelen ve hâlâ süren "meşru" körlük varsa onu geri yükler (takım flash'ı meşru körlüğü silemez)
- Slot bazlı hafif durum takibi — tick döngüsü yok, yalnızca event tabanlı
- Config gerektirmez

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `AntiTeamFlash` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/AntiTeamFlash/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load AntiTeamFlash` komutunu çalıştırın.

## Notlar

- Kendi attığınız flash sizi etkilemeye devam eder (yalnızca *takım arkadaşının* flash'ı engellenir).
