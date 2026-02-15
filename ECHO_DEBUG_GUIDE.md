# Echo Device Q Tuşu Debug Rehberi

## 🐛 Sorun: Q Tuşu Çalışmıyor

Echo cihazı alınıyor ama Q tuşu hiçbir şey yapmıyor.

## 🔍 Debug Adımları

### 1. Unity Console'u Aç
`Window → General → Console` (Ctrl+Shift+C)

### 2. Play Mode'a Gir ve Echo'yu Al
1. Play'e bas
2. Çekmceyi aç
3. Echo cihazını al (E tuşu)

**Konsoldaki Beklenen Loglar:**
```
[EchoPickupItem] Echo Cihazı collected and equipped!
[EchoDevice] Added EchoDevice component to player
[EchoDevice] EquipEchoDevice called! hasDevice = true
[EchoDevice] Camera found: [Kamera ismi]
[EchoDevice] Created EchoPulseEffect on camera
[EchoDevice] Device equipped and ready! hasDevice=True, pulseEffect=True
```

### 3. Q Tuşuna Bas
**Durum A: Q tuşu algılanmıyor**
Console'da hiçbir şey görünmüyorsa:
```
❌ SORUN: Input sistemi çalışmıyor veya hasDevice = false
```

**Kontrol Et:**
- Player GameObject'inde `EchoDevice` component'i var mı?
- Inspector'da `Has Device` checkbox işaretli mi?

**Durum B: "Q pressed but hasDevice = false!"**
```
⚠️ SORUN: EchoDevice component var ama hasDevice flag'i false
```

**Çözüm:**
- `EchoPickupItem` scriptinin `EquipEchoDevice()` metodunu çağırdığından emin ol
- Inspector'da manuel olarak `Has Device` checkbox'ını işaretle (geçici test için)

**Durum C: "Q pressed but cursor is unlocked"**
```
⚠️ SORUN: Menü açık, Cursor locked değil
```

**Çözüm:**
- ESC menüsünü kapat
- Oyun içinde olduğundan emin ol (cursor görünmemeli)

**Durum D: "Q Key pressed!" görünüyor**
```
✅ İYİ: Q algılanıyor, devam et
```

### 4. Cooldown veya Pil Kontrolü
**"Pulse on cooldown"** görüyorsan:
- İlk pulse'tan sonra 2 saniye bekle

**"Insufficient battery!"** görüyorsan:
- Inspector'da `Current Battery` değerini 100 yap

### 5. ActivatePulse Kontrolü
**"ActivatePulse() called!"** görünüyorsa:
```
✅ İYİ: Pulse fonksiyonu çalışıyor
```

**Sonraki satırlar:**
- "Pulse effect triggered!" → ✅ Görsel efekt çalışmalı
- "pulseEffect is NULL!" → ❌ SORUN: Pulse effect yok

## 🛠️ Çözümler

### Sorun: pulseEffect is NULL
**Neden:** Camera'ya `EchoPulseEffect` component'i eklenmemiş

**Çözüm 1 (Otomatik):**
```csharp
// EquipEchoDevice() metodu şimdi otomatik ekliyor
// Eğer çalışmıyorsa:
```

**Çözüm 2 (Manuel):**
1. Player → CameraHolder → Camera GameObject'ini seç
2. `Add Component` → `Echo Pulse Effect`
3. Tekrar dene

### Sorun: Shader Hatası
**Console'da shader hatası varsa:**
```
Shader error in 'Hidden/EchoEdgeDetection'...
```

**Çözüm:**
1. Project → Assets/Shaders/EchoEdgeDetection.shader'a tıkla
2. Import Settings kontrol et
3. Tekrar import et (sağ tık → Reimport)

### Sorun: Camera Bulunamıyor
**"Camera found: NULL" görüyorsan:**

**Çözüm:**
1. Player GameObject'inin child'ları arasında Camera var mı kontrol et
2. Camera'nın Tag'i "MainCamera" olmalı
3. Veya Camera'nın direkt Player'ın child'ı olmalı

## 📊 Hızlı Test Checklist

- [ ] Echo alındığında "EquipEchoDevice called!" logu var
- [ ] "hasDevice = true" logu var
- [ ] "Camera found: [isim]" logu var (NULL değil)
- [ ] "Created EchoPulseEffect on camera" logu var
- [ ] Q'ya basınca "Q Key pressed!" logu var
- [ ] "ActivatePulse() called!" logu var
- [ ] "Pulse effect triggered!" logu var
- [ ] Ekranda görsel efekt görünüyor

## 💡 Son Kontrol

Eğer tüm loglar doğru ama görsel efekt yok ise:

**Shader Kontrol:**
```
Assets/Shaders/EchoEdgeDetection.shader dosyası var mı?
```

**Camera Depth Texture:**
```csharp
// EchoPulseEffect.Start() içinde otomatik ayarlanıyor:
cam.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
```

**Fallback Test:**
1. `EchoPulseEffect.cs` içindeki `OnRenderImage` metoduna breakpoint koy
2. Çağrılıyor mu kontrol et

## 🎯 Beklenen Sonuç

Q tuşuna basıldığında:
1. Console'da bir dizi log mesajı ✅
2. Ekranda merkezden dışarı genişleyen dalga ✅
3. Duvarların wireframe edge'leri görünür ✅
4. 2 saniye sonra tekrar kullanılabilir ✅

---

**Hala çalışmıyorsa:** Console'daki BÜTÜN log mesajlarını bana gönder!
