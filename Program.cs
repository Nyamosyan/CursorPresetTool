using Microsoft.Win32;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;

namespace CursorPresetTool
{
    [SupportedOSPlatform("windows")]
    internal static partial class Program
    {
        // Win32 API 呼び出し（カーソル再読み込み用）
        [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SystemParametersInfo(
            uint uiAction, uint uiParam, nint pvParam, uint fWinIni);

        private const uint SPI_SETCURSORS = 0x0057;
        private const uint SPIF_SENDCHANGE = 0x0002;

        // プリセット保存先ディレクトリ（.\presets）
        private static readonly string PresetDirectory = Path.Combine(AppContext.BaseDirectory, "presets");

        // 対象とするカーソルキー
        private static readonly string[] CursorKeys =
        [
            "AppStarting",
            "Arrow",
            "Crosshair",
            "Hand",
            "Help",
            "IBeam",
            "No",
            "NWPen",
            "Person",
            "Pin",
            "SizeAll",
            "SizeNESW",
            "SizeNS",
            "SizeNWSE",
            "SizeWE",
            "UpArrow",
            "Wait"
        ];

        // JsonSerializerOptions を再利用
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        // プリセット用クラス
        private sealed class CursorPreset
        {
            public string Name { get; set; } = string.Empty;
            public Dictionary<string, string> Cursors { get; set; } = [];
        }

        static int Main(string[] args)
        {
            if(args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            var command = args[0].ToLowerInvariant();

            try
            {
                switch(command)
                {
                    case "export":
                        if(args.Length < 2)
                        {
                            Console.WriteLine("export にはプリセット名が必要です。");
                            PrintUsage();
                            return 1;
                        }
                        ExportPreset(args[1]);
                        break;

                    case "apply":
                        HandleApply(args);
                        break;

                    case "reset":
                        ResetToDefault();
                        break;

                    case "list":
                        ListPresets();
                        break;

                    default:
                        Console.WriteLine($"未知のコマンドです: {command}");
                        PrintUsage();
                        return 1;
                }

                return 0;
            }
            catch(Exception ex)
            {
                Console.WriteLine("エラーが発生しました:");
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("CursorPresetTool - マウスカーソルのプリセット保存・適用・リセットツール");
            Console.WriteLine();
            Console.WriteLine("使い方:");
            Console.WriteLine("  CursorPresetTool export <name>    現在のカーソル設定をプリセットとして保存");
            Console.WriteLine("  CursorPresetTool apply  <name>    プリセット名または JSON パスを指定して適用");
            Console.WriteLine("  CursorPresetTool apply  <index>   list で表示されたインデックス番号で適用 (0 は reset と同じ)");
            Console.WriteLine("  CursorPresetTool list             プリセット一覧を表示 (インデックス付き)");
            Console.WriteLine("  CursorPresetTool reset            Windows既定カーソルにリセット");
            Console.WriteLine();
            Console.WriteLine($"プリセット保存先: {PresetDirectory}<name>.json");
        }

        /// <summary>
        /// apply の引数を解釈して、名前／パス／インデックスのどれかとして処理する
        /// </summary>
        private static void HandleApply(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine("apply にはプリセット名またはインデックスが必要です。");
                PrintUsage();
                return;
            }

            var target = args[1];

            // 数字ならインデックス指定として扱う
            if(int.TryParse(target, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            {
                ApplyPresetByIndex(index);
            }
            else
            {
                // それ以外は従来通り名前／パス扱い
                ApplyPresetByNameOrPath(target);
            }
        }

        /// <summary>
        /// 現在のカーソル設定をプリセットとして出力
        /// </summary>
        private static void ExportPreset(string name)
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", writable: false)
                         ?? throw new InvalidOperationException(@"レジストリ 'HKCU\\Control Panel\\Cursors' を開けませんでした。");

            var preset = new CursorPreset
            {
                Name = name
            };

            foreach(var cursorKey in CursorKeys)
            {
                var value = key.GetValue(cursorKey) as string;
                if(!string.IsNullOrEmpty(value))
                {
                    preset.Cursors[cursorKey] = value;
                }
            }

            Directory.CreateDirectory(PresetDirectory);
            var filePath = Path.Combine(PresetDirectory, name + ".json");

            var json = JsonSerializer.Serialize(preset, JsonOptions);
            File.WriteAllText(filePath, json);

            Console.WriteLine($"プリセットをエクスポートしました: {filePath}");
        }

        /// <summary>
        /// プリセット一覧を表示（0 は Windows 既定）
        /// </summary>
        private static void ListPresets()
        {
            Console.WriteLine("利用可能なカーソルプリセット:");
            Console.WriteLine("  0: (Windows 既定のカーソル)");

            if(!Directory.Exists(PresetDirectory))
            {
                Console.WriteLine("プリセットディレクトリがまだ存在しません。");
                Console.WriteLine($"  {PresetDirectory}");
                return;
            }

            var files = GetPresetFiles();
            if(files.Length == 0)
            {
                Console.WriteLine("プリセットがまだありません。");
                return;
            }

            for(int i = 0; i < files.Length; i++)
            {
                var name = Path.GetFileNameWithoutExtension(files[i]);
                Console.WriteLine($"  {i + 1}: {name}");
            }
        }

        /// <summary>
        /// インデックス番号からプリセットを適用（0 は reset）
        /// </summary>
        private static void ApplyPresetByIndex(int index)
        {
            if(index == 0)
            {
                ResetToDefault();
                return;
            }

            if(index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "インデックスは 0 以上で指定してください。");
            }

            var files = GetPresetFiles();
            if(files.Length == 0)
            {
                throw new InvalidOperationException("適用可能なプリセットがありません。");
            }

            if(index > files.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"有効なインデックスは 0 - {files.Length} です。");
            }

            var filePath = files[index - 1];
            ApplyPresetFromFile(filePath);
        }

        /// <summary>
        /// プリセット名 or パスから適用
        /// </summary>
        private static void ApplyPresetByNameOrPath(string name)
        {
            string filePath;

            // 引数がパスっぽいならそのまま使う
            if(name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                name.Contains(Path.DirectorySeparatorChar) ||
                name.Contains(Path.AltDirectorySeparatorChar))
            {
                filePath = name;
            }
            else
            {
                filePath = Path.Combine(PresetDirectory, name + ".json");
            }

            if(!File.Exists(filePath))
            {
                throw new FileNotFoundException("プリセットファイルが見つかりません。", filePath);
            }

            ApplyPresetFromFile(filePath);
        }

        /// <summary>
        /// 実際にファイルから読み込んでレジストリに適用する処理本体
        /// </summary>
        private static void ApplyPresetFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var preset = JsonSerializer.Deserialize<CursorPreset>(json, JsonOptions)
                         ?? throw new InvalidOperationException("プリセット JSON の読み込みに失敗しました。");

            // 適用前にファイル存在チェック
            var missing = new List<string>();

            foreach(var kv in preset.Cursors)
            {
                if(!string.IsNullOrWhiteSpace(kv.Value) && !File.Exists(kv.Value))
                {
                    missing.Add($"{kv.Key}: {kv.Value}");
                }
            }

            if(missing.Count > 0)
            {
                Console.WriteLine("次のカーソルファイルが見つかりません:");
                foreach(var item in missing)
                {
                    Console.WriteLine("  - " + item);
                }
                Console.Write("このまま続行しますか？ (y/N): ");
                var response = Console.ReadLine()?.Trim().ToLowerInvariant();

                if(response != "y" && response != "yes")
                {
                    Console.WriteLine("適用を中止しました。");
                    return;
                }
            }

            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", writable: true)
                         ?? throw new InvalidOperationException(@"レジストリ 'HKCU\\Control Panel\\Cursors' を開けませんでした。");

            // スキーム名っぽいもの（"" に入る）も一応更新しておく
            key.SetValue("", preset.Name);

            foreach(var kv in preset.Cursors)
            {
                key.SetValue(kv.Key, kv.Value);
            }

            // 変更を反映
            SystemParametersInfo(SPI_SETCURSORS, 0, 0, SPIF_SENDCHANGE);

            Console.WriteLine($"プリセット '{preset.Name}' を適用しました。");
        }

        /// <summary>
        /// プリセットファイル（*.json）をファイル名順で取得
        /// </summary>
        private static string[] GetPresetFiles()
        {
            if(!Directory.Exists(PresetDirectory))
            {
                return [];
            }

            return [.. Directory.EnumerateFiles(PresetDirectory, "*.json")
                            .OrderBy(f => Path.GetFileNameWithoutExtension(f),
                                     StringComparer.OrdinalIgnoreCase)];
        }

        /// <summary>
        /// Windowsの既定カーソルにリセット
        /// </summary>
        private static void ResetToDefault()
        {
            // Windowsの「既定ユーザー」プロファイルのカーソル設定を参照
            using var defaultKey = Registry.Users.OpenSubKey(@".DEFAULT\Control Panel\Cursors", writable: false)
                               ?? throw new InvalidOperationException(@"レジストリ 'HKEY_USERS\\.DEFAULT\\Control Panel\\Cursors' を開けませんでした。");

            using var userKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", writable: true)
                             ?? throw new InvalidOperationException(@"レジストリ 'HKCU\\Control Panel\\Cursors' を開けませんでした。");

            // 既定スキーム名（デフォルト値 ""）
            var defaultSchemeName = defaultKey.GetValue("") as string;
            if(!string.IsNullOrEmpty(defaultSchemeName))
            {
                userKey.SetValue("", defaultSchemeName);
            }
            else
            {
                userKey.DeleteValue("", false);
            }

            // 各カーソルキーを既定ユーザーからコピー
            foreach(var cursorKey in CursorKeys)
            {
                var value = defaultKey.GetValue(cursorKey) as string;
                if(value is not null)
                {
                    userKey.SetValue(cursorKey, value);
                }
                else
                {
                    // 既定側に無ければ削除
                    userKey.DeleteValue(cursorKey, false);
                }
            }

            // 変更を反映
            SystemParametersInfo(SPI_SETCURSORS, 0, 0, SPIF_SENDCHANGE);

            Console.WriteLine("Windowsの既定カーソルにリセットしました。");
        }
    }
}
