using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Program
{
    private static ConcurrentDictionary<string, string> _matches = new ConcurrentDictionary<string, string>();
    public static void Main()
    {
        Console.Write("Please enter drive letter for search, or leave blank to iterate all drives: ");
        var driveLetter = Console.ReadLine();
        var filePaths = new ConcurrentBag<string>();

        DriveInfo[] drives;
        if (string.IsNullOrWhiteSpace(driveLetter))
        {
            drives = DriveInfo.GetDrives();
        }
        else
        {
            drives = new DriveInfo[] { new DriveInfo(driveLetter + @":\") };
        }

        // Background task to update console every 15 seconds
        var cts = new CancellationTokenSource();
        Task.Run(() => DisplayProgress(filePaths, cts.Token));

        Parallel.ForEach(drives, drive =>
        {
            if (drive.IsReady)
            {
                Console.WriteLine($"Searching in {drive.Name}...");
                SearchFiles(drive.RootDirectory, filePaths);
            }
        });

        cts.Cancel();
        Console.WriteLine($"Search complete. Found {filePaths.Count} files.");

        SearchForMnemonics(filePaths);
        foreach (var num in new int[20])
        {
            Console.WriteLine("...");
        }
        Console.WriteLine($"Found {_matches.Count} matches");
        foreach (var match in _matches) { Console.WriteLine($"{match.Key} - {match.Value}"); }
        foreach(var num in new int[20])
        {
            Console.WriteLine("...");
        }
        Console.WriteLine("Doneso. Press any key to exit.");
        Console.ReadLine();
    }

    public void TestNum()
    {
        //Does this work lol? Yes. Almost useless.
        foreach(var num in new int[100])
        {
            Console.WriteLine($"{num}");
        }
    }

    private static void SearchFiles(DirectoryInfo rootDirectory, ConcurrentBag<string> filePaths)
    {
        string[] fileExtensions = { "*.txt", "*.docx", "*.rtf", "*.json", "*.csv", "*.xml", "*.log", "*.md", "*.ini", "*.yaml", "*.yml", "*.cfg" };
        var directoriesToSearch = new Stack<DirectoryInfo>();

        directoriesToSearch.Push(rootDirectory);

        while (directoriesToSearch.Count > 0)
        {
            var currentDirectory = directoriesToSearch.Pop();

            try
            {
                foreach (var ext in fileExtensions)
                {
                    foreach (var file in currentDirectory.GetFiles(ext))
                    {
                        filePaths.Add(file.FullName);
                    }
                }

                foreach (var subDir in currentDirectory.GetDirectories())
                {
                    directoriesToSearch.Push(subDir);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Skipping restricted directory: {currentDirectory.FullName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing {currentDirectory.FullName}: {ex.Message}");
            }
        }
    }

    private static void SearchForMnemonics(ConcurrentBag<string> filePaths)
    {
        var mnemonicPatterns = new Dictionary<string, string>
    {
        { "BIP39 (12 words)", @"\b([a-z]+ ){11}[a-z]+\b" },
        { "BIP39 (24 words)", @"\b([a-z]+ ){23}[a-z]+\b" },
        { "GreenAddress 18-word", @"\b([a-z]+ ){17}[a-z]+\b" },
        { "Electrum Old (13 words)", @"\b([a-z]+ ){12}[a-z]+\b" },
        { "Electrum New (12 words)", @"\b([a-z]+ ){11}[a-z]+\b" }
    };

        Console.WriteLine("Available mnemonic formats:");
        foreach (var type in mnemonicPatterns.Keys)
        {
            Console.WriteLine($"- {type}");
        }

        Console.Write("Enter mnemonic format (or leave blank to check all): ");
        var userChoice = Console.ReadLine()?.Trim();

        var selectedPatterns = string.IsNullOrWhiteSpace(userChoice)
            ? mnemonicPatterns.Values
            : mnemonicPatterns.Where(kv => kv.Key.ToLower().Contains(userChoice.ToLower()))
                              .Select(kv => kv.Value);

        if (!selectedPatterns.Any())
        {
            Console.WriteLine("No matching mnemonic format found.");
            return;
        }

        Console.WriteLine("Scanning files for mnemonics...");
        foreach (var filePath in filePaths)
        {
            try
            {
                string content = File.ReadAllText(filePath);

                foreach (var pattern in selectedPatterns)
                {
                    var matches = Regex.Matches(content, pattern);
                    foreach (Match match in matches)
                    {
                        _matches.TryAdd(filePath, match.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not read {filePath}: {ex.Message}");
            }
        }

        Console.WriteLine("Mnemonic search completed.");
    }

    static void DisplayProgress(ConcurrentBag<string> filePaths, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Console.WriteLine($"Searching... Files found so far: {filePaths.Count}");
            Thread.Sleep(15000); 
        }
    }
}
