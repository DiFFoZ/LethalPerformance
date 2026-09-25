using LethalPerformance.Audio;

namespace LethalPerformance.Extensions
{
    internal static unsafe class OffsetPtrExtensions
    {
        public static T* Get<T>(ref this OffsetPtr<T> field) where T : unmanaged
        {
            fixed (OffsetPtr<T>* ptr = &field)
                return (T*)((byte*)ptr + ptr->Offset);
        }

        public static void Set<T>(ref this OffsetPtr<T> field, void* target) where T : unmanaged
        {
            fixed (OffsetPtr<T>* ptr = &field)
                ptr->Offset = (byte*)target - (byte*)ptr;
        }

        public static void Clear<T>(ref this OffsetPtr<T> field) where T : unmanaged
        {
            fixed (OffsetPtr<T>* ptr = &field)
                ptr->Offset = 0;
        }
    }
}
