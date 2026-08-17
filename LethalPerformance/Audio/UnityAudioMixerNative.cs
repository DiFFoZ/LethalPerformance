using System;
using System.Runtime.InteropServices;
using LethalPerformance.Patcher.Utilities;
using UnityEngine.Audio;

namespace LethalPerformance.Audio;

internal enum MixerEffect
{
    PitchShifter
}

internal static unsafe class UnityAudioMixerNative
{
    private const int c_FmodOk = 0;
    private const int c_NameBufferLength = 64;

    private static ChannelGroupGetDspHead? s_GetDspHead;
    private static DspGetNumInputs? s_GetNumInputs;
    private static DspGetInput? s_GetInput;
    private static DspGetInfo? s_GetInfo;
    private static DspSetBypass? s_SetBypass;
    private static AudioMixerGetChannelGroup? s_GetChannelGroup;

    // AudioMixerGroup->UnityGUID (offset)
    private static int s_GroupIdOffset;

    private static bool s_Initialized;
    private static bool s_Available;

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate int ChannelGroupGetDspHead(IntPtr channelGroup, out IntPtr dsp);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate int DspGetNumInputs(IntPtr dsp, out int count);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate int DspGetInput(IntPtr dsp, int index, out IntPtr input, out IntPtr connection);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate int DspGetInfo(IntPtr dsp, IntPtr name, IntPtr version, IntPtr channels,
        IntPtr configWidth, IntPtr configHeight);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate int DspSetBypass(IntPtr dsp, byte bypass);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr AudioMixerGetChannelGroup(IntPtr audioMixer, IntPtr groupId);

    internal static bool TryInitialize()
    {
        if (s_Initialized)
        {
            return s_Available;
        }

        s_Initialized = true;

        if (!UnityPlayerModule.TryInitialize())
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning("UnityPlayer module was not found");
            return false;
        }

        ApplyKnownLayoutFallback();

        if (!TryResolveFmodFunctions())
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning(
                "Failed to resolve AudioMixer native functions. RVA = Ghidra address minus image base (usually 180000000).");
            return false;
        }

        s_Available = true;
        return true;
    }

    internal static bool SetEffectBypass(AudioMixerGroup group, MixerEffect effect, bool bypass)
    {
        if (!s_Available || group == null)
        {
            return false;
        }

        var dsp = FindEffectDsp(group, effect);
        if (dsp == IntPtr.Zero)
        {
            return false;
        }

        return s_SetBypass!(dsp, bypass ? (byte)1 : (byte)0) == c_FmodOk;
    }

    private static void ApplyKnownLayoutFallback()
    {
        // AudioMixerGroup::GetGroupInGUIDListRecursive compares m_GroupID then the next 12 bytes.
        // Development Object is 0x20 larger than release (0x7C vs 0x5C).

        if (IsDevelopmentPlayer())
        {
            s_GroupIdOffset = 0x7C;
        }
        else
        {
            s_GroupIdOffset = 0x5C;
        }
    }

    private static bool IsDevelopmentPlayer()
    {
        return true;
    }

    private static bool TryResolveFmodFunctions()
    {
        var setBypass = ResolveFunction(developmentRva: 0x23286e0); // FMOD::DSP::setBypass
        var getDspHead = ResolveFunction(developmentRva: 0x2327510); // FMOD::ChannelGroup::getDSPHead
        var getInfo = ResolveFunction(developmentRva: 0x23283e0); // FMOD::DSP::getInfo
        var getInput = ResolveFunction(developmentRva: 0x2328450); // FMOD::DSP::getInput
        var getNumInputs = ResolveFunction(developmentRva: 0x23284a0); // FMOD::DSP::getNumInputs
        var getChannelGroup = ResolveFunction(developmentRva: 0x149fb20); // AudioMixer::GetFMODChannelGroup

        s_SetBypass = Marshal.GetDelegateForFunctionPointer<DspSetBypass>(setBypass);
        s_GetDspHead = Marshal.GetDelegateForFunctionPointer<ChannelGroupGetDspHead>(getDspHead);
        s_GetInfo = Marshal.GetDelegateForFunctionPointer<DspGetInfo>(getInfo);
        s_GetInput = Marshal.GetDelegateForFunctionPointer<DspGetInput>(getInput);
        s_GetNumInputs = Marshal.GetDelegateForFunctionPointer<DspGetNumInputs>(getNumInputs);
        s_GetChannelGroup = Marshal.GetDelegateForFunctionPointer<AudioMixerGetChannelGroup>(getChannelGroup);

        return true;
    }

    private static IntPtr ResolveFunction(int developmentRva)
    {
        var rva = UnityPlayerModule.IsDebugBuild ? developmentRva : 0;
        var address = UnityPlayerModule.GetRva(rva);

        return address;
    }

    private static IntPtr FindEffectDsp(AudioMixerGroup group, MixerEffect effect)
    {
        var channelGroup = GetChannelGroup(group);
        if (channelGroup == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        if (s_GetDspHead!(channelGroup, out var dsp) != c_FmodOk || dsp == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        // ChannelGroup inserts are a serial chain from the head toward the tail.
        // Only follow input 0 so child ChannelGroups (multiple inputs on the mix DSP) are not visited.
        for (var i = 0; i < 16 && dsp != IntPtr.Zero; i++)
        {
            if (IsTargetEffect(dsp, effect))
            {
                return dsp;
            }

            if (s_GetNumInputs!(dsp, out var count) != c_FmodOk || count <= 0)
            {
                break;
            }

            if (s_GetInput!(dsp, 0, out dsp, out _) != c_FmodOk)
            {
                break;
            }
        }

        return IntPtr.Zero;
    }

    private static bool IsTargetEffect(IntPtr dsp, MixerEffect effect)
    {
        var nameBuffer = stackalloc byte[c_NameBufferLength];
        var dummy = 0;
        if (s_GetInfo!(dsp, (IntPtr)nameBuffer, (IntPtr)(&dummy), (IntPtr)(&dummy),
                (IntPtr)(&dummy), (IntPtr)(&dummy)) != c_FmodOk)
        {
            return false;
        }

        var name = Marshal.PtrToStringAnsi((IntPtr)nameBuffer);
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return effect switch
        {
            MixerEffect.PitchShifter => name.Contains("FMOD Pitch Shifter", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static IntPtr GetChannelGroup(AudioMixerGroup group)
    {
        var nativeGroup = group.GetCachedPtr();
        var mixer = group.audioMixer;
        if (nativeGroup == IntPtr.Zero || mixer == null)
        {
            return IntPtr.Zero;
        }

        var nativeMixer = mixer.GetCachedPtr();
        if (nativeMixer == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        return s_GetChannelGroup!(nativeMixer, nativeGroup + s_GroupIdOffset);
    }
}
