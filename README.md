# Microsoft.Graph.ListItems.Extensions

A .NET 8 library that extends Microsoft Graph SDK functionality for SharePoint list items with enhanced change tracking, version comparison, batch operations, and strongly-typed model support.

## Overview

This library provides a rich set of extensions and utilities to simplify working with SharePoint list items through Microsoft Graph. It adds powerful features like automatic change tracking, version comparison, intelligent batch request handling with retry logic, and a fluent configuration API.

## Key Features

### 🔄 Change Tracking
- Automatic tracking of field modifications
- Detect changes before performing updates
- Skip unnecessary API calls when no changes are detected
- Get detailed change summaries and pending changes

### 📊 Version Comparison
- Compare different versions of list items
- Detailed field-by-field difference analysis
- Support for added, modified, removed, and unchanged fields
- Comprehensive change reporting

### 🚀 Batch Operations with Retry Logic
- Enhanced batch request handling with intelligent retry mechanisms
- Configurable retry policies with exponential backoff
- Automatic handling of failed dependencies
- Built-in support for throttling and rate limiting

### 🎯 Strongly-Typed Models
- Create strongly-typed models for your SharePoint list items
- Automatic serialization/deserialization
- Type-safe field access and validation
- Support for custom field mappings

### ⚙️ Fluent Configuration
- Flexible GraphServiceClient configuration
- Custom HTTP handlers support
- Configurable logging integration
- Retry policy customization

## Dependencies

- .NET 8.0
- Microsoft.Graph
- Microsoft.Extensions.Logging
- Azure.Identity

## Quick Start

### 1. Create a Strongly-Typed Model

```csharp
public class ProjectItem : GraphListItemModel<ProjectItem>
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }

    public override IEnumerable<string> GetViewFields =>
        new[] { "Title", "Description", "Status", "DueDate" };

    protected override Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "Title", Title ?? string.Empty },
            { "Description", Description ?? string.Empty },
            { "Status", Status ?? string.Empty },
            { "DueDate", DueDate?.ToString("yyyy-MM-dd") ?? string.Empty }
        };
    }

    protected override void FromDictionary(IDictionary<string, object?> data)
    {
        Title = data.TryGetValue("Title", out var title) ? title?.ToString() : null;
        Description = data.TryGetValue("Description", out var desc) ? desc?.ToString() : null;
        Status = data.TryGetValue("Status", out var status) ? status?.ToString() : null;
        
        if (data.TryGetValue("DueDate", out var dueDateValue) && 
            DateTime.TryParse(dueDateValue?.ToString(), out var dueDate))
        {
            DueDate = dueDate;
        }
    }
}
```

### 2. Configure GraphServiceClient

```csharp
using Microsoft.Graph.ListItems.Extensions.Configuration;

var graphClient = GraphConfiguration
    .RemoveDefaultRetryHandler()
    .WithLoggerFactory(loggerFactory)
    .AddHttpHandler(logger => new CustomThrottlingHandler(logger))
    .WithBatchRetryOptions(options =>
    {
        options.MaxRetries = 5;
        options.UseExponentialBackoff = true;
        options.MaxDelaySeconds = 60;
    })
    .CreateGraphServiceClient(credential, scopes);
```

### 3. Work with List Items

```csharp
// Read items with strong typing
var items = await graphClient.Sites[siteId]
    .Lists[listId]
    .Items
    .GetAsync<ProjectItem>();

// Get a single item
var item = await graphClient.Sites[siteId]
    .Lists[listId]
    .Items["1"]
    .GetAsync<ProjectItem>();

// Create new item
var newItem = new ProjectItem
{
    Title = "New Project",
    Description = "Project description",
    Status = "Active"
};

var created = await graphClient.Sites[siteId]
    .Lists[listId]
    .Items
    .PostAsync(newItem);

// Update with change tracking
item.Status = "Completed";

if (item.HasChanges())
{
    var updated = await graphClient.Sites[siteId]
        .Lists[listId]
        .Items[item.ID]
        .PatchAsync(item);
}
```

### 4. Batch Operations with Retry Logic

```csharp
var batch = new BatchRequestContentCollection(graphClient);
var requestMap = new Dictionary<string, string>();

// Add multiple requests to batch
for (int i = 1; i <= 100; i++)
{
    var reqInfo = graphClient.Sites[siteId]
        .Lists[listId]
        .Items[i.ToString()]
        .ToGetRequestInformation(config =>
        {
            config.QueryParameters.Expand = new[] { "fields" };
        });

    var reqId = await batch.AddBatchRequestStepAsync(reqInfo);
    requestMap[reqId] = i.ToString();
}

// Execute batch with automatic retry logic
var (statuses, responses) = await graphClient
    .PostBatchWithFailedDependencyRetriesAsync(batch, logger: logger);

// Process responses
foreach (var kv in requestMap)
{
    var response = responses[kv.Key];
    if (response.IsSuccessStatusCode)
    {
        // Process successful response
        var content = await response.Content.ReadAsStringAsync();
        // Parse and work with the data
    }
}
```

### 5. Change Tracking and Version Comparison

```csharp
// Check for changes
var item = await graphClient.Sites[siteId]
    .Lists[listId]
    .Items["1"]
    .GetAsync<ProjectItem>();

item.Status = "In Progress";

if (item.HasChanges())
{
    // Get detailed change information
    var changes = item.GetPendingChanges();
    var summary = item.GetChangesSummary();
    
    Console.WriteLine($"Changes: {summary}");
    
    foreach (var change in changes.Differences)
    {
        Console.WriteLine($"Field: {change.FieldName}");
        Console.WriteLine($"Old Value: {change.OldValue}");
        Console.WriteLine($"New Value: {change.NewValue}");
        Console.WriteLine($"Change Type: {change.ChangeType}");
    }
}

// Compare different versions
var version1 = await GetItemVersion(itemId, versionId1);
var version2 = await GetItemVersion(itemId, versionId2);

var comparison = version1.CompareVersions(version2, includeUnchanged: false);

Console.WriteLine($"Has differences: {comparison.HasDifferences}");
Console.WriteLine($"Changed fields: {comparison.ChangedFieldsCount}");

foreach (var diff in comparison.Differences)
{
    Console.WriteLine($"{diff.FieldName}: {diff.OldValue} → {diff.NewValue}");
}
```

## Configuration Options

### Batch Retry Configuration

```csharp
var options = new BatchRetryOptions
{
    MaxRetries = 5,                    // Maximum number of retry attempts
    InitialDelaySeconds = 1,           // Initial delay between retries
    UseExponentialBackoff = true,      // Enable exponential backoff
    MaxDelaySeconds = 60               // Maximum delay between retries
};

GraphConfiguration.WithBatchRetryOptions(options);
```

### Custom HTTP Handlers

```csharp
GraphConfiguration
    .AddHttpHandler(() => new CustomThrottlingHandler())
    .CreateGraphServiceClient(credential, scopes);
```

## API Reference

### Core Classes

#### `GraphListItemModel<T>`
Base class for creating strongly-typed list item models.

**Key Methods:**
- `Load(ListItem)` - Initialize model from Graph ListItem
- `HasChanges()` - Check if model has pending changes
- `GetCurrentValues()` - Get current field values
- `GetOriginalValues()` - Get original field values from load

#### `GraphConfiguration`
Fluent API for configuring GraphServiceClient with enhanced capabilities.

**Key Methods:**
- `RemoveDefaultRetryHandler()` - Remove default Graph SDK retry handler
- `WithLoggerFactory(ILoggerFactory)` - Configure logging
- `AddHttpHandler<T>()` - Add custom HTTP middleware
- `WithBatchRetryOptions()` - Configure batch retry behavior

#### `VersionComparison<T>`
Represents comparison results between two versions of a model.

**Key Properties:**
- `HasDifferences` - Whether any differences were found
- `Differences` - Collection of field differences
- `AddedFields` - Fields that were added
- `ModifiedFields` - Fields that were modified
- `RemovedFields` - Fields that were removed

### Extension Methods

#### `GraphBatchExtensions`
- `PostBatchWithFailedDependencyRetriesAsync()` - Execute batch with intelligent retry logic
- `PostBatchWithRetriesAsync()` - Simplified batch execution with retries

#### `ListItemExtensions`
- `ParseTo<T>()` - Convert ListItem to strongly-typed model
- `CompareVersions<T>()` - Compare two model versions
- `GetPendingChanges<T>()` - Get changes since last load
- `GetChangesSummary<T>()` - Get human-readable change summary

#### `GraphListItemRequestBuilderExtensions`
- `GetAsync<T>()` - Get strongly-typed items
- `PostAsync<T>()` - Create items with type safety
- `PatchAsync<T>()` - Update items with change tracking

## Advanced Scenarios

### Custom Field Mapping

```csharp
protected override void FromDictionary(IDictionary<string, object?> data)
{
    // Handle lookup fields
    if (data.TryGetValue("AssignedTo", out var assignedTo))
    {
        // Parse lookup field value
        AssignedToId = ExtractLookupId(assignedTo);
        AssignedToName = ExtractLookupValue(assignedTo);
    }
    
    // Handle choice fields
    if (data.TryGetValue("Priority", out var priority))
    {
        Priority = ParseChoiceField(priority?.ToString());
    }
}
```

### Error Handling and Logging

```csharp
var logger = loggerFactory.CreateLogger<MyClass>();

try
{
    var (statuses, responses) = await graphClient
        .PostBatchWithFailedDependencyRetriesAsync(batch, logger: logger);
    
    // Check for failed requests
    var failedRequests = statuses
        .Where(s => !IsSuccessStatusCode(s.Value))
        .ToList();
        
    if (failedRequests.Any())
    {
        logger.LogWarning("Found {Count} failed requests", failedRequests.Count);
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Batch operation failed");
    throw;
}
```

## Best Practices

1. **Change Tracking**: Always check `HasChanges()` before updates to avoid unnecessary API calls
2. **Batch Operations**: Use batch requests for multiple operations to improve performance
3. **Error Handling**: Implement proper error handling and logging for production scenarios
4. **Field Selection**: Use `GetViewFields` to limit field retrieval for better performance
5. **Retry Logic**: Configure appropriate retry policies based on your application's needs