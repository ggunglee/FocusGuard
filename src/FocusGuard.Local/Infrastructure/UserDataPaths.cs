using System;
using System.IO;

namespace FocusGuard.Infrastructure;

public static class UserDataPaths
{
    public static string RootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FocusGuard");

    public static string DatabasePath { get; } = Path.Combine(RootDirectory, "focusguard.db");

    public static string MemoPath { get; } = Path.Combine(RootDirectory, "memo.txt");

    public static void EnsureDirectory() => Directory.CreateDirectory(RootDirectory);
}
