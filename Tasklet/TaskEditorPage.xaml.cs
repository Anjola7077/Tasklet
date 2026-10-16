using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Tasklet.Models;

namespace Tasklet;

public partial class TaskEditorPage : ContentPage
{
    private readonly TaskItem? _existingTask;

    private readonly TaskCompletionSource<TaskItem?> _resultSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool _isSaving;
    private bool _isClosing;

    public TaskItem? Result { get; private set; }

    public Task<TaskItem?> WaitForResultAsync()
    {
        return _resultSource.Task;
    }

    public TaskEditorPage(TaskItem? existingTask = null)
    {
        InitializeComponent();

        _existingTask = existingTask;

        Title = existingTask is null
            ? "New task"
            : "Edit task";

        CategoryPicker.ItemsSource = new[]
        {
            "Personal",
            "Work",
            "Study",
            "Health",
            "Shopping",
            "Other"
        };

        PriorityPicker.ItemsSource = new[]
        {
            "Low",
            "Medium",
            "High",
            "Urgent"
        };

        RecurrencePicker.ItemsSource = new[]
        {
            "None",
            "Daily",
            "Weekly",
            "Monthly"
        };

        if (existingTask is not null)
        {
            LoadExistingTask(existingTask);
        }
        else
        {
            InitialiseNewTask();
        }

        UpdateReminderVisibility();
    }


    private void InitialiseNewTask()
    {
        TitleEntry.Text = string.Empty;
        DescriptionEditor.Text = string.Empty;

        DueDatePicker.Date = DateTime.Today;
        DueTimePicker.Time = new TimeSpan(17, 0, 0);

        CategoryPicker.SelectedItem = "Personal";
        PriorityPicker.SelectedItem = "Medium";
        RecurrencePicker.SelectedItem = "None";

        ReminderSwitch.IsToggled = false;
        ReminderTimePicker.Time = new TimeSpan(9, 0, 0);

        ValidationLabel.IsVisible = false;
        ValidationLabel.Text = string.Empty;
    }

    private void LoadExistingTask(TaskItem task)
    {
        TitleEntry.Text = task.Title;
        DescriptionEditor.Text = task.Description;

        DueDatePicker.Date = task.DueDate;
        DueTimePicker.Time = task.DueTime;

        CategoryPicker.SelectedItem = task.Category;
        PriorityPicker.SelectedItem = task.Priority;
        RecurrencePicker.SelectedItem = task.Recurrence;

        ReminderSwitch.IsToggled = task.ReminderTime.HasValue;

        ReminderTimePicker.Time =
            task.ReminderTime.GetValueOrDefault(
                new TimeSpan(9, 0, 0));

        ValidationLabel.IsVisible = false;
        ValidationLabel.Text = string.Empty;
    }

    

    private void OnReminderToggled(
        object sender,
        ToggledEventArgs e)
    {
        UpdateReminderVisibility();
    }

    private void UpdateReminderVisibility()
    {
        ReminderOptions.IsVisible = ReminderSwitch.IsToggled;
    }



    private async void OnSaveClicked(
        object sender,
        EventArgs e)
    {
        if (_isSaving || _isClosing)
            return;

        string title = TitleEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            ShowValidation("Enter a title for your task.");

            await DisplayAlert(
                "Task title required",
                "Please enter a title before saving.",
                "OK");

            TitleEntry.Focus();
            return;
        }

        _isSaving = true;

        try
        {
            DateTime dueDate =
                DueDatePicker.Date.GetValueOrDefault(DateTime.Today).Date;

            TimeSpan dueTime =
                DueTimePicker.Time.GetValueOrDefault(
                    new TimeSpan(17, 0, 0));

            TimeSpan? reminderTime = null;

            if (ReminderSwitch.IsToggled)
            {
                TimeSpan selectedReminderTime =
                    ReminderTimePicker.Time.GetValueOrDefault(
                        new TimeSpan(9, 0, 0));

                DateTime reminderDateTime =
                    dueDate.Add(selectedReminderTime);

                if (reminderDateTime <= DateTime.Now)
                {
                    bool saveWithoutReminder = await DisplayAlert(
                        "Reminder time has passed",
                        "The selected reminder time has already passed. Save this task without a reminder?",
                        "Save without reminder",
                        "Go back");

                    if (!saveWithoutReminder)
                        return;
                }
                else
                {
                    reminderTime = selectedReminderTime;
                }
            }

            Result = new TaskItem
            {
                Id = _existingTask?.Id ?? Guid.NewGuid(),

                Title = title,

                Description =
                    DescriptionEditor.Text?.Trim() ?? string.Empty,

                Category =
                    CategoryPicker.SelectedItem?.ToString()
                    ?? "Personal",

                Priority =
                    PriorityPicker.SelectedItem?.ToString()
                    ?? "Medium",

                Recurrence =
                    RecurrencePicker.SelectedItem?.ToString()
                    ?? "None",

                DueDate = dueDate,
                DueTime = dueTime,

                CreatedAt =
                    _existingTask?.CreatedAt ?? DateTime.Now,

                IsCompleted =
                    _existingTask?.IsCompleted ?? false,

                NotificationId =
                    _existingTask?.NotificationId ?? 0,

                ReminderTime = reminderTime
            };

            
            _isClosing = true;

            await Navigation.PopModalAsync();

            _resultSource.TrySetResult(Result);
        }
        catch (Exception ex)
        {
            _isClosing = false;

            await DisplayAlert(
                "Unable to save task",
                ex.Message,
                "OK");
        }
        finally
        {
            _isSaving = false;
        }
    }



    private async void OnCancelClicked(
        object sender,
        EventArgs e)
    {
        if (_isClosing)
            return;

        _isClosing = true;

        try
        {
            Result = null;

            await Navigation.PopModalAsync();

            _resultSource.TrySetResult(null);
        }
        catch (Exception ex)
        {
            _isClosing = false;

            await DisplayAlert(
                "Unable to close editor",
                ex.Message,
                "OK");
        }
    }

    

    private void ShowValidation(string message)
    {
        ValidationLabel.Text = message;
        ValidationLabel.IsVisible = true;
    }
}