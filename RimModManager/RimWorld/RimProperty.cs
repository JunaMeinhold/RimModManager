namespace RimModManager.RimWorld
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public struct RimProperty
    {
        public const int BaseOffset = 8;

        [FieldOffset(0)]
        public RimPropertyType Type;

        [FieldOffset(BaseOffset)] public bool Bool;
        [FieldOffset(BaseOffset)] public byte U8;
        [FieldOffset(BaseOffset)] public sbyte I8;
        [FieldOffset(BaseOffset)] public ushort U16;
        [FieldOffset(BaseOffset)] public short I16;
        [FieldOffset(BaseOffset)] public uint U32;
        [FieldOffset(BaseOffset)] public int I32;
        [FieldOffset(BaseOffset)] public ulong U64;
        [FieldOffset(BaseOffset)] public long I64;
        [FieldOffset(BaseOffset + 8)] public object? Object;

        public static RimPropertyType TToType<T>()
        {
            Type t = typeof(T);
            if (t == typeof(bool)) return RimPropertyType.Bool;
            if (t == typeof(byte)) return RimPropertyType.U8;
            if (t == typeof(sbyte)) return RimPropertyType.I8;
            if (t == typeof(ushort)) return RimPropertyType.U16;
            if (t == typeof(short)) return RimPropertyType.I16;
            if (t == typeof(uint)) return RimPropertyType.U32;
            if (t == typeof(int)) return RimPropertyType.I32;
            if (t == typeof(ulong)) return RimPropertyType.U64;
            if (t == typeof(long)) return RimPropertyType.I64;
            return RimPropertyType.Object;
        }

        public readonly T? Get<T>(T? defaultValue = default) where T : class
        {
            if (TToType<T>() != Type) return defaultValue;
            return Object as T ?? defaultValue;
        }

        public readonly unsafe T? Get<T>(T? defaultValue = default) where T : unmanaged
        {
            if (TToType<T>() != Type) return defaultValue;
            ulong v = U64;
            return *(T*)&v;
        }

        public void Set<T>(T? value) where T : class
        {
            Type = RimPropertyType.Object;
            Object = value;
        }

        public unsafe void Set<T>(T? value) where T : unmanaged
        {
            Type = TToType<T>();
            switch (Type)
            {
                case RimPropertyType.Bool:
                case RimPropertyType.U8:
                case RimPropertyType.I8:
                    U8 = *(byte*)&value;
                    break;

                case RimPropertyType.U16:
                case RimPropertyType.I16:
                    U16 = *(ushort*)&value;
                    break;

                case RimPropertyType.U32:
                case RimPropertyType.I32:
                    U32 = *(uint*)&value;
                    break;

                case RimPropertyType.U64:
                case RimPropertyType.I64:
                    U64 = *(ulong*)&value;
                    break;
            }
        }
    }
}