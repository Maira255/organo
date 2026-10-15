"""Uji pengenalan kartu Organo seperti ARCore Augmented Images (tanpa HP).

ARCore mengenali kartu dari titik-titik ciri (fitur) di gambar HITAM-PUTIH kamera (gambar CPU 640x480), lalu
mencocokkannya dengan semua gambar di database sekaligus. Skrip ini meniru itu dengan OpenCV:

  1. Database: semua kartu (Realistis & Kartun) diubah ke abu-abu, ciri ORB diambil.
  2. "Foto kamera" buatan: kartu ditempel di latar acak dengan perspektif (miring), ukuran tertentu di gambar kamera
     (persentase luas), buram, noise, terang/gelap; versi "layar" menambah pola piksel layar & silau.
  3. Pencocokan global ke semua kartu (ciri yang sama di banyak kartu otomatis gugur oleh uji rasio),
     lalu verifikasi geometri (homografi RANSAC). Kartu dengan inlier terbanyak menang bila >= MIN_INLIERS
     dan cukup unggul dari kartu kedua.

Hasil: persentase BENAR / SALAH kartu / TIDAK dikenali per ukuran & kondisi. Ini pendekatan, bukan ARCore asli,
tapi menangkap masalah yang sama: kartu terlalu kecil di kamera, ciri unik terlalu sedikit, atau kartu mirip satu sama lain.

Pakai:  python Tools/uji_pengenalan_kartu.py [folder_realistis folder_kartun] [--percobaan N] [--simpan folder]
Butuh:  pip install opencv-python-headless pillow
"""
import argparse, os, random, sys
import numpy as np
import cv2

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
IDS = ['mulut', 'kerongkongan', 'lambung', 'usushalus', 'ususbesar', 'paru', 'jantung', 'ginjal', 'hati',
       'pankreas', 'otak', 'tenggorokan', 'hidung', 'mata', 'rangka', 'reprolaki', 'reproperempuan']
CPU_W, CPU_H = 640, 480          # resolusi gambar CPU ARCore (dipakai untuk deteksi gambar)
DB_W = 480                       # lebar gambar database setelah diperkecil
MIN_INLIERS = 14
MARGIN = 1.5


def load_db(folders):
    orb = cv2.ORB_create(nfeatures=1200, scaleFactor=1.2, nlevels=8, fastThreshold=12)
    names, kps, descs, sizes, grays = [], [], [], [], []
    for style, folder in folders:
        for oid in IDS:
            img = cv2.imread(os.path.join(folder, oid + '.png'), cv2.IMREAD_GRAYSCALE)
            if img is None:
                continue
            h = int(round(img.shape[0] * DB_W / img.shape[1]))
            img = cv2.resize(img, (DB_W, h), interpolation=cv2.INTER_AREA)
            kp, d = orb.detectAndCompute(img, None)
            names.append(f'{style}:{oid}')
            kps.append(np.float32([k.pt for k in kp]))
            descs.append(d)
            sizes.append((DB_W, h))
            grays.append(img)
    owner = np.concatenate([np.full(len(d), i) for i, d in enumerate(descs)])
    alld = np.vstack(descs)
    allpts = np.vstack(kps)
    return dict(names=names, sizes=sizes, owner=owner, desc=alld, pts=allpts, grays=grays, orb=orb,
                counts=[len(d) for d in descs])


def background(rng):
    """Latar acak: gradasi + bercak + garis (meja/keyboard/ruangan), abu-abu."""
    bg = np.full((CPU_H, CPU_W), rng.integers(40, 200), np.float32)
    gx = np.linspace(0, 1, CPU_W)[None, :] * rng.uniform(-60, 60)
    gy = np.linspace(0, 1, CPU_H)[:, None] * rng.uniform(-60, 60)
    bg += gx + gy
    for _ in range(int(rng.integers(6, 18))):
        c = (int(rng.integers(0, CPU_W)), int(rng.integers(0, CPU_H)))
        cv2.circle(bg, c, int(rng.integers(10, 90)), float(rng.integers(0, 255)), -1)
    for _ in range(int(rng.integers(5, 25))):
        p1 = (int(rng.integers(0, CPU_W)), int(rng.integers(0, CPU_H)))
        p2 = (int(rng.integers(0, CPU_W)), int(rng.integers(0, CPU_H)))
        cv2.line(bg, p1, p2, float(rng.integers(0, 255)), int(rng.integers(1, 4)))
    bg = cv2.GaussianBlur(bg, (0, 0), rng.uniform(1.5, 4))
    return bg


HARSH = False   # --berat: HP goyang, fokus meleset, silau kuat, kontras layar rendah
BRIGHT = False  # --terang: kartu di layar laptop terlalu terang (kamera menaikkan eksposur karena sekitarnya gelap)
DIM = False     # --redup: tiru filter situs "Scan dari layar" (brightness & contrast)
DIM_BRIGHT, DIM_CONTRAST = 0.78, 1.25


def capture(card, coverage, screen, rng):
    """Satu 'foto kamera' 640x480 berisi kartu dengan luas = coverage x luas gambar."""
    h0, w0 = card.shape
    area = coverage * CPU_W * CPU_H
    w = (area * w0 / h0) ** 0.5
    h = w * h0 / w0
    src_card = card.astype(np.float32)
    if screen:
        # layar: pola subpiksel/moire halus + kartu sedikit terlalu terang (layar memancarkan cahaya)
        yy, xx = np.mgrid[0:h0, 0:w0]
        period = rng.uniform(2.6, 4.2)
        moire = np.sin(2 * np.pi * (xx * np.cos(0.3) + yy * np.sin(0.3)) / period)
        src_card = src_card * (1 + (0.14 if HARSH else 0.06) * moire)
        src_card = 255 * (np.clip(src_card, 0, 255) / 255) ** rng.uniform(0.7, 0.9)
    if HARSH:
        # kontras turun (kertas doff di cahaya redup / layar dari samping)
        src_card = 128 + (src_card - 128) * rng.uniform(0.55, 0.8)
    if DIM and screen:
        # situs menampilkan kartu sedikit redup & lebih kontras (CSS filter) supaya tidak terpotong putih oleh kamera
        src_card = np.clip((src_card / 255.0 - 0.5) * DIM_CONTRAST + 0.5, 0, 1) * 255 * DIM_BRIGHT
    if BRIGHT and screen:
        # layar memancarkan cahaya + ruangan gelap: kamera menaikkan eksposur, warna terang/pastel menjadi putih (terpotong)
        src_card = np.clip(src_card * rng.uniform(1.35, 1.75) + rng.uniform(10, 30), 0, 255)
    # perspektif: kartu potret di HP potret = terputar 90 derajat di gambar sensor (lanskap)
    cx, cy = CPU_W / 2 + rng.uniform(-0.12, 0.12) * CPU_W, CPU_H / 2 + rng.uniform(-0.12, 0.12) * CPU_H
    rot = np.deg2rad(90 + rng.uniform(-15, 15))
    tilt = rng.uniform(0, 30)            # derajat kemiringan kartu terhadap kamera
    corners = np.float32([[-w / 2, -h / 2], [w / 2, -h / 2], [w / 2, h / 2], [-w / 2, h / 2]])
    k = np.tan(np.deg2rad(tilt)) * 0.35
    side = rng.choice([-1, 1])
    persp = corners.copy()
    persp[:, 0] *= 1 + side * k * (corners[:, 1] / (h / 2)) * 0.5
    R = np.array([[np.cos(rot), -np.sin(rot)], [np.sin(rot), np.cos(rot)]], np.float32)
    dst = (persp @ R.T) + np.float32([cx, cy])
    src = np.float32([[0, 0], [w0, 0], [w0, h0], [0, h0]])
    H = cv2.getPerspectiveTransform(src, dst.astype(np.float32))
    frame = background(rng)
    warped = cv2.warpPerspective(src_card, H, (CPU_W, CPU_H), flags=cv2.INTER_AREA)
    mask = cv2.warpPerspective(np.ones_like(src_card), H, (CPU_W, CPU_H))
    frame = frame * (1 - mask) + warped * mask
    if screen:
        # silau lampu di layar
        gx = np.linspace(-1, 1, CPU_W)[None, :]
        gy = np.linspace(-1, 1, CPU_H)[:, None]
        a = rng.uniform(0, np.pi)
        glare = np.clip(np.cos(a) * gx + np.sin(a) * gy, 0, 1) * rng.uniform(15, 100 if HARSH else 60)
        frame = frame + glare * mask
    # eksposur, buram (fokus/gerak), noise sensor
    frame = frame * rng.uniform(0.75, 1.15) + rng.uniform(-15, 15)
    if HARSH:
        # gerak tangan anak: buram ke satu arah
        L = int(rng.integers(3, 8))
        kern = np.zeros((L, L), np.float32)
        kern[L // 2, :] = 1.0 / L
        M = cv2.getRotationMatrix2D((L / 2 - 0.5, L / 2 - 0.5), rng.uniform(0, 180), 1.0)
        kern = cv2.warpAffine(kern, M, (L, L))
        frame = cv2.filter2D(frame, -1, kern / max(kern.sum(), 1e-6))
    frame = cv2.GaussianBlur(frame, (0, 0), rng.uniform(1.2, 2.2) if HARSH else rng.uniform(0.6, 1.4))
    frame = frame + rng.normal(0, rng.uniform(5, 10) if HARSH else rng.uniform(2, 6), frame.shape)
    return np.clip(frame, 0, 255).astype(np.uint8)


def recognize(db, frame, bf):
    kp, d = db['orb'].detectAndCompute(frame, None)
    if d is None or len(kp) < 8:
        return None, 0, 0
    matches = bf.knnMatch(d, db['desc'], k=2)
    per = {}
    for m in matches:
        if len(m) < 2:
            continue
        a, b = m
        if a.distance < 0.8 * b.distance:
            per.setdefault(int(db['owner'][a.trainIdx]), []).append((kp[a.queryIdx].pt, db['pts'][a.trainIdx]))
    scores = []
    for img, pairs in per.items():
        if len(pairs) < 6:
            scores.append((len(pairs) // 3, img))
            continue
        q = np.float32([p[0] for p in pairs])
        t = np.float32([p[1] for p in pairs])
        H, inl = cv2.findHomography(t, q, cv2.RANSAC, 5.0)
        scores.append((int(inl.sum()) if inl is not None else 0, img))
    if not scores:
        return None, 0, 0
    scores.sort(reverse=True)
    best, img = scores[0]
    second = scores[1][0] if len(scores) > 1 else 0
    if best >= MIN_INLIERS and best >= MARGIN * max(second, 1):
        return img, best, second
    return None, best, second


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('folders', nargs='*')
    ap.add_argument('--percobaan', type=int, default=6)
    ap.add_argument('--simpan', default='')
    ap.add_argument('--seed', type=int, default=11)
    ap.add_argument('--berat', action='store_true', help='kondisi berat: goyang, buram, silau kuat, kontras rendah')
    ap.add_argument('--terang', action='store_true', help='kartu di layar terlalu terang (eksposur kamera naik)')
    ap.add_argument('--redup', action='store_true', help='kartu di layar ditampilkan redup & kontras (filter situs)')
    a = ap.parse_args()
    global HARSH, BRIGHT, DIM
    HARSH = a.berat
    BRIGHT = a.terang
    DIM = a.redup
    folders = a.folders or [os.path.join(ROOT, 'Assets/Organo/AR/Cards'), os.path.join(ROOT, 'Assets/Organo/AR/CardsKartun')]
    styles = list(zip(['R', 'K'], folders))
    db = load_db(styles)
    bf = cv2.BFMatcher(cv2.NORM_HAMMING)
    rng = np.random.default_rng(a.seed)
    coverages = [0.08, 0.15, 0.25, 0.40]
    if a.simpan:
        os.makedirs(a.simpan, exist_ok=True)
    print(f'database: {len(db["names"])} gambar, rata-rata ciri per kartu {int(np.mean(db["counts"]))}')
    per_card = {n: [0, 0] for n in db['names']}
    rows = []
    for screen in (False, True):
        for cov in coverages:
            stat = {'R': [0, 0, 0], 'K': [0, 0, 0]}
            for i, name in enumerate(db['names']):
                style = name[0]
                for t in range(a.percobaan):
                    frame = capture(db['grays'][i], cov, screen, rng)
                    got, best, second = recognize(db, frame, bf)
                    if got == i:
                        stat[style][0] += 1
                    elif got is None:
                        stat[style][2] += 1
                    else:
                        stat[style][1] += 1
                    if cov >= 0.25:
                        per_card[name][0] += got == i
                        per_card[name][1] += 1
                    if a.simpan and t == 0 and i in (2, 17):
                        cv2.imwrite(os.path.join(a.simpan, f'{"layar" if screen else "kertas"}_{int(cov*100)}_{name.replace(":", "_")}.png'), frame)
            rows.append((screen, cov, stat))
    print()
    print('kondisi        luas   Realistis (benar/salah/tidak)   Kartun (benar/salah/tidak)')
    for screen, cov, stat in rows:
        def f(s):
            n = sum(s)
            return f'{100*s[0]/n:5.1f}% / {100*s[1]/n:4.1f}% / {100*s[2]/n:5.1f}%'
        print(f'{"layar" if screen else "kertas":<14} {int(cov*100):>3}%   {f(stat["R"]):<30}  {f(stat["K"])}')
    print()
    print('per kartu (ukuran >= 25%, kertas + layar):')
    worst = sorted(per_card.items(), key=lambda kv: kv[1][0] / max(1, kv[1][1]))
    for n, (ok, tot) in worst:
        print(f'  {n:<16} {100*ok/max(1,tot):5.1f}%')


if __name__ == '__main__':
    main()
