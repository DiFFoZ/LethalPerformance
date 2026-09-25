using System.Runtime.InteropServices;

namespace LethalPerformance.Audio;

[StructLayout(LayoutKind.Sequential, Size = 8)]
internal struct OffsetPtr<T> where T : unmanaged
{
    public long Offset;

    public readonly bool IsEmpty => Offset == 0;
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
    [FieldOffset(0x04)] public uint VolumeIndex;
    [FieldOffset(0x08)] public uint PitchIndex;
    [FieldOffset(0x0C)] public byte Mute;
    [FieldOffset(0x0D)] public byte Solo;
    [FieldOffset(0x0E)] public byte BypassEffects;
}

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
internal struct EffectConstant
{
    [FieldOffset(0x00)] public int Type;
    [FieldOffset(0x04)] public uint GroupConstantIndex;
    [FieldOffset(0x08)] public uint SendTargetEffectIndex;
    [FieldOffset(0x0C)] public uint WetMixLevelIndex; // if -1 then it's not created
    [FieldOffset(0x10)] public uint PrevEffectIndex;
    [FieldOffset(0x14)] public byte Bypass;
    [FieldOffset(0x18)] public int ParameterCount;
    [FieldOffset(0x20)] public OffsetPtr<uint> ParameterIndices;
}

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
internal struct SnapshotConstant
{
    [FieldOffset(0x00)] public uint NameHash;
    [FieldOffset(0x04)] public int ValueCount;
    [FieldOffset(0x08)] public OffsetPtr<float> Values;
    [FieldOffset(0x14)] public int TransitionCount; // Not used
    [FieldOffset(0x18)] public OffsetPtr<uint> TransitionTypes; // Not used
    [FieldOffset(0x20)] public OffsetPtr<uint> TransitionIndices; // Not used
}

[StructLayout(LayoutKind.Explicit, Size = 0x98)]
internal struct AudioMixerConstant
{
    [FieldOffset(0x00)] public int GroupCount;
    [FieldOffset(0x08)] public OffsetPtr<GroupConstant> Groups; // GroupConstant
    [FieldOffset(0x10)] public OffsetPtr<UnityGuid> GroupGuids;
    [FieldOffset(0x18)] public int EffectCount;
    [FieldOffset(0x20)] public OffsetPtr<EffectConstant> Effects; // EffectConstant
    [FieldOffset(0x28)] public OffsetPtr<UnityGuid> EffectGuids;
    [FieldOffset(0x38)] public int SnapshotCount;
    [FieldOffset(0x40)] public OffsetPtr<SnapshotConstant> Snapshots; // SnapshotConstant
    [FieldOffset(0x48)] public OffsetPtr<UnityGuid> SnapshotsGuids;
    [FieldOffset(0x50)] public int GroupNameBufferSize;
    [FieldOffset(0x58)] public OffsetPtr<byte> GroupNames;
    [FieldOffset(0x60)] public int SnapshotNameBufferSize;
    [FieldOffset(0x68)] public OffsetPtr<byte> SnapshotName;
    [FieldOffset(0x70)] public int PluginEffectNameBufferSize;
    [FieldOffset(0x78)] public OffsetPtr<byte> PluginEffectName;
    [FieldOffset(0x80)] public int ExposedCount;
    [FieldOffset(0x88)] public OffsetPtr<uint> ExposedHashes;
    [FieldOffset(0x90)] public OffsetPtr<uint> ExposedIndices;
}
