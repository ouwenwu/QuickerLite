using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using QuickerLite.Models;
using QuickerLite.Services;

namespace QuickerLite;

public partial class TaskListWindow : Window, INotifyPropertyChanged
{
    private readonly TaskListService taskListService;
    private bool isDirty;
    private bool isClosingConfirmed;

    public TaskListWindow(TaskListService taskListService)
    {
        this.taskListService = taskListService;
        InitializeComponent();
        DataContext = this;
        LoadTasks();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TaskItem> Tasks { get; } = [];

    private void LoadTasks()
    {
        Tasks.Clear();
        foreach (var task in taskListService.Load())
        {
            SubscribeTask(task);
            Tasks.Add(task);
        }

        isDirty = false;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var task = new TaskItem
        {
            Date = System.DateTime.Today.ToString("yyyy-MM-dd"),
            Priority = "P1",
            Status = "待办",
            Title = "新任务"
        };
        SubscribeTask(task);
        Tasks.Add(task);
        isDirty = true;
        TaskGrid.SelectedItem = task;
        TaskGrid.ScrollIntoView(task);
    }

    private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = TaskGrid.SelectedItems.Cast<TaskItem>().ToList();
        if (selected.Count == 0)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(this, $"确定删除选中的 {selected.Count} 条任务吗？", "删除任务", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (result != MessageBoxResult.OK)
        {
            return;
        }

        foreach (var task in selected)
        {
            task.PropertyChanged -= Task_PropertyChanged;
            Tasks.Remove(task);
        }

        isDirty = true;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveTasks();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TaskGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        isDirty = true;
    }

    private void TaskGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
        isDirty = true;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!isDirty || isClosingConfirmed)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            this,
            "计划列表有未保存的修改，是否保存？",
            "计划列表",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
        {
            e.Cancel = true;
            return;
        }

        if (result == MessageBoxResult.Yes)
        {
            SaveTasks();
        }

        isClosingConfirmed = true;
    }

    private void SaveTasks()
    {
        TaskGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        TaskGrid.CommitEdit(DataGridEditingUnit.Row, true);
        taskListService.Save(Tasks);
        isDirty = false;
    }

    private void SubscribeTask(TaskItem task)
    {
        task.PropertyChanged += Task_PropertyChanged;
    }

    private void Task_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        isDirty = true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
