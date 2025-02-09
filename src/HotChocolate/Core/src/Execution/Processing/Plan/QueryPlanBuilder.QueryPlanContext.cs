using System.Collections.Generic;

namespace HotChocolate.Execution.Processing.Plan;

internal static partial class QueryPlanBuilder
{
    private sealed class QueryPlanContext
    {
        public QueryPlanContext(IPreparedOperation operation)
        {
            Operation = operation;
        }

        public IPreparedOperation Operation { get; }

        public QueryPlanNode? Root { get; set; }

        public List<QueryPlanNode> NodePath { get; } = new();

        public List<ISelection> SelectionPath { get; } = new();

        public List<IFragment> Deferred { get; } = new();

        public Dictionary<int, StreamPlanNode> Streams { get; } = new();

        public QueryPlanContext Branch() => new(Operation);
    }
}
