# ECHOES - El Feneri Kurulum Kılavuzu

## 🔦 The Forest Tarzı El Feneri Sistemi

**L tuşu ile açıp kapatabilirsiniz!**

---

## ⚡ Hızlı Kurulum (PlayerCapsule için)

### Adım 1: PlayerCapsule Prefab'ı Aç

1. Unity'de: `Assets/Prefabs/PlayerCapsule.prefab` (veya nerede ise)
2. Prefab'ı aç veya Hierarchy'de PlayerCapsule objesini seç

### Adım 2: FlashlightController Ekle

1. PlayerCapsule'ı seçin
2. Inspector > **Add Component**
3. **FlashlightController** yazıp enter

### Adım 3: Test Et!

1. Play'e basın
2. **L tuşuna** basın
3. El feneri eline gelsin, ışık açılsın ✅
4. Tekrar **L tuşuna** basın
5. El feneri kapansın ✅

**Hepsi bu kadar!** Script otomatik olarak her şeyi kurar.

---

## 🎮 Nasıl Çalışır

### L Tuşu İle Toggle

- **İlk L:** El feneri eline gelir, ışık açılır
- **İkinci L:** El feneri aşağı iner, ışık kapanır
- **Smooth animasyon** ile yumuşak geçiş

### Işık Özellikleri

- **SpotLight** (konik ışık huzmesi)
- **Renk:** Warm white (sıcak beyaz)
- **Intensity:** 3 (ayarlanabilir)
- **Range:** 15m (ayarlanabilir)
- **Spot Angle:** 45° (ayarlanabilir)
- **Soft Shadows:** Aktif

### Bonus - Karanlık Telafisi

El feneri açıldığında:
- Post-processing **exposure +0.5** artar
- Ortam daha parlak görünür
- El feneri kapatıldığında normal karanlığa döner

---

## 🔧 Inspector Ayarları

### Controls
- **Toggle Key:** L (değiştirebilirsiniz)

### Flashlight Settings
- **Equip Speed:** 5 (animasyon hızı)

### Light Settings
- **Light Color:** Warm white
- **Light Intensity:** 3 (daha parlak isterseniz artırın)
- **Light Range:** 15m (menzil)
- **Spot Angle:** 45° (ışık açısı)

### Position Settings
- **Equipped Position:** (0.3, -0.2, 0.5) - Elde pozisyon
- **Equipped Rotation:** (0, 0, 0)
- **Unequipped Position:** (0.3, -1, 0.5) - Ekran dışı

### Darkness Compensation
- **Adjust Exposure:** ✓ (açık/kapalı)
- **Exposure Boost:** 0.5 (ne kadar parlak olacak)

---

## 🎨 3D Model Ekleme (İsteğe Bağlı)

Şu an geçici bir **küp** kullanılıyor. Kendi modelinizi eklemek için:

### Yöntem 1: Inspector'da Manuel

1. PlayerCapsule > FlashlightController component
2. **Flashlight Object** alanını genişletin
3. Hierarchy'de görünen "Flashlight" objesini bulun
4. İçindeki "FlashlightModel_TEMP" objesini silin
5. Kendi el feneri modelinizi buraya sürükleyin

### Yöntem 2: Script İle

```csharp
// Başka bir scriptden:
FlashlightController flashlight = GetComponent<FlashlightController>();
flashlight.SetFlashlightModel(yourFlashlightPrefab);
```

**Model Gereksinimleri:**
- Forward axis: +Z (ışık yönü)
- Modelin pivot'u handle (sap) tarafında olmalı
- Scale: Uygun boyut (0.1-0.2 arası genelde iyi)

---

## 📝 Kod Örnekleri

### Başka Scriptlerden Kontrol

```csharp
// El fenerini zorla aç
FlashlightController flashlight = player.GetComponent<FlashlightController>();
flashlight.ForceEquip();

// El fenerini zorla kapat
flashlight.ForceUnequip();

// El feneri açık mı kontrol et
if (flashlight.IsEquipped())
{
    Debug.Log("Flashlight is on!");
}
```

### Pil Sistemi Eklemek İsterseniz

FlashlightController.cs'e ekleyebilirsiniz:

```csharp
[SerializeField] private float batteryLife = 100f;
[SerializeField] private float batteryDrain = 5f; // per second

void Update()
{
    if (isEquipped && batteryLife > 0)
    {
        batteryLife -= batteryDrain * Time.deltaTime;
        
        if (batteryLife <= 0)
        {
            ForceUnequip();
        }
    }
}
```

---

## 🐛 Sorun Giderme

### "L tuşu çalışmıyor"
- Console'da hata var mı kontrol edin
- PlayerCapsule aktif mi?
- Script enabled mi?

### "El feneri görünmüyor"
- Flashlight Object otomatik oluşturulmuş mu?
- Camera referansı doğru mu?
- Scene view'da Flashlight objesi var mı?

### "Işık çalışmıyor"
- Light component oluşturulmuş mu?
- Light enabled oluyor mu? (L'ye basınca)
- Range çok küçük olmadığından emin olun

### "Position yanlış"
- Inspector'da Position Settings'i ayarlayın
- Farklı karakter modelleri için farklı pozisyonlar gerekebilir

### "Exposure değişmiyor"
- Global Volume var mı?
- DefaultVolumeProfile atanmış mı?
- ColorAdjustments aktif mi?

---

## ✨ Gelecek Geliştirmeler

İsterseniz ekleyebilecekleriniz:

- [ ] **Pil sistemi** (yukarıda örnek kod var)
- [ ] **Titreme efekti** (korku için)
- [ ] **Açma/kapama sesi**
- [ ] **On/off animasyonu** (karakter için)
- [ ] **Farklı ışık modları** (weak/normal/strong)
- [ ] **Kırmızı ışık modu** (gece görüşü)

---

## 📊 Sahne Yapısı

```
PlayerCapsule
├── CharacterController
├── PlayerController
├── FlashlightController ← YENİ! ✅
├── CameraHolder
│   └── Camera
│       └── Flashlight (Otomatik oluşturulur) ← YENİ! ✅
│           ├── FlashlightModel_TEMP (Geçici)
│           └── Light (SpotLight)
└── ...
```

---

## 🎬 Sonuç

**El feneri sistemi hazır!**

✅ L tuşu ile toggle  
✅ Smooth animasyon  
✅ SpotLight ile ışık  
✅ Exposure kompensasyonu  
✅ Model değiştirilebilir  
✅ The Forest tarzı  

**Hemen test edebilirsiniz!** 🔦👻
