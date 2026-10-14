"""Membuat ikon aplikasi Organo (Android adaptive icon + ikon biasa) dari SVG, dirender dengan Microsoft Edge headless.

Hasil (Assets/Organo/Icon):
  Organo_Icon_Background.png  432x432  lapisan belakang adaptive icon (gradasi teal -> ungu, seperti layar pembuka)
  Organo_Icon_Foreground.png  432x432  lapisan depan: maskot Organo di dalam sudut bingkai scan (dalam zona aman 66dp)
  Organo_Icon.png             1024     ikon lengkap (bawaan / legacy)
  Organo_Icon_Round.png       1024     ikon bulat (Android round icon)
Ikon dipasang otomatis oleh Organo > Build > APK Android (OrganoAppIcon.Apply).

Pakai:  python Tools/ikon/buat_ikon.py
"""
import os, subprocess, tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, 'Assets', 'Organo', 'Icon')
EDGE = r'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'

# Satuan dp Android: kanvas 108x108, zona aman lingkaran diameter 66 di tengah (54,54).
BACKGROUND = '''
  <defs>
    <linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#1AB8A6"/>
      <stop offset="0.5" stop-color="#2E9CC0"/>
      <stop offset="1" stop-color="#6F52D1"/>
    </linearGradient>
  </defs>
  <rect width="108" height="108" fill="url(#g)"/>
  <circle cx="12" cy="10" r="34" fill="#FFFFFF" fill-opacity="0.10"/>
  <circle cx="100" cy="102" r="40" fill="#FFFFFF" fill-opacity="0.08"/>
'''

FOREGROUND = '''
  <!-- sudut bingkai scan (AR) -->
  <g fill="none" stroke="#FFFFFF" stroke-width="4.4" stroke-linecap="round" stroke-linejoin="round">
    <path d="M30 43V37Q30 30 37 30H43"/>
    <path d="M65 30H71Q78 30 78 37V43"/>
    <path d="M78 65V71Q78 78 71 78H65"/>
    <path d="M43 78H37Q30 78 30 71V65"/>
  </g>
  <!-- tangan: kiri melambai, kanan memegang pinggang -->
  <path d="M41.6 57.5L35.6 48.6" stroke="#E0674F" stroke-width="4.6" stroke-linecap="round"/>
  <path d="M41.6 57.5L35.6 48.6" stroke="#FF8A7A" stroke-width="3.4" stroke-linecap="round"/>
  <circle cx="34.9" cy="47.4" r="3.1" fill="#FF8A7A" stroke="#E0674F" stroke-width="0.7"/>
  <path d="M66.6 61.5L70.2 65.6" stroke="#E0674F" stroke-width="4.6" stroke-linecap="round"/>
  <path d="M66.6 61.5L70.2 65.6" stroke="#FF8A7A" stroke-width="3.4" stroke-linecap="round"/>
  <circle cx="70.9" cy="66.4" r="2.7" fill="#FF8A7A" stroke="#E0674F" stroke-width="0.7"/>
  <!-- badan maskot -->
  <circle cx="54" cy="56" r="15.5" fill="#1AB8A6" stroke="#FFFFFF" stroke-width="2.2"/>
  <ellipse cx="48" cy="46.6" rx="5" ry="2.6" transform="rotate(-28 48 46.6)" fill="#FFFFFF" fill-opacity="0.28"/>
  <ellipse cx="54" cy="65.4" rx="8.6" ry="4.9" fill="#7FE0D3"/>
  <path d="M54 68.6C51.6 67 50.9 65.8 51.4 64.8C52 63.9 53.3 64 54 65C54.7 64 56 63.9 56.6 64.8C57.1 65.8 56.4 67 54 68.6Z" fill="#FF6B8A"/>
  <!-- mata, pipi, senyum -->
  <circle cx="48.9" cy="54" r="3.7" fill="#FFFFFF"/>
  <circle cx="59.1" cy="54" r="3.7" fill="#FFFFFF"/>
  <circle cx="49.4" cy="54.6" r="2.1" fill="#2E2A4A"/>
  <circle cx="59.6" cy="54.6" r="2.1" fill="#2E2A4A"/>
  <circle cx="50.2" cy="53.7" r="0.75" fill="#FFFFFF"/>
  <circle cx="60.4" cy="53.7" r="0.75" fill="#FFFFFF"/>
  <circle cx="44" cy="59.4" r="2.1" fill="#FF8A7A"/>
  <circle cx="64" cy="59.4" r="2.1" fill="#FF8A7A"/>
  <path d="M50.5 59.2Q54 64 57.5 59.2Z" fill="#2E2A4A"/>
  <path d="M52.2 61.6Q54 63.2 55.8 61.6Q54 60.8 52.2 61.6Z" fill="#FF8A7A"/>
  <!-- kilau -->
  <path d="M70 34.5L71.2 37.8L74.5 39L71.2 40.2L70 43.5L68.8 40.2L65.5 39L68.8 37.8Z" fill="#FFD34E"/>
'''


def svg(body, px, extra_defs=''):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{px}" height="{px}" viewBox="0 0 108 108">'
            f'{extra_defs}{body}</svg>')


def render(svg_text, png_path, px):
    with tempfile.TemporaryDirectory() as tmp:
        src = os.path.join(tmp, 'ikon.svg')
        with open(src, 'w', encoding='utf8') as f:
            f.write(svg_text)
        subprocess.run([EDGE, '--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1',
                        f'--window-size={px},{px}', '--default-background-color=00000000',
                        f'--screenshot={png_path}', 'file:///' + src.replace('\\', '/')],
                       check=True, capture_output=True, timeout=60)


os.makedirs(OUT, exist_ok=True)
render(svg(BACKGROUND, 432), os.path.join(OUT, 'Organo_Icon_Background.png'), 432)
render(svg(FOREGROUND, 432), os.path.join(OUT, 'Organo_Icon_Foreground.png'), 432)
# ikon lengkap: isi diperbesar karena tidak ada potongan zona aman
full = BACKGROUND + '<g transform="translate(54 54) scale(1.28) translate(-54 -54)">' + FOREGROUND + '</g>'
render(svg(full, 1024), os.path.join(OUT, 'Organo_Icon.png'), 1024)
clip = '<defs><clipPath id="c"><circle cx="54" cy="54" r="54"/></clipPath></defs>'
render(svg('<g clip-path="url(#c)">' + full + '</g>', 1024, clip), os.path.join(OUT, 'Organo_Icon_Round.png'), 1024)
for f in sorted(os.listdir(OUT)):
    if f.endswith('.png'):
        print(f, os.path.getsize(os.path.join(OUT, f)), 'byte')
