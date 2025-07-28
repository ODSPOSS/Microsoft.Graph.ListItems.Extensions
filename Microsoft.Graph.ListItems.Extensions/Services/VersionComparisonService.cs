using Microsoft.Graph.ListItems.Extensions.Models;
using System.Reflection;

namespace Microsoft.Graph.ListItems.Extensions.Services
{
    public static class VersionComparisonService
    {
        public static VersionComparison<T> Compare<T>(T? fromVersion, T? toVersion, bool includeUnchanged = false) 
            where T : GraphListItemModel<T>
        {
            var comparison = new VersionComparison<T>
            {
                FromVersion = fromVersion,
                ToVersion = toVersion
            };

            if (fromVersion == null && toVersion == null)
            {
                return comparison;
            }

            var fromData = fromVersion?.GetCurrentValues() ?? new Dictionary<string, object>();
            var toData = toVersion?.GetCurrentValues() ?? new Dictionary<string, object>();

            var allFields = fromData.Keys.Union(toData.Keys).ToHashSet();

            foreach (var fieldName in allFields)
            {
                var hasFromValue = fromData.TryGetValue(fieldName, out var fromValue);
                var hasToValue = toData.TryGetValue(fieldName, out var toValue);

                var difference = CreateFieldDifference(fieldName, fromValue, toValue, hasFromValue, hasToValue);

                if (includeUnchanged || difference.HasChanged)
                {
                    comparison.Differences.Add(difference);
                }
            }

            return comparison;
        }

        public static VersionComparison<T> ComparePendingChanges<T>(T model) 
            where T : GraphListItemModel<T>, new()
        {
            ArgumentNullException.ThrowIfNull(model);

            if (!model.HasChanges())
            {
                return new VersionComparison<T> { ToVersion = model };
            }

            var originalValues = model.GetOriginalValues();
            if (originalValues == null)
            {
                return new VersionComparison<T> { ToVersion = model };
            }

            var currentValues = model.GetCurrentValues();
            var comparison = new VersionComparison<T> { ToVersion = model };

            var allFields = originalValues.Keys.Union(currentValues.Keys).ToHashSet();

            foreach (var fieldName in allFields)
            {
                var hasOriginal = originalValues.TryGetValue(fieldName, out var originalValue);
                var hasCurrent = currentValues.TryGetValue(fieldName, out var currentValue);

                var difference = CreateFieldDifference(fieldName, originalValue, currentValue, hasOriginal, hasCurrent);

                if (difference.HasChanged)
                {
                    comparison.Differences.Add(difference);
                }
            }

            return comparison;
        }

        private static FieldDifference CreateFieldDifference(
            string fieldName, 
            object? fromValue, 
            object? toValue, 
            bool hasFromValue, 
            bool hasToValue)
        {
            var changeType = DetermineChangeType(fromValue, toValue, hasFromValue, hasToValue);

            return new FieldDifference
            {
                FieldName = fieldName,
                OldValue = fromValue,
                NewValue = toValue,
                ChangeType = changeType
            };
        }

        private static ChangeType DetermineChangeType(
            object? fromValue, 
            object? toValue, 
            bool hasFromValue, 
            bool hasToValue)
        {
            if (!hasFromValue && hasToValue)
                return ChangeType.Added;

            if (hasFromValue && !hasToValue)
                return ChangeType.Removed;

            if (!hasFromValue && !hasToValue)
                return ChangeType.Unchanged;

            return Equals(fromValue, toValue) ? ChangeType.Unchanged : ChangeType.Modified;
        }
    }
}