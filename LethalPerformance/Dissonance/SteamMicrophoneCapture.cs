using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Dissonance.Audio.Capture;
using JetBrains.Annotations;
using NAudio.Wave;
using Steamworks;
using UnityEngine;

namespace LethalPerformance.Dissonance;
internal class SteamMicrophoneCapture : MonoBehaviour, IMicrophoneCapture
{
    public bool IsRecording { get; protected set; }

    public string Device { get; protected set; } = string.Empty;

    public TimeSpan Latency { get; protected set; }

    private readonly List<IMicrophoneSubscriber> m_Subscribers = [];
    private readonly MemoryStream m_CompressedVoiceStream = new(capacity: 65535);
    private readonly MemoryStream m_DecompressedVoiceStream = new();

    private WaveFormat m_Format = null!;

    public WaveFormat? StartCapture([CanBeNull] string? name)
    {
        var sampleRate = SteamUser.OptimalSampleRate;
        SteamUser.SampleRate = sampleRate;

        m_Format = new WaveFormat((int)sampleRate, 1);
        SteamUser.VoiceRecord = true;
        IsRecording = true;

        return m_Format;
    }

    public void StopCapture()
    {
        IsRecording = false;
        SteamUser.VoiceRecord = false;
    }

    public void Subscribe([NotNull] IMicrophoneSubscriber listener)
    {
        m_Subscribers.Add(listener);
    }

    public bool Unsubscribe([NotNull] IMicrophoneSubscriber listener)
    {
        return m_Subscribers.Remove(listener);
    }

    public bool UpdateSubscribers()
    {
        if (!SteamUser.HasVoiceData)
        {
            return false;
        }

        m_CompressedVoiceStream.Position = 0;
        var length = SteamUser.ReadVoiceData(m_CompressedVoiceStream);

        if (length <= 0)
        {
            return false;
        }

        m_CompressedVoiceStream.Position = 0;
        m_DecompressedVoiceStream.Position = 0;
        length = SteamUser.DecompressVoice(m_CompressedVoiceStream, length, m_DecompressedVoiceStream);

        if (length <= 0)
        {
            return false;
        }

        var samplesCount = length / 2;
        var samplesVoice = ArrayPool<float>.Shared.Rent(samplesCount);
        var samples = m_DecompressedVoiceStream.GetBuffer();

        for (int i = 0; i < samplesCount; i++)
        {
            // Read the two bytes and convert to a short
            var sample = (short)(samples[i * 2] | (samples[i * 2 + 1] << 8));

            // Normalize to the range of -1.0 to 1.0
            samplesVoice[i] = sample / 32768f;
        }

        var arraySegment = new ArraySegment<float>(samplesVoice, 0, samplesCount);
        foreach (var subscriber in m_Subscribers)
        {
            subscriber.ReceiveMicrophoneData(arraySegment, m_Format);
        }

        ArrayPool<float>.Shared.Return(samplesVoice);

        return false;
    }
}
