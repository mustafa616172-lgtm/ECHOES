# Echo Cihazı Kurulum Rehberi

## 🎯 Genel Bakış
Bu rehber, ECHOES oyununda Echo cihazının nasıl kurulacağını ve çalışır hale getirileceğini açıklar.

## 📦 Gerekli Scriptler
Aşağıdaki scriptler oluşturuldu:
- `EchoPickupItem.cs` - Çekmeceden Echo cihazını alma
- `EchoDevice.cs` - Echo cihazının ana kontrolcüsü
- `EchoPulseEffect.cs` - Yüksek kaliteli pulse/yankı efekti
- `EchoDeviceUI.cs` - Diegetic UI (pil/frekans göstergesi)
- `EchoEdgeDetection.shader` - Edge detection shader

## 🔧 Kurulum Adımları

### 1. Echo GameObject Hazırlığı
1. Unity sahnesinde **Echo** adlı GameObject'i bul
2. Echo GameObject'e **`EchoPickupItem`** component'ini ekle
3. Inspector'da ayarları yap:
   - **Echo Device Prefab**: Echo cihazı prefab'ını sürükle (oyuncunun eline gelecek model)
   - **Display Name**: "Echo Cihazı"
   - **Glow Color**: Cyan/mavi (varsayılan)
   - **Pickup Sound**: Alma sesi (opsiyonel)

### 2. Echo GameObject Tag Ayarı
```
Echo GameObject → Inspector → Tag → "Echo" (ZATEN AYARLI)
```

### 3. Çekmece İçine Yerleştirme
1. Echo GameObject'i istediğin **Drawer** (çekmece) GameObject'inin **child'ı** yap
2. Çekmeceyi `DrawerController` scriptine sahip olduğundan emin ol
3. Echo'yu çekmecenin içinde uygun bir pozisyona yerleştir

### 4. Collider Ayarları
Echo GameObject'inin etkileşim için **Collider** component'ine ihtiyacı var:
```
- Box Collider (veya Mesh Collider)
- Is Trigger: AÇIK (checked)
```

### 5. Player Hazırlığı
Player GameObject'e hiçbir şey eklemeye gerek yok! Echo alındığında otomatik olarak:
- `EchoDevice` component eklenir
- `EchoPulseEffect` component eklenir

## 🎨 Shader Kurulumu

### Edge Detection Shader
1. `EchoEdgeDetection.shader` dosyası `Assets/Shaders/` klasöründe
2. Unity otomatik olarak import edecek
3. Material oluşturmaya gerek yok, `EchoPulseEffect` scripti otomatik oluşturur

## 🎮 Kullanım Kontrolleri

| Tuş | Fonksiyon |
|-----|-----------|
| **Q** | Echo pulse/yankı dalgası gönder |
| **Mouse Scroll** | Frekans ayarı (20Hz - 20kHz) |
| **E** | Echo cihazını al (çekmeceye bakarken) |

## ⚙️ Echo Device Ayarları
Player'a eklenen `EchoDevice` component'inde ayarlayabilirsin:

### Pulse Ayarları
- **Pulse Cooldown**: 2 saniye (tekrar kullanma süresi)
- **Pulse Radius**: 30 metre (etki alanı)
- **Pulse Speed**: 10 (dalga hızı)
- **Pulse Duration**: 3 saniye (efekt süresi)

### Pil Ayarları
- **Max Battery**: 100%
- **Battery Consumption Per Pulse**: 5%
- **Battery Drain Per Second**: 0.5% (aktifken)

### Frekans Ayarları
- **Current Frequency**: 440 Hz (başlangıç)
- **Min Frequency**: 20 Hz
- **Max Frequency**: 20,000 Hz

## 🎯 Test Adımları

### 1. Çekmece Testi
- Play mode'a gir
- Çekmceye yaklaş
- `[E] Open Drawer` mesajı görünmeli
- E tuşu ile çekmece açılmalı

### 2. Echo Alma Testi
- Açık çekmeceye bak
- `[E] Echo Cihazı Al` mesajı görünmeli
- E tuşu ile alındığında:
  - Echo çekmeceden kaybolmalı
  - "Echo Cihazı Alındı!" mesajı görünmeli
  - Kontroller UI'da görünmeli

### 3. Echo Kullanım Testi
- Q tuşuna bas
- Pulse efekti merkezden dışarı yayılmalı
- Duvarların wireframe edge'leri kısa süre görünmeli
- Mouse scroll ile frekans değişmeli

## 🐛 Sorun Giderme

### Echo Alınamıyor
- Echo GameObject'inin **Collider** component'i var mı?
- Collider **Is Trigger** aktif mi?
- Echo GameObject **Tag'i "Echo"** mi?
- `EchoPickupItem` scripti ekli mi?

### Pulse Efekti Çalışmıyor
- Camera'da **Depth Texture** aktif mi? (Script otomatik aktifleştirir)
- `EchoEdgeDetection.shader` import edildi mi?
- Console'da shader hatası var mı?

### UI Görünmüyor
- `EchoDeviceUI` component'i eklenmediyse opsiyoneldir
- UI olmadan da oyun çalışır (sadece görsel feedback eksik olur)

## 💡 İpuçları

1. **Performance**: Pulse efekti shader-based olduğu için performanslıdır
2. **Frekans Sistemi**: Farklı frekanslarda farklı renkler kullanılır (düşük=kırmızı, yüksek=mavi)
3. **Pil Yönetimi**: Pil bitmeden önce dikkatli kullan!
4. **Cooldown**: Her pulse sonrası 2 saniye beklemelisin

## 🎨 Özelleştirme

### Pulse Rengini Değiştirme
`EchoPulseEffect` component'inde:
- **Pulse Color**: Efekt rengi
- **Wireframe Color**: Edge detection rengi

### Frekans Aralığını Değiştirme
`EchoDevice` component'inde:
- **Min Frequency** ve **Max Frequency** değerlerini ayarla

### Pil Tüketimini Ayarlama
`EchoDevice` component'inde:
- **Battery Consumption Per Pulse**: Pulse başına tüketim
- **Battery Drain Per Second**: Sürekli tüketim

## ✅ Başarılı Kurulum Kontrolü
- [ ] Echo GameObject'e `EchoPickupItem` scripti eklendi
- [ ] Echo GameObject çekmecenin child'ı
- [ ] Echo GameObject'de Collider var (Is Trigger = ON)
- [ ] Echo GameObject Tag = "Echo"
- [ ] Shader import edildi
- [ ] Play mode'da çekmece açılıyor
- [ ] Echo alınabiliyor
- [ ] Q tuşu ile pulse çalışıyor
- [ ] Mouse scroll ile frekans değişiyor
