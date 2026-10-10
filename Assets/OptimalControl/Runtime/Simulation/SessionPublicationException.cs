using System;
using System.Collections.Generic;
using System.Linq;

namespace OptCon.Simulation
{
    /// <summary>A consumer failure never invalidates the mathematical result. All other consumers are notified first.</summary>
    public sealed class SessionPublicationException : AggregateException
    {
        public SessionState Publication { get; }
        public IReadOnlyList<string> Consumers { get; }
        internal SessionPublicationException(SessionState publication, IEnumerable<string> consumers, IEnumerable<Exception> failures)
            : base("Session consumer notification failed; the committed mathematical result remains valid.", failures)
        { Publication = publication; Consumers = Array.AsReadOnly(consumers.ToArray()); }
    }
}
