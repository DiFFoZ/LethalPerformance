using System;
using System.Runtime.InteropServices;
using LethalPerformance.Patcher.Utilities;
using UnityEngine.Audio;

namespace LethalPerformance.Audio;

internal enum MixerEffect
{
    // When adding effect, please register the name in IsTargetEffect
    PitchShifter,
    Chorus,
    Compressor
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
            return false;
        }

        ApplyKnownLayoutFallback();

        if (!TryResolveFmodFunctions())
        {
            return false;
        }

        s_Available = true;
        return true;
    }

    internal static bool TrySetEffectBypass(AudioMixerGroup group, MixerEffect effect, bool bypass)
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

        if (UnityPlayerModule.IsDebugBuild)
        {
            s_GroupIdOffset = 0x7C;
        }
        else
        {
            s_GroupIdOffset = 0x5C;
        }
    }

    private static bool TryResolveFmodFunctions()
    {
        var setBypass = ResolveFunction         (0x23286e0, 0x16cdfa0); // FMOD::DSP::setBypass
        var getDspHead = ResolveFunction        (0x2327510, 0x16cd8b0); // FMOD::ChannelGroup::getDSPHead
        var getInfo = ResolveFunction           (0x23283e0, 0x16cdcd0); // FMOD::DSP::getInfo
        var getInput = ResolveFunction          (0x2328450, 0x16cdd40); // FMOD::DSP::getInput
        var getNumInputs = ResolveFunction      (0x23284a0, 0x16cdd90); // FMOD::DSP::getNumInputs
        var getChannelGroup = ResolveFunction   (0x149fb20, 0xbc88b0); // AudioMixer::GetFMODChannelGroup

        s_SetBypass = Marshal.GetDelegateForFunctionPointer<DspSetBypass>(setBypass);
        s_GetDspHead = Marshal.GetDelegateForFunctionPointer<ChannelGroupGetDspHead>(getDspHead);
        s_GetInfo = Marshal.GetDelegateForFunctionPointer<DspGetInfo>(getInfo);
        s_GetInput = Marshal.GetDelegateForFunctionPointer<DspGetInput>(getInput);
        s_GetNumInputs = Marshal.GetDelegateForFunctionPointer<DspGetNumInputs>(getNumInputs);
        s_GetChannelGroup = Marshal.GetDelegateForFunctionPointer<AudioMixerGetChannelGroup>(getChannelGroup);

        return true;
    }

    private static IntPtr ResolveFunction(int developmentRva, int releaseRva)
    {
        var rva = UnityPlayerModule.IsDebugBuild ? developmentRva : releaseRva;
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
            MixerEffect.Chorus => name.Contains("FMOD Chorus", StringComparison.OrdinalIgnoreCase),
            MixerEffect.Compressor => name.Contains("FMOD Compressor", StringComparison.OrdinalIgnoreCase),
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
