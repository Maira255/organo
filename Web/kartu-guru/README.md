# Situs Organo

Situs statis: Beranda (`index.html`), Kartu (`kartu.html`, unduh & scan dari layar), dan Unduh Aplikasi (`aplikasi.html`,
APK di `unduh/Organo.apk`). Buka `index.html`, atau unggah seluruh folder ini ke hosting statis.
Saat ini dionlinekan di Vercel: `npx vercel deploy --prod` dari folder ini.
Tambahkan `#scan` di akhir alamat halaman Kartu (`/kartu#scan`) untuk langsung membuka layar scan.

- `unduh/` : PDF A4 300 dpi siap cetak (Realistis & Kartun) dan ZIP gambar kartu.
- `img/kartu/` : gambar tiap kartu (780 x 1090 px). Tombol "Scan dari layar" menampilkannya besar
  supaya bisa langsung dipindai aplikasi tanpa dicetak.

Buat ulang dari proyek Unity: render kartu (OrganoSnapshots.RunCardsAndQuit),
"Organo > Kartu AR > Pakai Desain ...", lalu jalankan `python Web/buat_situs_kartu.py`.
