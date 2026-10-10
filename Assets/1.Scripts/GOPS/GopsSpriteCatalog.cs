using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jaeby.Gops.Assets
{
    [CreateAssetMenu(fileName = "GopsSpriteCatalog", menuName = "GOPS/Sprite Catalog")]
    public sealed class GopsSpriteCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string id;
            public string category;
            public Sprite sprite;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        public IReadOnlyList<Entry> Entries => entries;

        public Sprite Find(string id)
        {
            foreach (Entry entry in entries)
                if (entry.id == id) return entry.sprite;
            return null;
        }

#if UNITY_EDITOR
        public void SetEntries(List<Entry> newEntries)
        {
            entries = newEntries;
        }
#endif
    }
}
