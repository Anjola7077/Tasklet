using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Tasklet.Models;
using Tasklet.Services;

namespace Tasklet;

public partial class MainPage : ContentPage
{
    private readonly TaskStore _taskStore;
    private readonly NotificationService _notifications;

    private readonly List<TaskItem> _tasks = new();

    private string _searchText = string.Empty;
    private string _currentFilter = "All";

    private bool _isLoading;
    private bool _isBusy;
    private bool _notificationPermissionRequested;

    private static readonly Color Accent =
        Color.FromArgb("#5EEAD4");

    private static readonly Color Surface =
        Color.FromArgb("#141816");

    private static readonly Color SurfaceLight =
        Color.FromArgb("#1C211F");

    private static readonly Color BorderColor =
        Color.FromArgb("#303833");

    private static readonly Color PrimaryText =
        Color.FromArgb("#F4F4F5");

    private static readonly Color SecondaryText =
        Color.FromArgb("#A1A1AA");

    private static readonly Color MutedText =
        Color.FromArgb("#71717A");

    private static readonly Color Success =
        Color.FromArgb("#4ADE80");

    private static readonly Color Warning =
        Color.FromArgb("#FBBF24");

    private static readonly Color Danger =
        Color.FromArgb("#FB7185");

    public MainPage(
        TaskStore taskStore,
        NotificationService notifications)
    {
        InitializeComponent();

        _taskStore = taskStore;
        _notifications = notifications;

        DateLabel.Text = DateTime.Now
            .ToString("dddd, dd MMMM")
            .ToUpperInvariant();

        UpdateNavigationAppearance("Tasks");
    }

    

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isLoading)
            return;

        _isLoading = true;

        try
        {
            await LoadTasksAsync();

            if (!_notificationPermissionRequested)
            {
                _notificationPermissionRequested = true;

                await _notifications.RequestPermissionAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Unable to load tasks",
                ex.Message,
                "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

 

    private async Task LoadTasksAsync()
    {
        var savedTasks = await _taskStore.LoadAsync();

        _tasks.Clear();
        _tasks.AddRange(savedTasks);

        RefreshTasks();
    }

    private async Task SaveTasksAsync()
    {
        await _taskStore.SaveAsync(_tasks.ToList());
    }


    private void OnTasksTabClicked(
        object sender,
        EventArgs e)
    {
        ShowTasksPage("All", "Tasks");
    }

    

    private void OnTodayTabClicked(
        object sender,
        EventArgs e)
    {
        ShowTasksPage("Today", "Today");
    }

 

    private void OnProgressTabClicked(
        object sender,
        EventArgs e)
    {
        TaskSection.IsVisible = false;
        ProgressSection.IsVisible = true;

        UpdateNavigationAppearance("Progress");
        UpdateStatistics();
    }

    private void ShowTasksPage(
        string filter,
        string navigationTab)
    {
        TaskSection.IsVisible = true;
        ProgressSection.IsVisible = false;

        _currentFilter = filter;

        UpdateNavigationAppearance(navigationTab);
        RefreshTasks();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await MainScrollView.ScrollToAsync(
                    0,
                    0,
                    false);
            }
            catch
            {
                // Scrolling should never prevent navigation.
            }
        });
    }

    private void UpdateNavigationAppearance(string activeTab)
    {
        SetNavigationStyle(
            TasksNavItem,
            TasksNavIcon,
            TasksNavLabel,
            activeTab == "Tasks");

        SetNavigationStyle(
            TodayNavItem,
            TodayNavIcon,
            TodayNavLabel,
            activeTab == "Today");

        SetNavigationStyle(
            ProgressNavItem,
            ProgressNavIcon,
            ProgressNavLabel,
            activeTab == "Progress");
    }

    private static void SetNavigationStyle(
        Border item,
        Label icon,
        Label label,
        bool isActive)
    {
        item.BackgroundColor = isActive
            ? Color.FromArgb("#172B28")
            : Colors.Transparent;

        item.Stroke = isActive
            ? Color.FromArgb("#31534C")
            : Colors.Transparent;

        icon.TextColor = isActive
            ? Color.FromArgb("#99F6E4")
            : Color.FromArgb("#71717A");

        label.TextColor = isActive
            ? Color.FromArgb("#99F6E4")
            : Color.FromArgb("#71717A");

        label.FontAttributes = isActive
            ? FontAttributes.Bold
            : FontAttributes.None;
    }

    // =========================================================
    // SEARCH
    // =========================================================

    private void OnSearchChanged(
        object sender,
        TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue?.Trim() ?? string.Empty;

        RefreshTasks();
    }

    

    private void OnAllFilterClicked(
        object sender,
        EventArgs e)
    {
        SetTaskFilter("All");
    }

    private void OnTodayFilterClicked(
        object sender,
        EventArgs e)
    {
        SetTaskFilter("Today");
    }

    private void OnUpcomingFilterClicked(
        object sender,
        EventArgs e)
    {
        SetTaskFilter("Upcoming");
    }

    private void OnDoneFilterClicked(
        object sender,
        EventArgs e)
    {
        SetTaskFilter("Completed");
    }

    private void SetTaskFilter(string filter)
    {
        _currentFilter = filter;

        UpdateFilterAppearance();

        RefreshTasks();
    }

    private void UpdateFilterAppearance()
    {
        SetFilterStyle(
            AllFilterButton,
            _currentFilter == "All");

        SetFilterStyle(
            TodayFilterButton,
            _currentFilter == "Today");

        SetFilterStyle(
            UpcomingFilterButton,
            _currentFilter == "Upcoming");

        SetFilterStyle(
            DoneFilterButton,
            _currentFilter == "Completed");
    }

    private static void SetFilterStyle(
        Button button,
        bool selected)
    {
        button.BackgroundColor = selected
            ? Color.FromArgb("#17312B")
            : Color.FromArgb("#181C19");

        button.TextColor = selected
            ? Color.FromArgb("#99F6E4")
            : Color.FromArgb("#A1A1AA");

        button.BorderColor = selected
            ? Color.FromArgb("#31534C")
            : Color.FromArgb("#303833");
    }

    

    private async void OnAddTaskClicked(
        object sender,
        EventArgs e)
    {
        await OpenTaskEditorAsync(null);
    }

    

    private async Task OpenTaskEditorAsync(TaskItem? existingTask)
{
if (_isBusy)
return;

_isBusy = true;

try
{
    var editor = new TaskEditorPage(existingTask);

    await Navigation.PushModalAsync(editor);

    TaskItem? result = await editor.WaitForResultAsync();

    if (result is null)
        return;

    if (existingTask is null)
    {
        if (result.Id == Guid.Empty)
            result.Id = Guid.NewGuid();

        result.CreatedAt = DateTime.Now;

        if (result.NotificationId == 0)
            result.NotificationId = CreateNotificationId();

        _tasks.Add(result);
    }
    else
    {
        
        TaskItem? taskToUpdate = _tasks.FirstOrDefault(
            task => task.Id == existingTask.Id);

        if (taskToUpdate is null)
        {
            await DisplayAlert(
                "Task not found",
                "The task could not be found.",
                "OK");

            return;
        }

        int previousNotificationId = taskToUpdate.NotificationId;

        if (previousNotificationId != 0)
            _notifications.CancelReminder(previousNotificationId);

        CopyTask(result, taskToUpdate);

        taskToUpdate.NotificationId = previousNotificationId != 0
            ? previousNotificationId
            : CreateNotificationId();

        result = taskToUpdate;
    }

    await SaveTasksAsync();
    RefreshTasks();

    try
    {
        await ScheduleReminderIfNeededAsync(result);
        await SaveTasksAsync();
    }
    catch (Exception notificationException)
    {
        System.Diagnostics.Debug.WriteLine(
            $"Reminder scheduling failed: {notificationException}");
    }

    RefreshTasks();
}
catch (Exception ex)
{
    await DisplayAlert(
        "Unable to save task",
        ex.Message,
        "OK");
}
finally
{
    _isBusy = false;
}

}
    private static void CopyTask(
        TaskItem source,
        TaskItem destination)
    {
        destination.Title = source.Title;
        destination.Description = source.Description;
        destination.Category = source.Category;
        destination.Priority = source.Priority;
        destination.Recurrence = source.Recurrence;
        destination.DueDate = source.DueDate;
        destination.DueTime = source.DueTime;
        destination.ReminderTime = source.ReminderTime;
        destination.IsCompleted = source.IsCompleted;
    }

    // =========================================================
    // TASK COMPLETION
    // =========================================================

    private async Task SetTaskCompletedAsync(
        TaskItem task,
        bool isCompleted)
    {
        if (task.IsCompleted == isCompleted)
            return;

        bool previousState = task.IsCompleted;

        try
        {
            task.IsCompleted = isCompleted;

            if (isCompleted)
            {
                _notifications.CancelReminder(
                    task.NotificationId);

                // Generate the next occurrence for recurring tasks.
                DateTime? nextDate = task.GetNextDueDate();

                if (nextDate.HasValue)
                {
                    var nextTask = new TaskItem
                    {
                        Id = Guid.NewGuid(),
                        Title = task.Title,
                        Description = task.Description,
                        Category = task.Category,
                        Priority = task.Priority,
                        Recurrence = task.Recurrence,
                        DueDate = nextDate.Value,
                        DueTime = task.DueTime,
                        CreatedAt = DateTime.Now,
                        IsCompleted = false,
                        ReminderTime = task.ReminderTime,
                        NotificationId = CreateNotificationId()
                    };

                    _tasks.Add(nextTask);
                }
            }
            else
            {
                await ScheduleReminderIfNeededAsync(task);
            }

            await SaveTasksAsync();

            RefreshTasks();

            // Schedule the next recurring task's reminder, if applicable.
            if (isCompleted)
            {
                var nextTask = _tasks
                    .Where(t => t.Id != task.Id)
                    .Where(t => t.Title == task.Title)
                    .Where(t => t.CreatedAt >= DateTime.Now.AddSeconds(-5))
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefault();

                if (nextTask is not null)
                {
                    try
                    {
                        await ScheduleReminderIfNeededAsync(nextTask);
                        await SaveTasksAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Recurring reminder failed: {ex}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            task.IsCompleted = previousState;

            RefreshTasks();

            await DisplayAlert(
                "Unable to update task",
                ex.Message,
                "OK");
        }
    }

    // =========================================================
    // DELETE TASK
    // =========================================================

    private async Task DeleteTaskAsync(TaskItem task)
    {
        bool confirmed = await DisplayAlert(
            "Delete task",
            $"Delete \"{task.Title}\" permanently?",
            "Delete",
            "Cancel");

        if (!confirmed)
            return;

        try
        {
            _notifications.CancelReminder(
                task.NotificationId);

            _tasks.Remove(task);

            await SaveTasksAsync();

            RefreshTasks();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Unable to delete task",
                ex.Message,
                "OK");
        }
    }



    private async Task ShowTaskActionsAsync(TaskItem task)
    {
        string action = await DisplayActionSheet(
            task.Title,
            "Cancel",
            "Delete",
            "Edit",
            task.IsCompleted
                ? "Mark as active"
                : "Mark as completed");

        switch (action)
        {
            case "Edit":
                await OpenTaskEditorAsync(task);
                break;

            case "Delete":
                await DeleteTaskAsync(task);
                break;

            case "Mark as completed":
                await SetTaskCompletedAsync(task, true);
                break;

            case "Mark as active":
                await SetTaskCompletedAsync(task, false);
                break;
        }
    }



    private async Task ScheduleReminderIfNeededAsync(TaskItem task)
    {
        if (task.NotificationId == 0)
        {
            task.NotificationId = CreateNotificationId();
        }

        if (task.IsCompleted || !task.ReminderTime.HasValue)
        {
            _notifications.CancelReminder(task.NotificationId);
            return;
        }

        DateTime reminderDateTime =
            task.DueDate.Date.Add(task.ReminderTime.Value);

        if (reminderDateTime <= DateTime.Now)
        {
            _notifications.CancelReminder(task.NotificationId);
            return;
        }

        await _notifications.ScheduleReminderAsync(
            task.NotificationId,
            task.Title,
            reminderDateTime);
    }

    private static int CreateNotificationId()
    {
        return Random.Shared.Next(1, int.MaxValue);
    }



    private List<TaskItem> GetVisibleTasks()
    {
        IEnumerable<TaskItem> query = _tasks;

        switch (_currentFilter)
        {
            case "Today":
                query = query.Where(task =>
                    !task.IsCompleted &&
                    task.DueDate.Date == DateTime.Today);
                break;

            case "Upcoming":
                query = query.Where(task =>
                    !task.IsCompleted &&
                    task.DueDate.Date > DateTime.Today);
                break;

            case "Completed":
                query = query.Where(task => task.IsCompleted);
                break;
        }

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            query = query.Where(task =>
                ContainsText(task.Title, _searchText) ||
                ContainsText(task.Description, _searchText) ||
                ContainsText(task.Category, _searchText));
        }

        return query
            .OrderBy(task => task.IsCompleted)
            .ThenBy(task => task.DueDate)
            .ThenBy(task => task.DueTime)
            .ToList();
    }

    private static bool ContainsText(
        string? source,
        string search)
    {
        return !string.IsNullOrEmpty(source) &&
               source.Contains(
                   search,
                   StringComparison.OrdinalIgnoreCase);
    }



    private void RefreshTasks()
    {
        var visibleTasks = GetVisibleTasks();

        TaskList.Children.Clear();

        foreach (var task in visibleTasks)
        {
            TaskList.Children.Add(CreateTaskCard(task));
        }

        bool noTasks = visibleTasks.Count == 0;

        EmptyState.IsVisible = noTasks;

        if (noTasks)
        {
            bool searching = !string.IsNullOrWhiteSpace(_searchText);

            EmptyTitleLabel.Text = searching
                ? "No matching tasks"
                : _currentFilter == "Completed"
                    ? "Nothing completed yet"
                    : "A fresh start";

            EmptyDescriptionLabel.Text = searching
                ? "Try a different search."
                : _currentFilter == "Today"
                    ? "You have no tasks due today."
                    : _currentFilter == "Upcoming"
                        ? "You have no upcoming tasks."
                        : _currentFilter == "Completed"
                            ? "Complete a task and it will appear here."
                            : "Add your first task and build some momentum.";
        }

        TaskHeadingLabel.Text = _currentFilter switch
        {
            "Today" => "Today's tasks",
            "Upcoming" => "Coming up",
            "Completed" => "Completed tasks",
            _ => "Your tasks"
        };

        TaskSubheadingLabel.Text = _currentFilter switch
        {
            "Today" => "Focus on what matters today.",
            "Upcoming" => "A little preparation goes a long way.",
            "Completed" => "Look at what you've accomplished.",
            _ => "Small steps count."
        };

        TaskCountLabel.Text =
            $"{visibleTasks.Count} " +
            (visibleTasks.Count == 1 ? "task" : "tasks");

        UpdateStatistics();
        UpdateFilterAppearance();
    }

    

    private View CreateTaskCard(TaskItem task)
    {
        var completionBox = new CheckBox
        {
            IsChecked = task.IsCompleted,
            Color = Accent,
            VerticalOptions = LayoutOptions.Start
        };

        completionBox.CheckedChanged += async (sender, args) =>
        {
            await SetTaskCompletedAsync(task, args.Value);
        };

        var titleLabel = new Label
        {
            Text = task.Title,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = task.IsCompleted
                ? MutedText
                : PrimaryText,
            LineBreakMode = LineBreakMode.WordWrap
        };

        if (task.IsCompleted)
        {
            titleLabel.TextDecorations = TextDecorations.Strikethrough;
        }

        var descriptionLabel = new Label
        {
            Text = task.Description,
            FontSize = 13,
            TextColor = SecondaryText,
            LineBreakMode = LineBreakMode.WordWrap,
            IsVisible = !string.IsNullOrWhiteSpace(task.Description)
        };

        var categoryLabel = new Label
        {
            Text = $"{task.Category} · {task.Priority}",
            FontSize = 12,
            TextColor = Accent
        };

        var dueLabel = new Label
        {
            Text = task.DueLabel,
            FontSize = 12,
            TextColor = task.IsOverdue
                ? Danger
                : task.IsCompleted
                    ? Success
                    : SecondaryText
        };

        var titleAndDetails = new VerticalStackLayout
        {
            Spacing = 7,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                titleLabel,
                descriptionLabel,
                categoryLabel,
                dueLabel
            }
        };

        var editButton = new Button
        {
            Text = "Edit",
            FontSize = 12,
            Padding = new Thickness(13, 7),
            CornerRadius = 10,
            BackgroundColor = Accent,
            TextColor = Color.FromArgb("#09201C")
        };

        editButton.Clicked += async (sender, args) =>
        {
            await OpenTaskEditorAsync(task);
        };

        var moreButton = new Button
        {
            Text = "More",
            FontSize = 12,
            Padding = new Thickness(13, 7),
            CornerRadius = 10,
            BackgroundColor = SurfaceLight,
            TextColor = PrimaryText,
            BorderColor = BorderColor,
            BorderWidth = 1
        };

        moreButton.Clicked += async (sender, args) =>
        {
            await ShowTaskActionsAsync(task);
        };

        var actions = new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                editButton,
                moreButton
            }
        };

        var taskContent = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };

        taskContent.Add(completionBox, 0, 0);

        var detailsStack = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                titleAndDetails,
                actions
            }
        };

        taskContent.Add(detailsStack, 1, 0);

        return new Border
        {
            Padding = new Thickness(15),
            BackgroundColor = Surface,
            Stroke = BorderColor,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = 18
            },
            Content = taskContent
        };
    }

  

    private void UpdateStatistics()
    {
        int total = _tasks.Count;
        int completed = _tasks.Count(task => task.IsCompleted);
        int remaining = total - completed;

        double progress = total == 0
            ? 0
            : (double)completed / total;

        ProgressBarView.Progress = progress;
        ProgressLabel.Text = $"{Math.Round(progress * 100)}%";

        TotalLabel.Text = total.ToString();
        CompletedLabel.Text = completed.ToString();
        RemainingLabel.Text = remaining.ToString();

        ProgressDescription.Text = total == 0
            ? "Add a task to get started."
            : remaining == 0
                ? "Everything is done. Nicely organised."
                : $"{remaining} task{(remaining == 1 ? "" : "s")} left to tackle.";

        OverviewCompletedLabel.Text = completed.ToString();
        OverviewTotalLabel.Text = total.ToString();
        OverviewRemainingLabel.Text = remaining.ToString();

        OverviewProgressBar.Progress = progress;

        OverviewProgressDescription.Text = total == 0
            ? "Your progress will appear here."
            : $"{completed} of {total} tasks completed.";

        NextStepTitle.Text = remaining == 0
            ? "Everything is complete."
            : "Keep your momentum going.";

        NextStepDescription.Text = remaining == 0
            ? "You're all caught up. Add another task whenever you're ready."
            : $"You have {remaining} task{(remaining == 1 ? "" : "s")} remaining. Pick one and make progress.";

        ProgressBarView.Progress = Math.Clamp(progress, 0, 1);
        OverviewProgressBar.Progress = Math.Clamp(progress, 0, 1);
    }
}