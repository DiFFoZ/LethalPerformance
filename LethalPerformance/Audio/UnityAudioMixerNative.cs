using System;
using System.Collections.Generic;
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
    private static DspGetNumInputs? s_GetNumOutputs;
    private static DspGetInput? s_GetInput;
    private static DspGetInput? s_GetOutput;
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
        if (dsp == IntPtr.Zero || s_SetBypass == null)
        {
            return false;
        }

        return s_SetBypass(dsp, bypass ? (byte)1 : (byte)0) == c_FmodOk;
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
        var getOutput = ResolveFunction(developmentRva: 0x2328500); // FMOD::DSP::getOutput
        var getNumInputs = ResolveFunction(developmentRva: 0x23284a0); // FMOD::DSP::getNumInputs
        var getNumOutputs = ResolveFunction(developmentRva: 0x23284d0); // FMOD::DSP::getNumOutputs
        var getChannelGroup = ResolveFunction(developmentRva: 0x149fb20); // AudioMixer::GetFMODChannelGroup

        s_SetBypass = Marshal.GetDelegateForFunctionPointer<DspSetBypass>(setBypass);
        s_GetDspHead = Marshal.GetDelegateForFunctionPointer<ChannelGroupGetDspHead>(getDspHead);
        s_GetInfo = Marshal.GetDelegateForFunctionPointer<DspGetInfo>(getInfo);
        s_GetInput = Marshal.GetDelegateForFunctionPointer<DspGetInput>(getInput);
        s_GetOutput = Marshal.GetDelegateForFunctionPointer<DspGetInput>(getOutput);
        s_GetNumInputs = Marshal.GetDelegateForFunctionPointer<DspGetNumInputs>(getNumInputs);
        s_GetNumOutputs = Marshal.GetDelegateForFunctionPointer<DspGetNumInputs>(getNumOutputs);
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
        if (channelGroup == IntPtr.Zero || s_GetDspHead == null)
        {
            return IntPtr.Zero;
        }

        if (s_GetDspHead(channelGroup, out var head) != c_FmodOk || head == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var visited = new HashSet<IntPtr>();
        var queue = new Queue<IntPtr>();
        queue.Enqueue(head);

        while (queue.Count > 0)
        {
            var dsp = queue.Dequeue();
            if (!visited.Add(dsp))
            {
                continue;
            }

            if (IsTargetEffect(dsp, effect))
            {
                return dsp;
            }

            EnqueueConnected(dsp, queue, inputs: true);
            EnqueueConnected(dsp, queue, inputs: false);
        }

        return IntPtr.Zero;
    }

    private static void EnqueueConnected(IntPtr dsp, Queue<IntPtr> queue, bool inputs)
    {
        var getCount = inputs ? s_GetNumInputs : s_GetNumOutputs;
        var getNode = inputs ? s_GetInput : s_GetOutput;
        if (getCount == null || getNode == null)
        {
            return;
        }

        if (getCount(dsp, out var count) != c_FmodOk || count <= 0 || count > 32)
        {
            return;
        }

        for (var i = 0; i < count; i++)
        {
            if (getNode(dsp, i, out var connected, out _) == c_FmodOk && connected != IntPtr.Zero)
            {
                queue.Enqueue(connected);
            }
        }
    }

    private static bool IsTargetEffect(IntPtr dsp, MixerEffect effect)
    {
        if (s_GetInfo == null)
        {
            return false;
        }

        var nameBuffer = stackalloc byte[c_NameBufferLength];
        var dummy = 0;
        if (s_GetInfo(dsp, (IntPtr)nameBuffer, (IntPtr)(&dummy), (IntPtr)(&dummy),
                (IntPtr)(&dummy), (IntPtr)(&dummy)) != c_FmodOk)
        {
            return false;
        }

        var name = Marshal.PtrToStringAnsi((IntPtr)nameBuffer);
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        LethalPerformancePlugin.Instance.Logger.LogInfo(name);

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
