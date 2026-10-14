"""Membuat rekaman suara narasi Organo dengan layanan suara AI (lebih manusiawi daripada suara HP).

Langkah:
  1. Unity: Organo > Suara > Ekspor Teks Narasi        -> Tools/suara/narasi.json
  2. Isi kunci API di Tools/suara/kunci.txt (TIDAK ikut git), salah satu:
        ELEVENLABS_API_KEY=...            (opsional: ELEVENLABS_VOICE_ID=...)
        AZURE_SPEECH_KEY=...              AZURE_SPEECH_REGION=southeastasia   (opsional: AZURE_VOICE=id-ID-GadisNeural)
  3. Coba dulu 3 teks:   python Tools/suara/buat_suara.py --layanan elevenlabs --uji
     Semua teks:         python Tools/suara/buat_suara.py --layanan elevenlabs
  4. Unity: Organo > Suara > Pasang Rekaman Narasi       (lalu build APK)

Hasil: Assets/Organo/Audio/Narasi/<organ>_tentang.mp3, <organ>_faktaN.mp3, <organ>_kartu.mp3.
Berkas yang sudah ada dilewati (pakai --ulang untuk membuat ulang). Hanya pustaka standar Python.
"""
import argparse, json, os, sys, time, urllib.request, urllib.error
from xml.sax.saxutils import escape

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(ROOT, 'Assets', 'Organo', 'Audio', 'Narasi')

# Suara bawaan: ElevenLabs "Sarah" (lembut, jelas; Multilingual v2 berbahasa Indonesia). Bisa diganti dengan suara
# Indonesia dari Voice Library ElevenLabs lewat ELEVENLABS_VOICE_ID.
DEFAULT_ELEVEN_VOICE = 'EXAVITQu4vr4xnSDxMaL'
DEFAULT_AZURE_VOICE = 'id-ID-GadisNeural'


def load_keys():
    keys = dict(os.environ)
    path = os.path.join(HERE, 'kunci.txt')
    if os.path.exists(path):
        for line in open(path, encoding='utf8'):
            line = line.strip()
            if line and not line.startswith('#') and '=' in line:
                k, v = line.split('=', 1)
                keys[k.strip()] = v.strip()
    return keys


def eleven(text, keys):
    voice = keys.get('ELEVENLABS_VOICE_ID') or DEFAULT_ELEVEN_VOICE
    url = f'https://api.elevenlabs.io/v1/text-to-speech/{voice}?output_format=mp3_44100_64'
    body = json.dumps({
        'text': text,
        'model_id': keys.get('ELEVENLABS_MODEL', 'eleven_multilingual_v2'),
        # suara stabil & hangat untuk anak; sedikit ekspresif
        'voice_settings': {'stability': 0.55, 'similarity_boost': 0.75, 'style': 0.2, 'use_speaker_boost': True, 'speed': 0.95},
    }).encode('utf8')
    req = urllib.request.Request(url, data=body, method='POST', headers={
        'xi-api-key': keys['ELEVENLABS_API_KEY'], 'Content-Type': 'application/json', 'Accept': 'audio/mpeg'})
    with urllib.request.urlopen(req, timeout=120) as r:
        return r.read()


def azure(text, keys):
    region = keys.get('AZURE_SPEECH_REGION', 'southeastasia')
    voice = keys.get('AZURE_VOICE') or DEFAULT_AZURE_VOICE
    ssml = (f"<speak version='1.0' xml:lang='id-ID'><voice name='{voice}'>"
            f"<prosody rate='-6%' pitch='+2%'>{escape(text)}</prosody></voice></speak>")
    req = urllib.request.Request(f'https://{region}.tts.speech.microsoft.com/cognitiveservices/v1', data=ssml.encode('utf8'), method='POST', headers={
        'Ocp-Apim-Subscription-Key': keys['AZURE_SPEECH_KEY'], 'Content-Type': 'application/ssml+xml',
        'X-Microsoft-OutputFormat': 'audio-24khz-48kbitrate-mono-mp3', 'User-Agent': 'organo-narasi'})
    with urllib.request.urlopen(req, timeout=120) as r:
        return r.read()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--layanan', choices=['elevenlabs', 'azure'], required=True)
    ap.add_argument('--uji', action='store_true', help='hanya 3 teks pertama (untuk mendengar contoh dulu)')
    ap.add_argument('--ulang', action='store_true', help='buat ulang walau berkas sudah ada')
    ap.add_argument('--hanya', default='', help='hanya organ tertentu, mis. jantung,paru')
    a = ap.parse_args()
    keys = load_keys()
    need = 'ELEVENLABS_API_KEY' if a.layanan == 'elevenlabs' else 'AZURE_SPEECH_KEY'
    if not keys.get(need):
        sys.exit(f'Kunci {need} belum ada. Isi di {os.path.join(HERE, "kunci.txt")} (berkas ini tidak ikut git).')
    data = json.load(open(os.path.join(ROOT, 'Tools', 'suara', 'narasi.json'), encoding='utf8'))
    items = data['items']
    if a.hanya:
        only = tuple(x.strip() + '_' for x in a.hanya.split(','))
        items = [i for i in items if i['file'].startswith(only)]
    if a.uji:
        items = items[:3]
    os.makedirs(OUT, exist_ok=True)
    speak = eleven if a.layanan == 'elevenlabs' else azure
    done = skipped = chars = 0
    for it in items:
        path = os.path.join(OUT, it['file'] + '.mp3')
        if os.path.exists(path) and not a.ulang:
            skipped += 1
            continue
        for attempt in range(4):
            try:
                audio = speak(it['text'], keys)
                break
            except urllib.error.HTTPError as e:
                msg = e.read().decode('utf8', 'replace')[:300]
                if e.code in (429, 500, 502, 503) and attempt < 3:
                    time.sleep(3 * (attempt + 1)); continue
                sys.exit(f'Gagal ({e.code}) untuk {it["file"]}: {msg}')
        open(path, 'wb').write(audio)
        done += 1
        chars += len(it['text'])
        print(f'  {it["file"]}.mp3  ({len(audio) // 1024} KB)')
    print(f'Selesai: {done} rekaman baru ({chars} karakter), {skipped} sudah ada. Lalu di Unity: Organo > Suara > Pasang Rekaman Narasi.')


if __name__ == '__main__':
    main()
