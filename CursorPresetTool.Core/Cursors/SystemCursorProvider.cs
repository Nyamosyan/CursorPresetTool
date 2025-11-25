using CursorPresetTool.Core.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace CursorPresetTool.Core.Cursors
{
    /// <summary>
    /// レジストリから読み取った 1 種類のカーソル設定。
    /// </summary>
    public sealed class SystemCursorInfo
    {
        public string KeyName { get; }
        public string DisplayName { get; }
        public string? RawValue { get; }
        public string? FullPath { get; }
        public bool FileExists => !string.IsNullOrEmpty(FullPath) && File.Exists(FullPath);

        public SystemCursorInfo(string keyName, string displayName, string? rawValue, string? fullPath)
        {
            KeyName = keyName;
            DisplayName = displayName;
            RawValue = rawValue;
            FullPath = fullPath;
        }
    }

    public sealed class SystemCursorSnapshot
    {
        private readonly Dictionary<string, SystemCursorInfo> _byKey;

        public IReadOnlyList<SystemCursorInfo> Items { get; }

        public SystemCursorSnapshot(IReadOnlyList<SystemCursorInfo> items)
        {
            Items = items;
            _byKey = new(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
                _byKey[item.KeyName] = item;
        }

        public SystemCursorInfo? GetByKey(string keyName)
            => _byKey.TryGetValue(keyName, out var info) ? info : null;
    }

    /// <summary>
    /// HKEY_CURRENT_USER\Control Panel\Cursors の読取サービス。
    /// </summary>
    public sealed class SystemCursorProvider
    {
        private const string CursorsKeyPath = @"Control Panel\Cursors";

        private static readonly CursorSlot[] s_slots = CursorSlot.All;

        public SystemCursorSnapshot CaptureSnapshot()
        {
            using var key = Registry.CurrentUser.OpenSubKey(CursorsKeyPath, writable: false);

            var list = new List<SystemCursorInfo>(s_slots.Length);

            foreach (var slot in s_slots)
            {
                var raw = key?.GetValue(slot.Key) as string;
                var fullPath = ExpandCursorPath(raw);

                list.Add(new SystemCursorInfo(
                    keyName: slot.Key,
                    displayName: slot.DisplayName,
                    rawValue: raw,
                    fullPath: fullPath));
            }

            return new SystemCursorSnapshot(list);
        }

        private static string? ExpandCursorPath(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var expanded = Environment.ExpandEnvironmentVariables(raw);

            if (Path.IsPathRooted(expanded) || expanded.StartsWith(@"\\"))
                return expanded;

            var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var cursorDir = Path.Combine(windowsDir, "Cursors");
            return Path.Combine(cursorDir, expanded);
        }
    }
}
