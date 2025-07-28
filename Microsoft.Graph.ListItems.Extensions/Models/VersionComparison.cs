namespace Microsoft.Graph.ListItems.Extensions.Models
{
    public enum ChangeType
    {
        Added,
        Modified,
        Removed,
        Unchanged
    }

    public class FieldDifference
    {
        public string FieldName { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public ChangeType ChangeType { get; set; }

        public bool HasChanged => ChangeType != ChangeType.Unchanged;
    }

    public class VersionComparison<T> where T : GraphListItemModel<T>
    {
        public T? FromVersion { get; set; }
        public T? ToVersion { get; set; }
        public List<FieldDifference> Differences { get; set; } = new();
        public DateTime ComparedAt { get; set; } = DateTime.UtcNow;

        public bool HasDifferences => Differences.Any(d => d.HasChanged);
        public int ChangedFieldsCount => Differences.Count(d => d.HasChanged);

        public IEnumerable<FieldDifference> AddedFields => 
            Differences.Where(d => d.ChangeType == ChangeType.Added);

        public IEnumerable<FieldDifference> ModifiedFields => 
            Differences.Where(d => d.ChangeType == ChangeType.Modified);

        public IEnumerable<FieldDifference> RemovedFields => 
            Differences.Where(d => d.ChangeType == ChangeType.Removed);

        public IEnumerable<FieldDifference> UnchangedFields => 
            Differences.Where(d => d.ChangeType == ChangeType.Unchanged);
    }
}