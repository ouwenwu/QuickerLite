using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using QuickerLite.Models;

namespace QuickerLite.Services;

public sealed class TaskListService
{
    private readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string taskListPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QuickerLite",
        "task-list.json");

    public IReadOnlyList<TaskItem> Load()
    {
        try
        {
            if (File.Exists(taskListPath))
            {
                var json = File.ReadAllText(taskListPath);
                var items = JsonSerializer.Deserialize<List<TaskItem>>(json, options) ?? [];
                return Normalize(items);
            }
        }
        catch
        {
        }

        var defaults = CreateDefaultTasks();
        Save(defaults);
        return defaults;
    }

    public void Save(IEnumerable<TaskItem> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(taskListPath)!);
        File.WriteAllText(taskListPath, JsonSerializer.Serialize(Normalize(items), options));
    }

    private static List<TaskItem> Normalize(IEnumerable<TaskItem> items)
    {
        return items
            .Where(item => !string.IsNullOrWhiteSpace(item.Title)
                || !string.IsNullOrWhiteSpace(item.Goal)
                || !string.IsNullOrWhiteSpace(item.Notes))
            .Select(item =>
            {
                item.Date = item.Date?.Trim() ?? "";
                item.Priority = NormalizePriority(item.Priority);
                item.Title = item.Title?.Trim() ?? "";
                item.Goal = item.Goal?.Trim() ?? "";
                item.Status = NormalizeStatus(item.Status);
                item.Notes = item.Notes?.Trim() ?? "";
                return item;
            })
            .ToList();
    }

    private static string NormalizePriority(string? priority)
    {
        return priority is "P0" or "P1" or "P2" or "P3" ? priority : "P1";
    }

    private static string NormalizeStatus(string? status)
    {
        return status is "待办" or "进行中" or "完成" or "搁置" ? status : "待办";
    }

    private static List<TaskItem> CreateDefaultTasks()
    {
        return
        [
            new() { Date = "2026-05-18", Priority = "P0", Title = "修复动作左键单击异常", Goal = "所有动作左键点击恢复正常", Status = "待办", Notes = "优先排查最近新增的取色器/异步点击逻辑" },
            new() { Date = "2026-05-18", Priority = "P0", Title = "回归测试通用栏所有动作", Goal = "记事本、翻译、剪贴板、常用软件、置顶、Everything、取色等都能点击", Status = "待办", Notes = "每个动作至少点一次" },
            new() { Date = "2026-05-19", Priority = "P1", Title = "检查右键菜单功能", Goal = "删除动作、翻译菜单、局域网共享菜单、软件列表管理、Everything路径设置正常", Status = "待办", Notes = "确认右键不影响左键" },
            new() { Date = "2026-05-19", Priority = "P1", Title = "测试中键唤出和滚轮翻页", Goal = "中键弹窗、滚轮翻页、禁用 exe 放行都正常", Status = "待办", Notes = "尤其测试三维软件/浏览器" },
            new() { Date = "2026-05-20", Priority = "P1", Title = "整理默认动作分页", Goal = "确认通用栏动作顺序合理，常用动作在前两页", Status = "待办", Notes = "必要时调整 actions.json" },
            new() { Date = "2026-05-20", Priority = "P2", Title = "完善 ACTIONS.md", Goal = "每个内置动作都有复现说明", Status = "待办", Notes = "复查新动作说明" },
            new() { Date = "2026-05-21", Priority = "P1", Title = "测试常用软件列表", Goal = "从文件添加、从窗口添加、删除、启动软件都正常", Status = "待办", Notes = "测试 Chrome、VS Code、Apple Music" },
            new() { Date = "2026-05-21", Priority = "P2", Title = "测试取色器体验", Goal = "放大镜、HEX/RGB、自动复制、Esc取消正常", Status = "待办", Notes = "边缘屏幕、多显示器可选测" },
            new() { Date = "2026-05-22", Priority = "P1", Title = "发布一次稳定版 exe", Goal = "dist/QuickerLite.exe 可直接使用", Status = "待办", Notes = "发布前停掉旧进程" },
            new() { Date = "2026-05-23", Priority = "P2", Title = "备份配置文件", Goal = "保存 actions.json 和 AppData 关键配置", Status = "待办", Notes = "软件列表、Everything路径、局域网设置" }
        ];
    }
}
