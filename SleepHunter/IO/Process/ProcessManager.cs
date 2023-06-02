using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    public sealed class ProcessManager
    {
        private const string WindowClassName = "DarkAges";

        private static readonly ProcessManager instance = new();
        public static ProcessManager Instance => instance;

        private ProcessManager() { }

        private readonly ConcurrentDictionary<int, ClientProcess> clientProcesses = new();
        private readonly ConcurrentQueue<ClientProcess> deadClients = new();
        private readonly ConcurrentQueue<ClientProcess> newClients = new();

        public event ClientProcessEventHandler ProcessCreated;
        public event ClientProcessEventHandler ProcessTerminated;

        public int ActiveClientCount => clientProcesses.Count;
        public int DeadClientCount => deadClients.Count;
        public int NewClientCount => newClients.Count;

        public IEnumerable<ClientProcess> ActiveClients => from a in clientProcesses.Values select a;
        public IEnumerable<ClientProcess> DeadClients => from d in deadClients select d;
        public IEnumerable<ClientProcess> NewClients => from n in newClients select n;

        public void EnqueueDeadClient(ClientProcess process)
        {
            deadClients.Enqueue(process);
            OnProcessTerminated(process);
        }

        public void EnqueueNewClient(ClientProcess process)
        {
            newClients.Enqueue(process);
            OnProcessCreated(process);
        }

        public ClientProcess PeekDeadClient()
        {
            deadClients.TryPeek(out var process);
            return process;
        }

        public ClientProcess PeekNewClient()
        {
            newClients.TryPeek(out var process);
            return process;
        }

        public ClientProcess DequeueDeadClient()
        {
            deadClients.TryDequeue(out var process);
            return process;
        }

        public ClientProcess DequeueNewClient()
        {
            newClients.TryDequeue(out var process);
            return process;
        }

        public void ClearDeadClients() => deadClients.Clear();
        public void ClearNewClients() => newClients.Clear();

        public void ScanForProcesses(Action<ClientProcess> enumProcessCallback = null)
        {
            var foundClients = new Dictionary<int, ClientProcess>();
            var deadClients = new Dictionary<int, ClientProcess>();
            var newClients = new Dictionary<int, ClientProcess>();

            NativeMethods.EnumWindows((windowHandle, lParam) =>
            {
                // Get Process & Thread Id
                var threadId = NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);

                // Get Window Class Name
                var classNameBuffer = new StringBuilder(256);
                var classNameLength = NativeMethods.GetClassName(windowHandle, classNameBuffer, classNameBuffer.Capacity);
                var className = classNameBuffer.ToString();

                // Check Class Name (DA)
                if (!string.Equals(WindowClassName, className, StringComparison.OrdinalIgnoreCase))
                    return true;

                // Get Window Title
                var windowTextLength = NativeMethods.GetWindowTextLength(windowHandle);
                var windowTextBuffer = new StringBuilder(windowTextLength + 1);
                windowTextLength = NativeMethods.GetWindowText(windowHandle, windowTextBuffer, windowTextBuffer.Capacity);
                var windowText = windowTextBuffer.ToString();

                // Get Window Rectangle
                NativeMethods.GetClientRect(windowHandle, out var windowRect);

                var process = new ClientProcess
                {
                    ProcessId = processId,
                    WindowHandle = windowHandle,
                    WindowClassName = className,
                    WindowTitle = windowText,
                    WindowWidth = windowRect.Width,
                    WindowHeight = windowRect.Height,
                };

                // Add to found clients
                foundClients[processId] = process;

                // Callback
                enumProcessCallback?.Invoke(process);

                return true;

            }, IntPtr.Zero);

            // Find Dead Clients
            foreach (var client in clientProcesses.Values.ToArray())
            {
                if (!foundClients.ContainsKey(client.ProcessId))
                {
                    clientProcesses.TryRemove(client.ProcessId, out var process);
                    EnqueueDeadClient(client);
                }
            }

            // Find New Clients
            foreach (var client in foundClients.Values)
            {
                if (!clientProcesses.ContainsKey(client.ProcessId))
                {
                    clientProcesses[client.ProcessId] = client;
                    EnqueueNewClient(client);
                }
            }
        }

        void OnProcessCreated(ClientProcess process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            ProcessCreated?.Invoke(this, new ClientProcessEventArgs(process));
        }

        void OnProcessTerminated(ClientProcess process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            ProcessTerminated?.Invoke(this, new ClientProcessEventArgs(process));
        }
    }
}
