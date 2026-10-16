// GET  /api/kelas?kode=K7M2QX               → nama kelas & guru (dipakai aplikasi saat murid mengetik kode)
// POST /api/kelas {aksi:"buat", namaKelas, namaGuru} → kode kelas + kunci guru (kunci hanya ditampilkan sekali)
// POST /api/kelas {aksi:"hapus", kode, kunci}        → hapus kelas beserta semua data murid
import crypto from 'node:crypto';
import {
  kodeBersih, acak, hashKunci, kunciInfo, kunciMurid, kunciKeluar, redis, infoKelas, cekGuru, pesanGuru, lewatBatas, alamatIp,
  isi, kirim, teks, gagalServer, UMUR_DETIK,
} from './_lib.js';

export default async function handler(req, res) {
  try {
    if (req.method === 'GET') {
      const kode = kodeBersih(req.query.kode);
      const info = await infoKelas(kode);
      if (!info) return kirim(res, 404, { error: 'Kode kelas tidak ditemukan.' });
      return kirim(res, 200, { kode, namaKelas: info.namaKelas, namaGuru: info.namaGuru });
    }
    if (req.method !== 'POST') return kirim(res, 405, { error: 'Metode tidak didukung.' });
    const b = isi(req);

    if (b.aksi === 'buat') {
      const namaKelas = teks(b.namaKelas, 40), namaGuru = teks(b.namaGuru, 40);
      if (!namaKelas || !namaGuru) return kirim(res, 400, { error: 'Isi nama kelas dan nama guru.' });
      if (await lewatBatas('buat:' + alamatIp(req), 20, 3600))
        return kirim(res, 429, { error: 'Terlalu banyak kelas dibuat dari jaringan ini. Coba lagi 1 jam lagi.' });
      const kunci = acak(4) + '-' + acak(4);
      const salt = crypto.randomBytes(16).toString('hex');
      for (let i = 0; i < 8; i++) {
        const kode = acak(6);
        const info = { kode, namaKelas, namaGuru, salt, kunciHash: hashKunci(kunci, salt), dibuat: new Date().toISOString() };
        // NX: hanya tersimpan bila kode itu belum dipakai kelas lain
        const [ok] = await redis(['SET', kunciInfo(kode), JSON.stringify(info), 'NX', 'EX', UMUR_DETIK]);
        if (ok === 'OK') return kirim(res, 200, { kode, kunci, namaKelas, namaGuru });
      }
      return kirim(res, 500, { error: 'Gagal membuat kode kelas, coba lagi.' });
    }

    if (b.aksi === 'hapus') {
      const kode = kodeBersih(b.kode);
      const { info, status } = await cekGuru(kode, b.kunci);
      if (!info) return kirim(res, status, { error: pesanGuru[status] });
      await redis(['DEL', kunciInfo(kode), kunciMurid(kode), kunciKeluar(kode)]);
      return kirim(res, 200, { ok: true });
    }
    return kirim(res, 400, { error: 'Aksi tidak dikenal.' });
  } catch (e) {
    return gagalServer(res, e);
  }
}
