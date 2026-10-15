# Organo

**Aplikasi belajar organ tubuh untuk anak SD dengan kartu, Augmented Reality (AR), dan kuis.**

Anak mempelajari organ lewat model 3D yang bisa diputar dan dibedah, memindai (scan) **kartu Organo** sehingga organ
muncul di atas kartu, lalu menguji pemahamannya lewat **kuis bertahap** yang terbuka sedikit demi sedikit.
Aplikasi berjalan **sepenuhnya offline dan tanpa akun**: tidak ada pendaftaran, dan semua progres tersimpan di HP.

| Beranda | Pustaka | Halaman organ | Scan kartu |
|:---:|:---:|:---:|:---:|
| <img src="Dokumen/screenshots/08_beranda.jpg" width="180"> | <img src="Dokumen/screenshots/09_library.jpg" width="180"> | <img src="Dokumen/screenshots/10_detail_mulut.jpg" width="180"> | <img src="Dokumen/screenshots/18_scan.jpg" width="180"> |
| **Hasil scan** | **Kuis** | **Soal pilihan ganda 3D** | **Koleksi kartu** |
| <img src="Dokumen/screenshots/20_scan_hasil_baru.jpg" width="180"> | <img src="Dokumen/screenshots/14_kuis_hub.jpg" width="180"> | <img src="Dokumen/screenshots/16_kuis_pilihan.jpg" width="180"> | <img src="Dokumen/screenshots/21_koleksi.jpg" width="180"> |

> Penjelasan lengkap (fitur, isi materi, riwayat perubahan, hasil pengujian) ada di
> [Dokumen/Proposal_Organo.pdf](Dokumen/Proposal_Organo.pdf).

---

## Isi aplikasi

- **17 organ dari 8 sistem tubuh**: Mulut, Kerongkongan, Lambung, Usus Halus, Usus Besar, Hati, Pankreas (Pencernaan) ·
  Hidung, Tenggorokan, Paru-paru (Pernapasan) · Jantung (Peredaran Darah) · Ginjal (Ekskresi) · Otak (Saraf) ·
  Mata (Indera) · Rangka (Gerak) · Reproduksi Laki-laki & Perempuan (Reproduksi).
- **Model 3D** setiap organ dengan tiga tampilan: *Organ* (utuh), *Dalam* (organ dipotong), dan *Tubuh* (letaknya di
  tubuh anak). Bagian-bagian organ bisa disorot; bagian yang sangat kecil (alveolus, vili, nefron, sel darah)
  ditampilkan sebagai model mikro.
- **Info Lengkap** tiap organ: tugas utama, bagian-bagian, cara kerja, fakta menarik ("Tahukah Kamu?"), dan tips sehat,
  dengan **suara pembaca** berbahasa Indonesia (bisa dimatikan).
- **Scan kartu & AR**: organ muncul menempel di atas kartu, bisa diputar 360°, titik info bisa diketuk, dan organ
  berganti saat kartu lain diarahkan ke kamera. HP yang tidak mendukung AR tetap bisa mengenali kartu dengan kamera biasa.
- **Kuis bertahap**: 17 kuis organ + 6 kuis proses (5 soal per kuis) dengan tiga jenis soal: pilihan ganda 3D,
  tempel label, dan misi scan. Jawaban yang salah harus diulang sampai benar.
- **Koleksi kartu, bintang, dan lencana**; **Favoritku**; materi pubertas **"Tubuhku Tumbuh Besar"** dan pesan keamanan
  tubuh **"Tubuhku milikku"**.
- **Aksesibilitas**: ukuran teks, kontras tinggi, kurangi animasi, sorot kalimat yang dibacakan, kecepatan & pilihan suara.

---

## Cara memakai aplikasi

### 1. Memasang aplikasi (Android)
1. Salin file `Organo.apk` ke HP Android (Android 7.1 ke atas), lalu buka file tersebut.
2. Bila diminta, izinkan pemasangan dari sumber ini.
3. Buka **Organo**. Saat pertama kali dibuka, izinkan akses **kamera** (dipakai hanya untuk scan; gambar tidak disimpan).

> HP yang mendukung AR mungkin diminta memasang **Google Play Services for AR** sekali. Bila HP tidak mendukung AR,
> aplikasi otomatis memakai kamera biasa: kartu tetap dikenali dan organ tampil di layar 3D.

### 2. Pertama kali dibuka
Splash → tiga halaman pengenalan → **Perkenalan**: isi nama panggilan (boleh dikosongkan) dan pilih tubuh
laki-laki atau perempuan → Beranda. Setelah itu aplikasi selalu langsung membuka Beranda.

### 3. Belajar di Pustaka
1. Buka menu **Pustaka**, lalu ketuk bagian tubuh (kepala, leher, dada, perut, panggul) atau pilih organ dari daftar.
2. Di halaman organ: geser untuk memutar model, cubit untuk memperbesar, ketuk titik untuk melihat nama bagian.
3. Pilih tampilan **Organ / Dalam / Tubuh**, ketuk kartu bagian untuk menyorotnya, dan ketuk **Dengarkan** untuk
   mendengar penjelasan. Ikon speaker mematikan atau menyalakan suara pembaca.
4. Buka **Info Lengkap** untuk cara kerja, fakta, dan tips sehat.

### 4. Scan kartu
1. Siapkan kartu Organo: cetak dari situs kartu, atau tampilkan di laptop/tablet/HP lain (lihat bagian *Kartu Organo*).
2. Buka menu **Scan**, lalu **dekatkan HP sampai kartu memenuhi bingkai** (kartu cetak ±15 cm, kartu di layar ±20 cm).
   Tahan diam sebentar di tempat yang cukup terang.
3. Organ muncul di atas kartu dan kartu masuk **Koleksi**. Geser untuk memutar organ, ketuk titik hijau untuk melihat
   bagiannya, dan tarik lembar di bawah ke atas untuk info lengkap.
4. Arahkan kamera ke kartu lain untuk langsung berganti organ.

Tips bila kartu sulit dikenali: dekatkan HP, hindari pantulan lampu, nyalakan senter di ruangan gelap, dan untuk kartu
di layar pakai kecerahan sedang (jangan maksimal). Tombol **?** di layar Scan berisi tips dan *Info teknis* untuk guru.

### 5. Mengerjakan kuis
- Menu **Kuis** punya dua tab: **Kuis Organ** dan **Kuis Proses**.
- **Kuis organ** terbuka setelah **kartu organnya di-scan** atau setelah **kuis organ sebelumnya selesai**
  (Kuis Mulut terbuka sejak awal).
- **Kuis proses** (mis. Proses Pencernaan) terbuka setelah **semua kuis organ** dalam proses itu selesai.
- Ketuk kuis yang terkunci untuk melihat cara membukanya.
- Jawaban salah dicoret dan harus dicoba lagi sampai benar. Bintang dihitung dari jawaban benar pada percobaan pertama.
- Setelah selesai, lihat skor, bintang, kuis yang baru terbuka, **Pembahasan**, dan **Riwayat Kuis**.

### 6. Profil
Ubah nama panggilan (ketuk nama), lihat bintang, kartu, dan lencana, lalu atur **Materi Tubuh**, **Suara & Musik**,
**Suara Pembaca**, dan **Mode Aksesibilitas**.

### Alur yang dianjurkan untuk kelas
**Belajar di Pustaka → scan kartu organ → kerjakan kuis organ → kuis proses.**

---

## Kartu Organo

- 17 kartu bernomor (ukuran kartu remi 63×88 mm) dalam dua gaya, **Realistis** dan **Kartun**. Keduanya dikenali
  aplikasi dan boleh dipakai bersamaan.
- Situs kartu untuk guru & orang tua: **https://organo-kartu.vercel.app**
  - PDF A4 siap cetak: cetak dengan skala **100%** / "ukuran sebenarnya", lalu potong mengikuti garis.
  - **Scan dari layar**: tampilkan kartu besar di laptop/tablet tanpa perlu mencetak.

---

## Privasi & keamanan anak

- Tidak ada akun, password, server, maupun iklan. Aplikasi berjalan offline.
- Progres hanya disimpan di HP anak. Gambar kamera tidak disimpan dan tidak dikirim ke mana pun.
- Organ reproduksi gender lain terkunci sampai anak menyelesaikan kuis organ reproduksi miliknya sendiri. Setiap materi
  reproduksi memuat pesan "Tubuhku milikku".

---

## Untuk pengembang

### Kebutuhan
- **Unity 6000.3.25f1** dengan modul *Android Build Support* (dan *iOS Build Support* untuk iOS).
- Git LFS (beberapa aset disimpan dengan LFS).
- Opsional: Python 3 untuk alat bantu di `Tools/` dan `Web/`.

### Menjalankan di Editor
1. Clone repo (`git lfs install` dulu), lalu buka foldernya di Unity Hub.
2. Menu **Organo > Buka Scene Organo**, lalu **Play**.
3. Di Editor, layar Scan bisa dicoba dengan mengetuk bingkai scan (mode demo), atau memakai XR Simulation
   (**Organo > Kartu AR > Simulasi XR: Meja dengan Semua Kartu**).

### Build APK Android
Menu **Organo > Build > APK Android** → `Builds/Android/Organo.apk` (ARM64, IL2CPP, min. Android 7.1, ARCore opsional).
Atau lewat baris perintah:

```bash
Unity.exe -batchmode -quit -projectPath . -buildTarget Android -executeMethod Organo.EditorTools.OrganoAndroidBuild.BuildApk
```

### iOS
Kode sudah mendukung ARKit dan suara iOS. Build iOS membutuhkan **Mac dengan Xcode**: pilih platform iOS di Unity,
Build, buka proyek Xcode, lalu atur *Signing*. Distribusi uji bisa lewat TestFlight (Apple Developer Program).

### Pengujian otomatis
- Uji fungsi: `-executeMethod Organo.EditorTools.OrganoSnapshots.RunAndQuit`
- Uji tata letak di tiga ukuran layar: `-executeMethod Organo.EditorTools.OrganoSnapshots.RunLayoutAndQuit`

Keduanya dijalankan tanpa `-batchmode`. Hasil uji ada di log dengan awalan `[Organo][Uji]`, dan tangkapan layar di
`Library/OrganoPreview/`.

### Struktur folder
| Folder | Isi |
|---|---|
| `Assets/Organo/Scripts` | Kode aplikasi: `Core` (navigasi, suara), `UI/Screens` (semua layar), `ThreeD` (penampil 3D), `AR` (scan kartu), `Data` (organ, kuis, progres), `Editor` (alat pembuat model, kartu, build, uji) |
| `Assets/Organo/Models` | Model 3D organ, tubuh, dan model mikro |
| `Assets/Organo/AR` | Gambar kartu (Realistis & Kartun) dan library kartu AR |
| `Assets/Organo/UI` | Gaya tampilan (`Organo.uss`) |
| `Web/` | Pembuat & isi situs kartu untuk guru |
| `KartuCetak/` | Halaman kartu A4 siap cetak |
| `Tools/` | Alat bantu (uji pengenalan kartu, ikon, suara narasi) |
| `Dokumen/` | Proposal dan tangkapan layar |

Catatan teknis yang lebih rinci ada di [Assets/Organo/README.md](Assets/Organo/README.md).

### Lisensi aset
Font Poppins berlisensi SIL Open Font License (`Assets/Organo/Fonts/OFL.txt`).
