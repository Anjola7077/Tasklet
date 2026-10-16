using System.Text.Json;
using Tasklet.Models;

namespace Tasklet.Services;

public sealed class TaskStore
{
    private readonly string _filePath;
    private readonly string _backupPath;
    private readonly string _temporaryPath;

    private readonly SemaphoreSlim _saveLock = new(1, 1);

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public TaskStore()
    {
        var directory = FileSystem.AppDataDirectory;

        _filePath = Path.Combine(
            directory,
            "tasklet-tasks.json");

        _backupPath = _filePath + ".bak";
        _temporaryPath = _filePath + ".tmp";
    }

    public async Task<List<TaskItem>> LoadAsync()
    {
        await _saveLock.WaitAsync();

        try
        {
            var tasks = await TryLoadFileAsync(_filePath);

            if (tasks != null)
                return tasks;

            // If the primary file is damaged, try the backup.
            tasks = await TryLoadFileAsync(_backupPath);

            if (tasks != null)
            {
                // Restore the primary file from the backup.
                try
                {
                    File.Copy(
                        _backupPath,
                        _filePath,
                        overwrite: true);
                }
                catch (IOException)
                {
                    // The backup remains available for another attempt.
                }
                catch (UnauthorizedAccessException)
                {
                    // Loading can still succeed even if restoration fails.
                }

                return tasks;
            }

            // No saved file is normal for a first launch.
            return new List<TaskItem>();
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private async Task<List<TaskItem>?> TryLoadFileAsync(
        string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            return await JsonSerializer
                .DeserializeAsync<List<TaskItem>>(
                    stream,
                    _options);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    public async Task SaveAsync(IEnumerable<TaskItem> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        // Capture a snapshot before writing to disk.
        var snapshot = tasks.ToList();

        await _saveLock.WaitAsync();

        try
        {
            var directory = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            // Write the complete new data to a temporary file first.
            await using (var stream = new FileStream(
                _temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    snapshot,
                    _options);

                await stream.FlushAsync();
            }

            // Preserve the previous valid save before replacing it.
            if (File.Exists(_filePath))
            {
                File.Copy(
                    _filePath,
                    _backupPath,
                    overwrite: true);
            }

            // Replace the primary save with the completed temporary file.
            File.Move(
                _temporaryPath,
                _filePath,
                overwrite: true);
        }
        finally
        {
            // Avoid leaving temporary files after failed writes.
            try
            {
                if (File.Exists(_temporaryPath))
                    File.Delete(_temporaryPath);
            }
            catch (IOException)
            {
                // A leftover temporary file can be overwritten next time.
            }

            _saveLock.Release();
        }
    }
}