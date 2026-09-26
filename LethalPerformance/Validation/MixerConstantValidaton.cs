using LethalPerformance.Audio;
using LethalPerformance.Extensions;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace LethalPerformance.Validation;

internal static unsafe class MixerConstantValidaton
{
    internal static bool Enabled = false;

    public static void LogMixer(AudioMixerConstant* constant)
    {
        if (!Enabled || constant == null)
        {
            return;
        }

        var sb = new StringBuilder();

        LogCounts(sb, constant);
        LogGroupNames(sb, constant);
        LogGroups(sb, constant);
        LogEffects(sb, constant);
        LogExposed(sb, constant);
        LogSnapshots(sb, constant);

        LethalPerformancePlugin.Instance.Logger.LogInfo(sb.ToString());
    }

    private static void LogCounts(StringBuilder sb, AudioMixerConstant* c)
    {
        sb.Append("Counts: Groups=")
          .Append(c->GroupCount.ToString(CultureInfo.InvariantCulture))
          .Append(" Effects=").Append(c->EffectCount.ToString(CultureInfo.InvariantCulture))
          .Append(" Snapshots=").Append(c->SnapshotCount.ToString(CultureInfo.InvariantCulture))
          .Append(" Exposed=").Append(c->ExposedCount.ToString(CultureInfo.InvariantCulture))
          .Append(" GroupNameBufSize=")
          .Append(c->GroupNameBufferSize.ToString(CultureInfo.InvariantCulture))
          .AppendLine();
    }

    private static void LogGroupNames(StringBuilder sb, AudioMixerConstant* c)
    {
        sb.Append("Group names: ");
        if (c->GroupNameBufferSize <= 0 || c->GroupNames.IsEmpty)
        {
            sb.Append("<empty>").AppendLine();
            return;
        }

        var names = ReadPackedNames(c->GroupNames.Get(), c->GroupNameBufferSize);
        sb.Append(string.Join(", ", names)).AppendLine();
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

    private static void LogGroups(StringBuilder sb, AudioMixerConstant* c)
    {
        sb.Append("Groups:").AppendLine();
        if (c->GroupCount <= 0 || c->Groups.IsEmpty) return;

        var groups = c->Groups.Get();
        for (var i = 0; i < c->GroupCount; i++)
        {
            var g = groups + i;
            sb.Append("  [").Append(i.ToString(CultureInfo.InvariantCulture)).Append("] ")
              .Append("parent=").Append(g->ParentConstantIndex.ToString(CultureInfo.InvariantCulture))
              .Append(" volIndex=").Append(g->VolumeIndex.ToString(CultureInfo.InvariantCulture))
              .Append(" pitchIndex=").Append(g->PitchIndex.ToString(CultureInfo.InvariantCulture))
              .Append(" mute=").Append(g->Mute.ToString(CultureInfo.InvariantCulture))
              .Append(" solo=").Append(g->Solo.ToString(CultureInfo.InvariantCulture))
              .Append(" bypass=").Append(g->BypassEffects.ToString(CultureInfo.InvariantCulture))
              .AppendLine();
        }
    }

    private static void LogEffects(StringBuilder sb, AudioMixerConstant* c)
    {
        sb.Append("Effects:").AppendLine();
        if (c->EffectCount <= 0 || c->Effects.IsEmpty)
        {
            return;
        }

        var effects = c->Effects.Get();
        for (var i = 0; i < c->EffectCount; i++)
        {
            var e = effects + i;

            sb.Append("  [").Append(i.ToString(CultureInfo.InvariantCulture)).Append("] ")
              .Append("type=").Append(e->Type.ToString(CultureInfo.InvariantCulture))
              .Append(" group=").Append(e->GroupConstantIndex.ToString(CultureInfo.InvariantCulture))
              .Append(" send=").Append(e->SendTargetEffectIndex.ToString(CultureInfo.InvariantCulture))
              .Append(" wet=").Append(e->WetMixLevelIndex.ToString(CultureInfo.InvariantCulture))
              .Append(" prev=").Append(e->PrevEffectIndex.ToString(CultureInfo.InvariantCulture))
              .Append(" bypass=").Append(e->Bypass.ToString(CultureInfo.InvariantCulture))
              .Append(" paramCount=").Append(e->ParameterCount.ToString(CultureInfo.InvariantCulture))
              .Append(" params=[");

            if (e->ParameterCount > 0 && !e->ParameterIndices.IsEmpty)
            {
                var p = e->ParameterIndices.Get();
                for (var j = 0; j < e->ParameterCount; j++)
                {
                    if (j > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(p[j].ToString(CultureInfo.InvariantCulture));
                }
            }

            sb.Append(']').AppendLine();
        }
    }

    private static void LogExposed(StringBuilder sb, AudioMixerConstant* c)
    {
        sb.Append("Exposed:").AppendLine();
        if (c->ExposedCount <= 0)
        {
            return;
        }

        var hashes = c->ExposedHashes.IsEmpty ? null : c->ExposedHashes.Get();
        var indices = c->ExposedIndices.IsEmpty ? null : c->ExposedIndices.Get();

        for (var i = 0; i < c->ExposedCount; i++)
        {
            var hash = hashes == null ? 0u : hashes[i];
            var idx = indices == null ? 0u : indices[i];
            sb.Append("  [").Append(i.ToString(CultureInfo.InvariantCulture)).Append("] hash=0x")
              .Append(hash.ToString("X8", CultureInfo.InvariantCulture))
              .Append(" index=")
              .Append(idx.ToString(CultureInfo.InvariantCulture))
              .AppendLine();
        }
    }

    private static void LogSnapshots(StringBuilder sb, AudioMixerConstant* c)
    {
        sb.Append("Snapshots:").AppendLine();
        if (c->SnapshotCount <= 0 || c->Snapshots.IsEmpty)
        {
            return;
        }

        var snapshots = c->Snapshots.Get();
        for (var s = 0; s < c->SnapshotCount; s++)
        {
            var sn = snapshots + s;
            sb.Append("  [").Append(s.ToString(CultureInfo.InvariantCulture)).Append("] hash=0x")
              .Append(sn->NameHash.ToString("X8", CultureInfo.InvariantCulture))
              .Append(" valueCount=").Append(sn->ValueCount.ToString(CultureInfo.InvariantCulture))
              .Append(" transitionCount=").Append(sn->TransitionCount.ToString(CultureInfo.InvariantCulture))
              .AppendLine();

            if (sn->ValueCount <= 0 || sn->Values.IsEmpty) continue;

            var values = sn->Values.Get();
            const int row = 12;
            for (var start = 0; start < sn->ValueCount; start += row)
            {
                var end = Math.Min(start + row, sn->ValueCount);
                sb.Append("      [")
                  .Append(start.ToString(CultureInfo.InvariantCulture))
                  .Append("..")
                  .Append((end - 1).ToString(CultureInfo.InvariantCulture))
                  .Append("] ");
                for (var j = start; j < end; j++)
                {
                    if (j > start)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(values[j].ToString("G6", CultureInfo.InvariantCulture));
                }
                sb.AppendLine();
            }
        }
    }
}