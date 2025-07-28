using Microsoft.Graph.ListItems.Extensions.Models;
using Microsoft.Graph.ListItems.Extensions.Services;
using Microsoft.Graph.Models;

namespace Microsoft.Graph.ListItems.Extensions
{
    public static class ListItemExtensions
    {
        #region Model Conversion

        public static T ParseTo<T>(this ListItem listItem) where T : GraphListItemModel<T>, new()
        {
            ArgumentNullException.ThrowIfNull(listItem);

            return new T().Load(listItem);
        }

        public static T ParseTo<T>(this ListItemVersion listItemVersion) where T : GraphListItemModel<T>, new()
        {
            ArgumentNullException.ThrowIfNull(listItemVersion);

            var listItem = new ListItem
            {
                Id = listItemVersion.Fields?.Id,
                Fields = listItemVersion.Fields
            };

            return new T().Load(listItem);
        }

        #endregion

        #region Version Comparison

        public static VersionComparison<T> CompareVersions<T>(
            this T fromVersion, 
            T toVersion, 
            bool includeUnchanged = false)
            where T : GraphListItemModel<T>
        {
            return VersionComparisonService.Compare(fromVersion, toVersion, includeUnchanged);
        }

        public static VersionComparison<T> GetPendingChanges<T>(this T model)
            where T : GraphListItemModel<T>, new()
        {
            return VersionComparisonService.ComparePendingChanges(model);
        }

        public static string GetChangesSummary<T>(this T model)
            where T : GraphListItemModel<T>, new()
        {
            var comparison = model.GetPendingChanges();
            
            if (!comparison.HasDifferences)
                return "No changes detected.";

            var summary = new List<string>();
            
            if (comparison.AddedFields.Any())
                summary.Add($"{comparison.AddedFields.Count()} field(s) added");
            
            if (comparison.ModifiedFields.Any())
                summary.Add($"{comparison.ModifiedFields.Count()} field(s) modified");
            
            if (comparison.RemovedFields.Any())
                summary.Add($"{comparison.RemovedFields.Count()} field(s) removed");

            return string.Join(", ", summary);
        }

        #endregion
    }
}