// POST /api/sinkron {kode, muridId, nama, gender, varian, versi, bintang, kartu, dipelajari, kuisOrgan, kuisProses, riwayat}
//      → simpan ringkasan belajar murid (menimpa kiriman sebelumnya; aplikasi selalu mengirim ringkasan lengkap)
// POST /api/sinkron {aksi:"keluar", kode, muridId} → murid keluar dari kelas, datanya dihapus
// Jawaban: 200 tersimpan · 404 kelas tidak ada · 410 murid sudah dikeluarkan guru · 403 kelas penuh · 400 data tidak valid
import { kodeBersih, kunciInfo, kunciMurid, kunciKeluar, redis, infoKelas, isi, kirim, teks, gagalServer, UMUR_DETIK, MAKS_MURID } from './_lib.js';

const objek = x => x !== null && typeof x === 'object' && !Array.isArray(x);
const daftar = (a, maks, panjang = 40) =>
  [...new Set((Array.isArray(a) ? a : []).map(x => teks(x, panjang)).filter(Boolean))].slice(0, maks);
const angka = (n, maks) => Math.max(0, Math.min(maks, Math.round(Number(n) || 0)));

// riwayat terbaru lebih dulu; dibatasi supaya satu kelas tetap kecil di penyimpanan
export const MAKS_RIWAYAT = 150;

export function riwayat(a) {
  return (Array.isArray(a) ? a : []).filter(objek).slice(0, MAKS_RIWAYAT).map(r => {
    const total = angka(r.total, 50);
    return {
      quizId: teks(r.quizId, 40),
      title: teks(r.title, 60),
      correct: Math.min(angka(r.correct, 50), total),
      total,
      stars: angka(r.stars, 500),
      when: teks(r.when, 40),
      waktu: teks(r.waktu, 40),
      answers: (Array.isArray(r.answers) ? r.answers : []).filter(objek).slice(0, 10).map(x => ({
        type: teks(x.type, 30), question: teks(x.question, 300), yours: teks(x.yours, 200), right: teks(x.right, 200), isCorrect: x.isCorrect === true,
      })),
    };
  });
}

export default async function handler(req, res) {
  try {
    if (req.method !== 'POST') return kirim(res, 405, { error: 'Metode tidak didukung.' });
    const b = isi(req);
    const kode = kodeBersih(b.kode);
    const muridId = String(b.muridId ?? '').toLowerCase();
    if (!/^[a-f0-9]{32}$/.test(muridId)) return kirim(res, 400, { error: 'Data murid tidak valid.' });
    const info = await infoKelas(kode);
    if (!info) return kirim(res, 404, { error: 'Kode kelas tidak ditemukan.' });

    if (b.aksi === 'keluar') {
      await redis(['HDEL', kunciMurid(kode), muridId]);
      return kirim(res, 200, { ok: true });
    }

    const data = {
      muridId,
      nama: teks(b.nama, 30) || 'Teman',
      gender: teks(b.gender, 10), varian: teks(b.varian, 20), versi: teks(b.versi, 20),
      diperbarui: new Date().toISOString(),
      bintang: angka(b.bintang, 1000000),
      kartu: daftar(b.kartu, 40), dipelajari: daftar(b.dipelajari, 60),
      kuisOrgan: daftar(b.kuisOrgan, 40), kuisProses: daftar(b.kuisProses, 20),
      riwayat: riwayat(b.riwayat),
    };
    const [baru] = await redis(
      ['HSET', kunciMurid(kode), muridId, JSON.stringify(data)],
      ['EXPIRE', kunciMurid(kode), UMUR_DETIK],
      ['EXPIRE', kunciInfo(kode), UMUR_DETIK]);
    // murid baru (atau yang pernah dikeluarkan guru): cek dulu sebelum diterima. Murid lama tidak perlu dicek lagi.
    if (baru === 1) {
      const [jumlah, dikeluarkan] = await redis(['HLEN', kunciMurid(kode)], ['SISMEMBER', kunciKeluar(kode), muridId]);
      if (dikeluarkan === 1) {
        await redis(['HDEL', kunciMurid(kode), muridId]);
        return kirim(res, 410, { error: `Kamu sudah dikeluarkan dari ${info.namaKelas} oleh guru.` });
      }
      if (jumlah > MAKS_MURID) {
        await redis(['HDEL', kunciMurid(kode), muridId]);
        return kirim(res, 403, { error: `Kelas sudah penuh (maksimal ${MAKS_MURID} murid).` });
      }
    }
    return kirim(res, 200, { ok: true, namaKelas: info.namaKelas, namaGuru: info.namaGuru });
  } catch (e) {
    return gagalServer(res, e);
  }
}
