using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TagLib;
using File = System.IO.File;

namespace AsciiConverter
{
    public class TagProcessor
    {
        private static readonly Regex ValidRegex = new Regex(
            @"^[a-zа-яё\d\s\-\+\=\!\@\#\$\%\^\&\*\(\)\[\]\{\}\;\:\'\,\<\.\>\/\?\`\~_\""]*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        private static readonly Encoding Encoding1251 = CodePagesEncodingProvider.Instance.GetEncoding(1251);
        private static readonly Encoding Encoding8859 = Encoding.GetEncoding("iso-8859-1");
        
        private string LogFilename { get; }
        private string Folder { get; }
        private bool IsRecursive { get; }
        private List<PropertyInfo> Properties { get; }
        

        public TagProcessor(string folder, string logFilename, bool isRecursive = true)
        {
            Properties = GetStringProperties().ToList();
            LogFilename = logFilename;
            Folder = folder;
            IsRecursive = isRecursive;
        }
        
        public void Process()
        {   
            using (var logFile = new StreamWriter(LogFilename, true))
            {
                logFile.WriteLine("--=={ Tag renaming process started }==--");
                logFile.WriteLine($"Working folder{(IsRecursive ? " (recursive)" : "")}: {Folder}");
                logFile.WriteLine($"{DateTime.Now:g}: Enumerating files...");
                
                logFile.Flush();
                
                var files = Directory.EnumerateFiles(Folder, "*.mp3",
                    IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();
                
                logFile.WriteLine($"{DateTime.Now:g}: Found {files.Count} .mp3 files");
                logFile.WriteLine($"{DateTime.Now:g}: Processing tags...");

                logFile.Flush();
                
                var index = 1;
                var modifiedCounter = 0;
                foreach (var file in files)
                {
                    logFile.WriteLine($"{DateTime.Now:g}: Processing {file} ({index++}/{files.Count})...");
                    using (var mp3File = TagLib.File.Create(file))
                    {
                        var modified = ProcessTag(logFile, mp3File.Tag);
                        if (modified)
                        {
                            mp3File.Save();
                            modifiedCounter++;
                        }
                    }
                    logFile.Flush();
                }
                
                logFile.WriteLine($"{DateTime.Now:g}: Completed. Total files modified: {modifiedCounter} out of {files.Count}.");
                logFile.WriteLine();
                logFile.WriteLine();
            }
        }
        
        private bool ProcessTag(StreamWriter logFile, Tag tag)
        {
            var modified = false;
            foreach (var propertyInfo in Properties)
            {
                if (propertyInfo.PropertyType == typeof(string[]))
                {
                    var values = (string[])propertyInfo.GetValue(tag);
                    for (var i = 0; i < values.Length; i++)
                    {
                        var value = values[i];
                        if (string.IsNullOrWhiteSpace(value) || ValidRegex.IsMatch(value))
                        {
                            continue;
                        }

                        var newValue = Convert(value);
                        values[i] = newValue;
                        
                        logFile.WriteLine($"{DateTime.Now:g}: Replaced {propertyInfo.Name}[{i}] '{value}' with '{newValue}'");
                        modified = true;
                    }
                    
                    propertyInfo.SetValue(tag, values);
                }
                else
                {
                    var value = (string) propertyInfo.GetValue(tag);
                    if (string.IsNullOrWhiteSpace(value) || ValidRegex.IsMatch(value))
                    {
                        continue;
                    }

                    var newValue = Convert(value);
                    propertyInfo.SetValue(tag, newValue);

                    logFile.WriteLine($"{DateTime.Now:g}: Replaced {propertyInfo.Name} '{value}' with '{newValue}'");
                    modified = true;
                }
            }

            return modified;
        }

        private static void ConvertValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || ValidRegex.IsMatch(value))
            {
                return;
            }
                
            var newValue = Convert(value);
        }
        
        private static string Convert(string current)
        {
            var bytes = Encoding8859.GetBytes(current);
            return Encoding1251.GetString(bytes);
        }
        
        private static IEnumerable<PropertyInfo> GetStringProperties()
        {
            var type = typeof(Tag);
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var propertyInfo in properties)
            {
                if ((propertyInfo.PropertyType == typeof(string) || propertyInfo.PropertyType == typeof(string[])) &&
                    propertyInfo.CanWrite)
                {
                    yield return propertyInfo;
                }
            }
        }
    }
}