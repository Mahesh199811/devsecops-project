using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var store = new TaskStore(Path.Combine(app.Environment.ContentRootPath, "instance", "tasks.json"));
await store.InitializeAsync();

app.MapGet("/health", () => Results.Json(new { status = "ok", service = "devsecops-project" }));

app.MapGet("/api/tasks", async () => Results.Json(await store.ListAsync()));

app.MapPost("/api/tasks", async (TaskCreateRequest request) =>
{
    var title = (request.Title ?? string.Empty).Trim();
    var description = (request.Description ?? string.Empty).Trim();

    if (string.IsNullOrWhiteSpace(title))
    {
        return Results.BadRequest(new { error = "title is required" });
    }

    var task = await store.CreateAsync(title, description);
    return Results.Created($"/api/tasks/{task.Id}", task);
});

app.MapPatch("/api/tasks/{id:int}", async (int id, TaskUpdateRequest request) =>
{
    var result = await store.UpdateAsync(id, request);
    return result switch
    {
        UpdateResult.NotFoundResult => Results.NotFound(),
        UpdateResult.EmptyTitleResult => Results.BadRequest(new { error = "title cannot be empty" }),
        UpdateResult.Updated updated => Results.Json(updated.Task),
        _ => Results.StatusCode(500)
    };
});

app.MapDelete("/api/tasks/{id:int}", async (int id) =>
{
    var deleted = await store.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();

public sealed class TaskStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string filePath;
    private readonly SemaphoreSlim gate = new(1, 1);

    public TaskStore(string filePath)
    {
        this.filePath = filePath;
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        if (!File.Exists(filePath))
        {
            await File.WriteAllTextAsync(filePath, "[]");
        }
    }

    public async Task<IReadOnlyList<TaskResponse>> ListAsync()
    {
        await gate.WaitAsync();
        try
        {
            var tasks = await ReadUnsafeAsync();
            return tasks
                .OrderByDescending(task => task.CreatedAt)
                .Select(ToResponse)
                .ToList();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<TaskResponse> CreateAsync(string title, string description)
    {
        await gate.WaitAsync();
        try
        {
            var tasks = await ReadUnsafeAsync();
            var nextId = tasks.Count == 0 ? 1 : tasks.Max(task => task.Id) + 1;
            var task = new StoredTask(nextId, title, description, false, DateTimeOffset.UtcNow);

            tasks.Add(task);
            await WriteUnsafeAsync(tasks);

            return ToResponse(task);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<UpdateResult> UpdateAsync(int id, TaskUpdateRequest request)
    {
        await gate.WaitAsync();
        try
        {
            var tasks = await ReadUnsafeAsync();
            var index = tasks.FindIndex(task => task.Id == id);
            if (index < 0)
            {
                return new UpdateResult.NotFoundResult();
            }

            var existing = tasks[index];
            var title = request.Title is null ? existing.Title : request.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return new UpdateResult.EmptyTitleResult();
            }

            var description = request.Description is null ? existing.Description : request.Description.Trim();
            var done = request.Done ?? existing.Done;
            var updated = existing with { Title = title, Description = description, Done = done };

            tasks[index] = updated;
            await WriteUnsafeAsync(tasks);

            return new UpdateResult.Updated(ToResponse(updated));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await gate.WaitAsync();
        try
        {
            var tasks = await ReadUnsafeAsync();
            var deleted = tasks.RemoveAll(task => task.Id == id) > 0;
            if (deleted)
            {
                await WriteUnsafeAsync(tasks);
            }

            return deleted;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<List<StoredTask>> ReadUnsafeAsync()
    {
        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<List<StoredTask>>(stream, JsonOptions) ?? [];
    }

    private async Task WriteUnsafeAsync(List<StoredTask> tasks)
    {
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, tasks, JsonOptions);
    }

    private static TaskResponse ToResponse(StoredTask task)
    {
        return new TaskResponse(
            task.Id,
            task.Title,
            task.Description,
            task.Done,
            task.CreatedAt.ToUniversalTime().ToString("O"));
    }
}

public abstract record UpdateResult
{
    public sealed record NotFoundResult : UpdateResult;
    public sealed record EmptyTitleResult : UpdateResult;
    public sealed record Updated(TaskResponse Task) : UpdateResult;
}

public sealed record TaskCreateRequest(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("description")] string? Description);

public sealed record TaskUpdateRequest(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("done")] bool? Done);

public sealed record TaskResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("done")] bool Done,
    [property: JsonPropertyName("created_at")] string CreatedAt);

public sealed record StoredTask(
    int Id,
    string Title,
    string Description,
    bool Done,
    DateTimeOffset CreatedAt);
