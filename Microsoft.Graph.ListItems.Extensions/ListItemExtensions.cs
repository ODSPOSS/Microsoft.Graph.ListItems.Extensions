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

        #endregion

        #region Version Comparison

        /// <summary>
        /// Compares two versions of a list item model and returns the differences
        /// </summary>
        /// <typeparam name="T">The model type</typeparam>
        /// <param name="fromVersion">The source version to compare from</param>
        /// <param name="toVersion">The target version to compare to</param>
        /// <param name="includeUnchanged">Whether to include unchanged fields in the result</param>
        /// <returns>A detailed comparison showing all differences between versions</returns>
        public static VersionComparison<T> CompareVersions<T>(
            this T fromVersion, 
            T toVersion, 
            bool includeUnchanged = false)
            where T : GraphListItemModel<T>
        {
            return VersionComparisonService.Compare(fromVersion, toVersion, includeUnchanged);
        }

        /// <summary>
        /// Shows what changes would be applied if the model were saved now
        /// </summary>
        /// <typeparam name="T">The model type</typeparam>
        /// <param name="model">The model to check for pending changes</param>
        /// <returns>A comparison showing current vs original values</returns>
        public static VersionComparison<T> GetPendingChanges<T>(this T model)
            where T : GraphListItemModel<T>, new()
        {
            return VersionComparisonService.ComparePendingChanges(model);
        }

        /// <summary>
        /// Gets a summary of changes for the model
        /// </summary>
        /// <typeparam name="T">The model type</typeparam>
        /// <param name="model">The model to analyze</param>
        /// <returns>A formatted string describing the changes</returns>
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