using Organo.Data;
using UnityEditor;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// "Database" akun lokal Organo di Editor: lihat semua akun & progresnya, buat akun uji, hapus akun.
    /// Data yang ditampilkan sama dengan yang dipakai saat Play di Editor (PlayerPrefs proyek ini).
    /// </summary>
    public class OrganoAccountsWindow : EditorWindow
    {
        Vector2 m_Scroll;
        string m_Selected;

        [MenuItem("Organo/Akun Uji/Database Akun", priority = 60)]
        public static void Open() => GetWindow<OrganoAccountsWindow>("Database Akun Organo");

        [MenuItem("Organo/Akun Uji/Buat Akun Uji", priority = 61)]
        public static void Seed()
        {
            AccountStore.ReloadCache();
            int n = AccountStore.SeedTestAccounts();
            Debug.Log("[Organo] " + n + " akun uji dibuat dari Editor/TestData/OrganoTestAccounts.json.");
        }

        void OnGUI()
        {
            AccountStore.ReloadCache();
            EditorGUILayout.HelpBox("Akun Organo tersimpan lokal (PlayerPrefs). Password & PIN disimpan sebagai hash. " +
                                    "Akun uji: lihat Assets/Organo/Editor/TestData/OrganoTestAccounts.json (tidak ikut build rilis).", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Buat / Perbarui Akun Uji")) { Seed(); Repaint(); }
                if (GUILayout.Button("Keluar dari Akun Aktif")) { AccountStore.SignOut(); Repaint(); }
                if (GUILayout.Button("Hapus Semua Akun") && EditorUtility.DisplayDialog("Organo", "Hapus semua akun & progres di Editor?", "Hapus", "Batal"))
                { AccountStore.WipeAll(); Repaint(); }
            }
            EditorGUILayout.Space();
            var current = AccountStore.Current;
            EditorGUILayout.LabelField("Akun aktif", current != null ? "@" + current.username : "(belum masuk)");
            EditorGUILayout.Space();

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            foreach (var acc in AccountStore.Accounts)
            {
                var p = AccountStore.LoadProgress(acc);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("@" + acc.username + "  ·  " + acc.nickname, EditorStyles.boldLabel);
                        if (GUILayout.Button(m_Selected == acc.id ? "Tutup" : "Lihat Progres", GUILayout.Width(100)))
                            m_Selected = m_Selected == acc.id ? null : acc.id;
                        if (GUILayout.Button("Hapus", GUILayout.Width(60)) && EditorUtility.DisplayDialog("Organo", "Hapus akun @" + acc.username + "?", "Hapus", "Batal"))
                        { AccountStore.DeleteForDeveloper(acc); GUIUtility.ExitGUI(); }
                    }
                    EditorGUILayout.LabelField("Umur " + acc.ageRange + " · gender " + (Gender)acc.gender + " · dibuat " + acc.created + " · izin ortu " + acc.consentDate);
                    EditorGUILayout.LabelField("Bintang " + p.stars + " · kartu " + p.cards.Count + " · organ dibuka " + p.openedOrgans.Count +
                                               " · kuis " + p.QuizzesCompleted + " · favorit " + p.favorites.Count);
                    if (m_Selected == acc.id)
                        EditorGUILayout.TextArea(AccountStore.ProgressJson(acc), GUILayout.MinHeight(80));
                }
            }
            if (AccountStore.Accounts.Count == 0) EditorGUILayout.LabelField("Belum ada akun. Tekan \"Buat / Perbarui Akun Uji\".");
            EditorGUILayout.EndScrollView();
        }
    }
}
