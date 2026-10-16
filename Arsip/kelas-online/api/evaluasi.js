// POST /api/evaluasi {kode, kunci}                          → data kelas + ringkasan semua murid (khusus guru)
// POST /api/evaluasi {aksi:"hapus-murid", kode, kunci, muridId} → guru mengeluarkan satu murid dari kelas
import { kodeBersih, kunciMurid, kunciKeluar, redis, UMUR_DETIK, cekGuru, pesanGuru, semuaMurid, isi, kirim, gagalServer } from './_lib.js';

export default async function handler(req, res) {
  try {
    if (req.method !== 'POST') return kirim(res, 405, { error: 'Metode tidak didukung.' });
    const b = isi(req);
    const kode = kodeBersih(b.kode);
    const { info, status } = await cekGuru(kode, b.kunci);
    if (!info) return kirim(res, status, { error: pesanGuru[status] });

    if (b.aksi === 'hapus-murid') {
      const id = String(b.muridId ?? '').toLowerCase();
      if (!/^[a-f0-9]{32}$/.test(id)) return kirim(res, 400, { error: 'Murid tidak valid.' });
      await redis(['HDEL', kunciMurid(kode), id], ['SADD', kunciKeluar(kode), id], ['EXPIRE', kunciKeluar(kode), UMUR_DETIK]);
      return kirim(res, 200, { ok: true });
    }

    const murid = await semuaMurid(kode);
    murid.sort((a, b2) => String(a.nama).localeCompare(String(b2.nama), 'id'));
    return kirim(res, 200, {
      kelas: { kode, namaKelas: info.namaKelas, namaGuru: info.namaGuru, dibuat: info.dibuat },
      murid,
    });
  } catch (e) {
    return gagalServer(res, e);
  }
}
