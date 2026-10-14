"""Membuat situs unduhan kartu untuk guru (Web/kartu-guru) dari kartu & halaman cetak proyek.

Jalankan dari mana saja:  python Web/buat_situs_kartu.py   (butuh Pillow: pip install pillow)
Sumber: Assets/Organo/AR/Cards, Assets/Organo/AR/CardsKartun, KartuCetak/Realistis, KartuCetak/Kartun.
"""
import os, shutil, zipfile
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SITE = os.path.join(ROOT, 'Web', 'kartu-guru')
ORGANS = [
    (1, 'mulut', 'Mulut', 'Pencernaan'), (2, 'kerongkongan', 'Kerongkongan', 'Pencernaan'), (3, 'lambung', 'Lambung', 'Pencernaan'),
    (4, 'usushalus', 'Usus Halus', 'Pencernaan'), (5, 'ususbesar', 'Usus Besar', 'Pencernaan'), (6, 'paru', 'Paru-paru', 'Pernapasan'),
    (7, 'jantung', 'Jantung', 'Peredaran Darah'), (8, 'ginjal', 'Ginjal', 'Ekskresi'), (9, 'hati', 'Hati', 'Pencernaan'),
    (10, 'pankreas', 'Pankreas', 'Pencernaan'), (11, 'otak', 'Otak', 'Saraf'), (12, 'tenggorokan', 'Tenggorokan', 'Pernapasan'),
    (13, 'hidung', 'Hidung', 'Pernapasan'), (14, 'mata', 'Mata', 'Indera'), (15, 'rangka', 'Rangka', 'Gerak'),
]
STYLES = [('realistis', 'Realistis', os.path.join(ROOT, 'Assets/Organo/AR/Cards'), os.path.join(ROOT, 'KartuCetak/Realistis')),
          ('kartun', 'Kartun', os.path.join(ROOT, 'Assets/Organo/AR/CardsKartun'), os.path.join(ROOT, 'KartuCetak/Kartun'))]

os.makedirs(SITE, exist_ok=True)
for name in os.listdir(SITE):
    if name in ('.vercel', '.gitignore', '.env.local'):  # tautan & token proyek Vercel (tidak masuk git)
        continue
    path = os.path.join(SITE, name)
    shutil.rmtree(path) if os.path.isdir(path) else os.remove(path)
os.makedirs(os.path.join(SITE, 'unduh'))
sizes = {}
for key, label, cards, pages in STYLES:
    out = os.path.join(SITE, 'kartu', key)
    os.makedirs(out)
    zpath = os.path.join(SITE, 'unduh', f'Organo_Kartu_{label}_PNG.zip')
    with zipfile.ZipFile(zpath, 'w', zipfile.ZIP_DEFLATED) as z:
        for num, oid, name, sysname in ORGANS:
            src = os.path.join(cards, oid + '.png')
            dst = os.path.join(out, oid + '.png')
            shutil.copy(src, dst)
            z.write(src, f'{num:02d}_{oid}.png')
    # PDF A4 300 dpi (ukuran kartu asli 6,3 x 8,8 cm saat dicetak 100%)
    imgs = [Image.open(os.path.join(pages, f)).convert('RGB') for f in sorted(os.listdir(pages)) if f.endswith('.png')]
    pdf = os.path.join(SITE, 'unduh', f'Organo_Kartu_{label}_A4.pdf')
    imgs[0].save(pdf, save_all=True, append_images=imgs[1:], resolution=300.0, quality=92)
    sizes[key] = (os.path.getsize(pdf), os.path.getsize(zpath))

def mb(n):
    return f'{n / 1048576:.1f} MB'.replace('.', ',')

cards_js = ',\n'.join(f'  {{ n: {n}, id: "{i}", name: "{nm}", sys: "{s}" }}' for n, i, nm, s in ORGANS)

html = r'''<!doctype html>
<html lang="id">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<title>Kartu Organo untuk Kelas</title>
<meta name="description" content="Unduh dan cetak 15 kartu organ Organo, atau tampilkan di layar, untuk dipindai murid dengan aplikasi Organo.">
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Poppins:wght@400;500;600;700;800&display=swap" rel="stylesheet">
<style>
/* Tata letak: "meja guru" — kepala berisi tumpukan kartu fisik & unduhan, lalu alur kelas, tips cetak, dan galeri 15 kartu. */
:root {
  --bg: #FAFAFA; --surface: #FFFFFF; --ink: #2E2A4A; --muted: #6B6785; --line: #ECEAF2;
  --teal: #12857A; --teal-soft: #D5F3EF; --purple: #6F52D1; --purple-soft: #E8E2FB; --pink: #D6385E; --pink-soft: #FDE0E7; --sun: #FFD34E;
  --font: "Poppins", "Segoe UI", system-ui, -apple-system, sans-serif;
  --radius: 18px; --shadow: 0 6px 18px rgba(46, 42, 74, 0.08);
  color-scheme: light;
}
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) {
    --bg: #15131F; --surface: #201D2E; --ink: #F1EFF8; --muted: #B3AECB; --line: #312D44;
    --teal: #4FD1C1; --teal-soft: #173C39; --purple: #A892F0; --purple-soft: #2B2547; --pink: #F27B98; --pink-soft: #3D2230; --sun: #FFD34E;
    --shadow: 0 6px 18px rgba(0, 0, 0, 0.35); color-scheme: dark;
  }
}
:root[data-theme="dark"] {
  --bg: #15131F; --surface: #201D2E; --ink: #F1EFF8; --muted: #B3AECB; --line: #312D44;
  --teal: #4FD1C1; --teal-soft: #173C39; --purple: #A892F0; --purple-soft: #2B2547; --pink: #F27B98; --pink-soft: #3D2230; --sun: #FFD34E;
  --shadow: 0 6px 18px rgba(0, 0, 0, 0.35); color-scheme: dark;
}
* { box-sizing: border-box; }
main { overflow-x: clip; }
body { margin: 0; background: var(--bg); color: var(--ink); font-family: var(--font); font-size: 16px; line-height: 1.6; }
.wrap { max-width: 1080px; margin: 0 auto; padding-inline: 20px; }
h1, h2, h3 { line-height: 1.2; text-wrap: balance; margin: 0; }
h1 { font-size: clamp(30px, 5vw, 46px); font-weight: 800; letter-spacing: -0.5px; }
h2 { font-size: clamp(22px, 3.4vw, 30px); font-weight: 700; }
h3 { font-size: 17px; font-weight: 600; }
p { margin: 0; max-width: 62ch; }
a { color: var(--teal); }
:focus-visible { outline: 3px solid var(--purple); outline-offset: 3px; border-radius: 10px; }

header.top { padding-block: 18px; }
.brand { display: flex; align-items: center; gap: 10px; font-weight: 700; font-size: 18px; }
.brand .dot { width: 34px; height: 34px; border-radius: 11px; background: var(--teal); display: grid; place-items: center; color: #fff; font-weight: 800; }
.brand small { color: var(--muted); font-weight: 500; font-size: 13px; }

.hero { display: grid; grid-template-columns: 1.15fr 1fr; gap: 40px; align-items: center; padding-block: 24px 56px; }
.hero > * { min-width: 0; }
.eyebrow { text-transform: uppercase; letter-spacing: 1.4px; font-size: 12px; font-weight: 700; color: var(--purple); }
.hero .lead { color: var(--muted); font-size: 17px; margin-top: 14px; }
.downloads { display: grid; gap: 12px; margin-top: 26px; }
.dl { display: flex; align-items: center; gap: 14px; padding: 14px 16px; border-radius: var(--radius); background: var(--surface); border: 1.5px solid var(--line);
      color: var(--ink); text-decoration: none; box-shadow: var(--shadow); transition: transform .15s, border-color .15s; }
.dl:hover { transform: translateY(-2px); border-color: var(--teal); }
.dl .ico { flex: none; width: 46px; height: 46px; border-radius: 14px; display: grid; place-items: center; font-weight: 800; font-size: 13px; color: #fff; }
.dl .txt { display: grid; min-width: 0; }
.dl .txt b { font-weight: 600; }
.dl .txt span { color: var(--muted); font-size: 13px; }
.dl .arrow { margin-left: auto; font-weight: 700; color: var(--teal); }
.ico.pdf { background: var(--pink); } .ico.pdf2 { background: var(--purple); } .ico.zip { background: var(--teal); }

.fan { position: relative; height: 380px; }
.fan img { position: absolute; top: 20px; left: 50%; width: 38%; max-width: 220px; aspect-ratio: 780 / 1090; border-radius: 12px; box-shadow: 0 14px 30px rgba(46, 42, 74, 0.25); transition: transform .3s; }
.fan img:nth-child(1) { transform: translateX(-112%) rotate(-10deg); }
.fan img:nth-child(2) { transform: translateX(-50%) translateY(-10px); z-index: 2; }
.fan img:nth-child(3) { transform: translateX(12%) rotate(9deg); }
.fan:hover img:nth-child(1) { transform: translateX(-118%) rotate(-12deg); }
.fan:hover img:nth-child(3) { transform: translateX(18%) rotate(11deg); }

section { padding-block: 48px; border-top: 1px solid var(--line); }
.section-head { display: grid; gap: 8px; margin-bottom: 26px; }
.section-head p { color: var(--muted); }

.steps { list-style: none; padding: 0; margin: 0; display: grid; grid-template-columns: repeat(5, minmax(0, 1fr)); gap: 14px; counter-reset: step; }
.steps li { counter-increment: step; background: var(--surface); border: 1.5px solid var(--line); border-radius: var(--radius); padding: 16px; display: grid; gap: 6px; align-content: start; }
.steps li::before { content: counter(step); width: 32px; height: 32px; border-radius: 50%; display: grid; place-items: center; font-weight: 700; color: #fff; background: var(--teal); }
.steps li:nth-child(4)::before, .steps li:nth-child(5)::before { background: var(--purple); }
.steps li p { color: var(--muted); font-size: 14px; }
.who { display: inline-block; font-size: 11px; font-weight: 700; letter-spacing: 1px; text-transform: uppercase; color: var(--teal); }
.steps li:nth-child(n+3) .who { color: var(--purple); }

.tips { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 14px; }
.tip { display: grid; gap: 6px; padding: 16px 18px; border-radius: var(--radius); background: var(--teal-soft); }
.tip:nth-child(2) { background: var(--purple-soft); } .tip:nth-child(3) { background: var(--pink-soft); } .tip:nth-child(4) { background: var(--teal-soft); }
.tip .spec { font-variant-numeric: tabular-nums; font-weight: 700; font-size: 20px; }
.tip p { font-size: 14px; color: var(--ink); }

.toolbar { display: flex; flex-wrap: wrap; gap: 10px 18px; align-items: center; justify-content: space-between; margin-bottom: 18px; }
.seg { display: inline-flex; padding: 4px; border-radius: 999px; background: var(--surface); border: 1.5px solid var(--line); }
.seg button { font: inherit; font-weight: 600; font-size: 14px; border: 0; background: transparent; color: var(--muted); padding: 8px 18px; border-radius: 999px; cursor: pointer; }
.seg button[aria-pressed="true"] { background: var(--teal); color: #fff; }
.chips { display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 22px; }
.zip-link { font-weight: 600; font-size: 14px; text-decoration: none; padding: 8px 16px; border-radius: 999px; border: 1.5px solid var(--teal); }
.zip-link:hover { background: var(--teal-soft); }
.chips button { font: inherit; font-size: 13px; font-weight: 500; border: 1.5px solid var(--line); background: var(--surface); color: var(--ink); padding: 6px 14px; border-radius: 999px; cursor: pointer; }
.chips button[aria-pressed="true"] { border-color: var(--purple); background: var(--purple-soft); color: var(--ink); font-weight: 600; }
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(170px, 1fr)); gap: 18px; }
.card { display: grid; gap: 8px; }
/* height:auto wajib: atribut height="1090" di <img> membuat kartu tertarik memanjang kalau hanya lebarnya yang diatur */
.card img { display: block; width: 100%; max-width: 100%; height: auto; aspect-ratio: 780 / 1090; border-radius: 10px; box-shadow: var(--shadow); background: var(--line); }
.card .meta { display: flex; align-items: baseline; justify-content: space-between; gap: 8px; }
.card .meta b { font-weight: 600; font-size: 15px; }
.card .meta span { color: var(--muted); font-size: 12px; font-variant-numeric: tabular-nums; }
.card a { font-size: 13px; font-weight: 600; text-decoration: none; }
.card a:hover { text-decoration: underline; }

.screen-cta { display: flex; flex-wrap: wrap; align-items: center; gap: 10px 14px; margin-top: 18px; padding: 14px 16px; border-radius: var(--radius);
              background: var(--purple-soft); }
.screen-cta p { font-size: 14px; flex: 1 1 220px; min-width: 0; }
.btn { font: inherit; font-weight: 600; font-size: 14px; border: 0; border-radius: 999px; padding: 10px 18px; cursor: pointer; background: var(--purple); color: #fff; }
.btn:hover { filter: brightness(1.08); }
.card .actions { display: flex; flex-wrap: wrap; gap: 4px 14px; }
.card button.link { font: inherit; font-size: 13px; font-weight: 600; color: var(--purple); background: none; border: 0; padding: 0; cursor: pointer; }
.card button.link:hover { text-decoration: underline; }

/* Layar scan: kartu sebesar mungkin di atas latar abu-abu terang. Latar gelap membuat kamera HP menaikkan eksposur sehingga
   kartu jadi terlalu terang dan detailnya hilang; latar netral menjaga kartu tetap tajam. Warna tetap sama di kedua tema. */
dialog.viewer { position: fixed; inset: 0; z-index: 50; width: 100%; height: 100%; max-width: none; max-height: none; margin: 0; padding: 0; border: 0;
                background: #E6E3EC; color: #2E2A4A; color-scheme: light; overflow: hidden; }
dialog.viewer:fullscreen { width: 100vw; height: 100vh; }
body.viewer-open { overflow: hidden; }
.viewer-in { height: 100%; display: grid; grid-template-rows: auto 1fr auto; gap: 10px;
             padding: calc(12px + env(safe-area-inset-top, 0px)) 16px calc(14px + env(safe-area-inset-bottom, 0px)); }
.viewer-top { display: grid; grid-template-columns: minmax(0, 1fr) auto auto; align-items: center; gap: 10px 12px; }
@media (max-width: 560px) {
  .viewer-top { grid-template-columns: minmax(0, 1fr) auto; }
  .viewer-top .viewer-tools { grid-column: 1 / -1; grid-row: 2; }
}
.viewer-bottom { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.viewer-title { display: grid; min-width: 0; }
.viewer-title b { font-size: 18px; font-weight: 700; }
.viewer-title span { font-size: 13px; color: #5E5A78; font-variant-numeric: tabular-nums; }
.viewer-stage { position: relative; min-height: 0; touch-action: pan-y; }
.viewer-stage img { position: absolute; inset: 0; margin: auto; max-width: 100%; max-height: 100%; width: auto; height: auto; user-select: none; -webkit-user-drag: none; }
/* Kamera HP sering menangkap layar terlalu terang (ruangan lebih gelap dari layar): warna pastel kartu berubah putih dan
   kartu tidak dikenali. Kartu ditampilkan sedikit redup & lebih kontras (diuji: Tools/uji_pengenalan_kartu.py --terang --redup,
   pengenalan 16% -> 89% saat layar terlalu terang, normal tetap ±91%). "Warna asli" mematikannya. */
.viewer-stage img.scan-tone { filter: contrast(1.4) brightness(0.65); }
.vbtn { font: inherit; font-weight: 600; font-size: 14px; color: #2E2A4A; background: #FFFFFF; border: 1.5px solid #CFCBDA; border-radius: 999px; padding: 9px 16px; cursor: pointer; }
.vbtn:hover { background: #F4F2F8; }
.vbtn.round { flex: none; width: 46px; height: 46px; padding: 0; font-size: 20px; display: grid; place-items: center; }
.viewer .seg { background: #FFFFFF; border-color: #CFCBDA; }
.viewer .seg button { color: #5E5A78; }
.viewer .seg button[aria-pressed="true"] { background: #12857A; color: #FFFFFF; }
.viewer-hint { font-size: 13px; line-height: 1.45; color: #3E3A5A; text-align: center; flex: 1 1 auto; min-width: 0; max-width: 60ch; }
.viewer-tools { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }

footer { padding-block: 36px 48px; border-top: 1px solid var(--line); color: var(--muted); font-size: 14px; display: grid; gap: 8px; }

@media (max-width: 900px) {
  .hero { grid-template-columns: 1fr; gap: 12px; }
  .fan { height: 320px; order: -1; }
  .steps { grid-template-columns: 1fr 1fr; }
  .steps li:last-child { grid-column: 1 / -1; }
}
@media (max-width: 520px) {
  /* HP: jarak lebih rapat, langkah & tips dua kolom ringkas supaya halaman tidak terlalu panjang */
  .wrap { padding-inline: 16px; }
  section { padding-block: 32px; }
  .hero { padding-block: 8px 32px; }
  .hero .lead { font-size: 15px; }
  .fan { height: 230px; }
  .steps { grid-template-columns: 1fr 1fr; gap: 10px; }
  .steps li { padding: 12px; }
  .steps li h3 { font-size: 15px; }
  .steps li p { font-size: 13px; }
  .tips { grid-template-columns: 1fr 1fr; gap: 10px; }
  .tip { padding: 12px; }
  .tip .spec { font-size: 16px; }
  .tip p { font-size: 13px; }
  .grid { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
  .toolbar { gap: 10px; }
  .toolbar .viewer-tools { width: 100%; }
  .toolbar .viewer-tools > * { flex: 1 1 auto; text-align: center; }
  .dl { padding: 12px; }
  .screen-cta .btn { width: 100%; }
}
@media (prefers-reduced-motion: reduce) { .fan img, .dl { transition: none; } }
</style>
</head>
<body>
<header class="top wrap">
  <div class="brand"><span class="dot" aria-hidden="true">O</span><span>Organo <small>· Kartu untuk Kelas</small></span></div>
</header>

<main>
  <div class="hero wrap">
    <div>
      <div class="eyebrow">Untuk guru &amp; orang tua</div>
      <h1>Cetak kartu organ, lalu biarkan murid memindainya</h1>
      <p class="lead">15 kartu Organo (mulut sampai rangka) siap dicetak di kertas A4. Murid memindai kartu dengan aplikasi Organo: organnya muncul dalam 3D dan kartunya masuk ke koleksi mereka.</p>
      <div class="downloads">
        <a class="dl" href="unduh/Organo_Kartu_Realistis_A4.pdf" download>
          <span class="ico pdf">PDF</span>
          <span class="txt"><b>Kartu versi Realistis</b><span>A4 · 2 halaman · 15 kartu · __PDF_R__</span></span>
          <span class="arrow" aria-hidden="true">Unduh</span>
        </a>
        <a class="dl" href="unduh/Organo_Kartu_Kartun_A4.pdf" download>
          <span class="ico pdf2">PDF</span>
          <span class="txt"><b>Kartu versi Kartun</b><span>A4 · 2 halaman · 15 kartu · __PDF_K__</span></span>
          <span class="arrow" aria-hidden="true">Unduh</span>
        </a>
      </div>
      <div class="screen-cta">
        <p><b>Belum sempat mencetak?</b> Tampilkan kartu di layar laptop, tablet, atau HP lain, lalu pindai langsung dengan aplikasi Organo.</p>
        <button type="button" class="btn" data-open-viewer>Scan dari layar</button>
      </div>
    </div>
    <div class="fan" aria-hidden="true">
      <img src="kartu/realistis/jantung.png" alt="">
      <img src="kartu/realistis/paru.png" alt="">
      <img src="kartu/kartun/otak.png" alt="">
    </div>
  </div>

  <section>
    <div class="wrap">
      <div class="section-head">
        <div class="eyebrow">Alur di kelas</div>
        <h2>Belajar dulu, scan kartu, baru kuis</h2>
        <p>Kartu menjadi "hadiah" setelah murid mempelajari organnya. Urutan ini membuat kuis terasa sebagai tantangan, bukan hafalan.</p>
      </div>
      <ol class="steps">
        <li><span class="who">Guru</span><h3>Unduh &amp; cetak</h3><p>Cetak PDF di kertas A4 dengan skala 100% (ukuran sebenarnya).</p></li>
        <li><span class="who">Guru</span><h3>Potong &amp; bagikan</h3><p>Potong mengikuti garis tipis. Satu kartu berukuran 6,3 × 8,8 cm.</p></li>
        <li><span class="who">Murid</span><h3>Belajar di Library</h3><p>Murid membuka organ di aplikasi: model 3D, bagian dalam, dan fakta menarik.</p></li>
        <li><span class="who">Murid</span><h3>Scan kartu</h3><p>Buka menu Scan, dekatkan HP sampai kartu memenuhi bingkai (±15 cm). Organ muncul di atas kartu dan kartu masuk koleksi.</p></li>
        <li><span class="who">Murid</span><h3>Kerjakan kuis</h3><p>Uji pemahaman lewat kuis tempel label dan pilihan ganda, kumpulkan bintang.</p></li>
      </ol>
    </div>
  </section>

  <section>
    <div class="wrap">
      <div class="section-head">
        <div class="eyebrow">Supaya kartu mudah dipindai</div>
        <h2>Tips mencetak</h2>
      </div>
      <div class="tips">
        <div class="tip"><span class="spec">A4 · 100%</span><p>Pilih skala 100% atau "Ukuran sebenarnya". Kartu yang diperkecil lebih sulit dikenali.</p></div>
        <div class="tip"><span class="spec">6,3 × 8,8 cm</span><p>Ukuran kartu remi. Gunting tepat di garis supaya seluruh gambar kartu terlihat kamera.</p></div>
        <div class="tip"><span class="spec">Kertas tebal</span><p>Pakai art paper atau karton 210–260 gsm. Kalau dilaminasi, pilih laminasi doff (tidak mengilap).</p></div>
        <div class="tip"><span class="spec">±15 cm</span><p>Dekatkan HP sampai kartu memenuhi bingkai di layar Scan. Kamera AR hanya mengenali kartu yang mengisi sebagian besar gambar kamera, jadi kartu yang terlihat kecil tidak akan terdeteksi.</p></div>
        <div class="tip"><span class="spec">Cahaya cukup</span><p>Scan di ruangan terang dan hindari pantulan lampu. Tahan HP diam 2 detik. Kalau gelap, nyalakan senter di layar Scan.</p></div>
        <div class="tip"><span class="spec">Dari layar</span><p>Tanpa printer: buka "Scan dari layar", kecerahan layar sedang (jangan maksimal, kartu yang terlalu terang sulit dikenali), lalu dekatkan HP (±20 cm) sampai kartu memenuhi bingkai.</p></div>
      </div>
    </div>
  </section>

  <section id="galeri">
    <div class="wrap">
      <div class="section-head">
        <div class="eyebrow">Galeri</div>
        <h2>15 kartu Organo</h2>
        <p>Pilih versi yang paling cocok untuk kelasmu. Aplikasi Organo mengenali kedua versi, jadi kartu Realistis dan Kartun boleh dipakai bersamaan.</p>
      </div>
      <div class="toolbar">
        <div class="seg" role="group" aria-label="Versi kartu">
          <button type="button" id="style-realistis" data-style="realistis" aria-pressed="true">Realistis</button>
          <button type="button" id="style-kartun" data-style="kartun" aria-pressed="false">Kartun</button>
        </div>
        <div class="viewer-tools">
          <button type="button" class="btn" data-open-viewer>Scan dari layar</button>
          <a class="zip-link" id="zip" href="unduh/Organo_Kartu_Realistis_PNG.zip" download>Unduh 15 gambar (ZIP)</a>
        </div>
      </div>
      <div class="chips" id="filters" role="group" aria-label="Saring sistem organ"></div>
      <div class="grid" id="grid"></div>
    </div>
  </section>
</main>

<dialog class="viewer" id="viewer" aria-label="Kartu untuk dipindai" tabindex="-1">
  <div class="viewer-in">
    <div class="viewer-top">
      <div class="viewer-title"><b id="v-name">Mulut</b><span id="v-meta">Kartu 01 dari 15</span></div>
      <div class="viewer-tools">
        <div class="seg" role="group" aria-label="Versi kartu">
          <button type="button" data-vstyle="realistis" aria-pressed="true">Realistis</button>
          <button type="button" data-vstyle="kartun" aria-pressed="false">Kartun</button>
        </div>
        <button type="button" class="vbtn" id="v-tone" aria-pressed="true" title="Kartu ditampilkan sedikit redup supaya mudah dikenali kamera">Mode scan</button>
        <button type="button" class="vbtn" id="v-full" title="Sembunyikan bilah browser supaya kartu tampil lebih besar">Layar penuh</button>
      </div>
      <button type="button" class="vbtn round" id="v-close" aria-label="Tutup">&#x2715;</button>
    </div>
    <div class="viewer-stage" id="v-stage"><img id="v-img" src="" alt="" width="780" height="1090" draggable="false"></div>
    <div class="viewer-bottom">
      <button type="button" class="vbtn round" id="v-prev" aria-label="Kartu sebelumnya">&#x2039;</button>
      <p class="viewer-hint">Di aplikasi Organo buka <b>Scan</b>, lalu <b>dekatkan HP sampai kartu memenuhi bingkai</b> (±20 cm dari layar). Kecerahan layar <b>sedang</b>, jangan maksimal. Geser untuk kartu lain.</p>
      <button type="button" class="vbtn round" id="v-next" aria-label="Kartu berikutnya">&#x203A;</button>
    </div>
  </div>
</dialog>

<footer class="wrap">
  <p><b>Organo</b> · aplikasi belajar organ tubuh untuk anak SD lewat kartu, AR, dan kuis.</p>
  <p>Kartu boleh dicetak ulang untuk keperluan belajar di kelas dan di rumah. Kamera aplikasi hanya dipakai saat scan; gambarnya tidak disimpan.</p>
</footer>

<script>
const CARDS = [
__CARDS__
];
const SYSTEMS = ["Semua", "Pencernaan", "Pernapasan", "Peredaran Darah", "Ekskresi", "Saraf", "Indera", "Gerak"];
let style = "realistis";
let filter = "Semua";
try { style = localStorage.getItem("organo-kartu-gaya") || style; } catch (e) {}

const grid = document.getElementById("grid");
const filters = document.getElementById("filters");

const ZIP = { realistis: "unduh/Organo_Kartu_Realistis_PNG.zip", kartun: "unduh/Organo_Kartu_Kartun_PNG.zip" };
function render() {
  document.getElementById("zip").href = ZIP[style];
  grid.innerHTML = "";
  CARDS.filter(c => filter === "Semua" || c.sys === filter).forEach(c => {
    const el = document.createElement("article");
    el.className = "card";
    const src = `kartu/${style}/${c.id}.png`;
    el.innerHTML = `<img src="${src}" alt="Kartu Organo ${c.name}, sistem ${c.sys}" loading="lazy" width="780" height="1090">
      <div class="meta"><b>${c.name}</b><span>No. ${String(c.n).padStart(2, "0")}</span></div>
      <div class="actions"><button type="button" class="link" data-scan="${c.id}">Scan dari layar</button>
      <a href="${src}" download="Organo_${String(c.n).padStart(2, "0")}_${c.id}_${style}.png">Unduh PNG</a></div>`;
    el.querySelector("[data-scan]").addEventListener("click", () => openViewer(c.id));
    grid.appendChild(el);
  });
}

SYSTEMS.forEach(s => {
  const b = document.createElement("button");
  b.type = "button";
  b.textContent = s;
  b.setAttribute("aria-pressed", String(s === filter));
  b.addEventListener("click", () => {
    filter = s;
    filters.querySelectorAll("button").forEach(x => x.setAttribute("aria-pressed", String(x === b)));
    render();
  });
  filters.appendChild(b);
});

document.querySelectorAll(".seg button[data-style]").forEach(b => {
  b.setAttribute("aria-pressed", String(b.dataset.style === style));
  b.addEventListener("click", () => {
    setStyle(b.dataset.style);
    render();
  });
});
render();

// ---- Layar scan: satu kartu besar, geser/panah untuk kartu lain
const viewer = document.getElementById("viewer");
const vImg = document.getElementById("v-img");
let vIndex = 0;
function setStyle(s) {
  style = s;
  try { localStorage.setItem("organo-kartu-gaya", style); } catch (e) {}
  document.querySelectorAll(".seg button[data-style]").forEach(x => x.setAttribute("aria-pressed", String(x.dataset.style === style)));
  document.querySelectorAll("[data-vstyle]").forEach(x => x.setAttribute("aria-pressed", String(x.dataset.vstyle === style)));
}
function showCard(i) {
  vIndex = (i + CARDS.length) % CARDS.length;
  const c = CARDS[vIndex];
  vImg.src = `kartu/${style}/${c.id}.png`;
  vImg.alt = `Kartu Organo ${c.name}`;
  document.getElementById("v-name").textContent = c.name;
  document.getElementById("v-meta").textContent = `Kartu ${String(c.n).padStart(2, "0")} dari ${CARDS.length} · ${c.sys}`;
  const next = CARDS[(vIndex + 1) % CARDS.length];
  new Image().src = `kartu/${style}/${next.id}.png`;
}
// Dibuka sebagai lapisan biasa (show, bukan showModal): browser menolak membuat dialog modal menjadi layar penuh,
// itu sebabnya tombol "Layar penuh" dulu tidak berbuat apa-apa.
function openViewer(id) {
  const i = Math.max(0, CARDS.findIndex(c => c.id === id));
  showCard(i);
  setStyle(style);
  if (typeof viewer.show === "function") viewer.show(); else viewer.setAttribute("open", "");
  document.body.classList.add("viewer-open");
  viewer.focus();
  wakeLock(true);
}
function closeViewer() {
  if (fsEl()) (document.exitFullscreen || document.webkitExitFullscreen).call(document);
  if (viewer.open) viewer.close(); else viewer.removeAttribute("open");
  document.body.classList.remove("viewer-open");
  wakeLock(false);
  render();
}
let lock = null;
async function wakeLock(on) {
  try {
    if (on && "wakeLock" in navigator) lock = await navigator.wakeLock.request("screen");
    else if (!on && lock) { await lock.release(); lock = null; }
  } catch (e) { lock = null; }
}
document.querySelectorAll("[data-open-viewer]").forEach(b => b.addEventListener("click", () => openViewer(CARDS[0].id)));
document.getElementById("v-close").addEventListener("click", closeViewer);
document.getElementById("v-prev").addEventListener("click", () => showCard(vIndex - 1));
document.getElementById("v-next").addEventListener("click", () => showCard(vIndex + 1));
document.querySelectorAll("[data-vstyle]").forEach(b => b.addEventListener("click", () => { setStyle(b.dataset.vstyle); showCard(vIndex); }));
const fullBtn = document.getElementById("v-full");
// Seluruh halaman dibuat layar penuh (paling luas dukungannya); lapisan scan tetap di atas karena bukan dialog modal.
// iPhone & browser yang menolak layar penuh: tombol disembunyikan supaya tidak ada tombol yang tidak berbuat apa-apa.
const root = document.documentElement;
const reqFull = root.requestFullscreen || root.webkitRequestFullscreen;
const fsEl = () => document.fullscreenElement || document.webkitFullscreenElement;
if (!reqFull || !(document.fullscreenEnabled || document.webkitFullscreenEnabled)) fullBtn.hidden = true;
fullBtn.addEventListener("click", () => {
  if (fsEl()) { (document.exitFullscreen || document.webkitExitFullscreen).call(document); return; }
  try {
    const p = reqFull.call(root);
    if (p && p.catch) p.catch(() => { fullBtn.hidden = true; });
  } catch (e) { fullBtn.hidden = true; }
});
const onFs = () => { fullBtn.textContent = fsEl() ? "Keluar layar penuh" : "Layar penuh"; };
document.addEventListener("fullscreenchange", onFs);
document.addEventListener("webkitfullscreenchange", onFs);
const toneBtn = document.getElementById("v-tone");
let scanTone = true;
try { scanTone = localStorage.getItem("organo-kartu-tone") !== "asli"; } catch (e) {}
function applyTone() {
  vImg.classList.toggle("scan-tone", scanTone);
  toneBtn.setAttribute("aria-pressed", String(scanTone));
  toneBtn.textContent = scanTone ? "Mode scan" : "Warna asli";
}
toneBtn.addEventListener("click", () => {
  scanTone = !scanTone;
  try { localStorage.setItem("organo-kartu-tone", scanTone ? "scan" : "asli"); } catch (e) {}
  applyTone();
});
applyTone();
document.addEventListener("keydown", e => {
  if (!viewer.open) return;
  if (e.key === "ArrowRight") { showCard(vIndex + 1); e.preventDefault(); }
  else if (e.key === "ArrowLeft") { showCard(vIndex - 1); e.preventDefault(); }
  else if (e.key === "Escape" && !fsEl()) { closeViewer(); e.preventDefault(); }
});
let startX = null;
const stage = document.getElementById("v-stage");
stage.addEventListener("pointerdown", e => { startX = e.clientX; });
stage.addEventListener("pointerup", e => {
  if (startX === null) return;
  const dx = e.clientX - startX;
  startX = null;
  if (Math.abs(dx) > 50) showCard(vIndex + (dx < 0 ? 1 : -1));
});
if (location.hash === "#scan") openViewer(CARDS[0].id);
</script>
</body>
</html>
'''
html = html.replace('__CARDS__', cards_js)
html = html.replace('__PDF_R__', mb(sizes['realistis'][0])).replace('__PDF_K__', mb(sizes['kartun'][0]))
open(os.path.join(SITE, 'index.html'), 'w', encoding='utf8').write(html)

readme = '''# Kartu Organo untuk Kelas (situs unduhan guru)

Situs statis: buka `index.html`, atau unggah seluruh folder ini ke hosting statis.
Saat ini dionlinekan di Vercel: `npx vercel deploy --prod` dari folder ini.
Tambahkan `#scan` di akhir alamat untuk langsung membuka layar scan.

- `unduh/` : PDF A4 300 dpi siap cetak (Realistis & Kartun) dan ZIP gambar kartu.
- `kartu/` : gambar tiap kartu (780 x 1090 px). Tombol "Scan dari layar" menampilkannya besar
  supaya bisa langsung dipindai aplikasi tanpa dicetak.

Buat ulang dari proyek Unity: render kartu (OrganoSnapshots.RunCardsAndQuit),
"Organo > Kartu AR > Pakai Desain ...", lalu jalankan `python Web/buat_situs_kartu.py`.
'''
open(os.path.join(SITE, 'README.md'), 'w', encoding='utf8').write(readme)
# yang tidak ikut diunggah saat deploy Vercel
open(os.path.join(SITE, '.vercelignore'), 'w', encoding='utf8').write('README.md\n.env*\n')
print('situs:', SITE)
for k, (p, z) in sizes.items(): print(k, 'pdf', mb(p), 'zip', mb(z))
