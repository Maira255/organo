using System.Collections.Generic;
using Organo.Data;
using UnityEngine;

namespace Organo.ThreeD
{
    /// <summary>
    /// Mode tambahan studio 3D:
    /// - Library: anak 3D utuh; saat area tubuh dipilih kamera mendekat, tubuh memudar dan organ di area itu terlihat.
    /// - Di tubuh: anak transparan, satu organ menyala.
    /// - Sorot bagian organ & model mikro (alveolus, vili, nefron, neuron).
    /// Organ di dalam tubuh memakai model ringan (LOD) supaya aman untuk HP.
    /// </summary>
    public partial class OrganViewer3D
    {
        public enum ViewMode { Organ, Body, Library, Micro, Inside }

        public ViewMode Mode { get; private set; } = ViewMode.Organ;
        public BodyRegion? FocusedRegion { get; private set; }

        class OrganRig
        {
            public OrganInfo organ;
            public Transform holder;
            public Renderer[] renderers;
            public Material[][] original;
            public Transform tap;
        }

        readonly List<OrganRig> m_Rigs = new List<OrganRig>();
        readonly List<Hotspot> m_RegionSpots = new List<Hotspot>();
        Renderer[] m_KidRenderers;
        Material[][] m_KidOpaque;
        Material[] m_KidFaded;
        Material m_Ghost;
        Vector3 m_FitPosTarget, m_FitScaleTarget = Vector3.one;
        bool m_FitAnimating;

        // sorot bagian
        Renderer[] m_ModelRenderers;
        Material[][] m_ModelOriginal;
        readonly List<Hotspot> m_SavedHotspots = new List<Hotspot>();
        Transform m_PartAnchor;
        bool m_PartHighlighted;
        /// <summary>Ada bagian organ yang sedang disorot (bagian lain dipudarkan).</summary>
        public bool PartHighlighted => m_PartHighlighted;
        Vector3 m_BaseFitPos, m_BaseFitScale = Vector3.one;

        static readonly Dictionary<BodyRegion, (Vector3 center, float size, Vector3 tap)> Regions = new Dictionary<BodyRegion, (Vector3, float, Vector3)>
        {
            { BodyRegion.Kepala, (new Vector3(0f, 1.11f, 0f), 0.46f, new Vector3(0.1f, 1.25f, -0.14f)) },
            { BodyRegion.Leher, (new Vector3(0f, 0.93f, 0f), 0.42f, new Vector3(0f, 0.96f, -0.07f)) },
            { BodyRegion.Dada, (new Vector3(0f, 0.75f, 0f), 0.52f, new Vector3(0f, 0.77f, -0.14f)) },
            { BodyRegion.Perut, (new Vector3(0f, 0.52f, 0f), 0.52f, new Vector3(0f, 0.53f, -0.13f)) },
        };

        public static readonly BodyRegion[] RegionOrder = { BodyRegion.Kepala, BodyRegion.Leher, BodyRegion.Dada, BodyRegion.Perut };

        // ------------------------------------------------------------ rig anak + organ

        // tulang (rangka) di mode tubuh: tersembunyi, tembus pandang (tidak menutupi organ), atau penuh (saat organ Rangka dibuka)
        GameObject m_Bones;
        Renderer[] m_BoneRenderers;
        Material[][] m_BoneOriginal;
        Material m_BoneGhost;
        public const string SkeletonId = "rangka";

        /// <summary>Tulang sedang ditampilkan di mode tubuh.</summary>
        public bool BonesVisible { get; private set; }
        public bool HasBones => m_Bones != null;

        void ClearModel()
        {
            m_Bones = null; m_BoneRenderers = null; m_BoneOriginal = null; BonesVisible = false;
            if (m_Model != null) Destroy(m_Model);
            m_Model = null;
            m_Hotspots.Clear();
            m_SavedHotspots.Clear();
            m_Rigs.Clear();
            m_RegionSpots.Clear();
            m_ModelRenderers = null;
            m_ModelOriginal = null;
            m_PartAnchor = null;
            m_PartHighlighted = false;
            m_FocusHolder = null;
            for (int i = m_Norm.childCount - 1; i >= 0; i--)
                if (m_Norm.GetChild(i) != m_Fit) Destroy(m_Norm.GetChild(i).gameObject);
            m_Norm.localScale = Vector3.one;
        }

        bool BuildRig()
        {
            var db = DatabaseProvider?.Invoke();
            var kid = BodyPrefabProvider?.Invoke();
            if (db == null || kid == null) return false;
            m_Ghost = db.ghostMaterial;
            bool girl = kid == db.bodyGirlPrefab;
            m_KidFaded = girl ? db.bodyGirlFaded : db.bodyBoyFaded;

            m_Model = new GameObject("Body Rig");
            m_Model.transform.SetParent(m_Fit, false);
            var body = Instantiate(kid, m_Model.transform, false);
            body.name = "Anak";
            m_KidRenderers = body.GetComponentsInChildren<Renderer>(true);
            m_KidOpaque = new Material[m_KidRenderers.Length][];
            for (int i = 0; i < m_KidRenderers.Length; i++) m_KidOpaque[i] = m_KidRenderers[i].sharedMaterials;

            foreach (var o in db.organs)
            {
                // organ reproduksi tidak ditempatkan di tubuh anak; rangka punya lapisan sendiri (lihat ShowBones)
                if (o == null || o.system == OrganSystem.Reproduksi || o.id == SkeletonId) continue;
                var prefab = o.BodyPrefab;
                var go = prefab != null ? Instantiate(prefab) : OrganPlaceholder.Build(o.id, o.accent);
                foreach (var h in go.GetComponentsInChildren<OrganHotspot>(true)) h.enabled = false;
                var holder = new GameObject("Organ " + o.id).transform;
                holder.SetParent(m_Model.transform, false);
                go.transform.SetParent(holder, false);
                var (pos, size) = OrganDefaults.BodyPlacement(o.id);
                var b = LocalBounds(go, holder);
                float max = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                float s = max > 1e-5f ? size / max : 1f;
                go.transform.localScale = Vector3.one * s;
                go.transform.localPosition = -b.center * s;
                holder.localPosition = pos;
                // titik ketuk organ: sedikit di depan organ supaya menghadap kamera
                var tap = new GameObject("Ketuk " + o.id).transform;
                tap.SetParent(m_Model.transform, false);
                tap.localPosition = pos + new Vector3(0f, 0f, -Mathf.Max(0.03f, b.size.z * s * 0.5f));
                var rig = new OrganRig { organ = o, holder = holder, tap = tap, renderers = go.GetComponentsInChildren<Renderer>(true) };
                rig.original = new Material[rig.renderers.Length][];
                for (int i = 0; i < rig.renderers.Length; i++) rig.original[i] = rig.renderers[i].sharedMaterials;
                m_Rigs.Add(rig);
            }
            var skeleton = db.Get(SkeletonId);
            if (skeleton != null && skeleton.modelPrefab != null)
            {
                // rangka dibuat di koordinat yang sama dengan model anak: cukup dipasang tanpa geser/skala
                m_Bones = Instantiate(skeleton.modelPrefab, m_Model.transform, false);
                m_Bones.name = "Rangka";
                foreach (var h in m_Bones.GetComponentsInChildren<OrganHotspot>(true)) h.enabled = false;
                m_BoneRenderers = m_Bones.GetComponentsInChildren<Renderer>(true);
                m_BoneOriginal = new Material[m_BoneRenderers.Length][];
                for (int i = 0; i < m_BoneRenderers.Length; i++) m_BoneOriginal[i] = m_BoneRenderers[i].sharedMaterials;
                if (m_BoneGhost == null && m_Ghost != null)
                {
                    m_BoneGhost = new Material(m_Ghost) { name = "Tulang Tembus Pandang" };
                    var c = new Color(0.97f, 0.92f, 0.8f, 0.3f);
                    if (m_BoneGhost.HasProperty("_BaseColor")) m_BoneGhost.SetColor("_BaseColor", c); else m_BoneGhost.color = c;
                }
                SetBonesState(0);
            }
            PrepareModel(m_Model);
            return true;
        }

        /// <summary>0 tersembunyi, 1 tembus pandang tipis (organ tetap terlihat), 2 warna penuh.</summary>
        void SetBonesState(int state)
        {
            BonesVisible = state > 0;
            if (m_BoneRenderers == null) return;
            for (int i = 0; i < m_BoneRenderers.Length; i++)
            {
                var r = m_BoneRenderers[i];
                r.enabled = state > 0;
                if (state == 2 || m_BoneGhost == null) { r.sharedMaterials = m_BoneOriginal[i]; continue; }
                var mats = new Material[m_BoneOriginal[i].Length];
                for (int k = 0; k < mats.Length; k++) mats[k] = m_BoneGhost;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>
        /// Tampilkan tulang tembus pandang di mode "Tubuh" untuk melihat tulang mana yang melindungi organ.
        /// Organ yang dipilih tetap terlihat jelas karena tulangnya hanya garis tipis transparan.
        /// </summary>
        public void ShowBones(bool on)
        {
            if (Mode != ViewMode.Body || (m_Organ != null && m_Organ.id == SkeletonId)) return;
            SetBonesState(on ? 1 : 0);
        }

        void SetKidFaded(bool faded)
        {
            if (m_KidRenderers == null) return;
            for (int i = 0; i < m_KidRenderers.Length; i++)
            {
                var src = m_KidOpaque[i];
                if (!faded || m_KidFaded == null || m_KidFaded.Length == 0) { m_KidRenderers[i].sharedMaterials = src; continue; }
                var mats = new Material[src.Length];
                for (int k = 0; k < mats.Length; k++) mats[k] = k < m_KidFaded.Length && m_KidFaded[k] != null ? m_KidFaded[k] : src[k];
                m_KidRenderers[i].sharedMaterials = mats;
            }
        }

        /// <summary>0 tersembunyi, 1 pudar, 2 warna penuh.</summary>
        void SetOrganState(OrganRig rig, int state)
        {
            for (int i = 0; i < rig.renderers.Length; i++)
            {
                var r = rig.renderers[i];
                r.enabled = state > 0;
                if (state == 2 || m_Ghost == null) { r.sharedMaterials = rig.original[i]; continue; }
                var mats = new Material[rig.original[i].Length];
                for (int k = 0; k < mats.Length; k++) mats[k] = m_Ghost;
                r.sharedMaterials = mats;
            }
        }

        void FrameTo(Vector3 center, float size, bool snap)
        {
            float s = size > 1e-5f ? 1f / size : 1f;
            m_FitScaleTarget = Vector3.one * s;
            m_FitPosTarget = -center * s;
            m_FitAnimating = !snap;
            if (snap) { m_Fit.localScale = m_FitScaleTarget; m_Fit.localPosition = m_FitPosTarget; }
        }

        void UpdateFitAnimation(float dt)
        {
            if (!m_FitAnimating) return;
            float k = 1f - Mathf.Exp(-6f * dt);
            m_Fit.localScale = Vector3.Lerp(m_Fit.localScale, m_FitScaleTarget, k);
            m_Fit.localPosition = Vector3.Lerp(m_Fit.localPosition, m_FitPosTarget, k);
            if ((m_Fit.localPosition - m_FitPosTarget).sqrMagnitude < 1e-7f && (m_Fit.localScale - m_FitScaleTarget).sqrMagnitude < 1e-7f) m_FitAnimating = false;
        }

        // ------------------------------------------------------------ mode "Di tubuh"

        public void LoadBody(OrganInfo focus)
        {
            ClearModel();
            m_Organ = focus;
            if (!BuildRig()) { Load(focus); return; }
            Mode = ViewMode.Body;
            BodyMode = true;
            IsPlaceholder = false;
            if (focus != null) SetBackdrop(focus.tint);
            SetKidFaded(true);
            foreach (var rig in m_Rigs)
            {
                bool isFocus = focus != null && rig.organ.id == focus.id;
                SetOrganState(rig, isFocus ? 2 : 1);
                if (isFocus) m_FocusHolder = rig.holder;
            }
            bool skeleton = focus != null && focus.id == SkeletonId;
            SetBonesState(skeleton ? 2 : 0);
            var place = OrganDefaults.BodyPlacement(focus != null ? focus.id : "");
            if (skeleton) FrameTo(new Vector3(0f, 0.66f, 0f), 1.42f, true);
            else FrameTo(place.pos, Mathf.Clamp(place.size * 2.6f, 0.42f, 0.7f), true);
            ResetView();
        }

        // ------------------------------------------------------------ mode Library

        public void AttachLibrary(object owner, Color backdrop)
        {
            m_Owner = owner;
            m_Camera.enabled = true;
            if (Mode != ViewMode.Library || m_Model == null || m_LibraryKid != BodyPrefabProvider?.Invoke())
                LoadLibrary(backdrop);
        }

        GameObject m_LibraryKid;

        public void LoadLibrary(Color backdrop)
        {
            ClearModel();
            m_Organ = null;
            if (!BuildRig()) return;
            m_LibraryKid = BodyPrefabProvider?.Invoke();
            Mode = ViewMode.Library;
            BodyMode = false;
            IsPlaceholder = false;
            SetBackdrop(backdrop);
            foreach (var reg in RegionOrder)
            {
                var a = new GameObject("Area " + reg).transform;
                a.SetParent(m_Model.transform, false);
                a.localPosition = Regions[reg].tap;
                m_RegionSpots.Add(new Hotspot { anchor = a, title = OrganDefaults.RegionName(reg), description = "", key = reg.ToString() });
            }
            FocusRegion(null, true);
            m_Yaw = 0f; m_Pitch = 0f; m_YawVel = 0f; m_Zoom = 1f; m_Pan = Vector2.zero;
            ApplyRotation();
        }

        /// <summary>Dekati satu area tubuh (null = seluruh tubuh).</summary>
        public void FocusRegion(BodyRegion? region, bool snap = false)
        {
            FocusedRegion = region;
            if (Mode != ViewMode.Library) return;
            SetKidFaded(region.HasValue);
            foreach (var rig in m_Rigs)
                SetOrganState(rig, !region.HasValue ? 0 : rig.organ.region == region.Value ? 2 : 1);
            if (region.HasValue) FrameTo(Regions[region.Value].center, Regions[region.Value].size, snap);
            else FrameTo(new Vector3(0f, 0.7f, 0f), 1.5f, snap);

            // titik ketuk: area tubuh (seluruh tubuh) atau organ-organ di area yang didekati
            m_Hotspots.Clear();
            if (!region.HasValue) m_Hotspots.AddRange(m_RegionSpots);
            else
                foreach (var rig in m_Rigs)
                    if (rig.organ.region == region.Value)
                        m_Hotspots.Add(new Hotspot { anchor = rig.tap, title = rig.organ.displayName, description = rig.organ.shortDescription, key = rig.organ.id });
            m_Yaw = 0f; m_Pitch = 0f; m_YawVel = 0f;
            m_LastInput = Time.unscaledTime;
            m_Zoom = 1f;
            m_Pan = Vector2.zero;
        }

        /// <summary>Sorot satu organ di area yang sedang didekati (organ lain di area itu dipudarkan).</summary>
        public void HighlightOrganInRegion(string organId)
        {
            if (Mode != ViewMode.Library || !FocusedRegion.HasValue) return;
            foreach (var rig in m_Rigs)
            {
                bool inRegion = rig.organ.region == FocusedRegion.Value;
                SetOrganState(rig, !inRegion ? 1 : organId == null || rig.organ.id == organId ? 2 : 1);
            }
        }

        // ------------------------------------------------------------ sorot bagian organ

        void CaptureModelMaterials()
        {
            if (m_Model == null) return;
            m_ModelRenderers = m_Model.GetComponentsInChildren<Renderer>(true);
            m_ModelOriginal = new Material[m_ModelRenderers.Length][];
            for (int i = 0; i < m_ModelRenderers.Length; i++) m_ModelOriginal[i] = m_ModelRenderers[i].sharedMaterials;
            m_BaseFitPos = m_Fit.localPosition;
            m_BaseFitScale = m_Fit.localScale;
        }

        static bool IsInner(Renderer r) => r.gameObject.name == "Inner";

        static readonly Dictionary<(Mesh, int), Bounds> s_SubMeshBounds = new Dictionary<(Mesh, int), Bounds>();
        static readonly List<Vector3> s_Verts = new List<Vector3>();

        /// <summary>
        /// Batas sebenarnya satu submesh, dihitung dari segitiganya. Batas bawaan (SubMeshDescriptor.bounds) di model SDF
        /// mencakup rentang verteks yang dipakai bersama, jadi tiap bagian rangka "sebesar" seluruh rangka: tengkorak tidak
        /// pernah didekati. Hasilnya disimpan supaya cukup dihitung sekali.
        /// </summary>
        public static Bounds SubMeshBounds(Mesh mesh, int sub)
        {
            if (mesh == null || sub < 0 || sub >= mesh.subMeshCount) return default;
            if (s_SubMeshBounds.TryGetValue((mesh, sub), out var b)) return b;
            b = mesh.GetSubMesh(sub).bounds;
            if (mesh.isReadable)
            {
                s_Verts.Clear();
                mesh.GetVertices(s_Verts);
                var idx = mesh.GetIndices(sub);
                bool init = false;
                foreach (int i in idx)
                {
                    if (i < 0 || i >= s_Verts.Count) continue;
                    if (!init) { b = new Bounds(s_Verts[i], Vector3.zero); init = true; } else b.Encapsulate(s_Verts[i]);
                }
            }
            s_SubMeshBounds[(mesh, sub)] = b;
            return b;
        }

        // versi transparan dari warna asli (organ tetap terlihat seperti dirinya, bagian terpilih menonjol)
        readonly Dictionary<Material, Material> m_FadedCache = new Dictionary<Material, Material>();

        Material Faded(Material src)
        {
            if (src == null || m_Ghost == null) return m_Ghost;
            if (m_FadedCache.TryGetValue(src, out var m) && m != null) return m;
            m = new Material(m_Ghost) { name = src.name + " (pudar)" };
            Color c = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.color;
            c = Color.Lerp(c, Color.white, 0.35f);
            c.a = 0.22f;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); else m.color = c;
            m_FadedCache[src] = m;
            return m;
        }

        /// <summary>Sorot submesh tertentu (bagian lain dipudarkan). null/kosong = kembali normal.</summary>
        public void HighlightPart(int[] materials, string title, string description)
        {
            if ((Mode != ViewMode.Organ && Mode != ViewMode.Inside) || m_ModelRenderers == null) return;
            if (m_Ghost == null) m_Ghost = DatabaseProvider?.Invoke()?.ghostMaterial;
            bool clear = materials == null || materials.Length == 0;
            // belum ada yang disorot: tidak ada yang perlu dipulihkan (titik info asli jangan sampai terhapus)
            if (clear && !m_PartHighlighted) return;
            if (!m_PartHighlighted && m_Hotspots.Count > 0) { m_SavedHotspots.Clear(); m_SavedHotspots.AddRange(m_Hotspots); }
            m_PartHighlighted = !clear;
            if (m_PartAnchor != null) { Destroy(m_PartAnchor.gameObject); m_PartAnchor = null; }
            for (int i = 0; i < m_ModelRenderers.Length; i++)
            {
                var src = m_ModelOriginal[i];
                if (IsInner(m_ModelRenderers[i])) m_ModelRenderers[i].enabled = !clear;
                if (clear || m_Ghost == null) { m_ModelRenderers[i].sharedMaterials = src; continue; }
                var mats = new Material[src.Length];
                for (int k = 0; k < mats.Length; k++) mats[k] = System.Array.IndexOf(materials, k) >= 0 ? src[k] : Faded(src[k]);
                m_ModelRenderers[i].sharedMaterials = mats;
            }
            m_Hotspots.Clear();
            if (clear)
            {
                m_Hotspots.AddRange(m_SavedHotspots);
                m_SavedHotspots.Clear();
                m_FitPosTarget = m_BaseFitPos; m_FitScaleTarget = m_BaseFitScale; m_FitAnimating = true;
                return;
            }

            // gabungan batas semua bagian yang disorot (dalam ruang model), titik info di depannya
            Bounds b = default; bool init = false;
            foreach (var r in m_ModelRenderers)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mesh = mf.sharedMesh;
                var toModel = m_Model.transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
                foreach (int m in materials)
                {
                    if (m < 0 || m >= mesh.subMeshCount) continue;
                    var sb = SubMeshBounds(mesh, m);
                    if (sb.size == Vector3.zero) continue;
                    for (int c = 0; c < 8; c++)
                    {
                        var p = toModel.MultiplyPoint3x4(sb.center + Vector3.Scale(sb.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1)));
                        if (!init) { b = new Bounds(p, Vector3.zero); init = true; } else b.Encapsulate(p);
                    }
                }
            }
            if (!init) return;
            m_PartAnchor = new GameObject("Part Anchor").transform;
            m_PartAnchor.SetParent(m_Model.transform, false);
            // titik info di sisi bagian yang menghadap kamera (model potongan bisa diputar menyamping)
            var toCam = m_Model.transform.InverseTransformDirection(-m_Camera.transform.forward).normalized;
            float reach = Mathf.Abs(toCam.x) * b.extents.x + Mathf.Abs(toCam.y) * b.extents.y + Mathf.Abs(toCam.z) * b.extents.z;
            m_PartAnchor.localPosition = b.center + toCam * reach;
            m_Hotspots.Add(new Hotspot { anchor = m_PartAnchor, title = title, description = description });

            // bagian yang dipilih dipusatkan & selalu muat utuh di kartu (maks. 70% tinggi tampilan),
            // bagian kecil didekati sedikit (maks. 1,35x). Rangka (model setinggi badan): tengkorak, tangan, dsb.
            // didekati sampai memenuhi kartu (maks. 3x), kalau tidak bagiannya terlalu kecil untuk dilihat.
            float s0 = m_BaseFitScale.x;
            float partSize = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) * s0;
            float maxK = m_Organ != null && m_Organ.id == SkeletonId && Mode == ViewMode.Organ ? 3f : 1.35f;
            float zoomK = Mathf.Clamp(0.7f / Mathf.Max(partSize, 1e-3f), 1f, maxK);
            var fullCenter = -m_BaseFitPos / Mathf.Max(s0, 1e-5f);
            var centerInFit = m_Model.transform.localPosition + b.center;
            // bagian besar (>= 70%) tetap di tengah organ supaya organ tidak bergeser jauh
            float follow = zoomK > 1.001f ? 1f : Mathf.Clamp01(1.2f - partSize);
            var focus = Vector3.Lerp(fullCenter, centerInFit, follow);
            m_FitScaleTarget = m_BaseFitScale * zoomK;
            m_FitPosTarget = -focus * s0 * zoomK;
            m_FitAnimating = true;
            m_Zoom = 1f;
            m_Pan = Vector2.zero;
        }

        /// <summary>Tampilkan model mikro (bagian yang diperbesar). Organ dipulihkan dengan Load(organ).</summary>
        public void LoadMicro(GameObject prefab, OrganInfo organ)
        {
            if (prefab == null) return;
            ClearModel();
            m_Organ = organ;
            Mode = ViewMode.Micro;
            BodyMode = false;
            IsPlaceholder = false;
            if (organ != null) SetBackdrop(organ.tint);
            m_Model = Instantiate(prefab);
            m_Model.transform.SetParent(m_Fit, false);
            PrepareModel(m_Model);
            FitToUnit();
            m_FitAnimating = false;
            foreach (var h in m_Model.GetComponentsInChildren<OrganHotspot>(true))
                m_Hotspots.Add(new Hotspot { anchor = h.transform, title = h.title, description = h.description });
            ResetView();
        }
    }
}
