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
        private const string DarkAgesClassName = "DarkAges";
        private static readonly ProcessManager instance = new ProcessManager();

        public static ProcessManager Instance { get { return instance; } }

        private ProcessManager() { }

        private readonly ConcurrentDictionary<int, ClientProcess> clientProcesses = new ConcurrentDictionary<int, ClientProcess>();
        private readonly ConcurrentQueue<ClientProcess> deadClients = new ConcurrentQueue<ClientProcess>();
        private readonly ConcurrentQueue<ClientProcess> newClients = new ConcurrentQueue<ClientProcess>();

        public event ClientProcessEventHandler ProcessCreated;
        public event ClientProcessEventHandler ProcessTerminated;

        public int ActiveClientCount { get { return clientProcesses.Count; } }

        public int DeadClientCount { get { return deadClients.Count; } }

        public int NewClientCount { get { return newClients.Count; } }

        public IEnumerable<ClientProcess> ActiveClients => clientProcesses.Values;

        public IEnumerable<ClientProcess> DeadClients => deadClients;

        public IEnumerable<ClientProcess> NewClients => newClients;

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
                if (!string.Equals(className, DarkAgesClassName, StringComparison.OrdinalIgnoreCase))
                    return true;

                // Get Window Title
                var windowTextLength = NativeMethods.GetWindowTextLength(windowHandle);
                var windowTextBuffer = new StringBuilder(windowTextLength + 1);
                windowTextLength = NativeMethods.GetWindowText(windowHandle, windowTextBuffer, windowTextBuffer.Capacity);
                var windowText = windowTextBuffer.ToString();

                // Get Window Rectangle
                NativeMethods.GetWindowRect(windowHandle, out var windowRect);

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

        private void OnProcessCreated(ClientProcess process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            ProcessCreated?.Invoke(this, new ClientProcessEventArgs(process));
        }

        private void OnProcessTerminated(ClientProcess process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            ProcessTerminated?.Invoke(this, new ClientProcessEventArgs(process));
        }
    }
}
