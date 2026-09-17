using Unity.Burst;

namespace LethalPerformance.Unity
{
    [BurstCompile]
    public static unsafe class CachedShadowAtlasBurst
    {
        public const byte SlotFree = 0;

        [BurstCompile]
        public static bool FindFreeSlot(byte* slots, in int slotCount, in int res, in int numEntries, out int x, out int y)
        {
            var max = res - numEntries;
            if (slots == null || max < 0 || slotCount < res * res)
            {
                x = 0;
                y = 0;
                return false;
            }

            for (var i = 0; i <= max; i++)
            {
                var row = i * res;
                for (var j = 0; j <= max; j++)
                {
                    if (slots[row + j] != SlotFree)
                    {
                        continue;
                    }

                    if (!IsBlockFree(slots, res, j, i, numEntries))
                    {
                        continue;
                    }

                    x = j;
                    y = i;
                    return true;
                }
            }

            x = 0;
            y = 0;
            return false;
        }

        [BurstCompile]
        public static int CountFreeSlots(byte* slots, in int slotCount)
        {
            var free = 0;
            for (var i = 0; i < slotCount; i++)
            {
                if (slots[i] == SlotFree)
                {
                    free++;
                }
            }

            return free;
        }

        private static bool IsBlockFree(byte* slots, int res, int x, int y, int numEntries)
        {
            var endY = y + numEntries;
            var endX = x + numEntries;
            for (var i = y; i < endY; i++)
            {
                var row = i * res;
                for (var j = x; j < endX; j++)
                {
                    if (slots[row + j] != SlotFree)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}