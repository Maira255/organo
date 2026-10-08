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
