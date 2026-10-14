using Organo.Data;
using UnityEngine.UIElements;

namespace Organo
{
    public enum NavTab { None, Beranda, Library, Scan, Quiz, Profil }

    /// <summary>
    /// Satu layar Organo. Isi dibangun ulang setiap kali layar dibuka supaya data (bintang, kartu, dsb.) selalu baru.
    /// </summary>
    public abstract class AppScreen
    {
        public VisualElement Root { get; private set; }
        protected OrganoApp App { get; private set; }
        protected PlayerProgress P => App.Progress;
        protected OrganDatabase Db => App.Database;

        /// <summary>Tab navbar yang aktif; None = navbar disembunyikan.</summary>
        public virtual NavTab Tab => NavTab.None;
        /// <summary>Layar AR: latar transparan supaya feed kamera terlihat.</summary>
        public virtual bool UsesCamera => false;
        /// <summary>Layar sampai tepi bawah layar (tanpa jarak area aman bawah); layar sendiri yang memberi jarak pada isinya.</summary>
        public virtual bool FullBleedBottom => false;

        internal void Init(OrganoApp app)
        {
            App = app;
            Root = new VisualElement();
            Root.AddToClassList("screen");
            Root.AddToClassList("col");
            Root.name = GetType().Name;
        }

        internal void Open(object arg)
        {
            Root.Clear();
            Root.EnableInClassList("transparent", UsesCamera);
            Build(arg);
            KeepBarsFixed(Root);
        }

        /// <summary>
        /// Hanya area gulir (ScrollView) dan elemen yang sengaja Grow yang boleh mengecil. Bar atas/bawah, judul, kotak tetap,
        /// tidak pernah dimampatkan: tanpa ini, isi yang panjang membuat bar bawah ikut mengecil sehingga tombolnya keluar
        /// layar dan teks keluar dari kotaknya (mis. tombol "Mulai Quiz" di detail organ, kotak "Benar! Hebat!" di kuis).
        /// </summary>
        static void KeepBarsFixed(VisualElement root)
        {
            foreach (var child in root.Children())
            {
                if (child is ScrollView || child.ClassListContains("fill") || child.ClassListContains("grow") || child.ClassListContains("scroll")) continue;
                var pos = child.style.position;
                if (pos.keyword == StyleKeyword.Undefined && pos.value == Position.Absolute) continue;
                if (child.style.flexShrink.keyword == StyleKeyword.Undefined) continue; // sudah diatur sendiri
                var grow = child.style.flexGrow;
                if (grow.keyword == StyleKeyword.Undefined && grow.value > 0f) continue;
                child.style.flexShrink = 0f;
            }
        }

        protected abstract void Build(object arg);

        /// <summary>Dipanggil saat layar ditutup / diganti.</summary>
        public virtual void Close() { }

        /// <summary>Dipanggil tiap frame selama layar aktif.</summary>
        public virtual void Tick(float dt) { }

        /// <summary>Tombol kembali Android. Return true kalau sudah ditangani.</summary>
        public virtual bool OnBack() => false;

        /// <summary>Bangun ulang layar yang sedang terbuka dengan argumen yang sama.</summary>
        protected void Refresh(object arg = null) => Open(arg);
    }
}
