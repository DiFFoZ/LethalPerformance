using System;
using System.Runtime.InteropServices;
using System.Text;
using LethalPerformance.Extensions;
using LethalPerformance.Patcher.Utilities;
using LethalPerformance.Utilities;
using MonoMod.RuntimeDetour;
using UnityEngine;
using UnityEngine.Audio;

namespace LethalPerformance.Audio;

public static unsafe class DiageticVoiceMixerNative
{
    private const int c_VanillaGroupCount = 6; // Master, VoicePlayer0-VoicePlayer3, SFX
    private const int c_VanillaEffectCount = 20;
    private const int c_VanillaSnapshotCount = 5; // Default, MuffledEcho, Muffled, SoundsMuted, Drunkness
    private const int c_VanillaParamCount = 72; // Groups volume, pitch (12 or c_FaderInsertIndex) and all parameters of effects (60)
    private const int c_VanillaExposedCount = 10; // DiageticVolume, EchoWetness, (PlayerVolume0, PlayerPitch0 .. PlayerVolume3, PlayerPitch3)
    private const int c_VanillaVoiceCount = 4;

    private const int c_TemplateEffectIndex = 5;
    private const int c_EffectsPerVoice = 3; // Attenuation, Compressor, Pitch
    private const int c_FaderInsertIndex = 12; // Each group have 2 faders (volume, pitch), so 6(c_VanillaGroupCount) * 2 = 12
    private const int c_EffectParamsPerVoice = 8;

    // https://github.com/notnotnotswipez/MoreCompany/blob/master/MoreCompany/MainClass.cs#L34
    private const int c_TargetVoiceCount = 50;
    private const int c_NewVoiceCount = c_TargetVoiceCount - c_VanillaVoiceCount;
    private const int c_FaderInsertCount = c_NewVoiceCount * 2;
    private const int c_NewGroupCount = c_VanillaGroupCount + c_NewVoiceCount;
    private const int c_NewEffectCount = c_VanillaEffectCount + (c_NewVoiceCount * c_EffectsPerVoice);
    private const int c_NewParamCount = c_VanillaParamCount + c_FaderInsertCount + (c_NewVoiceCount * c_EffectParamsPerVoice);
    private const int c_NewExposedCount = c_VanillaExposedCount + (c_NewVoiceCount * 2);

    private static int s_MixerConstantOffset;
    private static int s_MixerMemoryOffset;
    private static int s_GroupIdOffset;

    private static NativeDetour? s_Detour;
    private static NativeDetour? s_DestroyDetour;
    private static EnsureValidRuntimeDelegate? s_Original;
    private static EnsureValidRuntimeDelegate? s_Hook;
    private static DestroyConstantDelegate? s_DestroyOriginal;
    private static DestroyConstantDelegate? s_DestroyHook;
    private static MallocInternalDebugDelegate? s_MallocDebug;
    private static MallocInternalReleaseDelegate? s_MallocRelease;
    private static AudioMixerGroup[]? s_ExtraVoiceGroups;

    private static bool s_Initialized;
    private static bool s_IgnoreEnsureValidRuntimeHook;

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate byte EnsureValidRuntimeDelegate(IntPtr mixer);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr ObjectStringDelegate(IntPtr obj);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr MallocInternalDebugDelegate(
        ulong size, ulong align, IntPtr memLabel, int options, IntPtr file, int line);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr MallocInternalReleaseDelegate(
        ulong size, ulong align, int memLabel, int options, IntPtr file, int line);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DestroyConstantDelegate(AudioMixerConstant* constant, IntPtr allocator);

    internal static AudioMixerGroup[]? ExtraVoiceGroups => s_ExtraVoiceGroups;

    internal static bool HasExpandedVoiceBuses => s_ExtraVoiceGroups is { Length: > 0 };

    //[InitializeOnAwake]
    // Called from diffoz tweaks
    public static void Initialize()
    {
        if (s_Initialized)
        {
            return;
        }

        if (!Dependencies.IsModLoaded(Dependencies.MoreCompany))
        {
            return;
        }

        if (!UnityPlayerModule.TryInitialize())
        {
            return;
        }

        s_Initialized = true;

        ApplyNativeLayout();

        var mallocRVA = ResolveRva(0x4CFF70, 0x35ADF0); // malloc_internal
        if (UnityPlayerModule.IsDebugBuild)
        {
            s_MallocDebug = Marshal.GetDelegateForFunctionPointer<MallocInternalDebugDelegate>(mallocRVA);
        }
        else
        {
            s_MallocRelease = Marshal.GetDelegateForFunctionPointer<MallocInternalReleaseDelegate>(mallocRVA);
        }

        s_Hook = OnEnsureValidRuntime;
        s_Detour = new NativeDetour(
            ResolveRva(0x149E070, 0xBC7890), // AudioMixer::EnsureValidRuntime
            Marshal.GetFunctionPointerForDelegate(s_Hook));
        s_Original = s_Detour.GenerateTrampoline<EnsureValidRuntimeDelegate>();

        s_DestroyHook = OnDestroyConstant;
        s_DestroyDetour = new NativeDetour(
            ResolveRva(0x14D4FA0, 0xBE8E10), // audio::mixer::DestroyAudioMixerConstant 
            Marshal.GetFunctionPointerForDelegate(s_DestroyHook));
        s_DestroyOriginal = s_DestroyDetour.GenerateTrampoline<DestroyConstantDelegate>();
    }

    private static void ApplyNativeLayout()
    {
        // Development Object is 0x20 larger; GetConstant is one vtable slot earlier.
        if (UnityPlayerModule.IsDebugBuild)
        {
            s_MixerConstantOffset = 0x88;
            s_MixerMemoryOffset = 0x90;
            s_GroupIdOffset = 0x7C;

            return;
        }

        s_MixerConstantOffset = 0x68;
        s_MixerMemoryOffset = 0x70;
        s_GroupIdOffset = 0x5C;
    }

    private static IntPtr ResolveRva(int developmentRva, int releaseRva)
    {
        var rva = UnityPlayerModule.IsDebugBuild ? developmentRva : releaseRva;
        return UnityPlayerModule.GetRva(rva);
    }

    private static AudioMixerConstant* GetMixerConstant(IntPtr mixer)
    {
        return *(AudioMixerConstant**)((byte*)mixer + s_MixerConstantOffset);
    }

    private static bool HasMixerMemory(IntPtr mixer)
    {
        return *(nint*)((byte*)mixer + s_MixerMemoryOffset) != 0;
    }

    private static byte OnEnsureValidRuntime(IntPtr mixer)
    {
        try
        {
            TryExpandDiagetic(mixer);
        }
        catch (Exception ex)
        {
            LethalPerformancePlugin.Instance.Logger.LogError(ex);
        }

        return s_Original!(mixer);
    }

    private static void OnDestroyConstant(AudioMixerConstant* constant, IntPtr allocator)
    {
        try
        {
            if (constant == null)
            {
                return;
            }

            if (constant->GroupCount != c_NewGroupCount
                || constant->EffectCount != c_NewEffectCount)
            {
                return;
            }

            // DestroyAudioMixerConstant frees OffsetPtr targets through the mixer
            // RuntimeBaseAllocator. Our replacement arrays are MemoryManager
            // allocations (or copies Unity did not take), so skip those frees.
            constant->EffectCount = 0;
            constant->SnapshotCount = 0;
            constant->Groups.Clear();
            constant->GroupGuids.Clear();
            constant->Effects.Clear();
            constant->EffectGuids.Clear();
            constant->Snapshots.Clear();
            constant->GroupNames.Clear();
            constant->ExposedHashes.Clear();
            constant->ExposedIndices.Clear();
        }
        catch (Exception ex)
        {
            LethalPerformancePlugin.Instance.Logger.LogError(ex);
        }

        s_DestroyOriginal!(constant, allocator);
    }

    private static void TryExpandDiagetic(IntPtr mixer)
    {
        if (mixer == IntPtr.Zero || s_ExtraVoiceGroups != null || s_IgnoreEnsureValidRuntimeHook)
        {
            return;
        }

        // FMOD already initialized, ignoring
        if (HasMixerMemory(mixer))
        {
            return;
        }

        var constant = GetMixerConstant(mixer);
        if (constant == null || !IsDiagetic(mixer))
        {
            return;
        }

        if (constant->EffectCount != c_VanillaEffectCount
            || constant->SnapshotCount != c_VanillaSnapshotCount
            || constant->ExposedCount != c_VanillaExposedCount
            || constant->GroupCount != c_VanillaGroupCount)
        {
            return;
        }

        ExpandConstant(constant);
        LethalPerformancePlugin.Instance.Logger.LogInfo(
            $"Expanded Diagetic mixer to {c_TargetVoiceCount} voice buses");

        // Creating voice groups after expanding as unity would pass invalid ptr to FMOD
        CreateVoiceGroups(mixer, constant);
    }

    private static void CreateVoiceGroups(IntPtr mixer, AudioMixerConstant* constant)
    {
        if (s_ExtraVoiceGroups != null || s_IgnoreEnsureValidRuntimeHook)
        {
            return;
        }

        s_IgnoreEnsureValidRuntimeHook = true;
        try
        {
            ProduceVoiceGroups(mixer, constant);
        }
        finally
        {
            s_IgnoreEnsureValidRuntimeHook = false;
        }
    }

    private static bool IsDiagetic(IntPtr mixer)
    {
        // We are still in launch options where real diagetic mixer is still not created
        var initGO = GameObject.Find("/InitSceneScript");
        if (initGO != null && initGO.TryGetComponent<PreInitSceneScript>(out var initScript) && !initScript.choseLaunchOption)
        {
            //LethalPerformancePlugin.Instance.Logger.LogInfo("Init still");
            return false;
        }

        var instanceId = *(int*)(mixer + Object.OffsetOfInstanceIDInCPlusPlusObject);
        if (instanceId <= 0)
        {
            return false;
        }

        var mixerScript = Resources.InstanceIDToObject(instanceId);
        return mixerScript is AudioMixer && mixerScript.name == "Diagetic";
    }

    private static void ExpandConstant(AudioMixerConstant* constant)
    {
        var oldGroups = constant->Groups.Get();
        var oldGroupGuids = constant->GroupGuids.Get();
        var oldEffects = constant->Effects.Get();
        var oldEffectGuids = constant->EffectGuids.Get();
        var oldSnapshots = constant->Snapshots.Get();
        var oldNames = constant->GroupNames.Get();
        var oldHashes = constant->ExposedHashes.Get();
        var oldIndices = constant->ExposedIndices.Get();

        var newGroups = AllocArray<GroupConstant>(c_NewGroupCount);
        var newGroupGuids = AllocArray<UnityGuid>(c_NewGroupCount);
        var newEffects = AllocArray<EffectConstant>(c_NewEffectCount);
        var newEffectGuids = AllocArray<UnityGuid>(c_NewEffectCount);
        var newSnapshots = AllocArray<SnapshotConstant>(c_VanillaSnapshotCount);
        var newHashes = AllocArray<uint>(c_NewExposedCount);
        var newIndices = AllocArray<uint>(c_NewExposedCount);

        Copy(oldGroups, newGroups, c_VanillaGroupCount);
        Copy(oldGroupGuids, newGroupGuids, c_VanillaGroupCount);

        for (uint k = 0; k < c_NewVoiceCount; k++)
        {
            var group = c_VanillaGroupCount + k;
            newGroups[group] = new GroupConstant
            {
                ParentConstantIndex = 0,
                VolumeIndex = group * 2,
                PitchIndex = (group * 2) + 1,
            };
            WriteNewGuid(newGroupGuids + group);
        }

        CopyAndRemapEffects(oldEffects, newEffects);
        Copy(oldEffectGuids, newEffectGuids, c_VanillaEffectCount);
        for (var i = c_VanillaEffectCount; i < c_NewEffectCount; i++)
        {
            WriteNewGuid(newEffectGuids + i);
        }

        CopySnapshots(oldSnapshots, newSnapshots);

        var nameBytes = BuildNameBuffer(oldNames);
        var newNames = Alloc(nameBytes.Length);
        Marshal.Copy(nameBytes, 0, (IntPtr)newNames, nameBytes.Length);

        Copy(oldHashes, newHashes, c_VanillaExposedCount);
        Copy(oldIndices, newIndices, c_VanillaExposedCount);
        RemapExistingExposed(newIndices);
        AppendNewExposed(newHashes, newIndices);

        constant->GroupCount = c_NewGroupCount;
        constant->EffectCount = c_NewEffectCount;
        constant->GroupNameBufferSize = nameBytes.Length;
        constant->ExposedCount = c_NewExposedCount;
        constant->Groups.Set(newGroups);
        constant->GroupGuids.Set(newGroupGuids);
        constant->Effects.Set(newEffects);
        constant->EffectGuids.Set(newEffectGuids);
        constant->Snapshots.Set(newSnapshots);
        constant->GroupNames.Set(newNames);
        constant->ExposedHashes.Set(newHashes);
        constant->ExposedIndices.Set(newIndices);
    }

    private static void CopyAndRemapEffects(EffectConstant* oldEffects, EffectConstant* newEffects)
    {
        for (var i = 0; i < c_VanillaEffectCount; i++)
        {
            Copy(oldEffects + i, newEffects + i, 1);
            RemapEffectIndices(oldEffects + i, newEffects + i);
        }

        var template = oldEffects + c_TemplateEffectIndex;
        for (uint k = 0; k < c_NewVoiceCount; k++)
        {
            uint group = c_VanillaGroupCount + k;
            uint baseEffect = c_VanillaEffectCount + (k * c_EffectsPerVoice);
            uint paramBase = c_VanillaParamCount + c_FaderInsertCount + (k * c_EffectParamsPerVoice);

            for (var e = 0; e < c_EffectsPerVoice; e++)
            {
                var src = template + e;
                var dst = newEffects + baseEffect + e;
                Copy(src, dst, 1);
                dst->GroupConstantIndex = group;
                dst->PrevEffectIndex = (uint)(baseEffect + e - 1);

                // Attenuation index
                if (e == 0)
                {
                    dst->ParameterCount = 0;
                    dst->ParameterIndices.Clear();
                    continue;
                }

                var count = src->ParameterCount;
                var indices = AllocArray<uint>(Math.Max(count, 1));
                for (var p = 0; p < count; p++)
                {
                    indices[p] = (uint)(paramBase + ((e - 1) * 4) + p);
                }

                dst->ParameterCount = count;
                dst->ParameterIndices.Set(indices);
            }
        }
    }

    private static void RemapEffectIndices(EffectConstant* src, EffectConstant* dst)
    {
        if (dst->WetMixLevelIndex >= c_FaderInsertIndex)
        {
            dst->WetMixLevelIndex += c_FaderInsertCount;
        }

        var count = src->ParameterCount;
        if (count <= 0)
        {
            dst->ParameterIndices.Clear();
            return;
        }

        var srcIndices = src->ParameterIndices.Get();
        var dstIndices = AllocArray<uint>(count);
        for (uint i = 0; i < count; i++)
        {
            var value = srcIndices[i];
            if (value >= c_FaderInsertIndex)
            {
                value += c_FaderInsertCount;
            }

            dstIndices[i] = value;
        }

        dst->ParameterIndices.Set(dstIndices);
    }

    private static void CopySnapshots(SnapshotConstant* oldSnapshots, SnapshotConstant* newSnapshots)
    {
        for (var s = 0; s < c_VanillaSnapshotCount; s++)
        {
            var src = oldSnapshots + s;
            var dst = newSnapshots + s;
            Copy(src, dst, 1);

            dst->ValueCount = c_NewParamCount;

            // 0, 1, 4, 1, 4, 1, 4, 1, 4, 1, 2, 1, (VOLUME MASTER, PITCH MASTER ... VOLUME SFX, PITCH SFX)
            // -3.2, 10, 47, 0, (COMPRESSOR MASTER)
            // 356, 0.085, 0, 1, 0, (ECHO MASTER)
            // 22000, 1, (LOWPASS MASTER)
            // 407, 0.305, 0, 1, 0, (ECHO MASTER)
            // -11.7, 17.4, 35, 0, 1, 1024, 6, 0, -11.7, 17.4, 35, 0, 1, 1024, 6, 0, -11.7, 17.4, 35, 0, 1, 1024, 6, 0, -11.7, 17.4, 35, 0, 1, 1024, 6, 0, 1, 0, 0, 0, 40, 0.8, 0.03, 7.707142E-44, 1, 1024, 4, 0
            var oldValues = src->Values.Get();
            var newValues = AllocArray<float>(c_NewParamCount);

            // Volume, Pitch of vanilla groups
            for (var i = 0; i < 12; i++)
            {
                newValues[i] = oldValues[i];
            }

            // Our new groups, copy vanilla player Volume, Pitch values
            for (var k = 0; k < c_NewVoiceCount; k++)
            {
                newValues[c_FaderInsertIndex + (k * 2)] = oldValues[2];
                newValues[c_FaderInsertIndex + (k * 2) + 1] = oldValues[3];
            }

            // Copy vanilla effect params
            for (var i = 12; i < c_VanillaParamCount; i++)
            {
                newValues[i + c_FaderInsertCount] = oldValues[i];
            }

            for (var k = 0; k < c_NewVoiceCount; k++)
            {
                var dest = c_VanillaParamCount + c_FaderInsertCount + (k * c_EffectParamsPerVoice);
                for (var p = 0; p < c_EffectParamsPerVoice; p++)
                {
                    newValues[dest + p] = oldValues[28 + p];
                }
            }

            dst->Values.Set(newValues);
            // TransitionRemap was here

            if (src->TransitionCount != 0)
            {
                LethalPerformancePlugin.Instance.Logger.LogWarning("Transition count exists!");
            }

            dst->TransitionCount = 0;
            dst->TransitionTypes.Clear();
            dst->TransitionIndices.Clear();        
        }
    }

    private static void RemapExistingExposed(uint* indices)
    {
        for (var i = 0; i < c_VanillaExposedCount; i++)
        {
            if (indices[i] >= c_FaderInsertIndex)
            {
                indices[i] += c_FaderInsertCount;
            }
        }
    }

    private static void AppendNewExposed(uint* hashes, uint* indices)
    {
        var slot = c_VanillaExposedCount;
        for (uint n = c_VanillaVoiceCount; n < c_TargetVoiceCount; n++)
        {
            var k = n - c_VanillaVoiceCount;
            // Extra VoicePlayer groups are appended after SFX (group 5). Player n>=4 is
            // group n+2, not n+1 — otherwise PlayerVolume4 writes the SFX fader.
            uint group = c_VanillaGroupCount + k;
            hashes[slot] = CRC32.Crc32Ascii("PlayerVolume" + n);
            indices[slot] = group * 2;
            slot++;
            hashes[slot] = CRC32.Crc32Ascii("PlayerPitch" + n);
            indices[slot] = c_VanillaParamCount + c_FaderInsertCount + (k * c_EffectParamsPerVoice) + 4;
            slot++;
        }
    }

    private static byte[] BuildNameBuffer(byte* oldNames)
    {
        var vanilla = ReadPackedNames(oldNames, c_VanillaGroupCount);
        var builder = new StringBuilder();
        for (var i = 0; i < vanilla.Length; i++)
        {
            builder.Append(vanilla[i]);
            builder.Append('\0');
        }

        for (var n = c_VanillaVoiceCount; n < c_TargetVoiceCount; n++)
        {
            builder.Append("VoicePlayer");
            builder.Append(n);
            builder.Append('\0');
        }

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string[] ReadPackedNames(byte* names, int count)
    {
        var result = new string[count];
        var p = names;
        for (var i = 0; i < count; i++)
        {
            result[i] = Marshal.PtrToStringAnsi((IntPtr)p) ?? string.Empty;
            p += Encoding.ASCII.GetByteCount(result[i]) + 1;
        }

        return result;
    }

    private static void ProduceVoiceGroups(IntPtr mixer, AudioMixerConstant* constant)
    {
        var template = FindPlayerVoiceGroup(mixer);
        if (template == null)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning("VoicePlayer0 AudioMixerGroup was not found");
            return;
        }

        var groups = new AudioMixerGroup[c_NewVoiceCount];
        var guids = constant->GroupGuids.Get();

        for (var k = 0; k < c_NewVoiceCount; k++)
        {
            var name = "VoicePlayer" + (c_VanillaVoiceCount + k);
            var guid = (IntPtr)(guids + c_VanillaGroupCount + k);
            var managed = CloneVoiceGroup(template, name, guid);
            if (managed == null)
            {
                LethalPerformancePlugin.Instance.Logger.LogWarning("Failed to create AudioMixerGroup " + name);
                return;
            }

            groups[k] = managed;
        }

        s_ExtraVoiceGroups = groups;
        LethalPerformancePlugin.Instance.Logger.LogInfo(
            "Created " + groups.Length.ToString() + " Diagetic AudioMixerGroup objects");
    }

    private static AudioMixerGroup? CloneVoiceGroup(AudioMixerGroup template, string name, IntPtr guid)
    {
        AudioMixerGroup? clone = null;

        try
        {
            clone = Object.Instantiate(template);
        }
        catch (Exception ex)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning("Instantiate AudioMixerGroup failed: " + ex.Message);
        }

        if (clone != null)
        {
            WriteGroupGuid(clone.GetCachedPtr(), guid);
            clone.name = name;
            clone.hideFlags = HideFlags.HideAndDontSave;
            return clone;
        }

        return null;
    }

    private static void WriteGroupGuid(IntPtr native, IntPtr guid)
    {
        if (native == IntPtr.Zero)
        {
            return;
        }

        Buffer.MemoryCopy((byte*)guid, (byte*)native + s_GroupIdOffset, sizeof(UnityGuid), sizeof(UnityGuid));
    }

    private static AudioMixerGroup? FindPlayerVoiceGroup(IntPtr mixerPtr)
    {
        var instanceId = *(int*)(mixerPtr + Object.OffsetOfInstanceIDInCPlusPlusObject);

        var mixer = Resources.InstanceIDToObject(instanceId) as AudioMixer;
        if (mixer == null)
        {
            return null;
        }

        var groups = mixer.FindMatchingGroups("VoicePlayer0");
        if (groups.Length == 0)
        {
            return null;
        }

        return groups[0];
    }

    private static void Copy<T>(T* src, T* dst, int count) where T : unmanaged
    {
        var bytes = count * sizeof(T);
        Buffer.MemoryCopy(src, dst, bytes, bytes);
    }

    private static T* AllocArray<T>(int count) where T : unmanaged
    {
        return (T*)Alloc(count * sizeof(T));
    }

    private static byte* Alloc(int size)
    {
        // FixPluginTypesSerialization uses that flag too, so should be ok?
        const int c_NullIfOutOfMemory = 1;

        byte* ptr;

        if (UnityPlayerModule.IsDebugBuild)
        {
            var label = stackalloc byte[16];
            ptr = (byte*)s_MallocDebug!((ulong)size, 16, (IntPtr)label, c_NullIfOutOfMemory, IntPtr.Zero, 0);
        }
        else
        {
            ptr = (byte*)s_MallocRelease!((ulong)size, 16, 0, c_NullIfOutOfMemory, IntPtr.Zero, 0);
        }

        if (ptr == null)
        {
            throw new OutOfMemoryException("Unity malloc_internal failed");
        }

        return ptr;
    }

    private static void WriteNewGuid(UnityGuid* dest)
    {
        var destination = new Span<byte>(dest, sizeof(UnityGuid));
        Guid.NewGuid().TryWriteBytes(destination);
    }
}
