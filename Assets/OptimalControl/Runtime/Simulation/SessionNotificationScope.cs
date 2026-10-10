using System;
using System.Collections.Generic;

namespace OptCon.Simulation
{
    /// <summary>Synchronous boundary shared by a session publication and its enclosing lesson operation.</summary>
    public sealed class SessionNotificationScope
    {
        private int depth;
        private readonly Queue<Action> pending = new Queue<Action>();
        public bool IsActive => depth != 0;
        public IDisposable Enter() { depth++; return new Lease(this); }
        public void Defer(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (!IsActive) throw new InvalidOperationException("Deferral requires an active session notification scope.");
            pending.Enqueue(action);
        }
        private void Leave()
        {
            if (--depth != 0) return;
            List<Exception> failures = null;
            while (pending.Count != 0)
            {
                try { pending.Dequeue()(); }
                catch (Exception exception) { (failures ?? (failures = new List<Exception>())).Add(exception); }
            }
            if (failures != null) throw new AggregateException("Deferred session-bound operations failed.", failures);
        }
        private sealed class Lease : IDisposable
        {
            private SessionNotificationScope owner;
            public Lease(SessionNotificationScope owner) { this.owner = owner; }
            public void Dispose() { var current = owner; owner = null; current?.Leave(); }
        }
    }
}
