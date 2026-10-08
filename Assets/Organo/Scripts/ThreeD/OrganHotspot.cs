using UnityEngine;

namespace Organo.ThreeD
{
    /// <summary>
    /// Taruh komponen ini pada child kosong di dalam prefab model organ untuk menandai titik info
    /// (misalnya "Asam Lambung"). Titik ini muncul di close-up 3D dan hasil scan AR.
    /// </summary>
    public class OrganHotspot : MonoBehaviour
    {
        public string title = "Asam Lambung";
        [TextArea] public string description = "Membantu hancurkan makanan";

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.1f, 0.72f, 0.65f);
            Gizmos.DrawSphere(transform.position, 0.01f * transform.lossyScale.magnitude + 0.005f);
        }
    }
}
