using System.Collections.Generic;
using UnityEngine;

namespace Organo.UI
{
    public static partial class OrganoIcons
    {
        // Ilustrasi tambahan yang belum ada di canvas desain (ginjal / Sistem Ekskresi),
        // digambar dengan gaya yang sama: bentuk isi flat, outline 1 tingkat lebih gelap, wajah kecil.
        const string Kidney =
            "<path d=\"M30 6C16 6 8 18 8 32s8 26 22 26c9 0 15-6 15-12 0-5-6-8-6-14s6-9 6-14C45 11 39 6 30 6z\" fill=\"#E06A7E\" stroke=\"#B8455B\" stroke-width=\"1.5\" stroke-linejoin=\"round\"/>" +
            "<path d=\"M14 22c2-6 7-10 13-10\" stroke=\"#F7A8B5\" stroke-width=\"3\" fill=\"none\" stroke-linecap=\"round\"/>";
        const string KidneyVessels =
            "<path d=\"M40 28c6-1 11-4 16-7\" stroke=\"#EF476F\" stroke-width=\"4\" fill=\"none\" stroke-linecap=\"round\"/>" +
            "<path d=\"M40 34c6 0 11 1 16 3\" stroke=\"#5B8DEF\" stroke-width=\"4\" fill=\"none\" stroke-linecap=\"round\"/>" +
            "<path d=\"M38 40c4 4 6 10 6 20\" stroke=\"#F2B705\" stroke-width=\"3.5\" fill=\"none\" stroke-linecap=\"round\"/>";

        static OrganoIcons()
        {
            Svg["organ_ginjal"] = "<svg viewBox=\"0 0 64 64\">" + KidneyVessels + Kidney +
                "<circle cx=\"19\" cy=\"31\" r=\"2.2\" fill=\"#2E2A4A\"/><circle cx=\"30\" cy=\"31\" r=\"2.2\" fill=\"#2E2A4A\"/>" +
                "<path d=\"M21 37q3.5 3 7 0\" stroke=\"#2E2A4A\" stroke-width=\"1.6\" fill=\"none\" stroke-linecap=\"round\"/></svg>";
            Svg["mini_ginjal"] = "<svg viewBox=\"0 0 64 64\">" + KidneyVessels + Kidney + "</svg>";
            Svg["cat_ekskresi"] = "<svg viewBox=\"0 0 24 24\"><path d=\"M11 3C6 3 3 7 3 12s3 9 8 9c3 0 5-2 5-4 0-2-2-3-2-5s2-3 2-5c0-2-2-4-5-4z\" fill=\"#E06A7E\"/><path d=\"M15 11h6\" stroke=\"#F2B705\" stroke-width=\"2\" stroke-linecap=\"round\"/></svg>";
            Svg["region_back"] = Svg["mini_ginjal"];

            // Hati: lobus kanan besar, lobus kiri meruncing, pita ligamen kuning, kantong empedu hijau
            const string Liver =
                "<path d=\"M6 30C6 18 16 12 30 12C42 12 54 14 59 20C61 25 54 28 46 30C40 33 35 43 26 48C14 53 6 43 6 30Z\" fill=\"#C8414B\" stroke=\"#A3333C\" stroke-width=\"1.5\" stroke-linejoin=\"round\"/>" +
                "<path d=\"M34 13C32 22 31 30 29 44\" stroke=\"#F2B705\" stroke-width=\"3\" fill=\"none\" stroke-linecap=\"round\"/>" +
                "<path d=\"M12 24c2-5 6-8 12-9\" stroke=\"#E87B83\" stroke-width=\"3\" fill=\"none\" stroke-linecap=\"round\"/>" +
                "<ellipse cx=\"25\" cy=\"50\" rx=\"5.5\" ry=\"4\" fill=\"#3E9B4F\" stroke=\"#2E7A3C\" stroke-width=\"1\"/>";
            Svg["organ_hati"] = "<svg viewBox=\"0 0 64 64\">" + Liver +
                "<circle cx=\"16\" cy=\"30\" r=\"2.2\" fill=\"#2E2A4A\"/><circle cx=\"26\" cy=\"30\" r=\"2.2\" fill=\"#2E2A4A\"/>" +
                "<path d=\"M18 36q3 3 6 0\" stroke=\"#2E2A4A\" stroke-width=\"1.6\" fill=\"none\" stroke-linecap=\"round\"/></svg>";
            Svg["mini_hati"] = "<svg viewBox=\"0 0 64 64\">" + Liver + "</svg>";

            // Pankreas: kelenjar memanjang krem dengan usus dua belas jari (C) di kepalanya
            const string Pancreas =
                "<path d=\"M16 14C5 18 3 40 14 50\" stroke=\"#D9555E\" stroke-width=\"6\" fill=\"none\" stroke-linecap=\"round\"/>" +
                "<path d=\"M10 34C10 26 17 24 23 26C31 28 39 22 47 20C53 18 59 20 59 25C59 29 53 31 47 33C39 37 31 43 21 45C13 47 10 41 10 34Z\" fill=\"#E9C39B\" stroke=\"#C99A6B\" stroke-width=\"1.5\" stroke-linejoin=\"round\"/>" +
                "<circle cx=\"30\" cy=\"34\" r=\"1.6\" fill=\"#D4A577\"/><circle cx=\"40\" cy=\"29\" r=\"1.6\" fill=\"#D4A577\"/><circle cx=\"50\" cy=\"25\" r=\"1.4\" fill=\"#D4A577\"/>";
            Svg["organ_pankreas"] = "<svg viewBox=\"0 0 64 64\">" + Pancreas +
                "<circle cx=\"18\" cy=\"33\" r=\"1.9\" fill=\"#2E2A4A\"/><circle cx=\"25\" cy=\"32\" r=\"1.9\" fill=\"#2E2A4A\"/>" +
                "<path d=\"M18.5 38q3 2.5 6 0\" stroke=\"#2E2A4A\" stroke-width=\"1.5\" fill=\"none\" stroke-linecap=\"round\"/></svg>";
            Svg["mini_pankreas"] = "<svg viewBox=\"0 0 64 64\">" + Pancreas + "</svg>";

            // Otak: dua belahan dengan lipatan, otak kecil & batang otak
            const string Brain =
                "<path d=\"M30 52v8\" stroke=\"#E7A0A0\" stroke-width=\"6\" stroke-linecap=\"round\"/>" +
                "<ellipse cx=\"38\" cy=\"48\" rx=\"10\" ry=\"6\" fill=\"#F2C46B\" stroke=\"#D9A743\" stroke-width=\"1.2\"/>" +
                "<path d=\"M31 10C22 6 10 10 8 21C2 25 4 38 10 41C12 49 22 52 31 48Z\" fill=\"#F29BAE\" stroke=\"#D97A92\" stroke-width=\"1.5\" stroke-linejoin=\"round\"/>" +
                "<path d=\"M33 10C42 6 54 10 56 21C62 25 60 38 54 41C52 49 42 52 33 48Z\" fill=\"#F6B3C2\" stroke=\"#D97A92\" stroke-width=\"1.5\" stroke-linejoin=\"round\"/>" +
                "<path d=\"M14 20c4 2 8 0 10 4M10 31c5-1 8 2 12 0M15 42c3-3 7-2 10-5M50 20c-4 2-8 0-10 4M54 31c-5-1-8 2-12 0M49 42c-3-3-7-2-10-5\" stroke=\"#D97A92\" stroke-width=\"1.6\" fill=\"none\" stroke-linecap=\"round\"/>";
            Svg["organ_otak"] = "<svg viewBox=\"0 0 64 64\">" + Brain +
                "<circle cx=\"24\" cy=\"28\" r=\"2.2\" fill=\"#2E2A4A\"/><circle cx=\"40\" cy=\"28\" r=\"2.2\" fill=\"#2E2A4A\"/>" +
                "<path d=\"M29 34q3 3 6 0\" stroke=\"#2E2A4A\" stroke-width=\"1.6\" fill=\"none\" stroke-linecap=\"round\"/></svg>";
            Svg["mini_otak"] = "<svg viewBox=\"0 0 64 64\">" + Brain + "</svg>";

            // suara pembaca: speaker dicoret (mati), tombol berhenti, dan ikon pengaturan suara pembaca
            const string Stroke = " fill=\"none\" stroke=\"#2E2A4A\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"";
            Svg["sound_off"] = "<svg viewBox=\"0 0 24 24\"" + Stroke + "><path d=\"M4 9v6h4l5 4V5L8 9z\"/><path d=\"M16.5 9.5l5 5M21.5 9.5l-5 5\"/></svg>";
            Svg["stop"] = "<svg viewBox=\"0 0 24 24\"><rect x=\"6\" y=\"6\" width=\"12\" height=\"12\" rx=\"2.5\" fill=\"#2E2A4A\"/></svg>";
            Svg["play"] = "<svg viewBox=\"0 0 24 24\"><path d=\"M8 5.5v13a1 1 0 0 0 1.5.86l10.5-6.5a1 1 0 0 0 0-1.72L9.5 4.64A1 1 0 0 0 8 5.5z\" fill=\"#2E2A4A\"/></svg>";
            Svg["set_voice"] = "<svg viewBox=\"0 0 24 24\"" + Stroke + "><path d=\"M4 5h16v10H10l-5 4v-4H4z\"/><path d=\"M8 9h8M8 12h5\"/></svg>";
            RegisterKidAndSenses();
        }

        public static string Get(string name)
        {
            if (name != null && Svg.TryGetValue(name, out var s)) return s;
            Debug.LogWarning("[Organo] Unknown icon: " + name);
            return null;
        }

        /// <summary>Fills {{holes}} of a template icon (e.g. body_template) with values.</summary>
        public static string Fill(string name, IDictionary<string, string> values)
        {
            var s = Get(name);
            if (s == null) return null;
            foreach (var kv in values) s = s.Replace("{{" + kv.Key + "}}", kv.Value);
            return s;
        }

        /// <summary>Icon key of the 2D illustration for an organ id.</summary>
        public static string Organ(string organId) => "organ_" + organId;
    }
}
