using Microsoft.Graph.ListItems.Extensions.Trackers;
using Microsoft.Graph.Models;

namespace Microsoft.Graph.ListItems.Extensions.Models
{
    public abstract class GraphListItemModel<T> where T : GraphListItemModel<T>
    {
        private readonly ChangeTracker _changeTracker;

        public string? ID { get; set; }

        public GraphListItemModel()
        {
            _changeTracker = new ChangeTracker(() => ToDictionary());
        }

        public abstract Dictionary<string, object> ToDictionary();

        protected abstract void FromDictionary(IDictionary<string, object?> data);

        public abstract IEnumerable<string> GetViewFields { get; }

        public virtual T Load(ListItem listItem)
        {
            ArgumentNullException.ThrowIfNull(listItem);

            ID = listItem.Id;

            var fields = listItem.Fields?.AdditionalData;
            var originalValues = fields != null
                ? new Dictionary<string, object?>(fields)
                : new Dictionary<string, object?>();

            _changeTracker.SetOriginalValues(originalValues);

            if (originalValues.Count > 0)
            {
                FromDictionary(originalValues);
            }

            return (T)this;
        }

        public virtual bool HasChanges() => _changeTracker.HasChanges();

        public virtual IReadOnlyDictionary<string, object?>? GetOriginalValues()
        {
            return _changeTracker.GetOriginalValues();
        }

        public virtual Dictionary<string, object> GetCurrentValues()
        {
            return _changeTracker.GetCurrentValues();
        }
    }
}