namespace RimModManager
{
    public struct RimVersion : IEquatable<RimVersion>
    {
        public int Major;
        public int Minor;
        public int Patch;
        public int Revision;

        public RimVersion(int major, int minor, int patch, int revision)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Revision = revision;
        }

        public readonly RimVersion ToCompareVersion()
        {
            return new(Major, Minor, 0, 0);
        }

        public static unsafe RimVersion Parse(ReadOnlySpan<char> value)
        {
            ReadOnlySpan<char> span = value.Trim();
            // Version = "1.5.4297 rev1078";

            RimVersion version = default;
            int* pResult = (int*)&version;
            int i = 0;
            while (!span.IsEmpty && i < 4)
            {
                int idx0 = span.IndexOfAny(['.', ' ']);
                if (idx0 == -1) idx0 = span.Length;
                var part = span[..idx0];
                if (part.StartsWith("rev")) break;
                pResult[i++] = int.Parse(span[..idx0]);
                if (idx0 == span.Length) return version;
                span = span[(idx0 + 1)..];
            }

            int idx1 = span.IndexOf("rev");
            if (idx1 == -1) return version;
            span = span[(idx1 + 3)..];
            version.Revision = int.Parse(span);
            return version;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is RimVersion version && Equals(version);
        }

        public readonly bool Equals(RimVersion other)
        {
            return Major == other.Major &&
                   Minor == other.Minor &&
                   Patch == other.Patch &&
                   Revision == other.Revision;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Patch, Revision);
        }

        public static bool operator ==(RimVersion left, RimVersion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RimVersion left, RimVersion right)
        {
            return !(left == right);
        }
    }
}