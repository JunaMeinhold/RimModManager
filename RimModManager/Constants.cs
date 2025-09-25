namespace RimModManager
{
    using System;
    using System.Collections.Generic;

    public static class Constants
    {
        public static readonly Dictionary<string, string> WellKnownNames = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ludeon.rimworld", "Core" },
            { "ludeon.rimworld.royalty", "Royalty" },
            { "ludeon.rimworld.ideology", "Ideology" },
            { "ludeon.rimworld.biotech", "Biotech" },
            { "ludeon.rimworld.anomaly", "Anomaly" },
            { "ludeon.rimworld.odyssey", "Odyssey" },
        };

        public static readonly HashSet<string> KnownTierOneMods = new(StringComparer.OrdinalIgnoreCase)
        {
            "zetrith.prepatcher",
            "brrainz.harmony",
            "me.samboycoding.betterloading.dev",
            "ludeon.rimworld",
            "ludeon.rimworld.royalty",
            "ludeon.rimworld.ideology",
            "ludeon.rimworld.biotech",
            "ludeon.rimworld.anomaly",
            "ludeon.rimworld.odyssey",
            "unlimitedhugs.hugslib"
        };
    }
}
