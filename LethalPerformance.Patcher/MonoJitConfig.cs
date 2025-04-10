// thanks to DaZombieKiller for providing on how to change Mono JIT settings.
// https://github.com/DaZombieKiller/JitInspector

using System.Runtime.InteropServices;

namespace LethalPerformance.Patcher;
internal static unsafe class MonoJitConfig
{
    [DllImport("__Internal")]
    private static extern void mono_jit_parse_options(int argc, byte** argv);

    public static void Initialize()
    {
        fixed (byte* arg = "-O=all,-aggressive-inlining"u8)
        {
            mono_jit_parse_options(1, &arg);
        }
    }
}