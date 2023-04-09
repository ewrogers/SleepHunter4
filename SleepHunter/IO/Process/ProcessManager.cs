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
        #region Singleton
        static readonly ProcessManager instance = new ProcessManager();

        public static ProcessManager Instance { get { return instance; } }

        private ProcessManager() { }
        #endregion

        ConcurrentDictionary<int, ClientProcess> clientProcesses = new ConcurrentDictionary<int, ClientProcess>();
        ConcurrentQueue<ClientProcess> deadClients = new ConcurrentQueue<ClientProcess>();
        ConcurrentQueue<ClientProcess> newClients = new ConcurrentQueue<ClientProcess>();

        public event ClientProcessEventHandler ProcessCreated;
        public event ClientProcessEventHandler ProcessTerminated;

        #region Client Process Properties
        public int ActiveClientCount { get { return clientProcesses.Count; } }

        public int DeadClientCount { get { return deadClients.Count; } }

        public int NewClientCount { get { return newClients.Count; } }

        public IEnumerable<ClientProcess> ActiveClients
        {
            get { return from a in clientProcesses.Values select a; }
        }

        public IEnumerable<ClientProcess> DeadClients
        {
            get { return from d in deadClients select d; }
        }

        public IEnumerable<ClientProcess> NewClients
        {
            get { return from n in newClients select n; }
        }
        #endregion

        #region Queue Methods
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
            ClientProcess process = null;
            deadClients.TryPeek(out process);

            return process;
        }

        public ClientProcess PeekNewClient()
        {
            ClientProcess process = null;
            newClients.TryPeek(out process);

            return process;
        }

        public ClientProcess DequeueDeadClient()
        {
            ClientProcess process = null;
            deadClients.TryDequeue(out process);

            return process;
        }

        public ClientProcess DequeueNewClient()
        {
            ClientProcess process = null;
            newClients.TryDequeue(out process);

            return process;
        }

        public void ClearDeadClients()
        {
            deadClients = new ConcurrentQueue<ClientProcess>();
        }

        public void ClearNewClients()
        {
            newClients = new ConcurrentQueue<ClientProcess>();
        }
        #endregion

        #region Scan Methods
        public void ScanForProcesses(Action<ClientProcess> enumProcessCallback = null)
        {
            var foundClients = new Dictionary<int, ClientProcess>();
            var deadClients = new Dictionary<int, ClientProcess>();
            var newClients = new Dictionary<int, ClientProcess>();

            NativeMethods.EnumWindows((windowHandle, lParam) =>
            {
                // Get Process & Thread Id
                var processId = 0;
                var threadId = NativeMethods.GetWindowThreadProcessId(windowHandle, out processId);

                // Get Window Class Name
                var classNameBuffer = new StringBuilder(256);
                var classNameLength = NativeMethods.GetClassName(windowHandle, classNameBuffer, classNameBuffer.Capacity);
                var className = classNameBuffer.ToString();

                // Check Class Name (DA)
                if (!string.Equals("DarkAges", className, StringComparison.OrdinalIgnoreCase))
                    return true;

                // Get Window Title
                var windowTextLength = NativeMethods.GetWindowTextLength(windowHandle);
                var windowTextBuffer = new StringBuilder(windowTextLength + 1);
                windowTextLength = NativeMethods.GetWindowText(windowHandle, windowTextBuffer, windowTextBuffer.Capacity);
                var windowText = windowTextBuffer.ToString();

                // Get Window Rectangle
                Rect windowRect;
                NativeMethods.GetWindowRect(windowHandle, out windowRect);

                var process = new ClientProcess
                {
                    ProcessId = processId,
                    WindowHandle = windowHandle,
                    WindowClassName = className,
                    WindowTitle = windowText,
                    WindowWidth = windowRect.Width,
                    WindowHeight = windowRect.Height
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
                ClientProcess process;

                if (!foundClients.ContainsKey(client.ProcessId))
                {
                    clientProcesses.TryRemove(client.ProcessId, out process);
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
        #endregion

        #region Event Handler Methods
        void OnProcessCreated(ClientProcess process)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            ProcessCreated?.Invoke(this, new ClientProcessEventArgs(process));
        }

        void OnProcessTerminated(ClientProcess process)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            ProcessTerminated?.Invoke(this, new ClientProcessEventArgs(process));
        }
        #endregion
    }
}
