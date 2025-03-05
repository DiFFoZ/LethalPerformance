using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LethalPerformance.Extensions;
using LethalPerformance.Patcher.API;
using NAudio.Wave;

namespace LethalPerformance.Patches;
[HarmonyPatch]
internal static class Patch_WaveFileWriter
{
    private static readonly byte[] s_RIFF = "RIFF"u8.ToArray();
    private static readonly byte[] s_WAVE = "WAVE"u8.ToArray();
    private static readonly byte[] s_Fmt = "fmt "u8.ToArray();
    private static readonly byte[] s_Data = "data"u8.ToArray();
    private static readonly byte[] s_Fact = "fact"u8.ToArray();

    private static readonly Dictionary<string, FieldInfo> s_StringToFieldMapping = new()
    {
        ["RIFF"] = typeof(Patch_WaveFileWriter).GetField(nameof(s_RIFF), AccessTools.all),
        ["WAVE"] = typeof(Patch_WaveFileWriter).GetField(nameof(s_WAVE), AccessTools.all),
        ["fmt "] = typeof(Patch_WaveFileWriter).GetField(nameof(s_Fmt), AccessTools.all),
        ["data"] = typeof(Patch_WaveFileWriter).GetField(nameof(s_Data), AccessTools.all),
        ["fact"] = typeof(Patch_WaveFileWriter).GetField(nameof(s_Fact), AccessTools.all),
    };

    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyPatch(typeof(WaveFileWriter), MethodType.Constructor, typeof(Stream), typeof(WaveFormat))]
    [HarmonyPatch(typeof(WaveFileWriter), nameof(WaveFileWriter.WriteDataChunkHeader))]
    [HarmonyPatch(typeof(WaveFileWriter), nameof(WaveFileWriter.CreateFactChunk))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ReplaceASCIICallToStaticField(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        matcher.MatchForward(false, [new(OpCodes.Call), new(OpCodes.Ldstr), new(OpCodes.Callvirt)])
            .Repeat(m =>
            {
                var loadStringValue = (string)m.InstructionAt(1).operand;
                var byteArray = s_StringToFieldMapping[loadStringValue];

                m.RemoveInstructions(3)
                .Insert(new CodeInstruction(OpCodes.Ldsfld, byteArray));
            });

        return matcher.InstructionEnumeration();
    }

    [HarmonyPatch(typeof(WaveFileWriter), nameof(WaveFileWriter.WriteSamples))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> FixWriterFloatAllocation(IEnumerable<CodeInstruction> _)
    {
        // if some mod are also patching this, lmk to add compatibility
        return
        [
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldarg_1),
            new(OpCodes.Ldarg_2),
            new(OpCodes.Ldarg_3),
            CodeInstruction.Call(() => WriteSamples),
            new(OpCodes.Ret)
        ];
    }

    private static void WriteSamples(WaveFileWriter waveFileWriter, float[] samples, int offset, int count)
    {
        // Mono BinaryWriter.Write(float) allocates array, using our write method

        Span<byte> span = stackalloc byte[sizeof(float)];
        for (var i = 0; i < count; i++)
        {
            BinaryPrimitivesExtension.WriteSingleLittleEndian(span, samples[offset + i]);
            waveFileWriter._outStream.Write(span);
            waveFileWriter._dataChunkSize += sizeof(float);
        }
    }
}
