using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SleepHunter.Models;
using SleepHunter.Win32;

namespace SleepHunter.Services.Clients
{
    public sealed class WindowsClientProcessScanner :
        IClientProcessScanner
    {
        private readonly ConcurrentDictionary<string, string> windowClassNames = new();
        private readonly ConcurrentDictionary<int, ClientProcess> clientProcesses = new();
        private readonly ConcurrentQueue<ClientProcess> deadClients = new();
        private readonly ConcurrentQueue<ClientProcess> newClients = new();

        public bool TryDequeueRemoved(out ClientProcess process) =>
            deadClients.TryDequeue(out process);

        public bool TryDequeueAdded(out ClientProcess process) =>
            newClients.TryDequeue(out process);

        public void RegisterWindowClassName(string className)
            => windowClassNames.TryAdd(className, className);

        public void ScanForProcesses()
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

                if (clientProcesses.TryGetValue(
                        processId,
                        out var existing))
                {
                    foundClients[processId] = existing;
                    return true;
                }

                if (!TryGetCreationTime(
                        processId,
                        out var creationTime))
                {
                    return true;
                }

                var process = new ClientProcess
                {
                    ProcessId = processId,
                    WindowHandle = windowHandle,
                    WindowTitle = windowText,
                    CreationTime = creationTime
                };

                // Add to found clients
                foundClients[processId] = process;

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
                if (clientProcesses.TryAdd(
                        client.ProcessId,
                        client))
                {
                    newClients.Enqueue(client);
                }
            }
        }

        private static bool TryGetCreationTime(
            int processId,
            out DateTime creationTime)
        {
            try
            {
                using var process =
                    Process.GetProcessById(processId);
                creationTime = process.StartTime;
                return true;
            }
            catch (ArgumentException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (Win32Exception)
            {
            }

            creationTime = default;
            return false;
        }
    }
}
