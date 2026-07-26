using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SleepHunter.Models;
using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    public sealed class ProcessManager
    {
        private static readonly ProcessManager instance = new();
        public static ProcessManager Instance => instance;

        private ProcessManager() { }

        private readonly ConcurrentDictionary<string, string> windowClassNames = new();
        private readonly ConcurrentDictionary<int, ClientProcess> clientProcesses = new();
        private readonly ConcurrentQueue<ClientProcess> deadClients = new();
        private readonly ConcurrentQueue<ClientProcess> newClients = new();

        public int DeadClientCount => deadClients.Count;
        public int NewClientCount => newClients.Count;

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

        public void RegisterWindowClassName(string className)
            => windowClassNames.TryAdd(className, className);

        public void ScanForProcesses(Action<ClientProcess> enumProcessCallback = null)
        {
            var foundClients = new Dictionary<int, ClientProcess>();

            var registeredClassNames = windowClassNames.Keys.ToList();

            NativeMethods.EnumWindows((windowHandle, lParam) =>
            {
                // Get Process Id
                NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);

                // Get Window Class Name
                var classNameBuffer = new StringBuilder(256);
                NativeMethods.GetClassName(windowHandle, classNameBuffer, classNameBuffer.Capacity);
                var className = classNameBuffer.ToString();

                // Check Class Name from Registered Values
                if (!registeredClassNames.Contains(className, StringComparer.OrdinalIgnoreCase))
                    return true;

                // Get Window Title
                var windowTextLength = NativeMethods.GetWindowTextLength(windowHandle);
                var windowTextBuffer = new StringBuilder(windowTextLength + 1);
                NativeMethods.GetWindowText(windowHandle, windowTextBuffer, windowTextBuffer.Capacity);
                var windowText = windowTextBuffer.ToString();

                var process = new ClientProcess
                {
                    ProcessId = processId,
                    WindowHandle = windowHandle,
                    WindowTitle = windowText
                };

                // Add to found clients
                foundClients[processId] = process;

                // Callback
                enumProcessCallback?.Invoke(process);

                return true;

            }, 0);

            // Find Dead Clients
            foreach (var client in clientProcesses.Values.ToArray())
            {
                if (!foundClients.ContainsKey(client.ProcessId))
                {
                    clientProcesses.TryRemove(client.ProcessId, out _);
                    this.deadClients.Enqueue(client);
                }
            }

            // Find New Clients
            foreach (var client in foundClients.Values)
            {
                if (!clientProcesses.ContainsKey(client.ProcessId))
                {
                    clientProcesses[client.ProcessId] = client;
                    this.newClients.Enqueue(client);
                }
            }
        }
    }
}
