namespace SleepHunter.Interop.Input;

public interface IWindowMessageSink
{
    bool TryPost(
        ClientWindowTarget target,
        WindowInputMessage message,
        out int nativeErrorCode);
}
