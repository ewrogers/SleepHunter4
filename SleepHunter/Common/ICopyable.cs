
namespace SleepHunter.Common
{
    internal interface ICopyable<T>
    {
        void CopyTo(T other);
        void CopyTo(T other, bool copyId);
        void CopyTo(T other, bool copyId, bool copyTimestamp);
    }
}
