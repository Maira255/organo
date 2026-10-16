// Fungsi bersama API Kelas Organo (berkas berawalan "_" tidak menjadi endpoint di Vercel).
// Data disimpan di Upstash Redis (Vercel Marketplace), lewat REST API-nya:
//   organo:kelas:<KODE>        string JSON: nama kelas, nama guru, hash kunci guru, tanggal dibuat
//   organo:kelas:<KODE>:murid  hash muridId → JSON ringkasan belajar satu murid (ditimpa setiap sinkron)
//   organo:kelas:<KODE>:keluar set muridId yang dikeluarkan guru
// Kedua kunci kedaluwarsa sendiri setelah ±13 bulan tanpa kegiatan (diperpanjang setiap murid/guru memakainya).
import crypto from 'node:crypto';

export const ABJAD = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'; // tanpa huruf/angka yang mirip (O/0, I/1)
export const UMUR_DETIK = 400 * 24 * 3600;
export const MAKS_MURID = 60;

export function kodeBersih(k) {
  return String(k ?? '').toUpperCase().replace(/[^A-Z0-9]/g, '');
}

export function kodeValid(k) {
  return /^[A-Z0-9]{6}$/.test(k) && [...k].every(c => ABJAD.includes(c));
}

export function acak(n) {
  // 256 habis dibagi 32: setiap huruf sama peluangnya
  return [...crypto.randomBytes(n)].map(x => ABJAD[x % ABJAD.length]).join('');
}

export function hashKunci(kunci, salt) {
  return crypto.scryptSync(String(kunci ?? ''), salt, 32).toString('hex');
}

export function samaKunci(kunci, info) {
  if (!info || !info.salt || !info.kunciHash) return false;
  const a = Buffer.from(hashKunci(String(kunci ?? '').toUpperCase().trim(), info.salt), 'hex');
  const b = Buffer.from(info.kunciHash, 'hex');
  return a.length === b.length && crypto.timingSafeEqual(a, b);
}

export const kunciInfo = kode => `organo:kelas:${kode}`;
export const kunciMurid = kode => `organo:kelas:${kode}:murid`;
/** Id murid yang dikeluarkan guru: kirimannya ditolak (410) supaya tidak masuk lagi dengan sendirinya. */
export const kunciKeluar = kode => `organo:kelas:${kode}:keluar`;

// ------------------------------------------------------------------ Upstash Redis (REST)

function alamatRedis() {
  const url = process.env.KV_REST_API_URL || process.env.UPSTASH_REDIS_REST_URL;
  const token = process.env.KV_REST_API_TOKEN || process.env.UPSTASH_REDIS_REST_TOKEN;
  if (!url || !token) throw new Error('Penyimpanan Upstash belum disambungkan (KV_REST_API_URL / KV_REST_API_TOKEN).');
  return { url: url.replace(/\/+$/, ''), token };
}

/** Jalankan beberapa perintah Redis sekaligus (satu permintaan HTTP). Mengembalikan hasil tiap perintah. */
export async function redis(...perintah) {
  const { url, token } = alamatRedis();
  const r = await fetch(url + '/pipeline', {
    method: 'POST',
    headers: { Authorization: 'Bearer ' + token, 'Content-Type': 'application/json' },
    body: JSON.stringify(perintah.map(p => p.map(String))),
  });
  if (!r.ok) throw new Error('Upstash ' + r.status + ': ' + (await r.text()).slice(0, 200));
  const hasil = await r.json();
  const salah = hasil.find(h => h && h.error);
  if (salah) throw new Error('Upstash: ' + salah.error);
  return hasil.map(h => h.result);
}

function jsonAtauNull(s) {
  if (typeof s !== 'string') return null;
  try { return JSON.parse(s); } catch { return null; }
}

export async function infoKelas(kode) {
  if (!kodeValid(kode)) return null;
  const [s] = await redis(['GET', kunciInfo(kode)]);
  return jsonAtauNull(s);
}

/** Semua ringkasan murid satu kelas (sekalian memperpanjang umur data kelas karena guru membukanya). */
export async function semuaMurid(kode) {
  const [isi] = await redis(['HGETALL', kunciMurid(kode)],
    ['EXPIRE', kunciInfo(kode), UMUR_DETIK], ['EXPIRE', kunciMurid(kode), UMUR_DETIK]);
  const out = [];
  for (let i = 0; i + 1 < (isi || []).length; i += 2) {
    const m = jsonAtauNull(isi[i + 1]);
    if (m) out.push(m);
  }
  return out;
}

// ------------------------------------------------------------------ batas percobaan

export function alamatIp(req) {
  const f = String(req.headers['x-forwarded-for'] || '').split(',')[0].trim();
  return f || String(req.headers['x-real-ip'] || '') || 'tanpa-ip';
}

/** Tambah hitungan untuk `nama` (berlaku `detik`); true bila sudah melewati `maks`. */
export async function lewatBatas(nama, maks, detik) {
  const k = 'organo:batas:' + nama;
  const [n] = await redis(['INCR', k], ['EXPIRE', k, detik, 'NX']);
  return n > maks;
}

/** Cek kunci guru dengan batas 20 kali salah per jam per kelas (mencegah kunci ditebak). */
export async function cekGuru(kode, kunci) {
  if (!kodeValid(kode)) return { info: null, status: 403 };
  const [s, salah] = await redis(['GET', kunciInfo(kode)], ['GET', 'organo:batas:kunci:' + kode]);
  const info = jsonAtauNull(s);
  if (!info) return { info: null, status: 403 };
  if (Number(salah) >= 20) return { info: null, status: 429 };
  if (!samaKunci(kunci, info)) {
    await lewatBatas('kunci:' + kode, 20, 3600);
    return { info: null, status: 403 };
  }
  return { info, status: 200 };
}

export const pesanGuru = {
  403: 'Kode kelas atau kunci guru salah.',
  429: 'Terlalu banyak percobaan kunci yang salah. Coba lagi 1 jam lagi.',
};

// ------------------------------------------------------------------ permintaan & jawaban

/** Isi JSON permintaan (Vercel sudah mengurainya bila Content-Type JSON). */
export function isi(req) {
  const b = req.body;
  if (b && typeof b === 'object' && !Array.isArray(b) && !Buffer.isBuffer(b)) return b;
  try {
    const o = JSON.parse(Buffer.isBuffer(b) ? b.toString('utf8') : (b || '{}'));
    return o && typeof o === 'object' && !Array.isArray(o) ? o : {};
  } catch { return {}; }
}

export function kirim(res, status, data) {
  res.setHeader('Cache-Control', 'no-store');
  res.status(status).json(data);
}

export function teks(s, max) {
  if (s !== null && typeof s === 'object') return '';
  return String(s ?? '').replace(/[\u0000-\u001f\u007f]/g, ' ').trim().slice(0, max);
}

export function gagalServer(res, e) {
  console.error(e);
  return kirim(res, 500, { error: 'Server sedang bermasalah. Coba lagi sebentar.' });
}
