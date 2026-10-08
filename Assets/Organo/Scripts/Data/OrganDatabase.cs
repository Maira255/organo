using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Organo.Data
{
    /// <summary>Daftar semua organ. Asset dibuat oleh menu Organo &gt; Setup; kalau kosong, isi bawaan dipakai.</summary>
    [CreateAssetMenu(menuName = "Organo/Organ Database", fileName = "OrganDatabase")]
    public class OrganDatabase : ScriptableObject
    {
        public List<OrganInfo> organs = new List<OrganInfo>();

        Dictionary<string, OrganInfo> m_ById;

        public static OrganDatabase CreateDefault()
        {
            var db = CreateInstance<OrganDatabase>();
            foreach (var id in OrganDefaults.Order) db.organs.Add(OrganDefaults.Create(id));
            return db;
        }

        public void EnsureComplete()
        {
            organs.RemoveAll(o => o == null);
            foreach (var id in OrganDefaults.Order)
                if (organs.All(o => o.id != id)) organs.Add(OrganDefaults.Create(id));
            m_ById = null;
        }

        public OrganInfo Get(string id)
        {
            if (m_ById == null)
            {
                m_ById = new Dictionary<string, OrganInfo>();
                foreach (var o in organs) if (o != null) m_ById[o.id] = o;
            }
            return id != null && m_ById.TryGetValue(id, out var r) ? r : null;
        }

        public OrganInfo FindByReferenceImage(string imageName)
        {
            return organs.FirstOrDefault(o => o != null && (o.ReferenceImageName == imageName || o.id == imageName || o.displayName == imageName));
        }

        public IEnumerable<OrganInfo> BySystem(OrganSystem s) => organs.Where(o => o != null && o.system == s).OrderBy(o => o.cardNumber);
        public IEnumerable<OrganInfo> ByRegion(BodyRegion r) => organs.Where(o => o != null && o.region == r).OrderBy(o => o.cardNumber);
        public IEnumerable<OrganInfo> Ordered => organs.Where(o => o != null).OrderBy(o => o.cardNumber);
        public int Count => organs.Count(o => o != null);
    }
}
