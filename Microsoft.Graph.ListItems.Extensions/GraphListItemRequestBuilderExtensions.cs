using Microsoft.Graph.Drives.Item.List.Items;
using Microsoft.Graph.ListItems.Extensions.Models;
using Microsoft.Graph.Models;
using Microsoft.Graph.Sites.Item.Lists.Item.Items.Item;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Graph.ListItems.Extensions
{
    public static class GraphListItemRequestBuilderExtensions
    {
        #region Create Operations

        public static async Task<T> PostAsync<T>(this ItemsRequestBuilder itemsBuilder, T item, string? contentTypeId = null)
        where T : GraphListItemModel<T>
        {
            ArgumentNullException.ThrowIfNull(itemsBuilder);
            ArgumentNullException.ThrowIfNull(item);

            var listItem = CreateListItem(item, contentTypeId);
            var created = await itemsBuilder.PostAsync(listItem) ?? throw new InvalidOperationException("ListItem creation failed.");
            return item.Load(created);
        }

        #endregion

        #region Read Operations

        public static async Task<T?> GetAsync<T>(
            this ListItemItemRequestBuilder builder,
            Action<RequestConfiguration<ListItemItemRequestBuilder.ListItemItemRequestBuilderGetQueryParameters>>? requestConfiguration = null)
            where T : GraphListItemModel<T>, new()
        {
            ArgumentNullException.ThrowIfNull(builder);

            ListItem? item;

            if (requestConfiguration != null)
            {
                item = await builder.GetAsync(requestConfiguration);
            }
            else
            {
                var model = new T();
                var fields = model.GetViewFields?.Select(f => $"fields/{f}").ToArray() ?? Array.Empty<string>();

                item = await builder.GetAsync(config =>
                {
                    config.QueryParameters.Expand = new[] { "fields" };
                    if (fields.Length > 0)
                    {
                        config.QueryParameters.Select = fields;
                    }
                });
            }

            return item != null ? new T().Load(item) : null;
        }

        #endregion

        #region Update Operations

        public static async Task<T> PatchAsync<T>(
            this ListItemItemRequestBuilder builder,
            T model,
            string? contentTypeId = null)
            where T : GraphListItemModel<T>
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(model);

            if (!model.HasChanges())
            {
                return model;
            }

            var listItem = CreateListItem(model, contentTypeId);
            var updated = await builder.PatchAsync(listItem) ?? throw new InvalidOperationException("Update failed.");
            return model.Load(updated);
        }

        public static async Task<T> PatchAsync<T>(
            this Microsoft.Graph.Sites.Item.Lists.Item.Items.Item.Fields.FieldsRequestBuilder fieldsBuilder,
            T model)
            where T : GraphListItemModel<T>
        {
            ArgumentNullException.ThrowIfNull(fieldsBuilder);
            ArgumentNullException.ThrowIfNull(model);

            if (!model.HasChanges())
            {
                return model;
            }

            var patchPayload = new FieldValueSet
            {
                AdditionalData = model.ToDictionary()
            };

            var updatedFields = await fieldsBuilder.PatchAsync(patchPayload) ?? 
                throw new InvalidOperationException("Field update failed.");

            return model.Load(new ListItem { Fields = updatedFields });
        }

        #endregion

        #region Helper Methods

        private static ListItem CreateListItem<T>(T model, string? contentTypeId = null)
            where T : GraphListItemModel<T>
        {
            var listItem = new ListItem
            {
                Fields = new FieldValueSet
                {
                    AdditionalData = model.ToDictionary() ?? new Dictionary<string, object>()
                }
            };

            if (!string.IsNullOrEmpty(contentTypeId))
            {
                listItem.ContentType = new ContentTypeInfo { Id = contentTypeId };
            }

            return listItem;
        }

        #endregion
    }
}