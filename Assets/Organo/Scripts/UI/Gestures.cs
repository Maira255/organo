using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Organo.UI
{
    /// <summary>
    /// Dasar gestur geser yang andal: setelah jari menekan elemen, gerakan & lepasnya dilacak dari akar panel,
    /// jadi tetap terbaca walau jari bergerak cepat keluar dari elemen (status tidak pernah "tersangkut").
    /// Begitu arah geser jelas, pointer diambil alih (ketukan tombol di bawah jari dibatalkan).
    /// </summary>
    public abstract class TrackedGesture : PointerManipulator
    {
        /// <summary>Log diagnostik (dipakai uji otomatis).</summary>
        public static bool Trace;
        void Log(string m) { if (Trace) Debug.Log("[Organo][Gestur] " + GetType().Name + ": " + m); }

        protected int Pointer = -1;
        protected Vector2 Start, Last;
        protected bool Dragging;
        protected Vector2 Velocity;
        protected IEventHandler StartTarget;
        double m_LastTime;
        VisualElement m_Root;

        protected override void RegisterCallbacksOnTarget()
        {
            target.AddToClassList("gesture-host");
            target.RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
            // setelah gestur mengunci pointer, event hanya dikirim ke elemen ini (tidak lewat akar panel)
            target.RegisterCallback<PointerMoveEvent>(OnMoveCaptured);
            target.RegisterCallback<PointerUpEvent>(OnUpCaptured);
            target.RegisterCallback<PointerCancelEvent>(OnCancelCaptured);
            target.RegisterCallback<DetachFromPanelEvent>(_ => Stop());
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerMoveEvent>(OnMoveCaptured);
            target.UnregisterCallback<PointerUpEvent>(OnUpCaptured);
            target.UnregisterCallback<PointerCancelEvent>(OnCancelCaptured);
            Stop();
        }

        bool Captured => Pointer >= 0 && target != null && target.HasPointerCapture(Pointer);
        void OnMoveCaptured(PointerMoveEvent e) { if (Captured) HandleMove(e); }
        void OnUpCaptured(PointerUpEvent e) { if (Captured) HandleUp(e); }
        void OnCancelCaptured(PointerCancelEvent e) { if (Captured) HandleCancel(e); }

        /// <summary>Boleh mulai melacak dari ketukan ini?</summary>
        protected virtual bool Accept(PointerDownEvent e) => true;
        /// <summary>0 = tunggu, 1 = mulai menyeret, -1 = bukan gestur ini.</summary>
        protected abstract int Decide(Vector2 total);
        protected virtual void OnBegin() { }
        protected virtual void OnDragStart() { }
        protected abstract void OnDrag(Vector2 total, Vector2 delta);
        protected abstract void OnDragEnd(Vector2 total, Vector2 velocity);

        protected static bool Within(IEventHandler t, VisualElement ancestor, VisualElement stop)
        {
            for (var e = t as VisualElement; e != null && e != stop; e = e.parent)
                if (e == ancestor) return true;
            return false;
        }

        protected static bool HasClassUpTo(IEventHandler t, VisualElement stop, params string[] classes)
        {
            for (var e = t as VisualElement; e != null && e != stop; e = e.parent)
                foreach (var c in classes)
                    if (e.ClassListContains(c)) return true;
            return false;
        }

        void OnDown(PointerDownEvent e)
        {
            if (e.pointerType == UnityEngine.UIElements.PointerType.mouse && e.button != 0) return;
            if (!Accept(e)) { Log("ditolak di " + (e.target as VisualElement)?.name); return; }
            Stop();
            Pointer = e.pointerId;
            Start = Last = e.position;
            StartTarget = e.target;
            Dragging = false;
            Velocity = Vector2.zero;
            m_LastTime = Time.realtimeSinceStartupAsDouble;
            m_Root = target.panel?.visualTree;
            Log("mulai " + e.position + " root=" + (m_Root != null));
            if (m_Root != null)
            {
                m_Root.RegisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
                m_Root.RegisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
                m_Root.RegisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
            }
            OnBegin();
        }

        // dari akar panel: hanya selama pointer belum dikunci gestur ini
        void OnMove(PointerMoveEvent e) { if (!Captured) HandleMove(e); }
        void OnUp(PointerUpEvent e) { if (!Captured) HandleUp(e); }
        void OnCancel(PointerCancelEvent e) { if (!Captured) HandleCancel(e); }

        void HandleMove(PointerMoveEvent e)
        {
            if (Trace && !Dragging) Log("move id=" + e.pointerId + "/" + Pointer + " pos=" + e.position + " btn=" + e.pressedButtons);
            if (e.pointerId != Pointer) return;
            // jari sudah terangkat tapi event "lepas" tidak sampai ke sini (dikunci elemen lain): berhenti
            if (e.pressedButtons == 0) { Stop(); return; }
            Vector2 pos = e.position;
            if (!Dragging)
            {
                // gestur lain (rail di dalamnya, kartu yang diseret, model 3D) sudah mengambil alih
                var cap = target.panel?.GetCapturingElement(Pointer) as VisualElement;
                if (cap != null && cap != target && (cap.ClassListContains("gesture-host") || cap.ClassListContains("drag-source") || cap.ClassListContains("viewport3d")))
                { Stop(); return; }
                int decision = Decide(pos - Start);
                Log("gerak " + (pos - Start) + " -> " + decision);
                if (decision < 0) { Stop(); return; }
                if (decision == 0) return;
                Dragging = true;
                target.CapturePointer(Pointer);
                TapGesture.CancelAll(); // kartu yang tadinya ditekan tidak jadi diketuk
                OnDragStart();
            }
            double now = Time.realtimeSinceStartupAsDouble;
            float dt = Mathf.Max(0.001f, (float)(now - m_LastTime));
            Velocity = Vector2.Lerp(Velocity, (pos - Last) / dt, 0.5f);
            m_LastTime = now;
            var delta = pos - Last;
            Last = pos;
            OnDrag(pos - Start, delta);
            e.StopPropagation();
        }

        void HandleUp(PointerUpEvent e)
        {
            if (Trace) Log("up id=" + e.pointerId + "/" + Pointer + " drag=" + Dragging + " pos=" + e.position);
            if (e.pointerId != Pointer) return;
            bool dragged = Dragging;
            var total = (Vector2)e.position - Start;
            if (Time.realtimeSinceStartupAsDouble - m_LastTime > 0.1) Velocity = Vector2.zero;
            var vel = Velocity;
            Stop();
            if (!dragged) return;
            e.StopPropagation();
            OnDragEnd(total, vel);
        }

        void HandleCancel(PointerCancelEvent e)
        {
            if (e.pointerId != Pointer) return;
            bool dragged = Dragging;
            var total = Last - Start;
            Stop();
            if (dragged) OnDragEnd(total, Vector2.zero);
        }

        protected void Stop()
        {
            if (m_Root != null)
            {
                m_Root.UnregisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
                m_Root.UnregisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
                m_Root.UnregisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
                m_Root = null;
            }
            if (Pointer >= 0 && target != null && target.HasPointerCapture(Pointer)) target.ReleasePointer(Pointer);
            Pointer = -1;
            Dragging = false;
        }
    }

    /// <summary>Tarik bottom sheet ke bawah untuk menutupnya (seperti di HP pada umumnya).</summary>
    public class SwipeToDismiss : TrackedGesture
    {
        readonly Action m_Dismiss;
        public SwipeToDismiss(Action dismiss) { m_Dismiss = dismiss; }

        protected override bool Accept(PointerDownEvent e) =>
            !HasClassUpTo(e.target, target, "drag-source", "viewport3d");

        protected override int Decide(Vector2 d)
        {
            if (d.y < -8f) return -1;
            if (d.y < 8f) return 0;
            if (Mathf.Abs(d.x) > d.y) return -1;
            // isi yang bisa digulir & sedang tidak di paling atas: biarkan digulir
            for (var e = StartTarget as VisualElement; e != null && e != target; e = e.parent)
                if (e is ScrollView sv && sv.mode != ScrollViewMode.Horizontal && sv.scrollOffset.y > 1f) return -1;
            return 1;
        }

        protected override void OnDragStart()
        {
            target.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { 0f };
        }

        protected override void OnDrag(Vector2 total, Vector2 delta)
        {
            target.style.translate = new Translate(0, Mathf.Max(0f, total.y));
        }

        protected override void OnDragEnd(Vector2 total, Vector2 velocity)
        {
            float h = Mathf.Max(1f, target.layout.height);
            target.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { "translate" };
            target.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { 0.22f };
            if (total.y > h * 0.25f || velocity.y > 700f)
            {
                target.style.translate = StyleKeyword.Null;
                target.style.transitionDuration = StyleKeyword.Null;
                target.style.transitionProperty = StyleKeyword.Null;
                m_Dismiss?.Invoke();
            }
            else
            {
                target.style.translate = new Translate(0, 0);
                var t = target;
                t.schedule.Execute(() => { t.style.translate = StyleKeyword.Null; t.style.transitionDuration = StyleKeyword.Null; t.style.transitionProperty = StyleKeyword.Null; }).ExecuteLater(240);
            }
        }
    }
}

namespace Organo.UI
{
    /// <summary>
    /// Ketukan yang TIDAK mengunci pointer (berbeda dengan Clickable bawaan). Dengan begitu, kalau jari bergeser,
    /// rail/halaman/sheet di belakangnya tetap menerima gerakan dan bisa digeser. Ketukan dibatalkan kalau
    /// jari bergerak lebih dari 12 px atau dilepas di luar elemen.
    /// </summary>
    public class TapGesture : PointerManipulator
    {
        const float Slop = 12f;
        static readonly System.Collections.Generic.List<TapGesture> s_Active = new System.Collections.Generic.List<TapGesture>();
        readonly Action m_Action;
        int m_Pointer = -1;
        Vector2 m_Start;

        public TapGesture(Action action) { m_Action = action; }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnDown);
            target.RegisterCallback<PointerMoveEvent>(OnMove);
            target.RegisterCallback<PointerUpEvent>(OnUp);
            target.RegisterCallback<PointerCancelEvent>(OnCancelEvt);
            target.RegisterCallback<PointerLeaveEvent>(OnLeave);
            target.RegisterCallback<NavigationSubmitEvent>(OnSubmit);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnDown);
            target.UnregisterCallback<PointerMoveEvent>(OnMove);
            target.UnregisterCallback<PointerUpEvent>(OnUp);
            target.UnregisterCallback<PointerCancelEvent>(OnCancelEvt);
            target.UnregisterCallback<PointerLeaveEvent>(OnLeave);
            target.UnregisterCallback<NavigationSubmitEvent>(OnSubmit);
        }

        void OnCancelEvt(PointerCancelEvent e) => Cancel();
        void OnLeave(PointerLeaveEvent e) => target.RemoveFromClassList("pressed");
        void OnSubmit(NavigationSubmitEvent e) => m_Action?.Invoke();

        void OnDown(PointerDownEvent e)
        {
            if (e.pointerType == UnityEngine.UIElements.PointerType.mouse && e.button != 0) return;
            m_Pointer = e.pointerId;
            m_Start = e.position;
            target.AddToClassList("pressed");
            if (!s_Active.Contains(this)) s_Active.Add(this);
        }

        /// <summary>Batalkan semua ketukan yang sedang ditekan (dipanggil saat geseran dimulai).</summary>
        public static void CancelAll()
        {
            for (int i = s_Active.Count - 1; i >= 0; i--) s_Active[i].Cancel();
            s_Active.Clear();
        }

        void OnMove(PointerMoveEvent e)
        {
            if (e.pointerId != m_Pointer) return;
            if (((Vector2)e.position - m_Start).magnitude > Slop) Cancel();
        }

        void OnUp(PointerUpEvent e)
        {
            if (e.pointerId != m_Pointer) return;
            bool inside = target.ContainsPoint(target.WorldToLocal(e.position));
            bool still = ((Vector2)e.position - m_Start).magnitude <= Slop;
            Cancel();
            if (inside && still)
            {
                e.StopPropagation();
                m_Action?.Invoke();
            }
        }

        void Cancel()
        {
            m_Pointer = -1;
            s_Active.Remove(this);
            target?.RemoveFromClassList("pressed");
        }
    }
}
