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

### Model 3D bawaan (dibuat langsung di Unity)
11 organ (mulut, kerongkongan, lambung, hati, pankreas, usus halus, usus besar, paru-paru, jantung, ginjal, otak)
dibuat per bagian dengan warna & titik info sendiri, ±40–50 ribu segitiga per model (ramah HP).
Close-up punya dua tampilan: **Organ** dan **Di tubuh** (tubuh anak transparan sesuai gender, organ terpilih menyala,
organ lain pudar). Letak organ di tubuh diatur di `OrganDefaults.BodyPlacement`. Model tubuh: `Models/Tubuh`.

### Library 3D, bagian organ & Info Lengkap
- **Halaman awal Library**: gambar anak 2D (laki-laki/perempuan); ketuk Kepala/Leher/Dada/Perut untuk melihat organnya.
  Organ di dalam tubuh (tampilan "Di tubuh") memakai versi ringan (`*_Low.prefab`) supaya aman untuk HP.
- **Bagian-bagian organ** (close-up): kartu tiap bagian (mis. trakea, bronkus, alveolus) dengan gambar & fungsinya.
  Memilih bagian akan menyorotnya di model 3D (bagian lain pudar, kamera mendekat). Bagian yang sangat kecil
  (alveolus, vili, nefron, sel saraf) ditampilkan sebagai model mikro yang diperbesar (`Models/Mikro`).
  Bagian yang tersembunyi di dalam organ (pohon bronkus) punya mesh "Inner" yang hanya tampil saat disorot.
- **Fakta menarik**: satu fakta acak setiap kali halaman dibuka; tombol *Fakta lain* mengacak lagi.
- **Rail & daftar** bisa digeser dengan menekan lalu menarik (`UI/DragScroll.cs`), tidak hanya dengan scroll.
- **Kuis drag & drop** (`UI/DragSource.cs`): seret organ ke titik di tubuh, dan seret jawaban ke kotak jawaban (ketuk tetap bisa).
- **Season kuis** (`Data/QuizBank.cs`): 6 season yang semuanya bisa dimainkan (Pemanasan, Pencernaan, Pernapasan,
  Peredaran Darah, Ekskresi, Saraf & Indera), masing-masing 5 soal. Season selesai & skor terbaik dihitung dari riwayat
  (`QuizRecord.quizId`, mis. `season:napas`); "Ulangi kuis" memulai kuis yang sama. Latihan per jenis soal mengambil soal
  dari semua season. Titik tempel label ada di `QuizPlayScreen.LabelSpots`.
- **Teks seperlunya**: tidak ada teks yang menjelaskan tampilan, fitur "segera hadir", atau notifikasi ganda. Petunjuk gerakan
  ("Geser untuk memutar", "Putar dengan jari") hanya tampil beberapa kali pertama (`UI/Hints.cs`).
- **Paru-paru** kini lengkap dengan hidung, faring, laring & kerongkongan; **jantung** punya model sel darah merah (+ sel darah putih & keping darah).
- **Info Lengkap**: gambar organ, apa itu, tugas utama, gambar & penjelasan tiap bagian, cara kerja, fakta, tips sehat.
- **Lihat dalam**: tombol *Organ | Dalam | Tubuh* di close-up. "Dalam" menampilkan organ yang dipotong
  (bilik & katup jantung, korteks/medula ginjal, lipatan lambung, lensa & retina mata, konka hidung, pita suara, dst).
  Bagian yang hanya ada di dalam otomatis membuka tampilan ini saat dipilih. Model potongan dibuat oleh
  `Scripts/Editor/OrganInsideBuilder.cs` (menu **Organo > Model 3D > Buat Model Lihat Dalam**), slot `insidePrefab` di asset organ.
- **Organ baru**: mata (Sistem Indera), hidung & tenggorokan (Pernapasan), dan mulut versi lengkung gigi
  (gigi seri, taring, geraham, lidah, langit-langit, anak tekak) + potongan gigi (email, dentin, pulpa).
  Acuan isi: KidsHealth & CHOP; dibuat di `Scripts/Editor/OrganNewModels.cs`.
- **Favoritku**: ikon bookmark menyimpan organ ke bagian *Favoritku* di Beranda dan chip *Favorit* di Library.
- **Gestur**: semua sheet bisa ditarik (sheet hasil scan naik/turun, sheet lain tarik ke bawah untuk menutup),
  rail & halaman bisa digeser dengan menarik (`UI/Gestures.cs`, `UI/DragScroll.cs`).
  Snapshot menjalankan uji interaksi otomatis, hasilnya di log dengan awalan `[Organo][Uji]`.
- Teks bagian, cara kerja, fakta & tips ada di `Scripts/Data/OrganContent.cs` (indeks material = submesh model).
- Gambar bagian (`Images/Parts`) dirender dari model: **Organo > Model 3D > Render Gambar Bagian Organ**.
  Anak 3D & model mikro: **Organo > Model 3D > Buat Aset Library**.


Lambung, Jantung, Paru-paru, dan Ginjal dibangun dari bentuk halus (SDF) lalu dijadikan mesh, tanpa software 3D:
`Models/Lambung`, `Models/Jantung`, `Models/ParuParu`, `Models/Ginjal`. Bentuknya bisa diubah di
`Scripts/Editor/StomachModelBuilder.cs` dan `Scripts/Editor/OrganModelBuilder.cs`, lalu jalankan
**Organo > Model 3D > Buat Ulang Semua Model**. Model ini otomatis dipakai untuk close-up dan AR.

## Cek tampilan tanpa HP
Menu **Organo > Snapshot Semua Layar** merender semua layar ke `Library/OrganoPreview/ui_*.png`.

## Style guide
Warna, tipografi, radius, elevasi (bayangan), tombol, chip, input, switch, kartu organ, lencana, navbar
mengikuti *Style Guide Organo*. Token warna ada di bagian atas `UI/Organo.uss`; elevasi di `Scripts/UI/ShadowElement.cs`.
Ikon, maskot, dan ilustrasi organ diambil langsung dari SVG desain (`Scripts/UI/Svg/OrganoIcons.Generated.cs`).

## Font
Poppins (Regular, Medium, SemiBold) ada di `Fonts/`, berlisensi SIL Open Font License (`Fonts/OFL.txt`).
Font dipasang di komponen **Organo App** (field Font Regular / SemiBold / Medium); teks tebal otomatis memakai
Poppins SemiBold (600) sesuai style guide. Untuk scene lama, jalankan **Organo > Pasang Font Poppins**.

## Struktur kode
- `Scripts/Core` – `OrganoApp` (navigasi, popup, safe area), `AppScreen`, audio.
- `Scripts/UI/Screens` – semua layar: pembuka (splash, onboarding, kenalan), beranda, library, close-up, scan, kuis, profil.
- `Scripts/ThreeD` – studio close-up 3D (`OrganViewer3D` → RenderTexture → `OrganViewport`).
- `Scripts/AR` – `ARScanController` (image tracking kartu, senter, deteksi gelap) & izin kamera.
- `Scripts/UI/Svg` – ikon/maskot/organ dari desain digambar langsung dari SVG.

## Kartu AR, progres, suara & build HP
- **Kartu AR**: desain 15 kartu cetak ada di `AR/Cards/{id}.png` (sudah dipasang ke slot *Card Image* & `AR/OrganoCardLibrary`,
  ukuran fisik 6,3 x 8,8 cm). Halaman cetak A4 300 dpi ada di folder proyek `KartuCetak/` — cetak di kertas A4,
  skala 100% / "Sesuaikan halaman", lalu potong mengikuti garis. Ubah desain di `Scripts/Editor/OrganoCards.cs`,
  render ulang dengan `OrganoSnapshots.RunCardsAndQuit`, lalu menu **Organo > Kartu AR > Pasang Gambar Kartu & Bangun Library**.
- **Koleksi kartu** (`Scripts/UI/Screens/CardScreens.cs`): album per sistem organ dengan gambar kartu asli,
  progres tiap set, dan penampil kartu (ketuk = balik ke data organ, geser = kartu lain, tombol Lihat 3D /
  Dengarkan / Info Lengkap / Scan AR). Dibuka dari Beranda, Profil, hasil scan, dan hasil kuis.
- **Rangka** (`Editor/OrganSkeletonModel.cs`, sistem "Gerak", kartu 15 yang dicetak & bisa di-scan): rangka anak ramah anak
  (tulang krem membulat, rongga mata dangkal, tulang rawan biru muda; 12 pasang rusuk, 27 tulang tangan disederhanakan).
  Model "Dalam" = tulang paha dibelah (tulang keras, tulang spons, sumsum merah & kuning, tulang rawan sendi).
  **Aturan penempatan tulang supaya organ tidak tertutup:** close-up organ (mode "Organ" & "Dalam") tidak pernah memakai
  tulang; di mode "Tubuh" ada tombol **Tulang** (mati di awal) yang menampilkan rangka tembus pandang tipis sehingga organ
  yang menyala tetap terlihat; rangka berwarna penuh hanya saat organ Rangka sendiri dibuka. Rangka dibuat di koordinat
  model anak 3D, jadi prefab yang sama dipakai untuk close-up, mode tubuh, dan AR. Peta tubuh 2D punya tombol tulang.
- **Uji scan tanpa HP**: **Organo > Kartu AR > Simulasi XR: Meja dengan Semua Kartu** membuat meja kelas berisi semua
  kartu cetak (ukuran asli) di XR Simulation. Matikan *Use Webcam In Editor* di ARScanController, Play, buka Scan:
  AR Foundation melacak kartu dengan library yang sama seperti di HP dan model 3D muncul di atas kartu
  (uji otomatis `9a/9b` di OrganoSnapshots). **Catatan:** XR Simulation tidak meniru batas kamera HP sungguhan.
- **Syarat scan di HP (pelajaran penting)**: ARCore hanya mengenali gambar yang memenuhi **minimal 25% gambar kamera**
  (developers.google.com/ar/develop/augmented-images) dan memakai gambar hitam-putih. Bingkai scan lama (persegi kecil
  + gambar kartu mini) membuat kartu hanya ±8% gambar kamera sehingga scan di HP gagal. Sekarang `CardFrame`
  (`UI/Screens/ScanScreens.cs`) berbentuk kartu 63×88 mm selebar ±88% layar (kartu ±31–41% gambar kamera), dipakai di
  Scan & Misi Scan; uji `90b`/`90f` menjaga angka ini ≥ 25%. Petunjuk: dekatkan HP sampai kartu memenuhi bingkai
  (kartu cetak ±15 cm, kartu di layar ±20 cm). Kartu yang baru dikenali ARCore langsung dihitung walau posisinya belum
  *Tracking* (kartu di layar laptop), model ditempel bila posisinya sudah pasti. Status ARCore (mis. "gerakkan HP pelan")
  tampil di layar Scan, dan tombol **?** punya *Info teknis* (status sesi, resolusi kamera, kartu yang dilihat ARCore)
  yang bisa disalin guru. Log HP: `adb logcat -s Unity | findstr Organo`.
- **Uji pengenalan kartu tanpa HP** (`Tools/uji_pengenalan_kartu.py`, butuh `pip install opencv-python-headless`):
  meniru foto kamera HP (abu-abu 640×480, miring, buram, noise, layar laptop dengan silau/moiré; `--berat` untuk HP
  goyang & kontras rendah) dan mencocokkan ke ke-30 kartu sekaligus. Hasil 2026-10-11 (kondisi berat): kartu 8% gambar
  kamera hanya 0–8% dikenali, 25% → 87–100%, 40% → ±100%; tidak pernah tertukar dengan kartu lain.
- **APK Android**: **Organo > Build > APK Android** → `Builds/Android/Organo.apk` (paket `com.organo.belajarorgan`,
  ARM64, minSdk 25, ARCore opsional, depth ARCore opsional). Ringkasan di
  `Library/OrganoPreview/build_android.txt`.
- **Tata letak di semua HP** (desain asli untuk iPhone 390×844 dengan area aman notch/home bar): area aman minimum
  `OrganoApp.MinTopInset/MinBottomInset` (28/14) untuk HP Android yang melaporkan 0; `AppScreen.KeepBarsFixed` membuat
  hanya area gulir yang boleh mengecil (bar bawah tidak lagi dimampatkan sehingga tombol keluar layar / teks keluar kotak);
  panggung tubuh Library & kartu 3D detail menyesuaikan tinggi layar. Uji: `-executeMethod
  Organo.EditorTools.OrganoSnapshots.RunLayoutAndQuit` (tanpa batchmode) membuka 22 layar di iPhone (47/34), Android
  390×866 dan HP kecil 390×693, menyimpan `Library/OrganoPreview/ui_L_*.png`, lalu mengecek tombol menempel/keluar tepi,
  teks terpotong / keluar kotak, dan bingkai scan ≥ 25% gambar kamera.
- **Suara**: tombol tidak berbunyi klik (`OrganoAudio.ClickSound = false`; bunyi benar/salah & kartu dikenali tetap).
  Pembaca (TTS) Android memakai mesin Google Speech Services bila ada, suara Indonesia terpasang terbaik, nada 1,05 &
  tempo 0,92; pilihan suara (dengan tombol Coba) di Profil > Aksesibilitas > Suara pembaca. iOS memakai suara id-ID
  kualitas tertinggi. Sorotan kalimat saat dibacakan memakai `<mark>` transparan (mark digambar di atas teks).
  Suara pembaca bisa dimatikan (Profil > Suara Pembaca, atau speaker di samping Dengarkan; `PlayerProgress.narration`);
  mengetuk model 3D tidak lagi membacakan (sering tersentuh tanpa sengaja), Dengarkan berubah jadi Berhenti saat membaca.
- **Hasil scan**: model AR berdiri di atas kartu seperti benda sungguhan (`ARScanController.UpdateModelPose`): saat
  pertama muncul menghadap kamera dengan tampak depan yang sama seperti close-up, setelah itu posisi & arahnya tetap
  terhadap kartu, jadi makin jauh makin kecil dan HP bisa memutari kartu. Geser jari memutar model 360° ke segala arah
  (`RotateSpawned`; viewer 3D hasil scan juga, `OrganViewer3D.FreeRotate`). Kartu baru cukup diberi kabar kecil yang
  hilang sendiri (tidak lagi menutupi organ). Splash lanjut sendiri setelah siap. Saat kartu terlalu jauh / keluar layar
  model tetap di posisi terakhir kartu (hanya disembunyikan kalau ARCore kehilangan kartu sama sekali), posisinya
  dihaluskan dan ukurannya mengikuti ukuran kartu. Build Android melepas patokan ukuran kartu 6,3 cm (`OrganoSetup.SetCardLibrarySpecifySize`)
  supaya ARCore memperkirakan ukuran kartu sebenarnya (kartu di layar laptop lebih besar → dulu model melayang & bergeser);
  iOS & simulasi XR tetap memakai ukuran. Kartu yang posisinya belum pasti tampil di viewer 3D dulu, lalu otomatis
  berganti ke AR begitu ARCore melacaknya.
- **Kartu di layar laptop**: kamera sering menangkap layar terlalu terang sehingga warna pastel kartu hilang (pengenalan
  16%). Situs "Scan dari layar" menampilkan kartu dengan `contrast(1.4) brightness(0.65)` (pengenalan 89%; tombol
  "Mode scan"/"Warna asli") dan menyarankan kecerahan layar sedang. Uji: `Tools/uji_pengenalan_kartu.py --berat --terang --redup`.
- **Panggul**: peta tubuh Library punya area Panggul sendiri (organ reproduksi sesuai tubuh yang tampil, tidak digabung
  ke Perut). Di model anak 3D pinggang celana/rok diturunkan ke pinggul dan organ perut dinaikkan ke rongga perut.
- **Narasi suara AI** (lebih manusiawi): **Organo > Suara > Ekspor Teks Narasi** → `Tools/suara/narasi.json`
  (116 teks, ±12 ribu karakter), lalu `python Tools/suara/buat_suara.py --layanan elevenlabs|azure` (kunci API di
  `Tools/suara/kunci.txt`, contoh di `kunci.contoh.txt`, tidak ikut git) → `Assets/Organo/Audio/Narasi`, lalu
  **Organo > Suara > Pasang Rekaman Narasi**. Organ tanpa rekaman tetap dibacakan suara HP.
- **Ikon aplikasi** (`Assets/Organo/Icon`, dibuat `python Tools/ikon/buat_ikon.py`): maskot Organo di dalam sudut
  bingkai scan di atas gradasi teal–ungu; Android adaptive icon (lapisan belakang + depan dalam zona aman), ikon bulat,
  dan ikon biasa. Dipasang otomatis tiap build oleh `OrganoAppIcon.Apply` (menu **Organo > Build > Pasang Ikon Aplikasi**).
- **Organ reproduksi** (`Data/ReproAccess.cs`, model di `Editor/OrganReproModels.cs`): Reproduksi Laki-laki (kartu 16)
  & Perempuan (kartu 17), sistem "Reproduksi", letak "Panggul" (di peta tubuh ikut area Perut sesuai tubuh yang tampil).
  Aturan: anak selalu bisa mempelajari organ reproduksi tubuhnya sendiri; organ gender lain **terkunci** (tanpa model,
  ikon, maupun gambar) sampai ia menyelesaikan kuis reproduksi miliknya sendiri (Quiz > Topik: Organ Reproduksi).
  Tubuh belum dipilih = keduanya terkunci. Kartunya tidak dicetak & tidak ada di library AR — didapat dari kuisnya.
  Model bergaya buku pelajaran (skematis, warna lembut), ada potongan "Dalam" serta sel sperma & sel telur (mikro);
  mode "Tubuh" tidak dipakai untuk organ ini. Halamannya selalu memuat pesan "Tubuhku milikku" & tautan materi pubertas.
- **Isi organ**: teks (penjelasan, fakta, fungsi) diambil dari `Data/OrganDefaults.cs` & `Data/OrganContent.cs`;
  **Organo > Setup** menyamakan aset organ dengan kode, jadi koreksi isi cukup dilakukan di kode.
- **Dua gaya kartu**: Realistis (`AR/Cards`) dan Kartun (`AR/CardsKartun`), halaman cetaknya di
  `KartuCetak/Realistis` & `KartuCetak/Kartun`. **Keduanya dikenali aplikasi**: library AR berisi 30 gambar
  (gaya Kartun bernama `organo-<id>@kartun`, dipetakan ke organ yang sama oleh `OrganDatabase.FindByReferenceImage`).
  Menu **Organo > Kartu AR > Pakai Desain ...** hanya mengganti gambar kartu yang tampil di album Koleksi.
  Skor `arcoreimg eval-img` ARCore (0–100, disarankan ≥ 75): Realistis semua 100; Kartun 100 kecuali Usus Besar 90.
  Pola coretan tiap kartu dipilih dari 8 varian dengan skor terbaik, tersimpan di `AR/card_seeds.json`.
- **Situs kartu untuk guru** (`Web/kartu-guru/`, di luar Assets): halaman statis berisi PDF A4 siap cetak (kedua gaya),
  ZIP gambar, galeri 15 kartu, mode **Scan dari layar** (kartu besar untuk dipindai langsung dari laptop/tablet/HP lain,
  alamat `.../#scan`), dan alur di kelas (belajar di Library → scan kartu → kuis). Online di
  **https://organo-kartu.vercel.app** (proyek Vercel `organo-kartu`, tidak tersambung ke Git — deploy manual).
  Perbarui dengan `python Web/buat_situs_kartu.py` lalu `npx vercel deploy --prod` dari folder `Web/kartu-guru`
  (di jaringan dengan pemeriksaan HTTPS antivirus, set `NODE_OPTIONS=--use-system-ca`). Alamatnya sudah jadi bawaan
  *OrganoApp > Cards Website Url* dan muncul di lembar "Kartunya dari mana?" pada layar Scan.
- **Offline sepenuhnya, tanpa akun**: tidak ada Masuk/Daftar/Mode Tamu. Progres (bintang, kartu, kuis, pengaturan)
  disimpan di HP ini saja (`PlayerProgress`, PlayerPrefs `organo.progress.local`). Pertama kali dibuka: splash →
  onboarding → *Kenalan Dulu* (nama panggilan & gender) → Beranda; berikutnya langsung ke Beranda. Nama bisa diubah
  dengan mengetuk nama di Profil. Progres dari versi yang masih memakai akun (profil terakhir dipakai) dipindahkan
  otomatis sekali. Kode akun lama disimpan di folder proyek `Arsip/akun-lokal` (di luar Assets, tidak ikut build).
  Scan demo tanpa kamera (ketuk bingkai = kartu didapat) hanya ada di Editor / Development Build.
- **Suara**: efek klik/benar/salah disintesis bila slot audio kosong; narasi organ memakai rekaman (slot *Narration*)
  atau text-to-speech HP berbahasa Indonesia (`Core/OrganoSpeech.cs`, `Plugins/iOS/OrganoSpeech.mm`).
- **Build HP**: menu **Organo > Terapkan Pengaturan Build HP** (Android min API 25, teks izin kamera iOS).
  ARCore *Optional*: HP tanpa AR memakai kamera biasa. Perlu modul *Android Build Support* / *iOS Build Support* di Unity Hub.
- **Ukuran aplikasi**: **Organo > Ukuran Aplikasi > Buat Laporan Ukuran** (build Windows untuk mengukur isi aset,
  hasilnya di `Library/OrganoPreview/ukuran_aplikasi.txt`). Setelah model 3D dibuat ulang, jalankan lagi
  **Organo > Ukuran Aplikasi > Rampingkan Mesh Model 3D** (buang tangent + kompresi mesh).
