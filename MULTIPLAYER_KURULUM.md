# ECHOES: FRAGMENTED - Multiplayer Kurulum Rehberi

## ?? Kurulum Adýmlarý

### 1?? Player Prefab'ý Ayarla

Unity Editor'de:
1. Üst menüden **Tools > ECHOES > Setup Player Prefab** seçeneðine týkla
2. Console'da baþarý mesajýný bekle: `??? Player prefab baþarýyla ayarlandý! ???`

Bu iþlem otomatik olarak:
- ? CharacterController ekler (Rigidbody'yi kaldýrýr)
- ? PlayerCamera child objesi oluþturur (göz seviyesinde, FOV 80)
- ? AudioListener ekler
- ? Ýsim etiketi UI'sý oluþturur (NameTag)
- ? PlayerNameTag scriptini ekler ve baðlar

### 2?? Network UI Oluþtur

Unity Editor'de:
1. Üst menüden **Tools > ECHOES > Create Network UI** seçeneðine týkla
2. Console'da baþarý mesajýný bekle: `??? Network UI baþarýyla oluþturuldu! ???`

Bu iþlem otomatik olarak:
- ? NetworkCanvas prefab'ý oluþturur
- ? Ana menü paneli (host/client butonlarý, IP giriþi)
- ? Lobi paneli (oyuncu sayýsý, durum)
- ? HUD paneli (oyun içi bilgi)
- ? EventSystem ekler

**Not**: Eðer NetworkCanvas sahneye eklenmiþse, onu kullan. Yoksa Prefabs klasöründen sahneye sürükle.

### 3?? Sahneyi Ayarla

1. **Sahneye NetworkCanvas ekle** (yoksa):
   - `Prefabs/NetworkCanvas.prefab` dosyasýný Hierarchy'ye sürükle

2. **LobbyManager ekle**:
   - Hierarchy'de sað týk > Create Empty
   - Ýsimlendir: "LobbyManager"
   - Inspector'da Add Component > LobbyManager

3. **NetworkManager kontrol et**:
   - Hierarchy'de NetworkManager nesnesini seç
   - Player Prefab olarak `Assets/Prefabs/Player.prefab` seçili olmalý
   - Network Transport'ta IP: 127.0.0.1, Port: 7777 olmalý

4. **Spawn noktasý ayarla** (opsiyonel):
   - Boþ bir GameObject oluþtur, isimlendir: "SpawnPoint"
   - Ýstediðin pozisyona yerleþtir

---

## ?? Oyunu Çalýþtýrma

### Tek Bilgisayarda Test (ParrelSync ile)

#### ParrelSync Kurulumu:
1. Window > Package Manager
2. Sol üstten "+" > Add package from git URL
3. URL: `https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync`
4. Add

#### Test:
1. **Ana proje**: ParrelSync > Clones Manager > Create new clone
2. **Ana proje**: Play > Host Baþlat
3. **Klon proje**: Play > IP: 127.0.0.1 > Client Baðlan
4. ? Her iki oyuncunun da hareket ettiðini gör!

### Ýki Farklý Bilgisayarda Test

#### Host bilgisayar:
1. Play moduna gir
2. "Host Baþlat" butonuna týkla
3. Host IP adresini öðren (CMD'de `ipconfig` komutuyla)

#### Client bilgisayar:
1. Play moduna gir
2. IP alanýna host'un IP'sini gir (örn: 192.168.1.100)
3. "Client Baðlan" butonuna týkla

---

## ?? Kontroller

| Tuþ | Aksiyon |
|-----|---------|
| W/A/S/D | Hareket |
| Shift | Koþ |
| Space | Zýpla |
| Mouse | Etrafýna bak |
| ESC | Fareyi serbest býrak/kilitle |

---

## ?? Oluþturulan Scriptler

### Oyuncu Sistemi:
- **`PlayerMovement.cs`** - Birinci þahýs hareket ve kamera kontrolü
- **`PlayerNameTag.cs`** - Ýsim etiketi, oyuncu renkleri

### Network Sistemi:
- **`NetworkUI.cs`** - UI yönetimi (host/client/disconnect)
- **`LobbyManager.cs`** - 4 oyuncu limiti, baðlantý yönetimi

### Editor Toollarý:
- **`PlayerPrefabSetup.cs`** - Player prefab otomatik kurulum
- **`NetworkUISetup.cs`** - Network UI otomatik oluþturma

---

## ?? Bilinen Sorunlar & Çözümler

### Problem: "GetComponentInChildren<Camera>() returned null"
**Çözüm**: Player prefab'ý henüz ayarlanmamýþ. Tools > ECHOES > Setup Player Prefab çalýþtýr.

### Problem: Butonlar çalýþmýyor
**Çözüm**: EventSystem eksik. Tools > ECHOES > Create Network UI tekrar çalýþtýr.

### Problem: 5. oyuncu baðlanabiliyor
**Çözüm**: LobbyManager sahneye eklenmiþ mi kontrol et.

### Problem: Oyuncular spawn olmuyor
**Çözüm**: NetworkManager'da Player Prefab referansý doðru mu kontrol et.

---

## ?? Sonraki Adýmlar

MVP tamamlandýktan sonra eklenecekler:
- [ ] Sesli sohbet (Voice chat)
- [ ] Proximity chat (yakýnlýk tabanlý ses)
- [ ] Echo sistemi (fragmented reality)
- [ ] Akýl saðlýðý (sanity) sistemi
- [ ] Varlýk (entity) senkronizasyonu
- [ ] Harita deðiþkenliði
- [ ] Dedicated server desteði

---

## ?? Baþarý!

Eðer tüm adýmlarý tamamladýysan, artýk 4 kiþilik co-op multiplayer sistemi hazýr!

Test et:
1. ? Host baþlat
2. ? Client baðlan
3. ? Her iki oyuncu da WASD ile hareket edebilmeli
4. ? Kamera fare ile dönmeli
5. ? Ýsim etiketleri görünür olmalý
6. ? Oyuncu renkleri farklý olmalý

**Sorun yaþarsan**: Console'u aç (Ctrl+Shift+C) ve hatalarý kontrol et.
