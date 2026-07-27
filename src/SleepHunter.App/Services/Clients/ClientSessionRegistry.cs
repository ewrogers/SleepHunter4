using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SleepHunter.Models;
using SleepHunter.Settings;

namespace SleepHunter.Services.Clients
{
    public sealed class ClientSessionRegistry
    {
        private readonly ConcurrentDictionary<int, ClientSession>
            sessions = new();
        private readonly ClientLayoutManager layouts;

        public ClientSessionRegistry(
            ClientLayoutManager layouts)
        {
            this.layouts = layouts ??
                throw new ArgumentNullException(nameof(layouts));
        }

        public event EventHandler<ClientSessionEventArgs> SessionAdded;

        public event EventHandler<ClientSessionEventArgs> SessionRemoved;

        public IEnumerable<ClientSession> Sessions =>
            sessions.Values
                .OrderBy(session => session.Process.CreationTime)
                .ThenBy(session => session.Process.ProcessId);

        public void AddDetectedClient(
            ClientProcess process,
            ClientLayout layout = null)
        {
            var session = new ClientSession(process)
            {
                Layout = layout ??
                    layouts.Layout
            };

            Add(session);
        }

        public void Add(ClientSession session)
        {
            ArgumentNullException.ThrowIfNull(session);

            var processId = session.Process.ProcessId;
            if (sessions.TryAdd(processId, session))
            {
                SessionAdded?.Invoke(
                    this,
                    new ClientSessionEventArgs(session));
                return;
            }

            if (!ReferenceEquals(sessions[processId], session))
            {
                throw new InvalidOperationException(
                    $"Process {processId} changed session ownership without being removed.");
            }
        }

        public bool Remove(int processId)
        {
            var wasRemoved = sessions.TryRemove(
                processId,
                out var removedSession);
            if (!wasRemoved)
                return false;

            SessionRemoved?.Invoke(
                this,
                new ClientSessionEventArgs(removedSession));

            return true;
        }

    }
}
