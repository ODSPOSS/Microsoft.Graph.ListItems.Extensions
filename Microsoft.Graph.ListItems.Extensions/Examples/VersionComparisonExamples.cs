using Microsoft.Graph.ListItems.Extensions.Models;

namespace Microsoft.Graph.ListItems.Extensions.Examples
{
    /// <summary>
    /// Example implementation of GraphListItemModel for demonstration purposes
    /// </summary>
    public class ExampleModel : GraphListItemModel<ExampleModel>
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? Priority { get; set; }
        public DateTime? DueDate { get; set; }

        public override Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(Title))
                dict["Title"] = Title;
            
            if (!string.IsNullOrEmpty(Description))
                dict["Description"] = Description;
            
            if (Priority.HasValue)
                dict["Priority"] = Priority.Value;
            
            if (DueDate.HasValue)
                dict["DueDate"] = DueDate.Value;

            return dict;
        }

        protected override void FromDictionary(IDictionary<string, object?> data)
        {
            if (data.TryGetValue("Title", out var title))
                Title = title?.ToString();
            
            if (data.TryGetValue("Description", out var description))
                Description = description?.ToString();
            
            if (data.TryGetValue("Priority", out var priority) && int.TryParse(priority?.ToString(), out var priorityInt))
                Priority = priorityInt;
            
            if (data.TryGetValue("DueDate", out var dueDate) && DateTime.TryParse(dueDate?.ToString(), out var dueDateValue))
                DueDate = dueDateValue;
        }

        public override IEnumerable<string> GetViewFields => 
            new[] { "Title", "Description", "Priority", "DueDate" };
    }

    /// <summary>
    /// Usage examples for version comparison functionality
    /// </summary>
    public static class VersionComparisonExamples
    {
        public static void DemonstrateVersionComparison()
        {
            // Example 1: Compare two different versions
            var version1 = new ExampleModel
            {
                Title = "Original Title",
                Description = "Original Description",
                Priority = 1
            };

            var version2 = new ExampleModel
            {
                Title = "Updated Title",
                Description = "Original Description",
                Priority = 2,
                DueDate = DateTime.Now.AddDays(7)
            };

            var comparison = version1.CompareVersions(version2, includeUnchanged: true);
            
            Console.WriteLine($"Comparison found {comparison.ChangedFieldsCount} changes:");
            foreach (var diff in comparison.Differences)
            {
                Console.WriteLine($"  {diff.FieldName}: {diff.ChangeType}");
                if (diff.HasChanged)
                {
                    Console.WriteLine($"    From: {diff.OldValue ?? "null"}");
                    Console.WriteLine($"    To: {diff.NewValue ?? "null"}");
                }
            }
        }

        public static void DemonstratePendingChanges()
        {
            // Example 2: Check pending changes (would require actual ListItem loading)
            var model = new ExampleModel();
            
            // Simulate loading from SharePoint (this would normally be done via Load method)
            var originalData = new Dictionary<string, object?>
            {
                { "Title", "Original Title" },
                { "Priority", 1 }
            };
            
            // Simulate the model being loaded with original values
            // model.Load(listItem); // This would be the actual usage
            
            // Make some changes
            model.Title = "Modified Title";
            model.Priority = 3;
            model.DueDate = DateTime.Now.AddDays(5);

            // Check what would change
            var pendingChanges = model.GetPendingChanges();
            var summary = model.GetChangesSummary();
            
            Console.WriteLine($"Pending changes summary: {summary}");
            
            foreach (var change in pendingChanges.Differences)
            {
                Console.WriteLine($"Will change {change.FieldName} from '{change.OldValue}' to '{change.NewValue}'");
            }
        }
    }
}