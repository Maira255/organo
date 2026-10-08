using System.Collections.Generic;
using UnityEngine;

namespace Organo.UI
{
    public static partial class OrganoIcons
    {
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
