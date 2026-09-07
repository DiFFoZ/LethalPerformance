using System.Runtime.InteropServices;

namespace LethalPerformance.Audio;

[StructLayout(LayoutKind.Sequential, Size = 8)]
internal unsafe struct OffsetPtr
{
    public long Offset;

    public readonly bool IsEmpty => Offset == 0;

    public static byte* Get(OffsetPtr* field)
    {
        return (byte*)field + field->Offset;
    }

    public static T* Get<T>(OffsetPtr* field) where T : unmanaged
    {
        return (T*)Get(field);
    }

    public static void Set(OffsetPtr* field, void* target)
    {
        field->Offset = (byte*)target - (byte*)field;
    }

    public static void Clear(OffsetPtr* field)
    {
        field->Offset = 0;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal struct UnityGuid
{
    [FieldOffset(0)] public long Lo;
    [FieldOffset(8)] public long Hi;
}

[StructLayout(LayoutKind.Explicit, Size = 0x10)]
internal struct GroupConstant
{
    [FieldOffset(0x00)] public int ParentConstantIndex;
    [FieldOffset(0x04)] public int VolumeIndex;
    [FieldOffset(0x08)] public int PitchIndex;
    [FieldOffset(0x0C)] public byte Mute;
    [FieldOffset(0x0D)] public byte Solo;
    [FieldOffset(0x0E)] public byte BypassEffects;
}

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
internal struct EffectConstant
{
    [FieldOffset(0x00)] public int Type;
    [FieldOffset(0x04)] public int GroupConstantIndex;
    [FieldOffset(0x08)] public int SendTargetEffectIndex;
    [FieldOffset(0x0C)] public int WetMixLevelIndex;
    [FieldOffset(0x10)] public int PrevEffectIndex;
    [FieldOffset(0x14)] public byte Bypass;
    [FieldOffset(0x18)] public int ParameterCount;
    [FieldOffset(0x20)] public OffsetPtr ParameterIndices;
}

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
internal struct SnapshotConstant
{
    [FieldOffset(0x00)] public uint NameHash;
    [FieldOffset(0x04)] public int ValueCount;
    [FieldOffset(0x08)] public OffsetPtr Values;
    [FieldOffset(0x14)] public int TransitionCount;
    [FieldOffset(0x18)] public OffsetPtr TransitionTypes;
    [FieldOffset(0x20)] public OffsetPtr TransitionIndices;
}

[StructLayout(LayoutKind.Explicit, Size = 0x98)]
internal struct AudioMixerConstant
{
    [FieldOffset(0x00)] public int GroupCount;
    [FieldOffset(0x08)] public OffsetPtr Groups;
    [FieldOffset(0x10)] public OffsetPtr GroupGuids;
    [FieldOffset(0x18)] public int EffectCount;
    [FieldOffset(0x20)] public OffsetPtr Effects;
    [FieldOffset(0x28)] public OffsetPtr EffectGuids;
    [FieldOffset(0x38)] public int SnapshotCount;
    [FieldOffset(0x40)] public OffsetPtr Snapshots;
    [FieldOffset(0x50)] public int ParameterCount;
    [FieldOffset(0x58)] public OffsetPtr Names;
    [FieldOffset(0x80)] public int ExposedCount;
    [FieldOffset(0x88)] public OffsetPtr ExposedHashes;
    [FieldOffset(0x90)] public OffsetPtr ExposedIndices;
}
