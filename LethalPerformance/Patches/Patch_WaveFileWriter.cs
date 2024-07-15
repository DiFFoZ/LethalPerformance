using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LethalPerformance.Extensions;
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

    [HarmonyPatch(typeof(WaveFileWriter), MethodType.Constructor, typeof(Stream), typeof(WaveFormat))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> FixConstructorAllocations(IEnumerable<CodeInstruction> instructions)
    {
        return ReplaceASCIICallToStaticField(instructions);
    }

    [HarmonyPatch(typeof(WaveFileWriter), nameof(WaveFileWriter.WriteDataChunkHeader))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> FixWriteDataChunkHeaderAllocations(IEnumerable<CodeInstruction> instructions)
    {
        return ReplaceASCIICallToStaticField(instructions);
    }

    [HarmonyPatch(typeof(WaveFileWriter), nameof(WaveFileWriter.CreateFactChunk))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> FixCreateFactChunkAllocations(IEnumerable<CodeInstruction> instructions)
    {
        return ReplaceASCIICallToStaticField(instructions);
    }

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

    [HarmonyPatch(typeof(WaveFileWriter), nameof(WaveFileWriter.WriteSample))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> FixWriterFloatAllocation(IEnumerable<CodeInstruction> _)
    {
        return
        [
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldarg_1),
            CodeInstruction.Call((WaveFileWriter x, float y) => WriteSample(x, y)),
            new(OpCodes.Ret)
        ];
    }

    private static void WriteSample(WaveFileWriter waveFileWriter, float sample)
    {
        // Mono BinaryWriter.Write(float) allocates array, using our write method

        Span<byte> buffer = stackalloc byte[sizeof(float)];
        BinaryPrimitivesExtension.WriteSingleLittleEndian(buffer, sample);

        waveFileWriter._outStream.Write(buffer);
        waveFileWriter._dataSizePos += 4;
    }
}
