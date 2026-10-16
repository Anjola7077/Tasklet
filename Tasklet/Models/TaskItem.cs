using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Tasklet.Models;

public class TaskItem : INotifyPropertyChanged
{
private string _title = string.Empty;
private string _description = string.Empty;
private string _category = "Personal";
private string _priority = "Medium";
private string _recurrence = "None";
private bool _isCompleted;
private DateTime _dueDate = DateTime.Today;
private TimeSpan _dueTime = new(17, 0, 0);
private DateTime _createdAt = DateTime.Now;
private TimeSpan? _reminderTime;
private int _notificationId;

public Guid Id { get; set; } = Guid.NewGuid();

public string Title
{
    get => _title;
    set => SetField(ref _title, value ?? string.Empty);
}

public string Description
{
    get => _description;
    set => SetField(ref _description, value ?? string.Empty);
}

public string Category
{
    get => _category;
    set => SetField(ref _category, value ?? "Personal");
}

public string Priority
{
    get => _priority;
    set => SetField(ref _priority, value ?? "Medium");
}

public string Recurrence
{
    get => _recurrence;
    set => SetField(ref _recurrence, NormalizeRecurrence(value));
}

public bool IsCompleted
{
    get => _isCompleted;
    set
    {
        if (SetField(ref _isCompleted, value))
            OnPropertyChanged(nameof(IsOverdue));
    }
}

public DateTime DueDate
{
    get => _dueDate;
    set
    {
        if (SetField(ref _dueDate, value))
        {
            OnPropertyChanged(nameof(IsOverdue));
            OnPropertyChanged(nameof(DueLabel));
            OnPropertyChanged(nameof(DueDateTime));
        }
    }
}

// The time the task itself is due.
public TimeSpan DueTime
{
    get => _dueTime;
    set
    {
        if (SetField(ref _dueTime, value))
        {
            OnPropertyChanged(nameof(DueLabel));
            OnPropertyChanged(nameof(DueDateTime));
            OnPropertyChanged(nameof(IsOverdue));
        }
    }
}

public DateTime CreatedAt
{
    get => _createdAt;
    set => SetField(ref _createdAt, value);
}

// Optional reminder time on the task's due date.
public TimeSpan? ReminderTime
{
    get => _reminderTime;
    set => SetField(ref _reminderTime, value);
}

public int NotificationId
{
    get => _notificationId;
    set => SetField(ref _notificationId, value);
}

[JsonIgnore]
public DateTime DueDateTime => DueDate.Date.Add(DueTime);

[JsonIgnore]
public bool IsOverdue =>
    !IsCompleted && DueDateTime < DateTime.Now;

[JsonIgnore]
public string DueLabel
{
    get
    {
        if (IsCompleted)
            return "Completed";

        if (IsOverdue)
            return $"Overdue · {DueDate:dd MMM}, {DueTime:hh\\:mm}";

        if (DueDate.Date == DateTime.Today)
            return $"Today · {DueTime:hh\\:mm}";

        if (DueDate.Date == DateTime.Today.AddDays(1))
            return $"Tomorrow · {DueTime:hh\\:mm}";

        return $"{DueDate:dd MMM yyyy} · {DueTime:hh\\:mm}";
    }
}

public DateTime? GetNextDueDate() => Recurrence switch
{
    "Daily" => DueDate.Date.AddDays(1),
    "Weekly" => DueDate.Date.AddDays(7),
    "Monthly" => DueDate.Date.AddMonths(1),
    _ => null
};

private static string NormalizeRecurrence(string? value) =>
    value?.Trim().ToLowerInvariant() switch
    {
        "daily" => "Daily",
        "weekly" => "Weekly",
        "monthly" => "Monthly",
        _ => "None"
    };

public event PropertyChangedEventHandler? PropertyChanged;

private bool SetField<T>(
    ref T field,
    T value,
    [CallerMemberName] string? propertyName = null)
{
    if (EqualityComparer<T>.Default.Equals(field, value))
        return false;

    field = value;
    OnPropertyChanged(propertyName);
    return true;
}

private void OnPropertyChanged(
    [CallerMemberName] string? propertyName = null)
{
    PropertyChanged?.Invoke(
        this,
        new PropertyChangedEventArgs(propertyName));
}

}