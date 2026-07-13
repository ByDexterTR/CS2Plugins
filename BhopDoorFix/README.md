# BhopDoorFix

Bhop / KZ haritalarındaki `func_door` kapılarının hareket etmesini engeller. Kapıların oyuncuyu fırlatması veya hareket ederek exploit oluşturması önlenir.

## Özellikler

- Haritadaki tüm `func_door` entity'lerini dondurur (`Speed = 0` + `Lock` input)
- Yeni spawn olan kapıları da otomatik yakalar (`OnEntitySpawned`)
- Her raunt başında tüm kapılar yeniden dondurulur
- Hot reload destekli — eklenti yeniden yüklendiğinde mevcut kapılar anında dondurulur
- Config gerektirmez

## Gereksinimler

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.371

## Kurulum

1. Derlenmiş `BhopDoorFix` klasörünü sunucuya kopyalayın:
   ```
   csgo/addons/counterstrikesharp/plugins/BhopDoorFix/
   ```
2. Sunucuyu yeniden başlatın veya `css_plugins load BhopDoorFix` komutunu çalıştırın.

## Komutlar

Bu eklentinin komutu yoktur; yüklendiği anda otomatik çalışır.

## Yapılandırma

Config dosyası yoktur. Etkilenen entity sınıfları kaynak kodda `DoorClasses` dizisinde tanımlıdır (varsayılan: yalnızca `func_door`). Farklı entity sınıfları eklemek için diziyi genişletip yeniden derlemeniz yeterlidir.

## Notlar

- Kapılar kilitlendiği için haritadaki kapı açma mekanikleri (buton, tetikleyici vb.) çalışmaz — eklenti bhop/surf sunucuları için tasarlanmıştır.
- Dil dosyası yoktur; oyunculara mesaj göndermez.