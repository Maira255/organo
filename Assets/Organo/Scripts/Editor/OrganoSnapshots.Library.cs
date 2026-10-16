using Organo.Screens;
using Organo.ThreeD;
using Organo.UI;
using UnityEngine.UIElements;

namespace Organo.EditorTools
{
    /// <summary>Uji Library terkunci: organ terbuka setelah kartunya di-scan.</summary>
    public static partial class OrganoSnapshots
    {
        static void GiveAllCards(Organo.Data.PlayerProgress p)
        {
            foreach (var o in App.Database.organs)
                if (o != null && !p.HasCard(o.id)) p.AddCard(o.id, 0);
        }

        static void TestLibraryLock()
        {
            var ginjal = App.Database.Get("ginjal");
            App.Progress.cards.Remove("ginjal");
            Report("Library: organ yang kartunya belum di-scan terkunci", Parts.Locked(ginjal) && !Parts.Locked(App.Database.Get("lambung")));
            App.Go<LibraryListScreen>(null, false);
            Parts.OpenOrgan(App, ginjal);
            Report("Library: membuka organ terkunci menampilkan ajakan scan kartunya (tidak membuka organnya)",
                App.Current is LibraryListScreen && AdaTeks(UiRoot, "Ginjal masih terkunci") && AdaTeks(UiRoot, "Scan kartu Ginjal"));
            App.CloseTopOverlay();
            // dibuka langsung (mis. dari Beranda / Kuis): halaman terkunci, tanpa model 3D
            App.Go<OrganDetailScreen>("ginjal", false);
        }

        static void TestLibraryLockDetail()
        {
            Report("Library: halaman organ terkunci tanpa model 3D, dengan tombol Scan kartu",
                App.Current is OrganDetailScreen && App.Current.Root.Q<OrganViewport>() == null
                && AdaTeks(App.Current.Root, "Masih terkunci") && AdaTeks(App.Current.Root, "Scan kartu Ginjal"));
            App.Progress.AddCard("ginjal", 0);
            App.Go<OrganDetailScreen>("ginjal", false);
            Report("Library: setelah kartunya di-scan, organ terbuka", App.Current.Root.Q<OrganViewport>() != null);
        }
    }
}
