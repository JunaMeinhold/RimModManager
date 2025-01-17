namespace RimModManager.RimWorld.Fluffy
{
    using RimModManager;
    using System;
    using System.Xml;

    public struct FluffyModDependency
    {
        public string Name { get; set; }

        public FluffyVersionComparison Comparsion { get; set; }

        public RimVersion Version { get; set; }

        public void Read(XmlReader reader)
        {
            string content = reader.ReadElementContentAsString();
            ReadOnlySpan<char> span = content;

            int i = 0;
            while (!span.IsEmpty && i < 3)
            {
                int idx = span.IndexOf(' ');
                if (idx == -1) idx = span.Length;
                var part = span[..idx];

                switch (i)
                {
                    case 0:
                        Name = part.ToString();
                        break;

                    case 1:
                        Comparsion = ParseComparison(part);
                        break;

                    case 2:
                        Version = RimVersion.Parse(part);
                        break;
                }

                if (idx == span.Length) break;
                span = span[(idx + 1)..];
                i++;
            }
        }

        private static FluffyVersionComparison ParseComparison(ReadOnlySpan<char> comparison)
        {
            return comparison switch
            {
                ">=" => FluffyVersionComparison.GreaterEquals,
                "<=" => FluffyVersionComparison.LessEquals,
                ">" => FluffyVersionComparison.Greater,
                "<" => FluffyVersionComparison.Less,
                "==" => FluffyVersionComparison.Equals,
                "!=" => FluffyVersionComparison.NotEquals,
                _ => FluffyVersionComparison.Unknown,
            };
        }
    }
}