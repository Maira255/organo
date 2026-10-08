# Organo – aplikasi AR (Unity 6 · UI Toolkit · AR Foundation)

Implementasi desain canvas Organo di Unity. Semua layar dibangun dengan UI Toolkit (C# + `UI/Organo.uss`).

## Mulai
1. Menu **Organo > Setup Scene & Asset** (otomatis dijalankan sekali saat pertama kali dikompilasi).
2. Menu **Organo > Buka Scene Organo**, lalu **Play**. Di Editor, scan bisa dicoba dengan mengetuk bingkai scan (mode demo).
3. Build ke Android/iOS: scene `Scenes/Organo.unity` sudah ada di urutan pertama Build Settings.

## Slot model 3D close-up
Setiap organ punya asset di `Data/Organ_<id>.asset` (pilih cepat: **Organo > Pilih Asset Organ**).

| Field | Fungsi |
|---|---|
| **Model Prefab** | Model 3D untuk close-up (Library → organ). Kosong = placeholder dari primitive. |
| Model Euler / Model Scale | Rotasi awal & ukuran di viewer. Model otomatis dipusatkan dan diskalakan. |
| Hotspots | Titik info (mis. "Asam Lambung"). Atau taruh komponen `OrganHotspot` di dalam prefab. |
| Narration | Rekaman suara penjelasan; kalimat yang dibacakan ikut tersorot. |
| Card Image | Gambar kartu fisik untuk AR. Setelah diisi, jalankan **Organo > Bangun Library Kartu AR**. |
| AR Model Prefab / AR Model Size | Model untuk hasil scan (kosong = pakai Model Prefab). |

Taruh file model (FBX/GLB/prefab) di folder `Models/`.

## Font
Desain memakai **Poppins**. Saat ini memakai Inter (bawaan template AR). Untuk mengganti, salin `Poppins-Regular.ttf`
ke `Fonts/` lalu ubah `url(...)` di bagian atas `UI/Organo.uss`.

## Struktur kode
- `Scripts/Core` – `OrganoApp` (navigasi, popup, safe area), `AppScreen`, audio.
- `Scripts/UI/Screens` – semua layar: akun, beranda, library, close-up, scan, kuis, profil.
- `Scripts/ThreeD` – studio close-up 3D (`OrganViewer3D` → RenderTexture → `OrganViewport`).
- `Scripts/AR` – `ARScanController` (image tracking kartu, senter, deteksi gelap) & izin kamera.
- `Scripts/UI/Svg` – ikon/maskot/organ dari desain digambar langsung dari SVG.
