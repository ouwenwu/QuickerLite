using System;
using System.Diagnostics;
using QuickerLite.Models;

namespace QuickerLite.Services;

public sealed class ActionExecutor
{
    public void Execute(ActionItem action)
    {
        if (string.IsNullOrWhiteSpace(action.Target))
        {
            return;
        }

        var type = action.Type.Trim().ToLowerInvariant();
        var target = Environment.ExpandEnvironmentVariables(action.Target);
        var args = Environment.ExpandEnvironmentVariables(action.Args ?? "");

        var startInfo = type switch
        {
            "shell" => new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = string.IsNullOrWhiteSpace(args) ? $"/c {target}" : $"/c {target} {args}",
                UseShellExecute = false,
                CreateNoWindow = false
            },
            "process" => new ProcessStartInfo
            {
                FileName = target,
                Arguments = args,
                UseShellExecute = true
            },
            "file" or "folder" or "url" => new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            },
            _ => new ProcessStartInfo
            {
                FileName = target,
                Arguments = args,
                UseShellExecute = true
            }
        };

        Process.Start(startInfo);
    }
}
