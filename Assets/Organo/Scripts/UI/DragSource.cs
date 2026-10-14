using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Organo.UI
{
    /// <summary>
    /// Elemen yang bisa diseret lalu dilepas ke salah satu zona (kuis drag &amp; drop).
    /// Saat diseret, salinan elemen mengikuti jari; zona di bawah jari diberi kelas <c>drop-hover</c>.
    /// Ketukan tanpa menyeret memanggil <see cref="Tapped"/> (cara alternatif untuk anak yang belum bisa menyeret).
    /// </summary>
    public class DragSource : PointerManipulator
    {
        const float Threshold = 6f;
        readonly VisualElement m_Layer;
        readonly Func<VisualElement> m_MakeGhost;
        readonly Func<IList<VisualElement>> m_Zones;
        readonly Action<int> m_Dropped;
        int m_Pointer = -1;
        Vector2 m_Start;
        VisualElement m_Ghost;
        int m_Hover = -1;

        public Action Tapped;
        public bool Enabled = true;

        /// <param name="layer">Tempat salinan yang diseret digambar (biasanya Root layar).</param>
        /// <param name="zones">Zona tujuan (dievaluasi saat diseret).</param>
        /// <param name="dropped">Indeks zona tempat dilepas, -1 bila di luar zona.</param>
        public DragSource(VisualElement layer, Func<VisualElement> makeGhost, Func<IList<VisualElement>> zones, Action<int> dropped)
        {
            m_Layer = layer;
            m_MakeGhost = makeGhost;
            m_Zones = zones;
            m_Dropped = dropped;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.AddToClassList("drag-source");
            target.RegisterCallback<PointerDownEvent>(OnDown);
            target.RegisterCallback<PointerMoveEvent>(OnMove);
            target.RegisterCallback<PointerUpEvent>(OnUp);
            target.RegisterCallback<PointerCaptureOutEvent>(_ => Cancel());
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.RemoveFromClassList("drag-source");
            target.UnregisterCallback<PointerDownEvent>(OnDown);
            target.UnregisterCallback<PointerMoveEvent>(OnMove);
            target.UnregisterCallback<PointerUpEvent>(OnUp);
        }

        void OnDown(PointerDownEvent e)
        {
            if (!Enabled || m_Pointer >= 0) return;
            m_Pointer = e.pointerId;
            m_Start = e.position;
            target.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        void OnMove(PointerMoveEvent e)
        {
            if (e.pointerId != m_Pointer) return;
            Vector2 pos = e.position;
            if (m_Ghost == null)
            {
                if ((pos - m_Start).magnitude < Threshold) return;
                m_Ghost = m_MakeGhost();
                m_Ghost.pickingMode = PickingMode.Ignore;
                m_Ghost.style.position = Position.Absolute;
                m_Ghost.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
                m_Ghost.style.scale = new Scale(new Vector3(1.08f, 1.08f, 1f));
                m_Ghost.style.opacity = 0.92f;
                m_Ghost.AddToClassList("drag-ghost");
                m_Layer.Add(m_Ghost);
                target.style.opacity = 0.35f;
            }
            var local = m_Layer.WorldToLocal(pos);
            m_Ghost.style.left = local.x;
            m_Ghost.style.top = local.y;
            SetHover(ZoneAt(pos));
            e.StopPropagation();
        }

        void OnUp(PointerUpEvent e)
        {
            if (e.pointerId != m_Pointer) return;
            bool dragged = m_Ghost != null;
            int zone = dragged ? ZoneAt(e.position) : -1;
            Cleanup();
            if (target.HasPointerCapture(e.pointerId)) target.ReleasePointer(e.pointerId);
            e.StopPropagation();
            if (dragged) m_Dropped?.Invoke(zone);
            else if (target.ContainsPoint(target.WorldToLocal(e.position)))
            {
                OrganoAudio.Click();
                Tapped?.Invoke();
            }
        }

        void Cancel()
        {
            if (m_Pointer < 0) return;
            Cleanup();
        }

        void Cleanup()
        {
            m_Pointer = -1;
            SetHover(-1);
            if (m_Ghost != null) { m_Ghost.RemoveFromHierarchy(); m_Ghost = null; }
            target.style.opacity = StyleKeyword.Null;
        }

        int ZoneAt(Vector2 world)
        {
            var zones = m_Zones?.Invoke();
            if (zones == null) return -1;
            // zona terdekat yang memuat jari (dengan sedikit kelonggaran untuk jari anak)
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                if (z == null || z.panel == null) continue;
                var r = z.worldBound;
                var grown = new Rect(r.x - 14, r.y - 14, r.width + 28, r.height + 28);
                if (!grown.Contains(world)) continue;
                float d = (r.center - world).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        void SetHover(int index)
        {
            if (index == m_Hover) return;
            var zones = m_Zones?.Invoke();
            if (zones != null)
            {
                if (m_Hover >= 0 && m_Hover < zones.Count) zones[m_Hover]?.RemoveFromClassList("drop-hover");
                if (index >= 0 && index < zones.Count) zones[index]?.AddToClassList("drop-hover");
            }
            m_Hover = index;
        }
    }
}
