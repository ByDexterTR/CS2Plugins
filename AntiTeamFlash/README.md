# AntiTeamFlash

Takım arkadaşlarının attığı flash bombalarının kör etme etkisini iptal eder. Düşman flash'ları normal şekilde çalışmaya devam eder.

## Özellikler

- Takım arkadaşından gelen flash etkisini anında sıfırlar
- Düşmandan gelen ve hâlâ süren "meşru" körlük varsa onu geri yükler (takım flash'ı meşru körlüğü silemez)
- Slot bazlı hafif durum takibi — tick döngüsü yok, yalnızca event tabanlı
- Config gerektirmez

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.369+

## Kurulum

1. Derlenmiş `AntiTeamFlash` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/AntiTeamFlash/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load AntiTeamFlash` komutunu çalıştırın.

## Komutlar

Bu eklentinin komutu yoktur; yüklendiği anda otomatik çalışır.

## Yapılandırma

Config dosyası yoktur.

## Çalışma Mantığı

1. `EventPlayerBlind` (Post) yakalanır.
2. Flash'ı atan oyuncu **farklı takımdan** ise körlük değerleri (başlangıç/bitiş zamanı, süre, alpha) kaydedilir — bu "meşru körlük"tür.
3. Flash'ı atan **aynı takımdan** ise:
   - Meşru körlük hâlâ sürüyorsa eski değerler geri yüklenir,
   - Sürmüyorsa körlük tamamen sıfırlanır (`FlashDuration = 0`).
4. Raunt başında ve oyuncu ayrıldığında durum temizlenir.

## Notlar

- Kendi attığınız flash sizi etkilemeye devam eder (yalnızca *takım arkadaşının* flash'ı engellenir).
- Eklentinin dil dosyası yoktur; oyunculara mesaj göndermez.