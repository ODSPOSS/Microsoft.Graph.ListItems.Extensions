namespace Microsoft.Graph.ListItems.Extensions.Trackers
{
    public class ChangeTracker
    {
        private Dictionary<string, object?>? _originalValues;
        private readonly Func<Dictionary<string, object>> _getCurrentValues;

        public ChangeTracker(Func<Dictionary<string, object>> getCurrentValues)
        {
            _getCurrentValues = getCurrentValues ?? throw new ArgumentNullException(nameof(getCurrentValues));
        }

        public void SetOriginalValues(Dictionary<string, object?>? originalValues)
        {
            _originalValues = originalValues;
        }

        public bool HasChanges()
        {
            if (_originalValues == null)
            {
                return true;
            }

            var currentValues = _getCurrentValues();

            foreach (var kvp in currentValues)
            {
                if (!_originalValues.TryGetValue(kvp.Key, out var originalValue))
                {
                    if (kvp.Value != null)
                    {
                        return true;
                    }

                    continue;
                }

                if (!Equals(originalValue, kvp.Value))
                {
                    return true;
                }
            }

            foreach (var original in _originalValues)
            {
                if (!currentValues.ContainsKey(original.Key) && original.Value != null)
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyDictionary<string, object?>? GetOriginalValues()
        {
            return _originalValues?.AsReadOnly();
        }

        public Dictionary<string, object> GetCurrentValues()
        {
            return _getCurrentValues();
        }
    }
}