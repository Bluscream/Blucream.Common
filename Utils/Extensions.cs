using ABI_RC.Core.Player;
using Bluscream;
using Bluscream.PropSpawner;
using ConfigLocker;
using Fnv1a;
using Gameloop.Vdf;
using Gameloop.Vdf.JsonConverter;
using Gameloop.Vdf.Linq;
using MelonLoader;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MMMUI;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NLog.Targets;
using Object = UnityEngine.Object;
using OverwolfPatcher.Classes;
using Photon.Realtime;
using static System.Net.Mime.MediaTypeNames;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Extensions {
    public static System.Random rnd = new System.Random();
    public enum BooleanStringMode {
        Numbers,
        TrueFalse,
        EnabledDisabled,
        OnOff,
        YesNo,
    }

    public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
        return instanceToConvert.GetType()
          .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
          .ToDictionary(
          propertyInfo => propertyInfo.Name,
          propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
    }
    public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
        Type propertyType = propertyInfo.PropertyType;
        object propertyValue = propertyInfo.GetValue(owner);
        if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
            var collectionItems = new List<Dictionary<string, object>>();
            var count = (int)propertyType.GetProperty("Count").GetValue(propertyValue);
            PropertyInfo indexerProperty = propertyType.GetProperty("Item");
            for (var index = 0; index < count; index++) {
                object item = indexerProperty.GetValue(propertyValue, new object[] { index });
                PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (itemProperties.Any()) {
                    Dictionary<string, object> dictionary = itemProperties
                      .ToDictionary(
                        subtypePropertyInfo => subtypePropertyInfo.Name,
                        subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
                    collectionItems.Add(dictionary);
                }
            }
            return collectionItems;
        }
        if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
            return propertyValue;
        }
        PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        if (properties.Any()) {
            return properties.ToDictionary(
                                subtypePropertyInfo => subtypePropertyInfo.Name,
                                subtypePropertyInfo => (object)Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
        }
        return propertyValue;
    }
    public static bool ExpiredSince(this DateTime dateTime, int minutes) {
        return (dateTime - DateTime.Now).TotalMinutes < minutes;
    }
    public static TimeSpan StripMilliseconds(this TimeSpan time) {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
    }
    public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new DirectoryInfo(final);
    }
    public static bool IsEmpty(this DirectoryInfo directory) {
        return !Directory.EnumerateFileSystemEntries(directory.FullName).Any();
    }
    public static string StatusString(this DirectoryInfo directory, bool existsInfo = false) {
        if (directory is null) return " (is null ❌)";
        if (File.Exists(directory.FullName)) return " (is file ❌)";
        if (!directory.Exists) return " (does not exist ❌)";
        if (directory.IsEmpty()) return " (is empty ⚠️)";
        return existsInfo ? " (exists ✅)" : string.Empty;
    }
    public static void Copy(this DirectoryInfo source, DirectoryInfo target, bool overwrite = false) {
        Directory.CreateDirectory(target.FullName);
        foreach (FileInfo fi in source.GetFiles())
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), overwrite);
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            Copy(diSourceSubDir, target.CreateSubdirectory(diSourceSubDir.Name));
    }
    public static bool Backup(this DirectoryInfo directory, bool overwrite = false) {
        if (!directory.Exists) return false;
        var backupDirPath = directory.FullName + ".bak";
        if (Directory.Exists(backupDirPath) && !overwrite) return false;
        Directory.CreateDirectory(backupDirPath);
        foreach (FileInfo fi in directory.GetFiles()) fi.CopyTo(Path.Combine(backupDirPath, fi.Name), overwrite);
        foreach (DirectoryInfo diSourceSubDir in directory.GetDirectories()) {
            diSourceSubDir.Copy(Directory.CreateDirectory(Path.Combine(backupDirPath, diSourceSubDir.Name)), overwrite);
        }
        return true;
    }
    public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static FileInfo Combine(this FileInfo file, params string[] paths) {
        var final = file.DirectoryName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static string FileNameWithoutExtension(this FileInfo file) {
        return Path.GetFileNameWithoutExtension(file.Name);
    }
    public static string StatusString(this FileInfo file, bool existsInfo = false) {
        if (file is null) return "(is null ❌)";
        if (Directory.Exists(file.FullName)) return "(is directory ❌)";
        if (!file.Exists) return "(does not exist ❌)";
        if (file.Length < 1) return "(is empty ⚠️)";
        return existsInfo ? "(exists ✅)" : string.Empty;
    }
    public static void AppendLine(this FileInfo file, string line) {
        try
        {
            if (!file.Exists) file.Create();
            File.AppendAllLines(file.FullName, new string[] { line });
        } catch { }
    }
    public static void WriteAllText(this FileInfo file, string text) => File.WriteAllText(file.FullName, text);
    public static string ReadAllText(this FileInfo file) => File.ReadAllText(file.FullName);
    public static List<string> ReadAllLines(this FileInfo file) => File.ReadAllLines(file.FullName).ToList();
    public static bool Backup(this FileInfo file, bool overwrite = false) {
        if (!file.Exists) return false;
        var backupFilePath = file.FullName + ".bak";
        if (File.Exists(backupFilePath) && !overwrite) return false;
        File.Copy(file.FullName, backupFilePath, overwrite);
        return true;
    }
    public static bool Restore(this FileInfo file, bool overwrite = false) {
        if (!file.Exists || !File.Exists(file.FullName + ".bak")) return false;
        if (overwrite) File.Delete(file.FullName);
        File.Move(file.FullName + ".bak", file.FullName);
        return true;
    }
    public static IEnumerable<string> SplitToLines(this string input) {
        if (input == null) {
            yield break;
        }
        using (System.IO.StringReader reader = new System.IO.StringReader(input)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                yield return line;
            }
        }
    }
    public static string ToTitleCase(this string source, string langCode = "en-US") {
        return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
    }
    public static bool Contains(this string source, string toCheck, StringComparison comp) {
        return source?.IndexOf(toCheck, comp) >= 0;
    }
    public static bool IsNullOrEmpty(this string source) {
        return string.IsNullOrEmpty(source);
    }
    public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
        if (count != -1) return source.Split(new string[] { split }, count, options);
        return source.Split(new string[] { split }, options);
    }
    public static string Remove(this string Source, string Replace) {
        return Source.Replace(Replace, string.Empty);
    }
    public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
        int place = Source.LastIndexOf(Find);
        if (place == -1)
            return Source;
        string result = Source.Remove(place, Find.Length).Insert(place, Replace);
        return result;
    }
    public static string EscapeLineBreaks(this string source) {
        return Regex.Replace(source, @"\r\n?|\n", @"\$&");
    }
    public static string Ext(this string text, string extension) {
        return text + "." + extension;
    }
    public static string Quote(this string text) {
        return SurroundWith(text, "\"");
    }
    public static string Enclose(this string text) {
        return SurroundWith(text, "(", ")");
    }
    public static string Brackets(this string text) {
        return SurroundWith(text, "[", "]");
    }
    public static string SurroundWith(this string text, string surrounds) {
        return surrounds + text + surrounds;
    }
    public static string SurroundWith(this string text, string starts, string ends) {
        return starts + text + ends;
    }
    public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
        if (!dictionary.ContainsKey(key))
            dictionary.Add(key, value);
    }
    public static Dictionary<string, object?> MergeWith(this Dictionary<string, object?> sourceDict, Dictionary<string, object?> destDict) {
        foreach (var kvp in sourceDict) {
            if (destDict.ContainsKey(kvp.Key)) {
                Console.WriteLine($"Key '{kvp.Key}' already exists and will be overwritten.");
            }
            destDict[kvp.Key] = kvp.Value;
        }
        return destDict;
    }
    public static Dictionary<string, object> MergeRecursiveWith(this Dictionary<string, object> sourceDict, Dictionary<string, object> targetDict) {
        foreach (var kvp in sourceDict) {
            if (targetDict.TryGetValue(kvp.Key, out var existingValue)) {
                if (existingValue is Dictionary<string, object> existingDict && kvp.Value is Dictionary<string, object> sourceDictValue) {
                    sourceDictValue.MergeRecursiveWith(existingDict);
                } else if (kvp.Value is null) {
                    targetDict.Remove(kvp.Key);
                    Console.WriteLine($"Removed key '{kvp.Key}' as it was set to null in the source dictionary.");
                } else
                {
                    targetDict[kvp.Key] = kvp.Value;
                    Console.WriteLine($"Overwriting existing value for key '{kvp.Key}'.");
                }
            } else
            {
                targetDict[kvp.Key] = kvp.Value;
            }
        }
        return targetDict;
    }
    public static string ToQueryString(this NameValueCollection nvc) {
        if (nvc == null) return string.Empty;
        StringBuilder sb = new StringBuilder();
        foreach (string key in nvc.Keys) {
            if (string.IsNullOrWhiteSpace(key)) continue;
            string[] values = nvc.GetValues(key);
            if (values == null) continue;
            foreach (string value in values) {
                sb.Append(sb.Length == 0 ? "?" : "&");
                sb.AppendFormat("{0}={1}", key, value);
            }
        }
        return sb.ToString();
    }
    public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
        if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
        var trueValues = new string[] { true.ToString(), "yes", "1" };
        if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        var falseValues = new string[] { false.ToString(), "no", "0" };
        if (falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        return defaultValue;
    }
    public static string GetString(this NameValueCollection collection, string key) {
        if (!collection.AllKeys.Contains(key)) return collection[key];
        return null;
    }
    public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);
    public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);
    public static T PopAt<T>(this List<T> list, int index) {
        T r = list.ElementAt<T>(index);
        list.RemoveAt(index);
        return r;
    }
    public static readonly Regex QueryRegex = new Regex(@"[?&](\w[\w.]*)=([^?&]+)");
    public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
        var match = QueryRegex.Match(uri.PathAndQuery);
        var paramaters = new Dictionary<string, string>();
        while (match.Success) {
            paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
            match = match.NextMatch();
        }
        return paramaters;
    }
    public static DescriptionAttribute GetEnumDescriptionAttribute<T>(
    this T value) where T : struct
    {
        Type type = typeof(T);
        if (!type.IsEnum)
            throw new InvalidOperationException(
                "The type parameter T must be an enum type.");
        if (!Enum.IsDefined(type, value))
            throw new InvalidEnumArgumentException(
                "value", Convert.ToInt32(value), type);
        FieldInfo fi = type.GetField(value.ToString(),
            BindingFlags.Static | BindingFlags.Public);
        return fi.GetCustomAttributes(typeof(DescriptionAttribute), true).
            Cast<DescriptionAttribute>().SingleOrDefault();
    }
    public static string? GetName(this Type enumType, object value) => Enum.GetName(enumType, value);
    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
        using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  
            } else
            {
                return default(TResult);
            }
        }
    }
    public static string ToYesNo(this bool input) => input ? "Yes" : "No";
    public static string ToEnabledDisabled(this bool input) => input ? "Enabled" : "Disabled";
    public static string ToOnOff(this bool input) => input ? "On" : "Off";
    public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
        return instanceToConvert.GetType()
          .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
          .ToDictionary(
          propertyInfo => propertyInfo.Name,
          propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
    }
    public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
        Type propertyType = propertyInfo.PropertyType;
        object propertyValue = propertyInfo.GetValue(owner);
        if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
            var collectionItems = new List<Dictionary<string, object>>();
            var count = (int)propertyType.GetProperty("Count").GetValue(propertyValue);
            PropertyInfo indexerProperty = propertyType.GetProperty("Item");
            for (var index = 0; index < count; index++) {
                object item = indexerProperty.GetValue(propertyValue, new object[] { index });
                PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (itemProperties.Any()) {
                    Dictionary<string, object> dictionary = itemProperties
                      .ToDictionary(
                        subtypePropertyInfo => subtypePropertyInfo.Name,
                        subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
                    collectionItems.Add(dictionary);
                }
            }
            return collectionItems;
        }
        if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
            return propertyValue;
        }
        PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        if (properties.Any()) {
            return properties.ToDictionary(
                                subtypePropertyInfo => subtypePropertyInfo.Name,
                                subtypePropertyInfo => (object)Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
        }
        return propertyValue;
    }
    public static bool ExpiredSince(this DateTime dateTime, int minutes) {
        return (dateTime - DateTime.Now).TotalMinutes < minutes;
    }
    public static TimeSpan StripMilliseconds(this TimeSpan time) {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
    }
    public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new DirectoryInfo(final);
    }
    public static bool IsEmpty(this DirectoryInfo directory) {
        return !Directory.EnumerateFileSystemEntries(directory.FullName).Any();
    }
    public static string StatusString(this DirectoryInfo directory, bool existsInfo = false) {
        if (directory is null) return " (is null ❌)";
        if (File.Exists(directory.FullName)) return " (is file ❌)";
        if (!directory.Exists) return " (does not exist ❌)";
        if (directory.IsEmpty()) return " (is empty ⚠️)";
        return existsInfo ? " (exists ✅)" : string.Empty;
    }
    public static void Copy(this DirectoryInfo source, DirectoryInfo target, bool overwrite = false) {
        Directory.CreateDirectory(target.FullName);
        foreach (FileInfo fi in source.GetFiles())
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), overwrite);
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            Copy(diSourceSubDir, target.CreateSubdirectory(diSourceSubDir.Name));
    }
    public static bool Backup(this DirectoryInfo directory, bool overwrite = false) {
        if (!directory.Exists) return false;
        var backupDirPath = directory.FullName + ".bak";
        if (Directory.Exists(backupDirPath) && !overwrite) return false;
        Directory.CreateDirectory(backupDirPath);
        foreach (FileInfo fi in directory.GetFiles()) fi.CopyTo(Path.Combine(backupDirPath, fi.Name), overwrite);
        foreach (DirectoryInfo diSourceSubDir in directory.GetDirectories()) {
            diSourceSubDir.Copy(Directory.CreateDirectory(Path.Combine(backupDirPath, diSourceSubDir.Name)), overwrite);
        }
        return true;
    }
    public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static FileInfo Combine(this FileInfo file, params string[] paths) {
        var final = file.DirectoryName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static string FileNameWithoutExtension(this FileInfo file) {
        return Path.GetFileNameWithoutExtension(file.Name);
    }
    public static string StatusString(this FileInfo file, bool existsInfo = false) {
        if (file is null) return "(is null ❌)";
        if (Directory.Exists(file.FullName)) return "(is directory ❌)";
        if (!file.Exists) return "(does not exist ❌)";
        if (file.Length < 1) return "(is empty ⚠️)";
        return existsInfo ? "(exists ✅)" : string.Empty;
    }
    public static void AppendLine(this FileInfo file, string line) {
        try
        {
            if (!file.Exists) file.Create();
            File.AppendAllLines(file.FullName, new string[] { line });
        } catch { }
    }
    public static void WriteAllText(this FileInfo file, string text) => File.WriteAllText(file.FullName, text);
    public static string ReadAllText(this FileInfo file) => File.ReadAllText(file.FullName);
    public static List<string> ReadAllLines(this FileInfo file) => File.ReadAllLines(file.FullName).ToList();
    public static bool Backup(this FileInfo file, bool overwrite = false) {
        if (!file.Exists) return false;
        var backupFilePath = file.FullName + ".bak";
        if (File.Exists(backupFilePath) && !overwrite) return false;
        File.Copy(file.FullName, backupFilePath, overwrite);
        return true;
    }
    public static bool Restore(this FileInfo file, bool overwrite = false) {
        if (!file.Exists || !File.Exists(file.FullName + ".bak")) return false;
        if (overwrite) File.Delete(file.FullName);
        File.Move(file.FullName + ".bak", file.FullName);
        return true;
    }
    public static IEnumerable<string> SplitToLines(this string input) {
        if (input == null) {
            yield break;
        }
        using (System.IO.StringReader reader = new System.IO.StringReader(input)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                yield return line;
            }
        }
    }
    public static string ToTitleCase(this string source, string langCode = "en-US") {
        return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
    }
    public static bool Contains(this string source, string toCheck, StringComparison comp) {
        return source?.IndexOf(toCheck, comp) >= 0;
    }
    public static bool IsNullOrEmpty(this string source) {
        return string.IsNullOrEmpty(source);
    }
    public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
        if (count != -1) return source.Split(new string[] { split }, count, options);
        return source.Split(new string[] { split }, options);
    }
    public static string Remove(this string Source, string Replace) {
        return Source.Replace(Replace, string.Empty);
    }
    public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
        int place = Source.LastIndexOf(Find);
        if (place == -1)
            return Source;
        string result = Source.Remove(place, Find.Length).Insert(place, Replace);
        return result;
    }
    public static string EscapeLineBreaks(this string source) {
        return Regex.Replace(source, @"\r\n?|\n", @"\$&");
    }
    public static string Ext(this string text, string extension) {
        return text + "." + extension;
    }
    public static string Quote(this string text) {
        return SurroundWith(text, "\"");
    }
    public static string Enclose(this string text) {
        return SurroundWith(text, "(", ")");
    }
    public static string Brackets(this string text) {
        return SurroundWith(text, "[", "]");
    }
    public static string SurroundWith(this string text, string surrounds) {
        return surrounds + text + surrounds;
    }
    public static string SurroundWith(this string text, string starts, string ends) {
        return starts + text + ends;
    }
    public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
        if (!dictionary.ContainsKey(key))
            dictionary.Add(key, value);
    }
    public static Dictionary<string, object?> MergeWith(this Dictionary<string, object?> sourceDict, Dictionary<string, object?> destDict) {
        foreach (var kvp in sourceDict) {
            if (destDict.ContainsKey(kvp.Key)) {
                Console.WriteLine($"Key '{kvp.Key}' already exists and will be overwritten.");
            }
            destDict[kvp.Key] = kvp.Value;
        }
        return destDict;
    }
    public static Dictionary<string, object> MergeRecursiveWith(this Dictionary<string, object> sourceDict, Dictionary<string, object> targetDict) {
        foreach (var kvp in sourceDict) {
            if (targetDict.TryGetValue(kvp.Key, out var existingValue)) {
                if (existingValue is Dictionary<string, object> existingDict && kvp.Value is Dictionary<string, object> sourceDictValue) {
                    sourceDictValue.MergeRecursiveWith(existingDict);
                } else if (kvp.Value is null) {
                    targetDict.Remove(kvp.Key);
                    Console.WriteLine($"Removed key '{kvp.Key}' as it was set to null in the source dictionary.");
                } else
                {
                    targetDict[kvp.Key] = kvp.Value;
                    Console.WriteLine($"Overwriting existing value for key '{kvp.Key}'.");
                }
            } else
            {
                targetDict[kvp.Key] = kvp.Value;
            }
        }
        return targetDict;
    }
    public static string ToQueryString(this NameValueCollection nvc) {
        if (nvc == null) return string.Empty;
        StringBuilder sb = new StringBuilder();
        foreach (string key in nvc.Keys) {
            if (string.IsNullOrWhiteSpace(key)) continue;
            string[] values = nvc.GetValues(key);
            if (values == null) continue;
            foreach (string value in values) {
                sb.Append(sb.Length == 0 ? "?" : "&");
                sb.AppendFormat("{0}={1}", key, value);
            }
        }
        return sb.ToString();
    }
    public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
        if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
        var trueValues = new string[] { true.ToString(), "yes", "1" };
        if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        var falseValues = new string[] { false.ToString(), "no", "0" };
        if (falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        return defaultValue;
    }
    public static string GetString(this NameValueCollection collection, string key) {
        if (!collection.AllKeys.Contains(key)) return collection[key];
        return null;
    }
    public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);
    public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);
    public static T PopAt<T>(this List<T> list, int index) {
        T r = list.ElementAt<T>(index);
        list.RemoveAt(index);
        return r;
    }
    public static readonly Regex QueryRegex = new Regex(@"[?&](\w[\w.]*)=([^?&]+)");
    public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
        var match = QueryRegex.Match(uri.PathAndQuery);
        var paramaters = new Dictionary<string, string>();
        while (match.Success) {
            paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
            match = match.NextMatch();
        }
        return paramaters;
    }
    public static void OpenInDefaultBrowser(this Uri uri) {
        Process.Start(new ProcessStartInfo
        {
            FileName = uri.AbsoluteUri,
            UseShellExecute = true
        });
    }
    public static DescriptionAttribute GetEnumDescriptionAttribute<T>(
    this T value) where T : struct
    {
        Type type = typeof(T);
        if (!type.IsEnum)
            throw new InvalidOperationException(
                "The type parameter T must be an enum type.");
        if (!Enum.IsDefined(type, value))
            throw new InvalidEnumArgumentException(
                "value", Convert.ToInt32(value), type);
        FieldInfo fi = type.GetField(value.ToString(),
            BindingFlags.Static | BindingFlags.Public);
        return fi.GetCustomAttributes(typeof(DescriptionAttribute), true).
            Cast<DescriptionAttribute>().SingleOrDefault();
    }
    public static string? GetName(this Type enumType, object value) => Enum.GetName(enumType, value);
    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
        using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  
            } else
            {
                return default(TResult);
            }
        }
    }
    public static string ToYesNo(this bool input) => input ? "Yes" : "No";
    public static string ToEnabledDisabled(this bool input) => input ? "Enabled" : "Disabled";
    public static string ToOnOff(this bool input) => input ? "On" : "Off";
    public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
        if (dir == null) throw new ArgumentNullException(nameof(dir));
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path ?? string.Empty);
        }
        return new DirectoryInfo(final);
    }
    public static bool IsEmpty(this DirectoryInfo directory) {
        if (directory == null) throw new ArgumentNullException(nameof(directory));
        return !Directory.EnumerateFileSystemEntries(directory.FullName).Any();
    }
    public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
        if (dir == null) throw new ArgumentNullException(nameof(dir));
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path ?? string.Empty);
        }
        return new FileInfo(final);
    }
    public static FileInfo Combine(this FileInfo file, params string[] paths) {
        if (file == null) throw new ArgumentNullException(nameof(file));
        var final = file.DirectoryName ?? string.Empty;
        foreach (var path in paths) {
            final = Path.Combine(final, path ?? string.Empty);
        }
        return new FileInfo(final);
    }
    public static string FileNameWithoutExtension(this FileInfo file) {
        if (file == null) throw new ArgumentNullException(nameof(file));
        return Path.GetFileNameWithoutExtension(file.Name);
    }
    public static bool WriteAllText(this FileInfo file, string content) {
        if (file == null) throw new ArgumentNullException(nameof(file));
        if (file.Directory != null && !file.Directory.Exists) file.Directory.Create();
        try
        {
            File.WriteAllText(file.FullName, content);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error writing to file {file.FullName}: {ex.Message}");
            return false;
        }
        return true;
    }
    public static string ReadAllText(this FileInfo file) {
        if (file == null) throw new ArgumentNullException(nameof(file));
        if (file.Directory != null && !file.Directory.Exists) file.Directory.Create();
        if (!file.Exists) return string.Empty;
        return File.ReadAllText(file.FullName);
    }
    public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
        if (dir == null) throw new ArgumentNullException(nameof(dir));
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path ?? string.Empty);
        }
        return new DirectoryInfo(final);
    }
    public static bool IsEmpty(this DirectoryInfo directory) {
        if (directory == null) throw new ArgumentNullException(nameof(directory));
        return !Directory.EnumerateFileSystemEntries(directory.FullName).Any();
    }
    public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
        if (dir == null) throw new ArgumentNullException(nameof(dir));
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path ?? string.Empty);
        }
        return new FileInfo(final);
    }
    public static FileInfo Combine(this FileInfo file, params string[] paths) {
        if (file == null) throw new ArgumentNullException(nameof(file));
        var final = file.DirectoryName ?? string.Empty;
        foreach (var path in paths) {
            final = Path.Combine(final, path ?? string.Empty);
        }
        return new FileInfo(final);
    }
    public static string FileNameWithoutExtension(this FileInfo file) {
        if (file == null) throw new ArgumentNullException(nameof(file));
        return Path.GetFileNameWithoutExtension(file.Name);
    }
    public static bool WriteAllText(this FileInfo file, string content) {
        if (file == null) throw new ArgumentNullException(nameof(file));
        if (file.Directory != null && !file.Directory.Exists) file.Directory.Create();
        try
        {
            File.WriteAllText(file.FullName, content);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error writing to file {file.FullName}: {ex.Message}");
            return false;
        }
        return true;
    }
    public static string ReadAllText(this FileInfo file) {
        if (file == null) throw new ArgumentNullException(nameof(file));
        if (file.Directory != null && !file.Directory.Exists) file.Directory.Create();
        if (!file.Exists) return string.Empty;
        return File.ReadAllText(file.FullName);
    }
    public static string toVdf(BindingList<Mount> Mounts) {
        var mountcfg = new Dictionary<string, string>();
        foreach (var mount in Mounts) {
            mountcfg.Add(mount.Name, mount.Path.FullName);
        }
        return VdfConvert.Serialize(new VProperty("mountcfg", JToken.FromObject(mountcfg).ToVdf()), new VdfSerializerSettings { UsesEscapeSequences = false });
    }
    public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
        return instanceToConvert.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .ToDictionary(
            propertyInfo => propertyInfo.Name,
            propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
    }
    public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
        Type propertyType = propertyInfo.PropertyType;
        object propertyValue = propertyInfo.GetValue(owner);
        if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
            var collectionItems = new List<Dictionary<string, object>>();
            var count = (int)propertyType.GetProperty("Count").GetValue(propertyValue);
            PropertyInfo indexerProperty = propertyType.GetProperty("Item");
            for (var index = 0; index < count; index++) {
                object item = indexerProperty.GetValue(propertyValue, new object[] { index });
                PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (itemProperties.Any()) {
                    Dictionary<string, object> dictionary = itemProperties
                        .ToDictionary(
                        subtypePropertyInfo => subtypePropertyInfo.Name,
                        subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
                    collectionItems.Add(dictionary);
                }
            }
            return collectionItems;
        }
        if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
            return propertyValue;
        }
        PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        if (properties.Any()) {
            return properties.ToDictionary(
                                subtypePropertyInfo => subtypePropertyInfo.Name,
                                subtypePropertyInfo => (object)Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
        }
        return propertyValue;
    }
    public static bool ExpiredSince(this DateTime dateTime, int minutes) {
        return (dateTime - DateTime.Now).TotalMinutes < minutes;
    }
    public static TimeSpan StripMilliseconds(this TimeSpan time) {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
    }
    public static DirectoryInfo Combine(this Environment.SpecialFolder specialFolder, params string[] paths) => Combine(new DirectoryInfo(Environment.GetFolderPath(specialFolder)), paths);
    public static FileInfo CombineFile(this Environment.SpecialFolder specialFolder, params string[] paths) => CombineFile(new DirectoryInfo(Environment.GetFolderPath(specialFolder)), paths);
    public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path.ReplaceInvalidFileNameChars());
        }
        return new DirectoryInfo(final);
    }
    public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static void ShowInExplorer(this DirectoryInfo dir) {
        Utils.StartProcess("explorer.exe", null, dir.FullName.Quote());
    }
    public static string PrintablePath(this FileSystemInfo file) => file.FullName.Replace(@"\\", @"\");
    public static FileInfo Backup(this FileInfo file, bool overwrite = true, string extension = ".bak") {
        return file.CopyTo(file.FullName + extension, overwrite);
    }
    public static FileInfo Combine(this FileInfo file, params string[] paths) {
        var final = file.DirectoryName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static string FileNameWithoutExtension(this FileInfo file) {
        return Path.GetFileNameWithoutExtension(file.Name);
    }
    public static void AppendLine(this FileInfo file, string line) {
        try
        {
            if (!file.Exists) file.Create();
            File.AppendAllLines(file.FullName, new string[] { line });
        }
        catch { }
    }
    public static void WriteAllText(this FileInfo file, string text) {
        file.Directory.Create();
        if (!file.Exists) file.Create().Close();
        File.WriteAllText(file.FullName, text);
    }
    public static string ReadAllText(this FileInfo file) => File.ReadAllText(file.FullName);
    public static List<string> ReadAllLines(this FileInfo file) => File.ReadAllLines(file.FullName).ToList();
    public static void ShowInExplorer(this FileInfo file) {
        Utils.StartProcess("explorer.exe", null, "/select, " + file.FullName.Quote());
    }
    public static IEnumerable<TreeNode> GetAllChilds(this TreeNodeCollection nodes) {
        foreach (TreeNode node in nodes) {
            yield return node;
            foreach (var child in GetAllChilds(node.Nodes))
                yield return child;
        }
    }
    public static void StretchLastColumn(this DataGridView dataGridView) {
        dataGridView.AutoResizeColumns();
        var lastColIndex = dataGridView.Columns.Count - 1;
        var lastCol = dataGridView.Columns[lastColIndex];
        lastCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
    }
    public static string ToJSON(this object obj, bool indented = true) {
        return JsonConvert.SerializeObject(obj, (indented ? Formatting.Indented : Formatting.None), new JsonConverter[] { new StringEnumConverter(), new IPAddressConverter(), new IPEndPointConverter() });
    }
    public static string GetDigits(this string input) {
        return new string(input.Where(char.IsDigit).ToArray());
    }
    public static string Format(this string input, params string[] args) {
        return string.Format(input, args);
    }
    public static IEnumerable<string> SplitToLines(this string input) {
        if (input == null) {
            yield break;
        }
        using (System.IO.StringReader reader = new System.IO.StringReader(input)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                yield return line;
            }
        }
    }
    public static string ToTitleCase(this string source, string langCode = "en-US") {
        return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
    }
    public static bool Contains(this string source, string toCheck, StringComparison comp) {
        return source?.IndexOf(toCheck, comp) >= 0;
    }
    public static bool IsNullOrEmpty(this string source) {
        return string.IsNullOrEmpty(source);
    }
    public static bool IsNullOrWhiteSpace(this string source) {
        return string.IsNullOrWhiteSpace(source);
    }
    public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
        if (count != -1) return source.Split(new string[] { split }, count, options);
        return source.Split(new string[] { split }, options);
    }
    public static string Remove(this string Source, string Replace) {
        return Source.Replace(Replace, string.Empty);
    }
    public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
        int place = Source.LastIndexOf(Find);
        if (place == -1)
            return Source;
        string result = Source.Remove(place, Find.Length).Insert(place, Replace);
        return result;
    }
    public static string EscapeLineBreaks(this string source) {
        return Regex.Replace(source, @"\r\n?|\n", @"\$&");
    }
    public static string Ext(this string text, string extension) {
        return text + "." + extension;
    }
    public static string Quote(this string text) {
        return SurroundWith(text, "\"");
    }
    public static string Enclose(this string text) {
        return SurroundWith(text, "(", ")");
    }
    public static string Brackets(this string text) {
        return SurroundWith(text, "[", "]");
    }
    public static string SurroundWith(this string text, string surrounds) {
        return surrounds + text + surrounds;
    }
    public static string SurroundWith(this string text, string starts, string ends) {
        return starts + text + ends;
    }
    public static string RemoveInvalidFileNameChars(this string filename) {
        return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
    }
    public static string ReplaceInvalidFileNameChars(this string filename) {
        return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
    }
    public static int Percentage(this int total, int part) {
        return (int)((double)part / total * 100);
    }
    public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
        if (!dictionary.ContainsKey(key))
            dictionary.Add(key, value);
    }
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self) => self.Select((item, index) => (item, index));
    public static string ToQueryString(this NameValueCollection nvc) {
        if (nvc == null) return string.Empty;
        StringBuilder sb = new StringBuilder();
        foreach (string key in nvc.Keys) {
            if (string.IsNullOrWhiteSpace(key)) continue;
            string[] values = nvc.GetValues(key);
            if (values == null) continue;
            foreach (string value in values) {
                sb.Append(sb.Length == 0 ? "?" : "&");
                sb.AppendFormat("{0}={1}", key, value);
            }
        }
        return sb.ToString();
    }
    public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
        if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
        var trueValues = new string[] { true.ToString(), "yes", "1" };
        if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        var falseValues = new string[] { false.ToString(), "no", "0" };
        if (falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        return defaultValue;
    }
    public static string GetString(this NameValueCollection collection, string key) {
        if (!collection.AllKeys.Contains(key)) return collection[key];
        return null;
    }
    public static string Join(this List<string> strings, string separator) {
        return string.Join(separator, strings);
    }
    public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);
    public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);
    public static T PopAt<T>(this List<T> list, int index) {
        T r = list.ElementAt<T>(index);
        list.RemoveAt(index);
        return r;
    }
    public static bool ContainsKey(this NameValueCollection collection, string key) {
        if (collection.Get(key) == null) {
            return collection.AllKeys.Contains(key);
        }
        return true;
    }
    public static NameValueCollection ParseQueryString(this Uri uri) {
        return HttpUtility.ParseQueryString(uri.Query);
    }
    public static void OpenIn(this Uri uri, string browser) {
        var url = uri.ToString();
        try
        {
            Process.Start(browser, url);
        }
        catch
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start \"{browser}\" {url}") { CreateNoWindow = true });
        }
    }
    public static void OpenInDefault(this Uri uri) {
        var url = uri.ToString();
        try
        {
            Process.Start(url);
        }
        catch
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }
    public static string GetDescription(this Enum value) {
        Type type = value.GetType();
        string name = Enum.GetName(type, value);
        if (name != null) {
            FieldInfo field = type.GetField(name);
            if (field != null) {
                DescriptionAttribute attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attr != null) {
                    return attr.Description;
                }
            }
        }
        return null;
    }
    public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
        var type = typeof(T);
        if (!type.IsEnum) throw new InvalidOperationException();
        foreach (var field in type.GetFields()) {
            var attribute = Attribute.GetCustomAttribute(field,
                typeof(DescriptionAttribute)) as DescriptionAttribute;
            if (attribute != null) {
                if (attribute.Description == description)
                    return (T)field.GetValue(null);
            }
            else
            {
                if (field.Name == description)
                    return (T)field.GetValue(null);
            }
        }
        if (returnDefault) return default(T);
        else throw new ArgumentException("Not found.", "description");
    }
    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
        using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  
            }
            else
            {
                return default(TResult);
            }
        }
    }
    public static Bitmap Resize(this Image image, int width, int height) {
        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height);
        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
        using (var graphics = Graphics.FromImage(destImage)) {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using (var wrapMode = new ImageAttributes()) {
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }
        return destImage;
    }
    public static Color Invert(this Color input) {
        return Color.FromArgb(input.ToArgb() ^ 0xffffff);
    }
    public static string ToYesNo(this bool input) => input ? "Yes" : "No";
    public static string ToEnabledDisabled(this bool input) => input ? "Enabled" : "Disabled";
    public static string ToOnOff(this bool input) => input ? "On" : "Off";
    public static T PickRandom<T>(this IList<T> source) {
        int randIndex = rnd.Next(source.Count);
        return source[randIndex];
    }
    public static string ToString(this IEnumerable<float>? floats) {
        return floats != null ? string.Join(", ", floats) : string.Empty;
    }
    public static string ToString(this IEnumerable<string>? strings) {
        return strings != null ? string.Join(", ", strings) : string.Empty;
    }
    public static Vector3? ToVector3(this IList<float> floats) {
        var cnt = floats.Count();
        if (cnt != 3) {
            return null;
        }
        return new Vector3(floats[0], floats[1], floats[2]);
    }
    public static Quaternion? ToQuaternion(this IList<float> floats) {
        var cnt = floats.Count();
        switch (cnt) {
            case 3: return new Quaternion() { x = floats[0], y = floats[1], z = floats[2] };
            case 4: return new Quaternion() { x = floats[0], y = floats[1], z = floats[2], w = floats[3] };
            default: Utils.Warn($"Tried to convert list with {cnt} floats to Quaternion!"); return null;
        }
    }
    public static List<float> ToList(this Vector3 vec) {
        return vec != null ? new List<float>() { vec.x, vec.y, vec.z } : new List<float>();
    }
    public static List<float> ToList(this Quaternion quat) {
        return quat != null ? new List<float>() { quat.x, quat.y, quat.z, quat.w } : new List<float>();
    }
    public static string ToString(this Vector3 v) {
        return $"X:{v.x} Y:{v.y} Z:{v.z}";
    }
    public static string ToString(this Quaternion q) {
        return $"X:{q.x} Y:{q.y} Z:{q.z} W:{q.w}";
    }
    public static string str(this FileInfo file) {
        return "\"" + file.FullName + "\"";
    }
    public static string str(this DirectoryInfo directory) {
        return "\"" + directory.FullName + "\"";
    }
    public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
        return instanceToConvert.GetType()
          .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
          .ToDictionary(
          propertyInfo => propertyInfo.Name,
          propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
    }
    public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
        Type propertyType = propertyInfo.PropertyType;
        object propertyValue = propertyInfo.GetValue(owner);
        if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(System.Collections.ObjectModel.Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
            var collectionItems = new List<Dictionary<string, object>>();
            var count = (int)propertyType.GetProperty("Count").GetValue(propertyValue);
            PropertyInfo indexerProperty = propertyType.GetProperty("Item");
            for (var index = 0; index < count; index++) {
                object item = indexerProperty.GetValue(propertyValue, new object[] { index });
                PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (itemProperties.Any()) {
                    Dictionary<string, object> dictionary = itemProperties
                      .ToDictionary(
                        subtypePropertyInfo => subtypePropertyInfo.Name,
                        subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
                    collectionItems.Add(dictionary);
                }
            }
            return collectionItems;
        }
        if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
            return propertyValue;
        }
        PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        if (properties.Any()) {
            return properties.ToDictionary(
                                subtypePropertyInfo => subtypePropertyInfo.Name,
                                subtypePropertyInfo => (object)Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
        }
        return propertyValue;
    }
    public static bool ExpiredSince(this DateTime dateTime, int minutes) {
        return (dateTime - DateTime.Now).TotalMinutes < minutes;
    }
    public static TimeSpan StripMilliseconds(this TimeSpan time) {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
    }
    public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new DirectoryInfo(final);
    }
    public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static FileInfo Combine(this FileInfo file, params string[] paths) {
        var final = file.DirectoryName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static string FileNameWithoutExtension(this FileInfo file) {
        return Path.GetFileNameWithoutExtension(file.Name);
    }
    public static string Extension(this FileInfo file) {
        return Path.GetExtension(file.Name);
    }
    public static void AppendLine(this FileInfo file, string line) {
        try {
            if (!file.Exists) file.Create();
            File.AppendAllLines(file.FullName, new string[] { line });
        } catch { }
    }
    public static void WriteAllText(this FileInfo file, string text) => File.WriteAllText(file.FullName, text);
    public static string ReadAllText(this FileInfo file) => File.ReadAllText(file.FullName);
    public static List<string> ReadAllLines(this FileInfo file) => File.ReadAllLines(file.FullName).ToList();
    public static string ToJson(this object obj, bool indented = true) {
        return JsonConvert.SerializeObject(obj, (indented ? Formatting.Indented : Formatting.None), [new StringEnumConverter(), new IPAddressConverter(), new IPEndPointConverter()]);
    }
    public static string ToMd5(this string input) {
        using (MD5 md5 = MD5.Create()) {
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++) {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
    public static UnityEngine.Vector3? ParseVector3(this string source) {
        var split = source.Split(",");
        switch (split.Length) {
            case 3:
                return new UnityEngine.Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
            case 2:
                return new UnityEngine.Vector3(float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.position.y, float.Parse(split[2]));
            case 1:
                return new UnityEngine.Vector3(PlayerSetup.Instance.gameObject.transform.position.x, float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.position.z);
        }
        return null;
    }
    public static UnityEngine.Quaternion? ParseQuaternion(this string source) {
        var split = source.Split(",");
        switch (split.Length) {
            case 4:
                return new UnityEngine.Quaternion(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
            case 3:
                return new UnityEngine.Quaternion(float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.rotation.y, float.Parse(split[1]), float.Parse(split[2]));
            case 2:
                return new UnityEngine.Quaternion(PlayerSetup.Instance.gameObject.transform.rotation.x, float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.rotation.z, float.Parse(split[1]));
            case 1:
                return new UnityEngine.Quaternion(PlayerSetup.Instance.gameObject.transform.rotation.x, float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.rotation.z, PlayerSetup.Instance.gameObject.transform.rotation.w);
        }
        return null;
    }
    public static IEnumerable<string> SplitToLines(this string input) {
        if (input == null) {
            yield break;
        }
        using (System.IO.StringReader reader = new System.IO.StringReader(input)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                yield return line;
            }
        }
    }
    public static string ToTitleCase(this string source, string langCode = "en-US") {
        return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
    }
    public static bool Contains(this string source, string toCheck, StringComparison comp) {
        return source?.IndexOf(toCheck, comp) >= 0;
    }
    public static bool IsNullOrEmpty(this string source) {
        return string.IsNullOrEmpty(source);
    }
    public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
        if (count != -1) return source.Split(new string[] { split }, count, options);
        return source.Split(new string[] { split }, options);
    }
    public static string Remove(this string Source, string Replace) {
        return Source.Replace(Replace, string.Empty);
    }
    public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
        int place = Source.LastIndexOf(Find);
        if (place == -1)
            return Source;
        string result = Source.Remove(place, Find.Length).Insert(place, Replace);
        return result;
    }
    public static string EscapeLineBreaks(this string source) {
        return Regex.Replace(source, @"\r\n?|\n", @"\$&");
    }
    public static string Ext(this string text, string extension) {
        return text + "." + extension;
    }
    public static string Quote(this string text) {
        return SurroundWith(text, "\"");
    }
    public static string Enclose(this string text) {
        return SurroundWith(text, "(", ")");
    }
    public static string Brackets(this string text) {
        return SurroundWith(text, "[", "]");
    }
    public static string SurroundWith(this string text, string surrounds) {
        return surrounds + text + surrounds;
    }
    public static string SurroundWith(this string text, string starts, string ends) {
        return starts + text + ends;
    }
    public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
        if (!dictionary.ContainsKey(key))
            dictionary.Add(key, value);
    }
    public static string ToQueryString(this NameValueCollection nvc) {
        if (nvc == null) return string.Empty;
        StringBuilder sb = new StringBuilder();
        foreach (string key in nvc.Keys) {
            if (string.IsNullOrWhiteSpace(key)) continue;
            string[] values = nvc.GetValues(key);
            if (values == null) continue;
            foreach (string value in values) {
                sb.Append(sb.Length == 0 ? "?" : "&");
                sb.AppendFormat("{0}={1}", key, value);
            }
        }
        return sb.ToString();
    }
    public static string GetValue(this IDictionary<string, object> dict, string key, string _default = null) {
        dict.TryGetValue(key, out object ret);
        return ret as string ?? _default;
    }
    public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
        if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
        var trueValues = new string[] { true.ToString(), "yes", "1" };
        if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        var falseValues = new string[] { false.ToString(), "no", "0" };
        if (falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        return defaultValue;
    }
    public static string GetString(this NameValueCollection collection, string key) {
        if (!collection.AllKeys.Contains(key)) return collection[key];
        return null;
    }
    public static bool ToBoolean(this string input) {
        var stringTrueValues = new[] { "true", "ok", "yes", "1", "y", "enabled", "on" };
        return stringTrueValues.Contains(input.ToLower());
    }
    public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);
    public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);
    public static T PopAt<T>(this List<T> list, int index) {
        T r = list.ElementAt<T>(index);
        list.RemoveAt(index);
        return r;
    }
    public static readonly Regex QueryRegex = new Regex(@"[?&](\w[\w.]*)=([^?&]+)");
    public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
        var match = QueryRegex.Match(uri.PathAndQuery);
        var paramaters = new Dictionary<string, string>();
        while (match.Success) {
            paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
            match = match.NextMatch();
        }
        return paramaters;
    }
    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
        using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  
            } else {
                return default(TResult);
            }
        }
    }
    public static string ToYesNo(this bool input) => input ? "Yes" : "No";
    public static string ToEnabledDisabled(this bool input) => input ? "Enabled" : "Disabled";
    public static string ToOnOff(this bool input) => input ? "On" : "Off";
    public static T PickRandom<T>(this IList<T> source) {
        int randIndex = rnd.Next(source.Count);
        return source[randIndex];
    }
    public static string ToString(this IEnumerable<float>? floats) {
        return floats != null ? string.Join(", ", floats) : string.Empty;
    }
    public static string ToString(this IEnumerable<string>? strings) {
        return strings != null ? string.Join(", ", strings) : string.Empty;
    }
    public static Vector3? ToVector3(this IList<float> floats) {
        var cnt = floats.Count();
        if (cnt != 3) {
            return null;
        }
        return new Vector3(floats[0], floats[1], floats[2]);
    }
    public static Quaternion? ToQuaternion(this IList<float> floats) {
        var cnt = floats.Count();
        switch (cnt) {
            case 3: return new Quaternion() { x = floats[0], y = floats[1], z = floats[2] };
            case 4: return new Quaternion() { x = floats[0], y = floats[1], z = floats[2], w = floats[3] };
            default: Utils.Warn($"Tried to convert list with {cnt} floats to Quaternion!"); return null;
        }
    }
    public static List<float> ToList(this Vector3 vec) {
        return vec != null ? new List<float>() { vec.x, vec.y, vec.z } : new List<float>();
    }
    public static List<float> ToList(this Quaternion quat) {
        return quat != null ? new List<float>() { quat.x, quat.y, quat.z, quat.w } : new List<float>();
    }
    public static string ToString(this Vector3 v) {
        return $"X:{v.x} Y:{v.y} Z:{v.z}";
    }
    public static string ToString(this Quaternion q) {
        return $"X:{q.x} Y:{q.y} Z:{q.z} W:{q.w}";
    }
    public static string str(this FileInfo file) {
        return "\"" + file.FullName + "\"";
    }
    public static string str(this DirectoryInfo directory) {
        return "\"" + directory.FullName + "\"";
    }
    public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
        return instanceToConvert.GetType()
          .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
          .ToDictionary(
          propertyInfo => propertyInfo.Name,
          propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
    }
    public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
        Type propertyType = propertyInfo.PropertyType;
        object propertyValue = propertyInfo.GetValue(owner);
        if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(System.Collections.ObjectModel.Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
            var collectionItems = new List<Dictionary<string, object>>();
            var count = (int)propertyType.GetProperty("Count").GetValue(propertyValue);
            PropertyInfo indexerProperty = propertyType.GetProperty("Item");
            for (var index = 0; index < count; index++) {
                object item = indexerProperty.GetValue(propertyValue, new object[] { index });
                PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (itemProperties.Any()) {
                    Dictionary<string, object> dictionary = itemProperties
                      .ToDictionary(
                        subtypePropertyInfo => subtypePropertyInfo.Name,
                        subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
                    collectionItems.Add(dictionary);
                }
            }
            return collectionItems;
        }
        if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
            return propertyValue;
        }
        PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        if (properties.Any()) {
            return properties.ToDictionary(
                                subtypePropertyInfo => subtypePropertyInfo.Name,
                                subtypePropertyInfo => (object)Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
        }
        return propertyValue;
    }
    public static bool ExpiredSince(this DateTime dateTime, int minutes) {
        return (dateTime - DateTime.Now).TotalMinutes < minutes;
    }
    public static TimeSpan StripMilliseconds(this TimeSpan time) {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
    }
    public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new DirectoryInfo(final);
    }
    public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static FileInfo Combine(this FileInfo file, params string[] paths) {
        var final = file.DirectoryName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static string FileNameWithoutExtension(this FileInfo file) {
        return Path.GetFileNameWithoutExtension(file.Name);
    }
    public static string Extension(this FileInfo file) {
        return Path.GetExtension(file.Name);
    }
    public static void AppendLine(this FileInfo file, string line) {
        try {
            if (!file.Exists) file.Create();
            File.AppendAllLines(file.FullName, new string[] { line });
        } catch { }
    }
    public static void WriteAllText(this FileInfo file, string text) => File.WriteAllText(file.FullName, text);
    public static string ReadAllText(this FileInfo file) => File.ReadAllText(file.FullName);
    public static List<string> ReadAllLines(this FileInfo file) => File.ReadAllLines(file.FullName).ToList();
    public static string ToJson(this object obj, bool indented = true) {
        return JsonConvert.SerializeObject(obj, (indented ? Formatting.Indented : Formatting.None), [new StringEnumConverter(), new IPAddressConverter(), new IPEndPointConverter()]);
    }
    public static string ToMd5(this string input) {
        using (MD5 md5 = MD5.Create()) {
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++) {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
    public static UnityEngine.Vector3? ParseVector3(this string source) {
        var split = source.Split(",");
        switch (split.Length) {
            case 3:
                return new UnityEngine.Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
            case 2:
                return new UnityEngine.Vector3(float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.position.y, float.Parse(split[2]));
            case 1:
                return new UnityEngine.Vector3(PlayerSetup.Instance.gameObject.transform.position.x, float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.position.z);
        }
        return null;
    }
    public static UnityEngine.Quaternion? ParseQuaternion(this string source) {
        var split = source.Split(",");
        switch (split.Length) {
            case 4:
                return new UnityEngine.Quaternion(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
            case 3:
                return new UnityEngine.Quaternion(float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.rotation.y, float.Parse(split[1]), float.Parse(split[2]));
            case 2:
                return new UnityEngine.Quaternion(PlayerSetup.Instance.gameObject.transform.rotation.x, float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.rotation.z, float.Parse(split[1]));
            case 1:
                return new UnityEngine.Quaternion(PlayerSetup.Instance.gameObject.transform.rotation.x, float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.rotation.z, PlayerSetup.Instance.gameObject.transform.rotation.w);
        }
        return null;
    }
    public static IEnumerable<string> SplitToLines(this string input) {
        if (input == null) {
            yield break;
        }
        using (System.IO.StringReader reader = new System.IO.StringReader(input)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                yield return line;
            }
        }
    }
    public static string ToTitleCase(this string source, string langCode = "en-US") {
        return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
    }
    public static bool Contains(this string source, string toCheck, StringComparison comp) {
        return source?.IndexOf(toCheck, comp) >= 0;
    }
    public static bool IsNullOrEmpty(this string source) {
        return string.IsNullOrEmpty(source);
    }
    public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
        if (count != -1) return source.Split(new string[] { split }, count, options);
        return source.Split(new string[] { split }, options);
    }
    public static string Remove(this string Source, string Replace) {
        return Source.Replace(Replace, string.Empty);
    }
    public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
        int place = Source.LastIndexOf(Find);
        if (place == -1)
            return Source;
        string result = Source.Remove(place, Find.Length).Insert(place, Replace);
        return result;
    }
    public static string EscapeLineBreaks(this string source) {
        return Regex.Replace(source, @"\r\n?|\n", @"\$&");
    }
    public static string Ext(this string text, string extension) {
        return text + "." + extension;
    }
    public static string Quote(this string text) {
        return SurroundWith(text, "\"");
    }
    public static string Enclose(this string text) {
        return SurroundWith(text, "(", ")");
    }
    public static string Brackets(this string text) {
        return SurroundWith(text, "[", "]");
    }
    public static string SurroundWith(this string text, string surrounds) {
        return surrounds + text + surrounds;
    }
    public static string SurroundWith(this string text, string starts, string ends) {
        return starts + text + ends;
    }
    public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
        if (!dictionary.ContainsKey(key))
            dictionary.Add(key, value);
    }
    public static string ToQueryString(this NameValueCollection nvc) {
        if (nvc == null) return string.Empty;
        StringBuilder sb = new StringBuilder();
        foreach (string key in nvc.Keys) {
            if (string.IsNullOrWhiteSpace(key)) continue;
            string[] values = nvc.GetValues(key);
            if (values == null) continue;
            foreach (string value in values) {
                sb.Append(sb.Length == 0 ? "?" : "&");
                sb.AppendFormat("{0}={1}", key, value);
            }
        }
        return sb.ToString();
    }
    public static string GetValue(this IDictionary<string, object> dict, string key, string _default = null) {
        dict.TryGetValue(key, out object ret);
        return ret as string ?? _default;
    }
    public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
        if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
        var trueValues = new string[] { true.ToString(), "yes", "1" };
        if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        var falseValues = new string[] { false.ToString(), "no", "0" };
        if (falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        return defaultValue;
    }
    public static string GetString(this NameValueCollection collection, string key) {
        if (!collection.AllKeys.Contains(key)) return collection[key];
        return null;
    }
    public static bool ToBoolean(this string input) {
        var stringTrueValues = new[] { "true", "ok", "yes", "1", "y", "enabled", "on" };
        return stringTrueValues.Contains(input.ToLower());
    }
    public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);
    public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);
    public static T PopAt<T>(this List<T> list, int index) {
        T r = list.ElementAt<T>(index);
        list.RemoveAt(index);
        return r;
    }
    public static readonly Regex QueryRegex = new Regex(@"[?&](\w[\w.]*)=([^?&]+)");
    public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
        var match = QueryRegex.Match(uri.PathAndQuery);
        var paramaters = new Dictionary<string, string>();
        while (match.Success) {
            paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
            match = match.NextMatch();
        }
        return paramaters;
    }
    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
        using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  
            } else {
                return default(TResult);
            }
        }
    }
    public static string ToYesNo(this bool input) => input ? "Yes" : "No";
    public static string ToEnabledDisabled(this bool input) => input ? "Enabled" : "Disabled";
    public static string ToOnOff(this bool input) => input ? "On" : "Off";
    public static T PickRandom<T>(this IList<T> source) {
        int randIndex = rnd.Next(source.Count);
        return source[randIndex];
    }
    public static string ToString(this IEnumerable<float>? floats) {
        return floats != null ? string.Join(", ", floats) : string.Empty;
    }
    public static string ToString(this IEnumerable<string>? strings) {
        return strings != null ? string.Join(", ", strings) : string.Empty;
    }
    public static Vector3? ToVector3(this IList<float> floats) {
        var cnt = floats.Count();
        if (cnt != 3) {
            return null;
        }
        return new Vector3(floats[0], floats[1], floats[2]);
    }
    public static Quaternion? ToQuaternion(this IList<float> floats) {
        var cnt = floats.Count();
        switch (cnt) {
            case 3: return new Quaternion() { x = floats[0], y = floats[1], z = floats[2] };
            case 4: return new Quaternion() { x = floats[0], y = floats[1], z = floats[2], w = floats[3] };
            default: Utils.Warn($"Tried to convert list with {cnt} floats to Quaternion!"); return null;
        }
    }
    public static List<float> ToList(this Vector3 vec) {
        return vec != null ? new List<float>() { vec.x, vec.y, vec.z } : new List<float>();
    }
    public static List<float> ToList(this Quaternion quat) {
        return quat != null ? new List<float>() { quat.x, quat.y, quat.z, quat.w } : new List<float>();
    }
    public static string ToString(this Vector3 v) {
        return $"X:{v.x} Y:{v.y} Z:{v.z}";
    }
    public static string ToString(this Quaternion q) {
        return $"X:{q.x} Y:{q.y} Z:{q.z} W:{q.w}";
    }
    public static string str(this FileInfo file) {
        return "\"" + file.FullName + "\"";
    }
    public static string str(this DirectoryInfo directory) {
        return "\"" + directory.FullName + "\"";
    }
    public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
        return instanceToConvert.GetType()
          .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
          .ToDictionary(
          propertyInfo => propertyInfo.Name,
          propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
    }
    public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
        Type propertyType = propertyInfo.PropertyType;
        object propertyValue = propertyInfo.GetValue(owner);
        if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(System.Collections.ObjectModel.Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
            var collectionItems = new List<Dictionary<string, object>>();
            var count = (int)propertyType.GetProperty("Count").GetValue(propertyValue);
            PropertyInfo indexerProperty = propertyType.GetProperty("Item");
            for (var index = 0; index < count; index++) {
                object item = indexerProperty.GetValue(propertyValue, new object[] { index });
                PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (itemProperties.Any()) {
                    Dictionary<string, object> dictionary = itemProperties
                      .ToDictionary(
                        subtypePropertyInfo => subtypePropertyInfo.Name,
                        subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
                    collectionItems.Add(dictionary);
                }
            }
            return collectionItems;
        }
        if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
            return propertyValue;
        }
        PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        if (properties.Any()) {
            return properties.ToDictionary(
                                subtypePropertyInfo => subtypePropertyInfo.Name,
                                subtypePropertyInfo => (object)Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
        }
        return propertyValue;
    }
    public static bool ExpiredSince(this DateTime dateTime, int minutes) {
        return (dateTime - DateTime.Now).TotalMinutes < minutes;
    }
    public static TimeSpan StripMilliseconds(this TimeSpan time) {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
    }
    public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new DirectoryInfo(final);
    }
    public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static FileInfo Combine(this FileInfo file, params string[] paths) {
        var final = file.DirectoryName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static string FileNameWithoutExtension(this FileInfo file) {
        return Path.GetFileNameWithoutExtension(file.Name);
    }
    public static void AppendLine(this FileInfo file, string line) {
        try {
            if (!file.Exists) file.Create();
            File.AppendAllLines(file.FullName, new string[] { line });
        } catch { }
    }
    public static void WriteAllText(this FileInfo file, string text) => File.WriteAllText(file.FullName, text);
    public static string ReadAllText(this FileInfo file) => File.ReadAllText(file.FullName);
    public static List<string> ReadAllLines(this FileInfo file) => File.ReadAllLines(file.FullName).ToList();
    public static string ToJSON(this object obj, bool indented = true) {
        return JsonConvert.SerializeObject(obj, (indented ? Formatting.Indented : Formatting.None), [new StringEnumConverter(), new IPAddressConverter(), new IPEndPointConverter()]);
    }
    public static string ToMd5(this string input) {
        using (MD5 md5 = MD5.Create()) {
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++) {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
    public static UnityEngine.Vector3? ParseVector3(this string source) {
        var split = source.Split(",");
        switch (split.Length) {
            case 3:
                return new UnityEngine.Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
            case 2:
                return new UnityEngine.Vector3(float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.position.y, float.Parse(split[2]));
            case 1:
                return new UnityEngine.Vector3(PlayerSetup.Instance.gameObject.transform.position.x, float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.position.z);
        }
        return null;
    }
    public static UnityEngine.Quaternion? ParseQuaternion(this string source) {
        var split = source.Split(",");
        switch (split.Length) {
            case 4:
                return new UnityEngine.Quaternion(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
            case 3:
                return new UnityEngine.Quaternion(float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.rotation.y, float.Parse(split[1]), float.Parse(split[2]));
            case 2:
                return new UnityEngine.Quaternion(PlayerSetup.Instance.gameObject.transform.rotation.x, float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.rotation.z, float.Parse(split[1]));
            case 1:
                return new UnityEngine.Quaternion(PlayerSetup.Instance.gameObject.transform.rotation.x, float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.rotation.z, PlayerSetup.Instance.gameObject.transform.rotation.w);
        }
        return null;
    }
    public static IEnumerable<string> SplitToLines(this string input) {
        if (input == null) {
            yield break;
        }
        using (System.IO.StringReader reader = new System.IO.StringReader(input)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                yield return line;
            }
        }
    }
    public static string ToTitleCase(this string source, string langCode = "en-US") {
        return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
    }
    public static bool Contains(this string source, string toCheck, StringComparison comp) {
        return source?.IndexOf(toCheck, comp) >= 0;
    }
    public static bool IsNullOrEmpty(this string source) {
        return string.IsNullOrEmpty(source);
    }
    public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
        if (count != -1) return source.Split(new string[] { split }, count, options);
        return source.Split(new string[] { split }, options);
    }
    public static string Remove(this string Source, string Replace) {
        return Source.Replace(Replace, string.Empty);
    }
    public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
        int place = Source.LastIndexOf(Find);
        if (place == -1)
            return Source;
        string result = Source.Remove(place, Find.Length).Insert(place, Replace);
        return result;
    }
    public static string EscapeLineBreaks(this string source) {
        return Regex.Replace(source, @"\r\n?|\n", @"\$&");
    }
    public static string Ext(this string text, string extension) {
        return text + "." + extension;
    }
    public static string Quote(this string text) {
        return SurroundWith(text, "\"");
    }
    public static string Enclose(this string text) {
        return SurroundWith(text, "(", ")");
    }
    public static string Brackets(this string text) {
        return SurroundWith(text, "[", "]");
    }
    public static string SurroundWith(this string text, string surrounds) {
        return surrounds + text + surrounds;
    }
    public static string SurroundWith(this string text, string starts, string ends) {
        return starts + text + ends;
    }
    public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
        if (!dictionary.ContainsKey(key))
            dictionary.Add(key, value);
    }
    public static string ToQueryString(this NameValueCollection nvc) {
        if (nvc == null) return string.Empty;
        StringBuilder sb = new StringBuilder();
        foreach (string key in nvc.Keys) {
            if (string.IsNullOrWhiteSpace(key)) continue;
            string[] values = nvc.GetValues(key);
            if (values == null) continue;
            foreach (string value in values) {
                sb.Append(sb.Length == 0 ? "?" : "&");
                sb.AppendFormat("{0}={1}", key, value);
            }
        }
        return sb.ToString();
    }
    public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
        if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
        var trueValues = new string[] { true.ToString(), "yes", "1" };
        if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        var falseValues = new string[] { false.ToString(), "no", "0" };
        if (falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        return defaultValue;
    }
    public static string GetString(this NameValueCollection collection, string key) {
        if (!collection.AllKeys.Contains(key)) return collection[key];
        return null;
    }
    public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);
    public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);
    public static T PopAt<T>(this List<T> list, int index) {
        T r = list.ElementAt<T>(index);
        list.RemoveAt(index);
        return r;
    }
    public static readonly Regex QueryRegex = new Regex(@"[?&](\w[\w.]*)=([^?&]+)");
    public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
        var match = QueryRegex.Match(uri.PathAndQuery);
        var paramaters = new Dictionary<string, string>();
        while (match.Success) {
            paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
            match = match.NextMatch();
        }
        return paramaters;
    }
    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
        using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  
            } else {
                return default(TResult);
            }
        }
    }
    public static string ToYesNo(this bool input) => input ? "Yes" : "No";
    public static string ToEnabledDisabled(this bool input) => input ? "Enabled" : "Disabled";
    public static string ToOnOff(this bool input) => input ? "On" : "Off";
    public static bool isZero(this UnityEngine.Vector3 vector) {
        return vector.Equals(new Vector3(0.0f, 0.0f, 0.0f));
    }
    public static bool isZero(this UnityEngine.Quaternion quaternion) {
        return quaternion.Equals(new Quaternion(0.0f, 0.0f, 0.0f, 0.0f));
    }
    public static bool ExpiredSince(this DateTime dateTime, int minutes) {
        return (dateTime - DateTime.Now).TotalMinutes < minutes;
    }
    public static TimeSpan StripMilliseconds(this TimeSpan time) {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
    }
    public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
        string final = dir.FullName;
        foreach (string path in paths) {
            final = Path.Combine(final, path);
        }
        return new DirectoryInfo(final);
    }
    public static FileInfo Combine(this FileInfo file, params string[] paths) {
        string final = file.DirectoryName;
        foreach (string path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static string FileNameWithoutExtension(this FileInfo file) {
        return Path.GetFileNameWithoutExtension(file.Name);
    }
    public static void AppendLine(this FileInfo file, string line) {
        try {
            if (!file.Exists) {
                _ = file.Create();
            }
            File.AppendAllLines(file.FullName, new string[] { line });
        } catch { }
    }
    public static string ReadAllText(this FileInfo file) {
        return File.ReadAllText(file.FullName);
    }
    public static List<string> ReadAllLines(this FileInfo file) {
        return File.ReadAllLines(file.FullName).ToList();
    }
    public static object ToJson(this object obj, bool indented = true) {
        return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None, new JsonConverter[] { new StringEnumConverter() });
    }
    public static UnityEngine.Vector3? ParseVector3(this string source) {
        var split = source.Split(",");
        switch (split.Length) {
            case 3:
                return new UnityEngine.Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
            case 2:
                return new UnityEngine.Vector3(float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.position.y, float.Parse(split[2]));
            case 1:
                return new UnityEngine.Vector3(PlayerSetup.Instance.gameObject.transform.position.x, float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.position.z);
        }
        return null;
    }
    public static UnityEngine.Quaternion? ParseQuaternion(this string source) {
        var split = source.Split(",");
        switch (split.Length) {
            case 4:
                return new UnityEngine.Quaternion(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
            case 3:
                return new UnityEngine.Quaternion(float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.rotation.y, float.Parse(split[1]), float.Parse(split[2]));
            case 2:
                return new UnityEngine.Quaternion(PlayerSetup.Instance.gameObject.transform.rotation.x, float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.rotation.z, float.Parse(split[1]));
            case 1:
                return new UnityEngine.Quaternion(PlayerSetup.Instance.gameObject.transform.rotation.x, float.Parse(split[0]), PlayerSetup.Instance.gameObject.transform.rotation.z, PlayerSetup.Instance.gameObject.transform.rotation.w);
        }
        return null;
    }
    public static bool Contains(this string source, string toCheck, StringComparison comp) {
        return source?.IndexOf(toCheck, comp) >= 0;
    }
    public static bool IsNullOrEmpty(this string source) {
        return string.IsNullOrEmpty(source);
    }
    public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
        return count != -1 ? source.Split(new string[] { split }, count, options) : source.Split(new string[] { split }, options);
    }
    public static string Remove(this string Source, string Replace) {
        return Source.Replace(Replace, string.Empty);
    }
    public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
        int place = Source.LastIndexOf(Find);
        if (place == -1) {
            return Source;
        }
        string result = Source.Remove(place, Find.Length).Insert(place, Replace);
        return result;
    }
    public static string Ext(this string text, string extension) {
        return text + "." + extension;
    }
    public static string Quote(this string text) {
        return SurroundWith(text, "\"");
    }
    public static string Enclose(this string text) {
        return SurroundWith(text, "(", ")");
    }
    public static string Brackets(this string text) {
        return SurroundWith(text, "[", "]");
    }
    public static string SurroundWith(this string text, string surrounds) {
        return surrounds + text + surrounds;
    }
    public static string SurroundWith(this string text, string starts, string ends) {
        return starts + text + ends;
    }
    public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
        if (!dictionary.ContainsKey(key)) {
            dictionary.Add(key, value);
        }
    }
    public static string ToQueryString(this NameValueCollection nvc) {
        if (nvc == null) {
            return string.Empty;
        }
        StringBuilder sb = new();
        foreach (string key in nvc.Keys) {
            if (string.IsNullOrWhiteSpace(key)) {
                continue;
            }
            string[] values = nvc.GetValues(key);
            if (values == null) {
                continue;
            }
            foreach (string value in values) {
                _ = sb.Append(sb.Length == 0 ? "?" : "&");
                _ = sb.AppendFormat("{0}={1}", key, value);
            }
        }
        return sb.ToString();
    }
    public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
        if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) {
            return false;
        }
        string[] trueValues = new string[] { true.ToString(), "yes", "1" };
        if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) {
            return true;
        }
        string[] falseValues = new string[] { false.ToString(), "no", "0" };
        return falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase) || defaultValue;
    }
    public static string GetString(this NameValueCollection collection, string key) {
        return !collection.AllKeys.Contains(key) ? collection[key] : null;
    }
    public static T PopFirst<T>(this IEnumerable<T> list) {
        return list.ToList().PopAt(0);
    }
    public static T PopLast<T>(this IEnumerable<T> list) {
        return list.ToList().PopAt(list.Count() - 1);
    }
    public static T PopAt<T>(this List<T> list, int index) {
        T r = list.ElementAt<T>(index);
        list.RemoveAt(index);
        return r;
    }
    public static readonly Regex QueryRegex = new(@"[?&](\w[\w.]*)=([^?&]+)");
    public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
        Match match = QueryRegex.Match(uri.PathAndQuery);
        Dictionary<string, string> paramaters = new();
        while (match.Success) {
            paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
            match = match.NextMatch();
        }
        return paramaters;
    }
    public static Dictionary<string, string> ParseQueryString(this string queryString) {
        NameValueCollection nvc = HttpUtility.ParseQueryString(queryString);
        return nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
    }
    public static Dictionary<string, string> GetQueryDict(this Uri uri) => uri.Query.ParseQueryString();
    public static bool CVRIsValid(this Uri uri) {
        return uri.Scheme.Equals("cvr", StringComparison.OrdinalIgnoreCase);
    }
    public static bool TryParseCVRUri(this string url, out Classes.CVRUrl cvruri) {
        cvruri = null;
        bool success = Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri uri);
        if (!success) return false;
        if (!uri.CVRIsValid()) return false;
        cvruri = new Classes.CVRUrl(uri);
        return true;
    }
    public static string CVRGetInstance(this Uri uri) => uri.GetQueryValue("id");
    public static string CVRGetPosition(this Uri uri) => uri.GetQueryValue("pos");
    public static string CVRGetRotation(this Uri uri) => uri.GetQueryValue("rot");
    public static string GetQueryValue(this Uri uri, string key) {
        var dict = uri.GetQueryDict();
        if (dict.ContainsKey(key)) {
            return dict[key].Replace("%3A", ":").Replace(" ", "+");
        }
        return null;
    }
    public static string GetDescription(this Enum value) {
        Type type = value.GetType();
        string name = Enum.GetName(type, value);
        if (name != null) {
            FieldInfo field = type.GetField(name);
            if (field != null) {
                if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr) {
                    return attr.Description;
                }
            }
        }
        return null;
    }
    public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
        Type type = typeof(T);
        if (!type.IsEnum) {
            throw new InvalidOperationException();
        }
        foreach (FieldInfo field in type.GetFields()) {
            if (Attribute.GetCustomAttribute(field,
                typeof(DescriptionAttribute)) is DescriptionAttribute attribute) {
                if (attribute.Description == description) {
                    return (T)field.GetValue(null);
                }
            } else {
                if (field.Name == description) {
                    return (T)field.GetValue(null);
                }
            }
        }
        return returnDefault ? default : throw new ArgumentException("Not found.", "description");
    }
    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
        using CancellationTokenSource timeoutCancellationTokenSource = new();
        Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
        if (completedTask == task) {
            timeoutCancellationTokenSource.Cancel();
            return await task;  
        } else {
            return default;
        }
    }
    public static bool ExpiredSince(this DateTime dateTime, int minutes) {
        return (dateTime - DateTime.Now).TotalMinutes < minutes;
    }
    public static TimeSpan StripMilliseconds(this TimeSpan time) {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
    }
    public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
        string final = dir.FullName;
        foreach (string path in paths) {
            final = Path.Combine(final, path);
        }
        return new DirectoryInfo(final);
    }
    public static FileInfo Combine(this FileInfo file, params string[] paths) {
        string final = file.DirectoryName;
        foreach (string path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static string FileNameWithoutExtension(this FileInfo file) {
        return Path.GetFileNameWithoutExtension(file.Name);
    }
    public static void AppendLine(this FileInfo file, string line) {
        try {
            if (!file.Exists) {
                _ = file.Create();
            }
            File.AppendAllLines(file.FullName, new string[] { line });
        } catch { }
    }
    public static object ToJson(this object obj, bool indented = true) {
        return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None, new JsonConverter[] { new StringEnumConverter() });
    }
    public static bool Contains(this string source, string toCheck, StringComparison comp) {
        return source?.IndexOf(toCheck, comp) >= 0;
    }
    public static bool IsNullOrEmpty(this string source) {
        return string.IsNullOrEmpty(source);
    }
    public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
        return count != -1 ? source.Split(new string[] { split }, count, options) : source.Split(new string[] { split }, options);
    }
    public static string Remove(this string Source, string Replace) {
        return Source.Replace(Replace, string.Empty);
    }
    public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
        int place = Source.LastIndexOf(Find);
        if (place == -1) {
            return Source;
        }
        string result = Source.Remove(place, Find.Length).Insert(place, Replace);
        return result;
    }
    public static string Ext(this string text, string extension) {
        return text + "." + extension;
    }
    public static string Quote(this string text) {
        return SurroundWith(text, "\"");
    }
    public static string Enclose(this string text) {
        return SurroundWith(text, "(", ")");
    }
    public static string Brackets(this string text) {
        return SurroundWith(text, "[", "]");
    }
    public static string SurroundWith(this string text, string surrounds) {
        return surrounds + text + surrounds;
    }
    public static string SurroundWith(this string text, string starts, string ends) {
        return starts + text + ends;
    }
    public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
        if (!dictionary.ContainsKey(key)) {
            dictionary.Add(key, value);
        }
    }
    public static string ToQueryString(this NameValueCollection nvc) {
        if (nvc == null) {
            return string.Empty;
        }
        StringBuilder sb = new();
        foreach (string key in nvc.Keys) {
            if (string.IsNullOrWhiteSpace(key)) {
                continue;
            }
            string[] values = nvc.GetValues(key);
            if (values == null) {
                continue;
            }
            foreach (string value in values) {
                _ = sb.Append(sb.Length == 0 ? "?" : "&");
                _ = sb.AppendFormat("{0}={1}", key, value);
            }
        }
        return sb.ToString();
    }
    public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
        if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) {
            return false;
        }
        string[] trueValues = new string[] { true.ToString(), "yes", "1" };
        if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) {
            return true;
        }
        string[] falseValues = new string[] { false.ToString(), "no", "0" };
        return falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase) || defaultValue;
    }
    public static string GetString(this NameValueCollection collection, string key) {
        return !collection.AllKeys.Contains(key) ? collection[key] : null;
    }
    public static T PopFirst<T>(this IEnumerable<T> list) {
        return list.ToList().PopAt(0);
    }
    public static T PopLast<T>(this IEnumerable<T> list) {
        return list.ToList().PopAt(list.Count() - 1);
    }
    public static T PopAt<T>(this List<T> list, int index) {
        T r = list.ElementAt<T>(index);
        list.RemoveAt(index);
        return r;
    }
    public static readonly Regex QueryRegex = new(@"[?&](\w[\w.]*)=([^?&]+)");
    public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
        Match match = QueryRegex.Match(uri.PathAndQuery);
        Dictionary<string, string> paramaters = new();
        while (match.Success) {
            paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
            match = match.NextMatch();
        }
        return paramaters;
    }
    public static string GetDescription(this Enum value) {
        Type type = value.GetType();
        string name = Enum.GetName(type, value);
        if (name != null) {
            FieldInfo field = type.GetField(name);
            if (field != null) {
                if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr) {
                    return attr.Description;
                }
            }
        }
        return null;
    }
    public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
        Type type = typeof(T);
        if (!type.IsEnum) {
            throw new InvalidOperationException();
        }
        foreach (FieldInfo field in type.GetFields()) {
            if (Attribute.GetCustomAttribute(field,
                typeof(DescriptionAttribute)) is DescriptionAttribute attribute) {
                if (attribute.Description == description) {
                    return (T)field.GetValue(null);
                }
            } else {
                if (field.Name == description) {
                    return (T)field.GetValue(null);
                }
            }
        }
        return returnDefault ? default : throw new ArgumentException("Not found.", "description");
    }
    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
        using CancellationTokenSource timeoutCancellationTokenSource = new();
        Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
        if (completedTask == task) {
            timeoutCancellationTokenSource.Cancel();
            return await task;  
        } else {
            return default;
        }
    }
    public static T GetOrAddComponent<T>(this GameObject obj) where T : Behaviour {
        T comp;
        try {
            comp = obj.GetComponent<T>();
            if (comp == null) {
                comp = obj.AddComponent<T>();
            }
        } catch {
            comp = obj.AddComponent<T>();
        }
        return comp;
    }
    public static T GetOrAddComponent<T>(this Transform obj) where T : Behaviour {
        T comp;
        try {
            comp = obj.gameObject.GetComponent<T>();
            if (comp == null) {
                comp = obj.gameObject.AddComponent<T>();
            }
        } catch {
            comp = obj.gameObject.AddComponent<T>();
        }
        return comp;
    }
    public static T[] GetAllInstancesOfCurrentScene<T>(bool includeInactive = false, Func<T, bool> Filter = null) where T : Behaviour {
        GameObject[] AllRootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        T[] GrabbedObjects = AllRootObjects.SelectMany(o => o.GetComponentsInChildren<T>(includeInactive)).ToArray();
        if (Filter != null) {
            GrabbedObjects = GrabbedObjects.Where(Filter).ToArray();
        }
        return GrabbedObjects;
    }
    [Obsolete]
    public static T[] GetAllInstancesOfAllScenes<T>(bool includeInactive = false, Func<T, bool> Filter = null) where T : Behaviour {
        IEnumerable<GameObject> AllRootObjects = SceneManager.GetAllScenes().SelectMany(o => o.GetRootGameObjects());
        T[] GrabbedObjects = AllRootObjects.SelectMany(o => o.GetComponentsInChildren<T>(includeInactive)).ToArray();
        if (Filter != null) {
            GrabbedObjects = GrabbedObjects.Where(Filter).ToArray();
        }
        return GrabbedObjects;
    }
    public static GameObject FindObject(this GameObject parent, string name) {
        Transform[] array = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform transform in array) {
            if (transform.name == name) {
                return transform.gameObject;
            }
        }
        return null;
    }
    public static string GetPath(this GameObject gameObject) {
        string text = "/" + gameObject.name;
        while (gameObject.transform.parent != null) {
            gameObject = gameObject.transform.parent.gameObject;
            text = "/" + gameObject.name + text;
        }
        return text;
    }
    public static void DestroyChildren(this Transform transform, Func<Transform, bool> exclude, bool DirectChildrenOnly = false) {
        foreach (Transform child in (DirectChildrenOnly ? transform.GetChildren() : transform.GetComponentsInChildren<Transform>(true)).Where(o => o != transform)) {
            if (child != null) {
                if (exclude == null || !exclude(child)) {
                    Object.Destroy(child.gameObject);
                }
            }
        }
    }
    public static void DestroyChildren(this Transform transform, bool DirectChildrenOnly = false) {
        transform.DestroyChildren(null, DirectChildrenOnly);
    }
    public static Vector3 SetX(this Vector3 vector, float x) {
        return new Vector3(x, vector.y, vector.z);
    }
    public static Vector3 SetY(this Vector3 vector, float y) {
        return new Vector3(vector.x, y, vector.z);
    }
    public static Vector3 SetZ(this Vector3 vector, float z) {
        return new Vector3(vector.x, vector.y, z);
    }
    public static float RoundAmount(this float i, float nearestFactor) {
        return (float)Math.Round(i / nearestFactor) * nearestFactor;
    }
    public static Vector3 RoundAmount(this Vector3 i, float nearestFactor) {
        return new Vector3(i.x.RoundAmount(nearestFactor), i.y.RoundAmount(nearestFactor), i.z.RoundAmount(nearestFactor));
    }
    public static Vector2 RoundAmount(this Vector2 i, float nearestFactor) {
        return new Vector2(i.x.RoundAmount(nearestFactor), i.y.RoundAmount(nearestFactor));
    }
    public static string ReplaceFirst(this string text, string search, string replace) {
        int num = text.IndexOf(search);
        return num < 0 ? text : text.Substring(0, num) + replace + text.Substring(num + search.Length);
    }
    public static ColorBlock SetColor(this ColorBlock block, Color color) {
        ColorBlock result = default;
        result.colorMultiplier = block.colorMultiplier;
        result.disabledColor = Color.grey;
        result.highlightedColor = color;
        result.normalColor = color / 1.5f;
        result.pressedColor = Color.white;
        result.selectedColor = color / 1.5f;
        return result;
    }
    public static void DelegateSafeInvoke(this Delegate @delegate, params object[] args) {
        Delegate[] invocationList = @delegate.GetInvocationList();
        for (int i = 0; i < invocationList.Length; i++) {
            try {
                _ = invocationList[i].DynamicInvoke(args);
            } catch (Exception ex) {
                MelonLogger.Error("Error while executing delegate:\n" + ex);
            }
        }
    }
    public static string ToEasyString(this TimeSpan timeSpan) {
        return Mathf.FloorToInt(timeSpan.Ticks / 864000000000L) > 0
            ? $"{timeSpan:%d} days"
            : Mathf.FloorToInt(timeSpan.Ticks / 36000000000L) > 0
            ? $"{timeSpan:%h} hours"
            : Mathf.FloorToInt(timeSpan.Ticks / 600000000) > 0 ? $"{timeSpan:%m} minutes" : $"{timeSpan:%s} seconds";
    }
    public static Quaternion LookAtThisWithoutRef(this Transform transform, Vector3 FromThisPosition) {
        GameObject obj = new("TempObj");
        obj.transform.position = FromThisPosition;
        obj.transform.LookAt(transform);
        Quaternion rot = obj.transform.localRotation;
        Object.Destroy(obj);
        return rot;
    }
    public static Quaternion FlipX(this Quaternion rot) {
        return new Quaternion(-rot.x, rot.y, rot.z, rot.w);
    }
    public static Quaternion FlipY(this Quaternion rot) {
        return new Quaternion(rot.x, -rot.y, rot.z, rot.w);
    }
    public static Quaternion FlipZ(this Quaternion rot) {
        return new Quaternion(rot.x, rot.y, -rot.z, rot.w);
    }
    public static Quaternion Combine(this Quaternion rot1, Quaternion rot2) {
        return new Quaternion(rot1.x + rot2.x, rot1.y + rot2.y, rot1.z + rot2.z, rot1.w + rot2.w);
    }
    public static Transform[] GetChildren(this Transform transform) {
        List<Transform> Children = new();
        for (int i = 0; i < transform.childCount; i++) {
            Children.Add(transform.GetChild(i));
        }
        return Children.ToArray();
    }
    public static Transform[] GetAllChildren(this Transform transform) {
        List<Transform> Children = new();
        void GetChildrenR(Transform trans) {
            for (int i = 0; i < trans.childCount; i++) {
                Children.Add(trans.GetChild(i));
                _ = GetChildren(trans.GetChild(i));
            }
        }
        GetChildrenR(transform);
        return Children.ToArray();
    }
    public static string GetPath(this Transform transform) {
        string path = $"{transform.name}";
        Transform CurrentObj = transform;
        while (CurrentObj.parent != null) {
            CurrentObj = CurrentObj.parent;
            path = $"{CurrentObj.name}/" + path;
        }
        return path;
    }
        public static bool ExpiredSince(this DateTime dateTime, int minutes) {
            return (dateTime - DateTime.Now).TotalMinutes < minutes;
        }
        public static TimeSpan StripMilliseconds(this TimeSpan time) {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
        public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
            string final = dir.FullName;
            foreach (string path in paths) {
                final = Path.Combine(final, path);
            }
            return new DirectoryInfo(final);
        }
        public static FileInfo Combine(this FileInfo file, params string[] paths) {
            string final = file.DirectoryName;
            foreach (string path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string FileNameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        public static void AppendLine(this FileInfo file, string line) {
            try {
                if (!file.Exists) {
                    _ = file.Create();
                }
                File.AppendAllLines(file.FullName, new string[] { line });
            } catch { }
        }
        public static object ToJson(this object obj, bool indented = true) {
            return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None, new JsonConverter[] { new StringEnumConverter() });
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp) {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static bool IsNullOrEmpty(this string source) {
            return string.IsNullOrEmpty(source);
        }
        public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
            return count != -1 ? source.Split(new string[] { split }, count, options) : source.Split(new string[] { split }, options);
        }
        public static string Remove(this string Source, string Replace) {
            return Source.Replace(Replace, string.Empty);
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
            int place = Source.LastIndexOf(Find);
            if (place == -1) {
                return Source;
            }
            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
        public static string Ext(this string text, string extension) {
            return text + "." + extension;
        }
        public static string Quote(this string text) {
            return SurroundWith(text, "\"");
        }
        public static string Enclose(this string text) {
            return SurroundWith(text, "(", ")");
        }
        public static string Brackets(this string text) {
            return SurroundWith(text, "[", "]");
        }
        public static string SurroundWith(this string text, string surrounds) {
            return surrounds + text + surrounds;
        }
        public static string SurroundWith(this string text, string starts, string ends) {
            return starts + text + ends;
        }
        public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
            if (!dictionary.ContainsKey(key)) {
                dictionary.Add(key, value);
            }
        }
        public static string ToQueryString(this NameValueCollection nvc) {
            if (nvc == null) {
                return string.Empty;
            }
            StringBuilder sb = new();
            foreach (string key in nvc.Keys) {
                if (string.IsNullOrWhiteSpace(key)) {
                    continue;
                }
                string[] values = nvc.GetValues(key);
                if (values == null) {
                    continue;
                }
                foreach (string value in values) {
                    _ = sb.Append(sb.Length == 0 ? "?" : "&");
                    _ = sb.AppendFormat("{0}={1}", key, value);
                }
            }
            return sb.ToString();
        }
        public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
            if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) {
                return false;
            }
            string[] trueValues = new string[] { true.ToString(), "yes", "1" };
            if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) {
                return true;
            }
            string[] falseValues = new string[] { false.ToString(), "no", "0" };
            return falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase) || defaultValue;
        }
        public static string GetString(this NameValueCollection collection, string key) {
            return !collection.AllKeys.Contains(key) ? collection[key] : null;
        }
        public static T PopFirst<T>(this IEnumerable<T> list) {
            return list.ToList().PopAt(0);
        }
        public static T PopLast<T>(this IEnumerable<T> list) {
            return list.ToList().PopAt(list.Count() - 1);
        }
        public static T PopAt<T>(this List<T> list, int index) {
            T r = list.ElementAt<T>(index);
            list.RemoveAt(index);
            return r;
        }
        public static readonly Regex QueryRegex = new(@"[?&](\w[\w.]*)=([^?&]+)");
        public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
            Match match = QueryRegex.Match(uri.PathAndQuery);
            Dictionary<string, string> paramaters = new();
            while (match.Success) {
                paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
                match = match.NextMatch();
            }
            return paramaters;
        }
        public static string GetDescription(this Enum value) {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null) {
                FieldInfo field = type.GetField(name);
                if (field != null) {
                    if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr) {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
        public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
            Type type = typeof(T);
            if (!type.IsEnum) {
                throw new InvalidOperationException();
            }
            foreach (FieldInfo field in type.GetFields()) {
                if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute) {
                    if (attribute.Description == description) {
                        return (T)field.GetValue(null);
                    }
                } else {
                    if (field.Name == description) {
                        return (T)field.GetValue(null);
                    }
                }
            }
            return returnDefault ? default : throw new ArgumentException("Not found.", "description");
        }
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
            using CancellationTokenSource timeoutCancellationTokenSource = new();
            Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  
            } else {
                return default;
            }
        }
        public static bool ExpiredSince(this DateTime dateTime, int minutes) {
            return (dateTime - DateTime.Now).TotalMinutes < minutes;
        }
        public static TimeSpan StripMilliseconds(this TimeSpan time) {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
        public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
            string final = dir.FullName;
            foreach (string path in paths) {
                final = Path.Combine(final, path);
            }
            return new DirectoryInfo(final);
        }
        public static FileInfo Combine(this FileInfo file, params string[] paths) {
            string final = file.DirectoryName;
            foreach (string path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string FileNameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        public static void AppendLine(this FileInfo file, string line) {
            try {
                if (!file.Exists) {
                    _ = file.Create();
                }
                File.AppendAllLines(file.FullName, new string[] { line });
            } catch { }
        }
        public static string ReadAllText(this FileInfo file) {
            return File.ReadAllText(file.FullName);
        }
        public static List<string> ReadAllLines(this FileInfo file) {
            return File.ReadAllLines(file.FullName).ToList();
        }
        public static object ToJson(this object obj, bool indented = true) {
            return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None, new JsonConverter[] { new StringEnumConverter() });
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp) {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static bool IsNullOrEmpty(this string source) {
            return string.IsNullOrEmpty(source);
        }
        public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
            return count != -1 ? source.Split(new string[] { split }, count, options) : source.Split(new string[] { split }, options);
        }
        public static string Remove(this string Source, string Replace) {
            return Source.Replace(Replace, string.Empty);
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
            int place = Source.LastIndexOf(Find);
            if (place == -1) {
                return Source;
            }
            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
        public static string Ext(this string text, string extension) {
            return text + "." + extension;
        }
        public static string Quote(this string text) {
            return SurroundWith(text, "\"");
        }
        public static string Enclose(this string text) {
            return SurroundWith(text, "(", ")");
        }
        public static string Brackets(this string text) {
            return SurroundWith(text, "[", "]");
        }
        public static string SurroundWith(this string text, string surrounds) {
            return surrounds + text + surrounds;
        }
        public static string SurroundWith(this string text, string starts, string ends) {
            return starts + text + ends;
        }
        public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
            if (!dictionary.ContainsKey(key)) {
                dictionary.Add(key, value);
            }
        }
        public static string ToQueryString(this NameValueCollection nvc) {
            if (nvc == null) {
                return string.Empty;
            }
            StringBuilder sb = new();
            foreach (string key in nvc.Keys) {
                if (string.IsNullOrWhiteSpace(key)) {
                    continue;
                }
                string[] values = nvc.GetValues(key);
                if (values == null) {
                    continue;
                }
                foreach (string value in values) {
                    _ = sb.Append(sb.Length == 0 ? "?" : "&");
                    _ = sb.AppendFormat("{0}={1}", key, value);
                }
            }
            return sb.ToString();
        }
        public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
            if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) {
                return false;
            }
            string[] trueValues = new string[] { true.ToString(), "yes", "1" };
            if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) {
                return true;
            }
            string[] falseValues = new string[] { false.ToString(), "no", "0" };
            return falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase) || defaultValue;
        }
        public static string GetString(this NameValueCollection collection, string key) {
            return !collection.AllKeys.Contains(key) ? collection[key] : null;
        }
        public static T PopFirst<T>(this IEnumerable<T> list) {
            return list.ToList().PopAt(0);
        }
        public static T PopLast<T>(this IEnumerable<T> list) {
            return list.ToList().PopAt(list.Count() - 1);
        }
        public static T PopAt<T>(this List<T> list, int index) {
            T r = list.ElementAt<T>(index);
            list.RemoveAt(index);
            return r;
        }
        public static readonly Regex QueryRegex = new(@"[?&](\w[\w.]*)=([^?&]+)");
        public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
            Match match = QueryRegex.Match(uri.PathAndQuery);
            Dictionary<string, string> paramaters = new();
            while (match.Success) {
                paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
                match = match.NextMatch();
            }
            return paramaters;
        }
        public static Dictionary<string, string> ParseQueryString(this string queryString) {
            NameValueCollection nvc = HttpUtility.ParseQueryString(queryString);
            return nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
        }
        public static string GetDescription(this Enum value) {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null) {
                FieldInfo field = type.GetField(name);
                if (field != null) {
                    if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr) {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
        public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
            Type type = typeof(T);
            if (!type.IsEnum) {
                throw new InvalidOperationException();
            }
            foreach (FieldInfo field in type.GetFields()) {
                if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute) {
                    if (attribute.Description == description) {
                        return (T)field.GetValue(null);
                    }
                } else {
                    if (field.Name == description) {
                        return (T)field.GetValue(null);
                    }
                }
            }
            return returnDefault ? default : throw new ArgumentException("Not found.", "description");
        }
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
            using CancellationTokenSource timeoutCancellationTokenSource = new();
            Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  
            } else {
                return default;
            }
        }
    public static T JSonToItem<T>(this string jsonString) => JsonSerializer.Deserialize<T>(jsonString);
    public static (bool result, Exception exception) JsonToFile<T>(this T sender, string fileName, bool format = true) {
        try {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(fileName, JsonSerializer.Serialize(sender, format ? options : null));
            return (true, null);
        } catch (Exception exception) {
            return (false, exception);
        }
    }
    public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
        return instanceToConvert.GetType()
          .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
          .ToDictionary(
          propertyInfo => propertyInfo.Name,
          propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
    }
    public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
        Type propertyType = propertyInfo.PropertyType;
        object propertyValue = propertyInfo.GetValue(owner);
        if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
            var collectionItems = new List<Dictionary<string, object>>();
            var count = (int)propertyType.GetProperty("Count").GetValue(propertyValue);
            PropertyInfo indexerProperty = propertyType.GetProperty("Item");
            for (var index = 0; index < count; index++) {
                object item = indexerProperty.GetValue(propertyValue, new object[] { index });
                PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (itemProperties.Any()) {
                    Dictionary<string, object> dictionary = itemProperties
                      .ToDictionary(
                        subtypePropertyInfo => subtypePropertyInfo.Name,
                        subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
                    collectionItems.Add(dictionary);
                }
            }
            return collectionItems;
        }
        if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
            return propertyValue;
        }
        PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        if (properties.Any()) {
            return properties.ToDictionary(
                                subtypePropertyInfo => subtypePropertyInfo.Name,
                                subtypePropertyInfo => (object)Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
        }
        return propertyValue;
    }
    public static bool ExpiredSince(this DateTime dateTime, int minutes) {
        return (dateTime - DateTime.Now).TotalMinutes < minutes;
    }
    public static TimeSpan StripMilliseconds(this TimeSpan time) {
        return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
    }
    public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new DirectoryInfo(final);
    }
    public static bool IsEmpty(this DirectoryInfo directory) {
        return !Directory.EnumerateFileSystemEntries(directory.FullName).Any();
    }
    public static string StatusString(this DirectoryInfo directory, bool existsInfo = false) {
        if (directory is null) return " (is null ❌)";
        if (File.Exists(directory.FullName)) return " (is file ❌)";
        if (!directory.Exists) return " (does not exist ❌)";
        if (directory.IsEmpty()) return " (is empty ⚠️)";
        return existsInfo ? " (exists ✅)" : string.Empty;
    }
    public static void Copy(this DirectoryInfo source, DirectoryInfo target, bool overwrite = false) {
        Directory.CreateDirectory(target.FullName);
        foreach (FileInfo fi in source.GetFiles())
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), overwrite);
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            Copy(diSourceSubDir, target.CreateSubdirectory(diSourceSubDir.Name));
    }
    public static bool Backup(this DirectoryInfo directory, bool overwrite = false) {
        if (!directory.Exists) return false;
        var backupDirPath = directory.FullName + ".bak";
        if (Directory.Exists(backupDirPath) && !overwrite) return false;
        Directory.CreateDirectory(backupDirPath);
        foreach (FileInfo fi in directory.GetFiles()) fi.CopyTo(Path.Combine(backupDirPath, fi.Name), overwrite);
        foreach (DirectoryInfo diSourceSubDir in directory.GetDirectories()) {
            diSourceSubDir.Copy(Directory.CreateDirectory(Path.Combine(backupDirPath, diSourceSubDir.Name)), overwrite);
        }
        return true;
    }
    public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
        var final = dir.FullName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static FileInfo Combine(this FileInfo file, params string[] paths) {
        var final = file.DirectoryName;
        foreach (var path in paths) {
            final = Path.Combine(final, path);
        }
        return new FileInfo(final);
    }
    public static string FileNameWithoutExtension(this FileInfo file) {
        return Path.GetFileNameWithoutExtension(file.Name);
    }
    public static string StatusString(this FileInfo file, bool existsInfo = false) {
        if (file is null) return "(is null ❌)";
        if (Directory.Exists(file.FullName)) return "(is directory ❌)";
        if (!file.Exists) return "(does not exist ❌)";
        if (file.Length < 1) return "(is empty ⚠️)";
        return existsInfo ? "(exists ✅)" : string.Empty;
    }
    public static void AppendLine(this FileInfo file, string line) {
        try {
            if (!file.Exists) file.Create();
            File.AppendAllLines(file.FullName, new string[] { line });
        } catch { }
    }
    public static void WriteAllText(this FileInfo file, string text) => File.WriteAllText(file.FullName, text);
    public static string ReadAllText(this FileInfo file) => File.ReadAllText(file.FullName);
    public static List<string> ReadAllLines(this FileInfo file) => File.ReadAllLines(file.FullName).ToList();
    public static bool Backup(this FileInfo file, bool overwrite = false) {
        if (!file.Exists) return false;
        var backupFilePath = file.FullName + ".bak";
        if (File.Exists(backupFilePath) && !overwrite) return false;
        File.Copy(file.FullName, backupFilePath, overwrite);
        return true;
    }
    public static string ToJson(this object obj, bool indented = true) {
        var options = new JsonSerializerOptions {
            WriteIndented = indented,
            Converters = { new JsonStringEnumConverter() }
        };
        return JsonSerializer.Serialize(obj, options);
    }
    public static IEnumerable<string> SplitToLines(this string input) {
        if (input == null) {
            yield break;
        }
        using (System.IO.StringReader reader = new System.IO.StringReader(input)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                yield return line;
            }
        }
    }
    public static string ToTitleCase(this string source, string langCode = "en-US") {
        return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
    }
    public static bool Contains(this string source, string toCheck, StringComparison comp) {
        return source?.IndexOf(toCheck, comp) >= 0;
    }
    public static bool IsNullOrEmpty(this string source) {
        return string.IsNullOrEmpty(source);
    }
    public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
        if (count != -1) return source.Split(new string[] { split }, count, options);
        return source.Split(new string[] { split }, options);
    }
    public static string Remove(this string Source, string Replace) {
        return Source.Replace(Replace, string.Empty);
    }
    public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
        int place = Source.LastIndexOf(Find);
        if (place == -1)
            return Source;
        string result = Source.Remove(place, Find.Length).Insert(place, Replace);
        return result;
    }
    public static string EscapeLineBreaks(this string source) {
        return Regex.Replace(source, @"\r\n?|\n", @"\$&");
    }
    public static string Ext(this string text, string extension) {
        return text + "." + extension;
    }
    public static string Quote(this string text) {
        return SurroundWith(text, "\"");
    }
    public static string Enclose(this string text) {
        return SurroundWith(text, "(", ")");
    }
    public static string Brackets(this string text) {
        return SurroundWith(text, "[", "]");
    }
    public static string SurroundWith(this string text, string surrounds) {
        return surrounds + text + surrounds;
    }
    public static string SurroundWith(this string text, string starts, string ends) {
        return starts + text + ends;
    }
    public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
        if (!dictionary.ContainsKey(key))
            dictionary.Add(key, value);
    }
    public static Dictionary<string, object?> MergeWith(this Dictionary<string, object?> sourceDict, Dictionary<string, object?> destDict) {
        foreach (var kvp in sourceDict) {
            if (destDict.ContainsKey(kvp.Key)) {
                Program.Log($"Key '{kvp.Key}' already exists and will be overwritten.");
            }
            destDict[kvp.Key] = kvp.Value;
        }
        return destDict;
    }
    public static Dictionary<string, object> MergeRecursiveWith(this Dictionary<string, object> sourceDict, Dictionary<string, object> targetDict) {
        foreach (var kvp in sourceDict) {
            if (targetDict.TryGetValue(kvp.Key, out var existingValue)) {
                if (existingValue is Dictionary<string, object> existingDict && kvp.Value is Dictionary<string, object> sourceDictValue) {
                    sourceDictValue.MergeRecursiveWith(existingDict);
                } else if (kvp.Value is null) {
                    targetDict.Remove(kvp.Key);
                    Program.Log($"Removed key '{kvp.Key}' as it was set to null in the source dictionary.");
                } else {
                    targetDict[kvp.Key] = kvp.Value;
                    Program.Log($"Overwriting existing value for key '{kvp.Key}'.");
                }
            } else {
                targetDict[kvp.Key] = kvp.Value;
            }
        }
        return targetDict;
    }
    public static string ToQueryString(this NameValueCollection nvc) {
        if (nvc == null) return string.Empty;
        StringBuilder sb = new StringBuilder();
        foreach (string key in nvc.Keys) {
            if (string.IsNullOrWhiteSpace(key)) continue;
            string[] values = nvc.GetValues(key);
            if (values == null) continue;
            foreach (string value in values) {
                sb.Append(sb.Length == 0 ? "?" : "&");
                sb.AppendFormat("{0}={1}", key, value);
            }
        }
        return sb.ToString();
    }
    public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
        if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
        var trueValues = new string[] { true.ToString(), "yes", "1" };
        if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        var falseValues = new string[] { false.ToString(), "no", "0" };
        if (falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
        return defaultValue;
    }
    public static string GetString(this NameValueCollection collection, string key) {
        if (!collection.AllKeys.Contains(key)) return collection[key];
        return null;
    }
    public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);
    public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);
    public static T PopAt<T>(this List<T> list, int index) {
        T r = list.ElementAt<T>(index);
        list.RemoveAt(index);
        return r;
    }
    public static readonly Regex QueryRegex = new Regex(@"[?&](\w[\w.]*)=([^?&]+)");
    public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
        var match = QueryRegex.Match(uri.PathAndQuery);
        var paramaters = new Dictionary<string, string>();
        while (match.Success) {
            paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
            match = match.NextMatch();
        }
        return paramaters;
    }
    public static DescriptionAttribute GetEnumDescriptionAttribute<T>(
    this T value) where T : struct {
        Type type = typeof(T);
        if (!type.IsEnum)
            throw new InvalidOperationException(
                "The type parameter T must be an enum type.");
        if (!Enum.IsDefined(type, value))
            throw new InvalidEnumArgumentException(
                "value", Convert.ToInt32(value), type);
        FieldInfo fi = type.GetField(value.ToString(),
            BindingFlags.Static | BindingFlags.Public);
        return fi.GetCustomAttributes(typeof(DescriptionAttribute), true).
            Cast<DescriptionAttribute>().SingleOrDefault();
    }
    public static string? GetName(this Type enumType, object value) => Enum.GetName(enumType, value);
    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
        using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  
            } else {
                return default(TResult);
            }
        }
    }
    public static string ToYesNo(this bool input) => input ? "Yes" : "No";
    public static string ToEnabledDisabled(this bool input) => input ? "Enabled" : "Disabled";
    public static string ToOnOff(this bool input) => input ? "On" : "Off";
    public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
        return instanceToConvert.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .ToDictionary(
            propertyInfo => propertyInfo.Name,
            propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
    }
        public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
            Type propertyType = propertyInfo.PropertyType;
            object propertyValue = propertyInfo.GetValue(owner);
            if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
                var collectionItems = new List<Dictionary<string, object>>();
                var count = (int)propertyType.GetProperty("Count").GetValue(propertyValue);
                PropertyInfo indexerProperty = propertyType.GetProperty("Item");
                for (var index = 0; index < count; index++) {
                    object item = indexerProperty.GetValue(propertyValue, new object[] { index });
                    PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                    if (itemProperties.Any()) {
                        Dictionary<string, object> dictionary = itemProperties
                          .ToDictionary(
                            subtypePropertyInfo => subtypePropertyInfo.Name,
                            subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
                        collectionItems.Add(dictionary);
                    }
                }
                return collectionItems;
            }
            if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
                return propertyValue;
            }
            PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (properties.Any()) {
                return properties.ToDictionary(
                                    subtypePropertyInfo => subtypePropertyInfo.Name,
                                    subtypePropertyInfo => (object)Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
            }
            return propertyValue;
        }
        public static bool ExpiredSince(this DateTime dateTime, int minutes) {
            return (dateTime - DateTime.Now).TotalMinutes < minutes;
        }
        public static TimeSpan StripMilliseconds(this TimeSpan time) {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
        public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new DirectoryInfo(final);
        }
        public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static FileInfo Combine(this FileInfo file, params string[] paths) {
            var final = file.DirectoryName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string FileNameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        public static void AppendLine(this FileInfo file, string line) {
            try
            {
                if (!file.Exists) file.Create();
                File.AppendAllLines(file.FullName, new string[] { line });
            }
            catch { }
        }
        public static IEnumerable<TreeNode> GetAllChilds(this TreeNodeCollection nodes) {
            foreach (TreeNode node in nodes) {
                yield return node;
                foreach (var child in GetAllChilds(node.Nodes))
                    yield return child;
            }
        }
        public static string ToJson(this object obj, bool indented = true) {
            return JsonConvert.SerializeObject(obj, (indented ? Formatting.Indented : Formatting.None), new JsonConverter[] { new StringEnumConverter() });
        }
        public static string ToTitleCase(this string source, string langCode = "en-US") {
            return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp) {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static bool IsNullOrEmpty(this string source) {
            return string.IsNullOrEmpty(source);
        }
        public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
            if (count != -1) return source.Split(new string[] { split }, count, options);
            return source.Split(new string[] { split }, options);
        }
        public static string Remove(this string Source, string Replace) {
            return Source.Replace(Replace, string.Empty);
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
            int place = Source.LastIndexOf(Find);
            if (place == -1)
                return Source;
            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
        public static string Ext(this string text, string extension) {
            return text + "." + extension;
        }
        public static string Quote(this string text) {
            return SurroundWith(text, "\"");
        }
        public static string Enclose(this string text) {
            return SurroundWith(text, "(", ")");
        }
        public static string Brackets(this string text) {
            return SurroundWith(text, "[", "]");
        }
        public static string SurroundWith(this string text, string surrounds) {
            return surrounds + text + surrounds;
        }
        public static string SurroundWith(this string text, string starts, string ends) {
            return starts + text + ends;
        }
        public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, value);
        }
        public static string ToQueryString(this NameValueCollection nvc) {
            if (nvc == null) return string.Empty;
            StringBuilder sb = new StringBuilder();
            foreach (string key in nvc.Keys) {
                if (string.IsNullOrWhiteSpace(key)) continue;
                string[] values = nvc.GetValues(key);
                if (values == null) continue;
                foreach (string value in values) {
                    sb.Append(sb.Length == 0 ? "?" : "&");
                    sb.AppendFormat("{0}={1}", key, value);
                }
            }
            return sb.ToString();
        }
        public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
            if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
            var trueValues = new string[] { true.ToString(), "yes", "1" };
            if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
            var falseValues = new string[] { false.ToString(), "no", "0" };
            if (falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
            return defaultValue;
        }
        public static string GetString(this NameValueCollection collection, string key) {
            if (!collection.AllKeys.Contains(key)) return collection[key];
            return null;
        }
        public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);
        public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);
        public static T PopAt<T>(this List<T> list, int index) {
            T r = list.ElementAt<T>(index);
            list.RemoveAt(index);
            return r;
        }
        public static readonly Regex QueryRegex = new Regex(@"[?&](\w[\w.]*)=([^?&]+)");
        public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
            var match = QueryRegex.Match(uri.PathAndQuery);
            var paramaters = new Dictionary<string, string>();
            while (match.Success) {
                paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
                match = match.NextMatch();
            }
            return paramaters;
        }
        public static string GetDescription(this Enum value) {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null) {
                FieldInfo field = type.GetField(name);
                if (field != null) {
                    DescriptionAttribute attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null) {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
        public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields()) {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null) {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            if (returnDefault) return default(T);
            else throw new ArgumentException("Not found.", "description");
        }
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task) {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;  
                }
                else
                {
                    return default(TResult);
                }
            }
        }
        public static string ToYesNo(this bool input) => input ? "Yes" : "No";
        public static string ToEnabledDisabled(this bool input) => input ? "Enabled" : "Disabled";
        public static string ToOnOff(this bool input) => input ? "On" : "Off";
        public static string getName(this Player player) => player.photonView.owner.NickName;
        public static string str(this int value) {
            return $"#{value}";
        }
        public static string str(this string value) {
            return value.Quote();
        }
        public static string str(this DateTime value) {
            return value.ToString().str();
        }
        public static bool isFalse(this DialogResult result) {
            return result == DialogResult.No || result == DialogResult.Cancel || result == DialogResult.Abort || result == DialogResult.Ignore || result == DialogResult.None;
        }
        public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
            return instanceToConvert.GetType()
              .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
              .ToDictionary(
              propertyInfo => propertyInfo.Name,
              propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
        }
        public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
            Type propertyType = propertyInfo.PropertyType;
            object propertyValue = propertyInfo.GetValue(owner);
            if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
                List<Dictionary<string, object>> collectionItems = new List<Dictionary<string, object>>();
                int count = (int)propertyType.GetProperty("Count").GetValue(propertyValue);
                PropertyInfo indexerProperty = propertyType.GetProperty("Item");
                for (int index = 0; index < count; index++) {
                    object item = indexerProperty.GetValue(propertyValue, new object[] { index });
                    PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                    if (itemProperties.Any()) {
                        Dictionary<string, object> dictionary = itemProperties
                          .ToDictionary(
                            subtypePropertyInfo => subtypePropertyInfo.Name,
                            subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
                        collectionItems.Add(dictionary);
                    }
                }
                return collectionItems;
            }
            if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
                return propertyValue;
            }
            PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (properties.Any()) {
                return properties.ToDictionary(
                                    subtypePropertyInfo => subtypePropertyInfo.Name,
                                    subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
            }
            return propertyValue;
        }
        public static bool ExpiredSince(this DateTime dateTime, int minutes) {
            return (dateTime - DateTime.Now).TotalMinutes < minutes;
        }
        public static TimeSpan StripMilliseconds(this TimeSpan time) {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
        public static DirectoryInfo Combine(this Environment.SpecialFolder specialFolder, params string[] paths) {
            return Combine(new DirectoryInfo(Environment.GetFolderPath(specialFolder)), paths);
        }
        public static FileInfo CombineFile(this Environment.SpecialFolder specialFolder, params string[] paths) {
            return CombineFile(new DirectoryInfo(Environment.GetFolderPath(specialFolder)), paths);
        }
        public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
            string final = dir.FullName;
            foreach (string path in paths) {
                final = Path.Combine(final, path.ReplaceInvalidFileNameChars());
            }
            return new DirectoryInfo(final);
        }
        public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
            string final = dir.FullName;
            foreach (string path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string PrintablePath(this FileSystemInfo file) {
            return file.FullName.Replace(@"\\", @"\");
        }
        public static FileInfo Backup(this FileInfo file, bool overwrite = true, string extension = ".bak") {
            return file.CopyTo(file.FullName + extension, overwrite);
        }
        public static FileInfo Combine(this FileInfo file, params string[] paths) {
            string final = file.DirectoryName;
            foreach (string path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string FileNameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        public static void AppendLine(this FileInfo file, string line) {
            try {
                if (!file.Exists) {
                    file.Create();
                }
                File.AppendAllLines(file.FullName, new[] { line });
            } catch { }
        }
        public static void WriteAllText(this FileInfo file, string text) {
            file.Directory.Create();
            if (!file.Exists) {
                file.Create().Close();
            }
            File.WriteAllText(file.FullName, text);
        }
        public static string ReadAllText(this FileInfo file) {
            return File.ReadAllText(file.FullName);
        }
        public static List<string> ReadAllLines(this FileInfo file) {
            return File.ReadAllLines(file.FullName).ToList();
        }
        public static string RunWait(this FileInfo file, params string[] args) {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = file.FullName;
            p.StartInfo.Arguments = string.Join(' ', args);
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }
        public static string GetVersion(this FileInfo _file) {
            var version = "0.0.0.0";
            try {
                version = GetVersion(Assembly.LoadFile(_file.FullName));
            } catch { }
            version = ShellFile.FromFilePath(_file.FullName).Properties.System.FileVersion.Value;
            if (!string.IsNullOrWhiteSpace(version)) return version;
            return version;
        }
        public static string GetVersion(this Assembly assembly) {
            string version = "0.0.0.0";
            version = assembly.GetName().Version.ToString();
            if (!string.IsNullOrWhiteSpace(version)) return version;
            var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            version = versionInfo.FileVersion;
            if (!string.IsNullOrWhiteSpace(version)) return version;
            version = versionInfo.ProductVersion;
            if (!string.IsNullOrWhiteSpace(version)) return version;
            return version;
        }
        public static string GetAuthor(this FileInfo _file) {
            string author;
            try {
                author = GetAuthor(Assembly.LoadFile(_file.FullName));
                if (!string.IsNullOrWhiteSpace(author)) return author;
            } catch { }
            var val = ShellFile.FromFilePath(_file.FullName).Properties.System.Author.Value;
            if (val != null && val.Length > 0) {
                author = string.Join(", ", val);
                if (!string.IsNullOrWhiteSpace(author)) return author;
            }
            return "Unknown Author";
        }
        public static string GetAuthor(this Assembly assembly) {
            var author = FileVersionInfo.GetVersionInfo(assembly.Location).CompanyName;
            if (!string.IsNullOrWhiteSpace(author)) return author;
            object[] attribs = assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), true);
            if (attribs.Length > 0) return ((AssemblyCompanyAttribute)attribs[0]).Company;
            attribs = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true);
            if (attribs.Length > 0) return ((AssemblyCopyrightAttribute)attribs[0]).Copyright;
            attribs = assembly.GetCustomAttributes(typeof(AssemblyTrademarkAttribute), true);
            if (attribs.Length > 0) return ((AssemblyTrademarkAttribute)attribs[0]).Trademark;
            return "Unknown Author";
        }
        public static string GetDigits(this string input) {
            return new string(input.Where(char.IsDigit).ToArray());
        }
        public static string Format(this string input, params string[] args) {
            return string.Format(input, args);
        }
        public static IEnumerable<string> SplitToLines(this string input) {
            if (input == null) {
                yield break;
            }
            using (System.IO.StringReader reader = new System.IO.StringReader(input)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    yield return line;
                }
            }
        }
        public static string ToTitleCase(this string source, string langCode = "en-US") {
            return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp) {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static bool IsNullOrEmpty(this string source) {
            return string.IsNullOrEmpty(source);
        }
        public static bool IsNullOrWhiteSpace(this string source) {
            return string.IsNullOrWhiteSpace(source);
        }
        public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
            if (count != -1) {
                return source.Split(new[] { split }, count, options);
            }
            return source.Split(new[] { split }, options);
        }
        public static string Remove(this string Source, string Replace) {
            return Source.Replace(Replace, string.Empty);
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
            int place = Source.LastIndexOf(Find);
            if (place == -1) {
                return Source;
            }
            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
        public static string Ext(this string text, string extension) {
            return text + "." + extension;
        }
        public static string Quote(this string text) {
            return SurroundWith(text, "\"");
        }
        public static string Enclose(this string text) {
            return SurroundWith(text, "(", ")");
        }
        public static string Brackets(this string text) {
            return SurroundWith(text, "[", "]");
        }
        public static string SurroundWith(this string text, string surrounds) {
            return surrounds + text + surrounds;
        }
        public static string SurroundWith(this string text, string starts, string ends) {
            return starts + text + ends;
        }
        public static string RemoveInvalidFileNameChars(this string filename) {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }
        public static string ReplaceInvalidFileNameChars(this string filename) {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
        public static int Percentage(this int total, int part) {
            return (int)((double)part / total * 100);
        }
        public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
            if (!dictionary.ContainsKey(key)) {
                dictionary.Add(key, value);
            }
        }
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self) {
            return self.Select((item, index) => (item, index));
        }
        public static string Join(this List<string> strings, string separator) {
            return string.Join(separator, strings);
        }
        public static T PopFirst<T>(this IEnumerable<T> list) {
            return list.ToList().PopAt(0);
        }
        public static T PopLast<T>(this IEnumerable<T> list) {
            return list.ToList().PopAt(list.Count() - 1);
        }
        public static T PopAt<T>(this List<T> list, int index) {
            T r = list.ElementAt(index);
            list.RemoveAt(index);
            return r;
        }
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
            using (CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource()) {
                Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task) {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;
                } else {
                    return default(TResult);
                }
            }
        }
        public static string ToString(this bool value, BooleanStringMode mode = BooleanStringMode.TrueFalse, bool capitalize = true) {
            string str;
            switch (mode) {
                case BooleanStringMode.Numbers:
                    str = value ? "1" : "0"; break;
                case BooleanStringMode.TrueFalse:
                    str = value ? "True" : "False"; break;
                case BooleanStringMode.EnabledDisabled:
                    str = value ? "Enabled" : "Disabled"; break;
                case BooleanStringMode.OnOff:
                    str = value ? "On" : "Off"; break;
                case BooleanStringMode.YesNo:
                    str = value ? "Yes" : "No"; break;
                default: throw new ArgumentNullException("mode");
            }
            return capitalize ? str : str.ToLower();
        }
        public static bool ExpiredSince(this DateTime dateTime, int minutes) {
            return (dateTime - DateTime.Now).TotalMinutes < minutes;
        }
        public static TimeSpan StripMilliseconds(this TimeSpan time) {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
        public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
            string final = dir.FullName;
            foreach (string path in paths) {
                final = Path.Combine(final, path);
            }
            return new DirectoryInfo(final);
        }
        public static FileInfo Combine(this FileInfo file, params string[] paths) {
            string final = file.DirectoryName;
            foreach (string path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string FileNameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        public static void AppendLine(this FileInfo file, string line) {
            try {
                if (!file.Exists) {
                    _ = file.Create();
                }
                File.AppendAllLines(file.FullName, new string[] { line });
            } catch { }
        }
        public static string ReadAllText(this FileInfo file) {
            return File.ReadAllText(file.FullName);
        }
        public static List<string> ReadAllLines(this FileInfo file) {
            return File.ReadAllLines(file.FullName).ToList();
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp) {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static bool IsNullOrEmpty(this string source) {
            return string.IsNullOrEmpty(source);
        }
        public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
            return count != -1 ? source.Split(new string[] { split }, count, options) : source.Split(new string[] { split }, options);
        }
        public static string Remove(this string Source, string Replace) {
            return Source.Replace(Replace, string.Empty);
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
            int place = Source.LastIndexOf(Find);
            if (place == -1) {
                return Source;
            }
            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
        public static string Ext(this string text, string extension) {
            return text + "." + extension;
        }
        public static string Quote(this string text) {
            return SurroundWith(text, "\"");
        }
        public static string Enclose(this string text) {
            return SurroundWith(text, "(", ")");
        }
        public static string Brackets(this string text) {
            return SurroundWith(text, "[", "]");
        }
        public static string SurroundWith(this string text, string surrounds) {
            return surrounds + text + surrounds;
        }
        public static string SurroundWith(this string text, string starts, string ends) {
            return starts + text + ends;
        }
        public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
            if (!dictionary.ContainsKey(key)) {
                dictionary.Add(key, value);
            }
        }
        public static string ToQueryString(this NameValueCollection nvc) {
            if (nvc == null) {
                return string.Empty;
            }
            StringBuilder sb = new();
            foreach (string key in nvc.Keys) {
                if (string.IsNullOrWhiteSpace(key)) {
                    continue;
                }
                string[] values = nvc.GetValues(key);
                if (values == null) {
                    continue;
                }
                foreach (string value in values) {
                    _ = sb.Append(sb.Length == 0 ? "?" : "&");
                    _ = sb.AppendFormat("{0}={1}", key, value);
                }
            }
            return sb.ToString();
        }
        public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
            if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) {
                return false;
            }
            string[] trueValues = new string[] { true.ToString(), "yes", "1" };
            if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) {
                return true;
            }
            string[] falseValues = new string[] { false.ToString(), "no", "0" };
            return falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase) || defaultValue;
        }
        public static string GetString(this NameValueCollection collection, string key) {
            return !collection.AllKeys.Contains(key) ? collection[key] : null;
        }
        public static T PopFirst<T>(this IEnumerable<T> list) {
            return list.ToList().PopAt(0);
        }
        public static T PopLast<T>(this IEnumerable<T> list) {
            return list.ToList().PopAt(list.Count() - 1);
        }
        public static T PopAt<T>(this List<T> list, int index) {
            T r = list.ElementAt<T>(index);
            list.RemoveAt(index);
            return r;
        }
        public static readonly Regex QueryRegex = new(@"[?&](\w[\w.]*)=([^?&]+)");
        public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
            Match match = QueryRegex.Match(uri.PathAndQuery);
            Dictionary<string, string> paramaters = new();
            while (match.Success) {
                paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
                match = match.NextMatch();
            }
            return paramaters;
        }
        public static Dictionary<string, string> ParseQueryString(this string queryString) {
            NameValueCollection nvc = HttpUtility.ParseQueryString(queryString);
            return nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
        }
        public static Dictionary<string, string> GetQueryDict(this Uri uri) => uri.Query.ParseQueryString();
        public static string GetQueryValue(this Uri uri, string key) {
            var dict = uri.GetQueryDict();
            if (dict.ContainsKey(key)) {
                return dict[key].Replace("%3A", ":").Replace(" ", "+");
            }
            return null;
        }
        public static string GetDescription(this Enum value) {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null) {
                FieldInfo field = type.GetField(name);
                if (field != null) {
                    if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr) {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
        public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
            Type type = typeof(T);
            if (!type.IsEnum) {
                throw new InvalidOperationException();
            }
            foreach (FieldInfo field in type.GetFields()) {
                if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute) {
                    if (attribute.Description == description) {
                        return (T)field.GetValue(null);
                    }
                } else {
                    if (field.Name == description) {
                        return (T)field.GetValue(null);
                    }
                }
            }
            return returnDefault ? default : throw new ArgumentException("Not found.", "description");
        }
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
            using CancellationTokenSource timeoutCancellationTokenSource = new();
            Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  
            } else {
                return default;
            }
        }
        public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
            return instanceToConvert.GetType()
              .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
              .ToDictionary(
              propertyInfo => propertyInfo.Name,
              propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
        }
        public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
            Type propertyType = propertyInfo.PropertyType;
            object propertyValue = propertyInfo.GetValue(owner);
            if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
                var collectionItems = new List<Dictionary<string, object>>();
                var count = (int)propertyType.GetProperty("Count").GetValue(propertyValue);
                PropertyInfo indexerProperty = propertyType.GetProperty("Item");
                for (var index = 0; index < count; index++) {
                    object item = indexerProperty.GetValue(propertyValue, new object[] { index });
                    PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                    if (itemProperties.Any()) {
                        Dictionary<string, object> dictionary = itemProperties
                          .ToDictionary(
                            subtypePropertyInfo => subtypePropertyInfo.Name,
                            subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
                        collectionItems.Add(dictionary);
                    }
                }
                return collectionItems;
            }
            if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
                return propertyValue;
            }
            PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (properties.Any()) {
                return properties.ToDictionary(
                                    subtypePropertyInfo => subtypePropertyInfo.Name,
                                    subtypePropertyInfo => (object)Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
            }
            return propertyValue;
        }
        public static bool ExpiredSince(this DateTime dateTime, int minutes) {
            return (dateTime - DateTime.Now).TotalMinutes < minutes;
        }
        public static TimeSpan StripMilliseconds(this TimeSpan time) {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
        public static DirectoryInfo Combine(this Environment.SpecialFolder specialFolder, params string[] paths) => Combine(new DirectoryInfo(Environment.GetFolderPath(specialFolder)), paths);
        public static FileInfo CombineFile(this Environment.SpecialFolder specialFolder, params string[] paths) => CombineFile(new DirectoryInfo(Environment.GetFolderPath(specialFolder)), paths);
        public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path.ReplaceInvalidFileNameChars());
            }
            return new DirectoryInfo(final);
        }
        public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static void ShowInExplorer(this DirectoryInfo dir) {
            Utils.StartProcess("explorer.exe", null, dir.FullName.Quote());
        }
        public static string PrintablePath(this FileSystemInfo file) => file.FullName.Replace(@"\\", @"\");
        public static FileInfo Backup(this FileInfo file, bool overwrite = true, string extension = ".bak") {
            return file.CopyTo(file.FullName + extension, overwrite);
        }
        public static FileInfo Combine(this FileInfo file, params string[] paths) {
            var final = file.DirectoryName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string FileNameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        public static void AppendLine(this FileInfo file, string line) {
            try
            {
                if (!file.Exists) file.Create();
                File.AppendAllLines(file.FullName, new string[] { line });
            }
            catch { }
        }
        public static void WriteAllText(this FileInfo file, string text) {
            file.Directory.Create();
            if (!file.Exists) file.Create().Close();
            File.WriteAllText(file.FullName, text);
        }
        public static bool IsDisabled(this FileInfo file) => file.FullName.EndsWith(".disabled");
        public static void Disable(this FileInfo file) {
            if (!file.IsDisabled()) file.MoveTo(file.FullName + ".disabled");
        }
        public static void Enable(this FileInfo file) {
            if (file.IsDisabled()) file.MoveTo(file.FullName.TrimEnd(".disabled"));
        }
        public static void Toggle(this FileInfo file) {
            if (file.IsDisabled()) file.Enable();
            else file.Disable();
        }
        public static string ReadAllText(this FileInfo file) => File.ReadAllText(file.FullName);
        public static List<string> ReadAllLines(this FileInfo file) => File.ReadAllLines(file.FullName).ToList();
        public static void ShowInExplorer(this FileInfo file) {
            Utils.StartProcess("explorer.exe", null, "/select, " + file.FullName.Quote());
        }
        public static IEnumerable<TreeNode> GetAllChilds(this TreeNodeCollection nodes) {
            foreach (TreeNode node in nodes) {
                yield return node;
                foreach (var child in GetAllChilds(node.Nodes))
                    yield return child;
            }
        }
        public static void StretchLastColumn(this DataGridView dataGridView) {
            dataGridView.AutoResizeColumns();
            var lastColIndex = dataGridView.Columns.Count - 1;
            var lastCol = dataGridView.Columns[lastColIndex];
            lastCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }
        public static string GetDigits(this string input) {
            return new string(input.Where(char.IsDigit).ToArray());
        }
        public static string Format(this string input, params string[] args) {
            return string.Format(input, args);
        }
        public static IEnumerable<string> SplitToLines(this string input) {
            if (input == null) {
                yield break;
            }
            using (System.IO.StringReader reader = new System.IO.StringReader(input)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    yield return line;
                }
            }
        }
        public static string ToTitleCase(this string source, string langCode = "en-US") {
            return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp) {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static bool IsNullOrEmpty(this string source) {
            return string.IsNullOrEmpty(source);
        }
        public static bool IsNullOrWhiteSpace(this string source) {
            return string.IsNullOrWhiteSpace(source);
        }
        public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
            if (count != -1) return source.Split(new string[] { split }, count, options);
            return source.Split(new string[] { split }, options);
        }
        public static string Remove(this string Source, string Replace) {
            return Source.Replace(Replace, string.Empty);
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
            int place = Source.LastIndexOf(Find);
            if (place == -1)
                return Source;
            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
        public static string EscapeLineBreaks(this string source) {
            return Regex.Replace(source, @"\r\n?|\n", @"\$&");
        }
        public static string Ext(this string text, string extension) {
            return text + "." + extension;
        }
        public static string Quote(this string text) {
            return SurroundWith(text, "\"");
        }
        public static string Enclose(this string text) {
            return SurroundWith(text, "(", ")");
        }
        public static string Brackets(this string text) {
            return SurroundWith(text, "[", "]");
        }
        public static string SurroundWith(this string text, string surrounds) {
            return surrounds + text + surrounds;
        }
        public static string SurroundWith(this string text, string starts, string ends) {
            return starts + text + ends;
        }
        public static string RemoveInvalidFileNameChars(this string filename) {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }
        public static string ReplaceInvalidFileNameChars(this string filename) {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
        public static string TrimStart(this string inputText, string value, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase) {
            if (!string.IsNullOrEmpty(value)) {
                while (!string.IsNullOrEmpty(inputText) && inputText.StartsWith(value, comparisonType)) {
                    inputText = inputText.Substring(value.Length - 1);
                }
            }
            return inputText;
        }
        public static string TrimEnd(this string inputText, string value, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase) {
            if (!string.IsNullOrEmpty(value)) {
                while (!string.IsNullOrEmpty(inputText) && inputText.EndsWith(value, comparisonType)) {
                    inputText = inputText.Substring(0, (inputText.Length - value.Length));
                }
            }
            return inputText;
        }
        public static string Trim(this string inputText, string value, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase) {
            return TrimStart(TrimEnd(inputText, value, comparisonType), value, comparisonType);
        }
        public static int Percentage(this int total, int part) {
            return (int)((double)part / total * 100);
        }
        public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, value);
        }
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
   => self.Select((item, index) => (item, index));
        public static string ToQueryString(this NameValueCollection nvc) {
            if (nvc == null) return string.Empty;
            StringBuilder sb = new StringBuilder();
            foreach (string key in nvc.Keys) {
                if (string.IsNullOrWhiteSpace(key)) continue;
                string[] values = nvc.GetValues(key);
                if (values == null) continue;
                foreach (string value in values) {
                    sb.Append(sb.Length == 0 ? "?" : "&");
                    sb.AppendFormat("{0}={1}", key, value);
                }
            }
            return sb.ToString();
        }
        public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
            if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
            var trueValues = new string[] { true.ToString(), "yes", "1" };
            if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
            var falseValues = new string[] { false.ToString(), "no", "0" };
            if (falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
            return defaultValue;
        }
        public static string GetString(this NameValueCollection collection, string key) {
            if (!collection.AllKeys.Contains(key)) return collection[key];
            return null;
        }
        public static string Join(this List<string> strings, string separator) {
            return string.Join(separator, strings);
        }
        public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);
        public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);
        public static T PopAt<T>(this List<T> list, int index) {
            T r = list.ElementAt<T>(index);
            list.RemoveAt(index);
            return r;
        }
        public static bool ContainsKey(this NameValueCollection collection, string key) {
            if (collection.Get(key) == null) {
                return collection.AllKeys.Contains(key);
            }
            return true;
        }
        public static NameValueCollection ParseQueryString(this Uri uri) {
            return HttpUtility.ParseQueryString(uri.Query);
        }
        public static void OpenIn(this Uri uri, string browser) {
            var url = uri.ToString();
            try
            {
                Process.Start(browser, url);
            }
            catch
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start \"{browser}\" {url}") { CreateNoWindow = true });
            }
        }
        public static void OpenInDefault(this Uri uri) {
            var url = uri.ToString();
            try
            {
                Process.Start(url);
            }
            catch
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
        }
        public static string GetDescription(this Enum value) {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null) {
                FieldInfo field = type.GetField(name);
                if (field != null) {
                    DescriptionAttribute attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null) {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
        public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields()) {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null) {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            if (returnDefault) return default(T);
            else throw new ArgumentException("Not found.", "description");
        }
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task) {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;  
                }
                else
                {
                    return default(TResult);
                }
            }
        }
        public static Bitmap Resize(this Image image, int width, int height) {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (var graphics = Graphics.FromImage(destImage)) {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (var wrapMode = new ImageAttributes()) {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }
        public static Color Invert(this Color input) {
            return Color.FromArgb(input.ToArgb() ^ 0xffffff);
        }
        public static string ToYesNo(this bool input) => input ? "Yes" : "No";
        public static string ToEnabledDisabled(this bool input) => input ? "Enabled" : "Disabled";
        public static string ToOnOff(this bool input) => input ? "On" : "Off";
        public static bool ExpiredSince(this DateTime dateTime, int minutes) {
            return (dateTime - DateTime.Now).TotalMinutes < minutes;
        }
        public static TimeSpan StripMilliseconds(this TimeSpan time) {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
        public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
            string final = dir.FullName;
            foreach (string path in paths) {
                final = Path.Combine(final, path);
            }
            return new DirectoryInfo(final);
        }
        public static FileInfo Combine(this FileInfo file, params string[] paths) {
            string final = file.DirectoryName;
            foreach (string path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string FileNameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        public static void AppendLine(this FileInfo file, string line) {
            try {
                if (!file.Exists) {
                    _ = file.Create();
                }
                File.AppendAllLines(file.FullName, new string[] { line });
            } catch { }
        }
        public static string ReadAllText(this FileInfo file) {
            return File.ReadAllText(file.FullName);
        }
        public static List<string> ReadAllLines(this FileInfo file) {
            return File.ReadAllLines(file.FullName).ToList();
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp) {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static bool IsNullOrEmpty(this string source) {
            return string.IsNullOrEmpty(source);
        }
        public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
            return count != -1 ? source.Split(new string[] { split }, count, options) : source.Split(new string[] { split }, options);
        }
        public static string Remove(this string Source, string Replace) {
            return Source.Replace(Replace, string.Empty);
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
            int place = Source.LastIndexOf(Find);
            if (place == -1) {
                return Source;
            }
            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
        public static string Ext(this string text, string extension) {
            return text + "." + extension;
        }
        public static string Quote(this string text) {
            return SurroundWith(text, "\"");
        }
        public static string Enclose(this string text) {
            return SurroundWith(text, "(", ")");
        }
        public static string Brackets(this string text) {
            return SurroundWith(text, "[", "]");
        }
        public static string SurroundWith(this string text, string surrounds) {
            return surrounds + text + surrounds;
        }
        public static string SurroundWith(this string text, string starts, string ends) {
            return starts + text + ends;
        }
        public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
            if (!dictionary.ContainsKey(key)) {
                dictionary.Add(key, value);
            }
        }
        public static string ToQueryString(this NameValueCollection nvc) {
            if (nvc == null) {
                return string.Empty;
            }
            StringBuilder sb = new();
            foreach (string key in nvc.Keys) {
                if (string.IsNullOrWhiteSpace(key)) {
                    continue;
                }
                string[] values = nvc.GetValues(key);
                if (values == null) {
                    continue;
                }
                foreach (string value in values) {
                    _ = sb.Append(sb.Length == 0 ? "?" : "&");
                    _ = sb.AppendFormat("{0}={1}", key, value);
                }
            }
            return sb.ToString();
        }
        public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
            if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) {
                return false;
            }
            string[] trueValues = new string[] { true.ToString(), "yes", "1" };
            if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) {
                return true;
            }
            string[] falseValues = new string[] { false.ToString(), "no", "0" };
            return falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase) || defaultValue;
        }
        public static string GetString(this NameValueCollection collection, string key) {
            return !collection.AllKeys.Contains(key) ? collection[key] : null;
        }
        public static T PopFirst<T>(this IEnumerable<T> list) {
            return list.ToList().PopAt(0);
        }
        public static T PopLast<T>(this IEnumerable<T> list) {
            return list.ToList().PopAt(list.Count() - 1);
        }
        public static T PopAt<T>(this List<T> list, int index) {
            T r = list.ElementAt<T>(index);
            list.RemoveAt(index);
            return r;
        }
        public static readonly Regex QueryRegex = new(@"[?&](\w[\w.]*)=([^?&]+)");
        public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
            Match match = QueryRegex.Match(uri.PathAndQuery);
            Dictionary<string, string> paramaters = new();
            while (match.Success) {
                paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
                match = match.NextMatch();
            }
            return paramaters;
        }
        public static Dictionary<string, string> ParseQueryString(this string queryString) {
            NameValueCollection nvc = HttpUtility.ParseQueryString(queryString);
            return nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
        }
        public static Dictionary<string, string> GetQueryDict(this Uri uri) => uri.Query.ParseQueryString();
        public static string GetQueryValue(this Uri uri, string key) {
            var dict = uri.GetQueryDict();
            if (dict.ContainsKey(key)) {
                return dict[key].Replace("%3A", ":").Replace(" ", "+");
            }
            return null;
        }
        public static string GetDescription(this Enum value) {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null) {
                FieldInfo field = type.GetField(name);
                if (field != null) {
                    if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr) {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
        public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
            Type type = typeof(T);
            if (!type.IsEnum) {
                throw new InvalidOperationException();
            }
            foreach (FieldInfo field in type.GetFields()) {
                if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute) {
                    if (attribute.Description == description) {
                        return (T)field.GetValue(null);
                    }
                } else {
                    if (field.Name == description) {
                        return (T)field.GetValue(null);
                    }
                }
            }
            return returnDefault ? default : throw new ArgumentException("Not found.", "description");
        }
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
            using CancellationTokenSource timeoutCancellationTokenSource = new();
            Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  
            } else {
                return default;
            }
        }
        public static bool ExpiredSince(this DateTime dateTime, int minutes) {
            return (dateTime - DateTime.Now).TotalMinutes < minutes;
        }
        public static TimeSpan StripMilliseconds(this TimeSpan time) {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
        public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
            string final = dir.FullName;
            foreach (string path in paths) {
                final = Path.Combine(final, path);
            }
            return new DirectoryInfo(final);
        }
        public static FileInfo Combine(this FileInfo file, params string[] paths) {
            string final = file.DirectoryName;
            foreach (string path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string FileNameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        public static void AppendLine(this FileInfo file, string line) {
            try {
                if (!file.Exists) {
                    _ = file.Create();
                }
                File.AppendAllLines(file.FullName, new string[] { line });
            } catch { }
        }
        public static string ReadAllText(this FileInfo file) {
            return File.ReadAllText(file.FullName);
        }
        public static List<string> ReadAllLines(this FileInfo file) {
            return File.ReadAllLines(file.FullName).ToList();
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp) {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static bool IsNullOrEmpty(this string source) {
            return string.IsNullOrEmpty(source);
        }
        public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
            return count != -1 ? source.Split(new string[] { split }, count, options) : source.Split(new string[] { split }, options);
        }
        public static string Remove(this string Source, string Replace) {
            return Source.Replace(Replace, string.Empty);
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
            int place = Source.LastIndexOf(Find);
            if (place == -1) {
                return Source;
            }
            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
        public static string Ext(this string text, string extension) {
            return text + "." + extension;
        }
        public static string Quote(this string text) {
            return SurroundWith(text, "\"");
        }
        public static string Enclose(this string text) {
            return SurroundWith(text, "(", ")");
        }
        public static string Brackets(this string text) {
            return SurroundWith(text, "[", "]");
        }
        public static string SurroundWith(this string text, string surrounds) {
            return surrounds + text + surrounds;
        }
        public static string SurroundWith(this string text, string starts, string ends) {
            return starts + text + ends;
        }
        public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
            if (!dictionary.ContainsKey(key)) {
                dictionary.Add(key, value);
            }
        }
        public static string ToQueryString(this NameValueCollection nvc) {
            if (nvc == null) {
                return string.Empty;
            }
            StringBuilder sb = new();
            foreach (string key in nvc.Keys) {
                if (string.IsNullOrWhiteSpace(key)) {
                    continue;
                }
                string[] values = nvc.GetValues(key);
                if (values == null) {
                    continue;
                }
                foreach (string value in values) {
                    _ = sb.Append(sb.Length == 0 ? "?" : "&");
                    _ = sb.AppendFormat("{0}={1}", key, value);
                }
            }
            return sb.ToString();
        }
        public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
            if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) {
                return false;
            }
            string[] trueValues = new string[] { true.ToString(), "yes", "1" };
            if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) {
                return true;
            }
            string[] falseValues = new string[] { false.ToString(), "no", "0" };
            return falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase) || defaultValue;
        }
        public static string GetString(this NameValueCollection collection, string key) {
            return !collection.AllKeys.Contains(key) ? collection[key] : null;
        }
        public static T PopFirst<T>(this IEnumerable<T> list) {
            return list.ToList().PopAt(0);
        }
        public static T PopLast<T>(this IEnumerable<T> list) {
            return list.ToList().PopAt(list.Count() - 1);
        }
        public static T PopAt<T>(this List<T> list, int index) {
            T r = list.ElementAt<T>(index);
            list.RemoveAt(index);
            return r;
        }
        public static readonly Regex QueryRegex = new(@"[?&](\w[\w.]*)=([^?&]+)");
        public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
            Match match = QueryRegex.Match(uri.PathAndQuery);
            Dictionary<string, string> paramaters = new();
            while (match.Success) {
                paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
                match = match.NextMatch();
            }
            return paramaters;
        }
        public static Dictionary<string, string> ParseQueryString(this string queryString) {
            NameValueCollection nvc = HttpUtility.ParseQueryString(queryString);
            return nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
        }
        public static Dictionary<string, string> GetQueryDict(this Uri uri) => uri.Query.ParseQueryString();
        public static string GetQueryValue(this Uri uri, string key) {
            var dict = uri.GetQueryDict();
            if (dict.ContainsKey(key)) {
                return dict[key].Replace("%3A", ":").Replace(" ", "+");
            }
            return null;
        }
        public static string GetDescription(this Enum value) {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null) {
                FieldInfo field = type.GetField(name);
                if (field != null) {
                    if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr) {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
        public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
            Type type = typeof(T);
            if (!type.IsEnum) {
                throw new InvalidOperationException();
            }
            foreach (FieldInfo field in type.GetFields()) {
                if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute) {
                    if (attribute.Description == description) {
                        return (T)field.GetValue(null);
                    }
                } else {
                    if (field.Name == description) {
                        return (T)field.GetValue(null);
                    }
                }
            }
            return returnDefault ? default : throw new ArgumentException("Not found.", "description");
        }
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
            using CancellationTokenSource timeoutCancellationTokenSource = new();
            Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task) {
                timeoutCancellationTokenSource.Cancel();
                return await task;  
            } else {
                return default;
            }
        }
        public const UInt32 FnvPrime = 0xB3CB2E29; public const UInt32 FnvOffsetBasis = 0x319712C3;
        public static string ToHashFnv1a32(this string text, Fnv1a32 hasher = null) {
            text = text.Trim().ToLowerInvariant() + "\0";
            var bytes_encoded = Encoding.ASCII.GetBytes(text);
            if (hasher is null) hasher = new Fnv1a32(fnvPrime: FnvPrime, fnvOffsetBasis: FnvOffsetBasis);
            var byte_hash = hasher.ComputeHash(bytes_encoded);
            var uint32 = BitConverter.ToUInt32(byte_hash, 0);
            var uint32_hex = string.Format("0x{0:X}", uint32);
            return uint32_hex;
        }
        public static bool IsHash(this string source) {
            return source.StartsWith("0x");
        }
  public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
    return instanceToConvert.GetType()
      .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
      .ToDictionary(
      propertyInfo => propertyInfo.Name,
      propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
  }
  public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
    Type propertyType = propertyInfo.PropertyType;
    object propertyValue = propertyInfo.GetValue(owner);
    if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
      var collectionItems = new List<Dictionary<string, object>>();
      var count = (int) propertyType.GetProperty("Count").GetValue(propertyValue);
      PropertyInfo indexerProperty = propertyType.GetProperty("Item");
      for (var index = 0; index < count; index++) {
        object item = indexerProperty.GetValue(propertyValue, new object[] { index });
        PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        if (itemProperties.Any()) {
          Dictionary<string, object> dictionary = itemProperties
            .ToDictionary(
              subtypePropertyInfo => subtypePropertyInfo.Name,
              subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
          collectionItems.Add(dictionary);
        }
      }
      return collectionItems;
    }
    if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
      return propertyValue;
    }
    PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
    if (properties.Any()) {
      return properties.ToDictionary(
                          subtypePropertyInfo => subtypePropertyInfo.Name, 
                          subtypePropertyInfo => (object) Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
    }
    return propertyValue;
  }
        public static bool ExpiredSince(this DateTime dateTime, int minutes) {
            return (dateTime - DateTime.Now).TotalMinutes < minutes;
        }
        public static TimeSpan StripMilliseconds(this TimeSpan time) {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
        public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new DirectoryInfo(final);
        }
        public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static FileInfo Combine(this FileInfo file, params string[] paths) {
            var final = file.DirectoryName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string FileNameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        public static void AppendLine(this FileInfo file, string line) {
            try {
                if (!file.Exists) file.Create();
                File.AppendAllLines(file.FullName, new string[] { line });
            } catch { }
        }
        public static void WriteAllText(this FileInfo file, string text) => File.WriteAllText(file.FullName, text);
        public static string ReadAllText(this FileInfo file) => File.ReadAllText(file.FullName);
        public static List<string> ReadAllLines(this FileInfo file) => File.ReadAllLines(file.FullName).ToList();
        public static IEnumerable<TreeNode> GetAllChilds(this TreeNodeCollection nodes) {
            foreach (TreeNode node in nodes) {
                yield return node;
                foreach (var child in GetAllChilds(node.Nodes))
                    yield return child;
            }
        }
        public static string ToJson(this object obj, bool indented = true) {
            return JsonConvert.SerializeObject(obj, (indented ? Formatting.Indented : Formatting.None), new JsonConverter[] { new StringEnumConverter() });
        }
    public static IEnumerable<string> SplitToLines(this string input) {
        if (input == null) {
            yield break;
        }
        using (System.IO.StringReader reader = new System.IO.StringReader(input)) {
            string line;
            while( (line = reader.ReadLine()) != null) {
                yield return line;
            }
        }
    }
        public static string ToTitleCase(this string source, string langCode = "en-US") {
            return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp) {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static bool IsNullOrEmpty(this string source) {
            return string.IsNullOrEmpty(source);
        }
        public static string[] Split(this string source, string split, int count=-1, StringSplitOptions options = StringSplitOptions.None) {
            if (count != -1) return source.Split(new string[]{split}, count, options);
            return source.Split(new string[]{split}, options);
        }
        public static string Remove(this string Source, string Replace) {
            return Source.Replace(Replace, string.Empty);
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
            int place = Source.LastIndexOf(Find);
            if(place == -1)
                return Source;
            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
        public static string EscapeLineBreaks(this string source) {
             return Regex.Replace(source, @"\r\n?|\n", @"\$&");
        }
        public static string Ext(this string text, string extension) {
            return text + "." + extension;
        }
        public static string Quote(this string text) {
            return SurroundWith(text, "\"");
        }
        public static string Enclose(this string text) {
            return SurroundWith(text, "(",")");
        }
        public static string Brackets(this string text) {
            return SurroundWith(text, "[","]");
        }
        public static string SurroundWith(this string text, string surrounds) {
            return surrounds + text + surrounds;
        }
        public static string SurroundWith(this string text, string starts, string ends) {
            return starts + text + ends;
        }
        public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, value);
        }
        public static string ToQueryString(this NameValueCollection nvc) {
        if (nvc == null) return string.Empty;
        StringBuilder sb = new StringBuilder();
        foreach (string key in nvc.Keys) {
        if (string.IsNullOrWhiteSpace(key)) continue;
        string[] values = nvc.GetValues(key);
        if (values == null) continue;
        foreach (string value in values) {
            sb.Append(sb.Length == 0 ? "?" : "&");
            sb.AppendFormat("{0}={1}", key, value);
        }
        }
        return sb.ToString();
        }
        public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
            if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
            var trueValues = new string[] { true.ToString(), "yes", "1" };
            if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
            var falseValues = new string[] { false.ToString(), "no", "0" };
            if (falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
            return defaultValue;
        }
        public static string GetString(this NameValueCollection collection, string key) {
            if (!collection.AllKeys.Contains(key)) return collection[key];
            return null;
        }
        public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);
        public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);
        public static T PopAt<T>(this List<T> list, int index) {
            T r = list.ElementAt<T>(index);
            list.RemoveAt(index);
            return r;
        }
        public static readonly Regex QueryRegex = new Regex(@"[?&](\w[\w.]*)=([^?&]+)");
        public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
            var match = QueryRegex.Match(uri.PathAndQuery);
            var paramaters = new Dictionary<string, string>();
            while (match.Success) {
                paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
                match = match.NextMatch();
            }
            return paramaters;
        }
        public static string GetDescription(this Enum value) {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null) {
                FieldInfo field = type.GetField(name);
                if (field != null) {
                    DescriptionAttribute attr =  Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null) {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
    public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
        var type = typeof(T);
        if(!type.IsEnum) throw new InvalidOperationException();
        foreach(var field in type.GetFields()) {
            var attribute = Attribute.GetCustomAttribute(field,
                typeof(DescriptionAttribute)) as DescriptionAttribute;
            if(attribute != null) {
                if(attribute.Description == description)
                    return (T)field.GetValue(null);
            }
            else
            {
                if(field.Name == description)
                    return (T)field.GetValue(null);
            }
        }
        if (returnDefault) return default(T);
        else throw new ArgumentException("Not found.", "description");
    }
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task) {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;  
                } else {
                    return default(TResult);
                }
            }
        }
        public static string ToYesNo(this bool input) => input ? "Yes" : "No";
        public static string ToEnabledDisabled(this bool input) => input ? "Enabled" : "Disabled";
        public static string ToOnOff(this bool input) => input ? "On" : "Off";
        public const string Base64Prefix = "data:image/";
        public static Image ImageFromBase64(this string base64String) {
            base64String = base64String.Split(";base64,").Last(); 
            var converted = Convert.FromBase64String(base64String);
            using (MemoryStream ms = new MemoryStream(converted)) return Image.FromStream(ms);
        }
        public static Task<Image> GetImageAsync(this Uri uri) {
            using (var httpClient = new HttpClient()) {
                var byteArray = httpClient.GetByteArrayAsync(uri).Result;
                return Task.FromResult(Image.FromStream(new MemoryStream(byteArray)));
            }
        }
        public static Image? ParseImage(this string input) {
            if (input.StartsWith(Base64Prefix, StringComparison.OrdinalIgnoreCase)) {
                try { return ImageFromBase64(input); } catch (Exception ex) { Console.WriteLine(ex.Message); return null; }
            }
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) {
                try { return GetImageAsync(uri).Result; } catch (Exception ex) { Console.WriteLine(ex.Message); return null; }
            }
            return null;
        }
        public static Image Resize(this Image imgToResize, Size size) => new Bitmap(imgToResize, size) as Image;
        public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
            return instanceToConvert.GetType()
              .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
              .ToDictionary(
              propertyInfo => propertyInfo.Name,
              propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
        }
        public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
            Type propertyType = propertyInfo.PropertyType;
            object propertyValue = propertyInfo.GetValue(owner);
            if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
                var collectionItems = new List<Dictionary<string, object>>();
                var count = (int)propertyType.GetProperty("Count").GetValue(propertyValue);
                PropertyInfo indexerProperty = propertyType.GetProperty("Item");
                for (var index = 0; index < count; index++) {
                    object item = indexerProperty.GetValue(propertyValue, new object[] { index });
                    PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                    if (itemProperties.Any()) {
                        Dictionary<string, object> dictionary = itemProperties
                          .ToDictionary(
                            subtypePropertyInfo => subtypePropertyInfo.Name,
                            subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
                        collectionItems.Add(dictionary);
                    }
                }
                return collectionItems;
            }
            if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
                return propertyValue;
            }
            PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (properties.Any()) {
                return properties.ToDictionary(
                                    subtypePropertyInfo => subtypePropertyInfo.Name,
                                    subtypePropertyInfo => (object)Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
            }
            return propertyValue;
        }
        public static bool ExpiredSince(this DateTime dateTime, int minutes) {
            return (dateTime - DateTime.Now).TotalMinutes < minutes;
        }
        public static TimeSpan StripMilliseconds(this TimeSpan time) {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
        public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new DirectoryInfo(final);
        }
        public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static FileInfo Combine(this FileInfo file, params string[] paths) {
            var final = file.DirectoryName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string FileNameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        public static void AppendLine(this FileInfo file, string line) {
            try {
                if (!file.Exists) file.Create();
                File.AppendAllLines(file.FullName, new string[] { line });
            } catch { }
        }
        public static void WriteAllText(this FileInfo file, string text) => File.WriteAllText(file.FullName, text);
        public static string ReadAllText(this FileInfo file) => File.ReadAllText(file.FullName);
        public static List<string> ReadAllLines(this FileInfo file) => File.ReadAllLines(file.FullName).ToList();
        public static string ToJson(this object obj, bool indented = true) {
            var options = new JsonSerializerOptions {
                WriteIndented = indented,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Serialize(obj, options);
        }
        public static IEnumerable<string> SplitToLines(this string input) {
            if (input == null) {
                yield break;
            }
            using (System.IO.StringReader reader = new System.IO.StringReader(input)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    yield return line;
                }
            }
        }
        public static string ToTitleCase(this string source, string langCode = "en-US") {
            return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp) {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static bool IsNullOrEmpty(this string source) {
            return string.IsNullOrEmpty(source);
        }
        public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
            if (count != -1) return source.Split(new string[] { split }, count, options);
            return source.Split(new string[] { split }, options);
        }
        public static string Remove(this string Source, string Replace) {
            return Source.Replace(Replace, string.Empty);
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
            int place = Source.LastIndexOf(Find);
            if (place == -1)
                return Source;
            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
        public static string EscapeLineBreaks(this string source) {
            return Regex.Replace(source, @"\r\n?|\n", @"\$&");
        }
        public static string Ext(this string text, string extension) {
            return text + "." + extension;
        }
        public static string Quote(this string text) {
            return SurroundWith(text, "\"");
        }
        public static string Enclose(this string text) {
            return SurroundWith(text, "(", ")");
        }
        public static string Brackets(this string text) {
            return SurroundWith(text, "[", "]");
        }
        public static string SurroundWith(this string text, string surrounds) {
            return surrounds + text + surrounds;
        }
        public static string SurroundWith(this string text, string starts, string ends) {
            return starts + text + ends;
        }
        public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, value);
        }
        public static string ToQueryString(this NameValueCollection nvc) {
            if (nvc == null) return string.Empty;
            StringBuilder sb = new StringBuilder();
            foreach (string key in nvc.Keys) {
                if (string.IsNullOrWhiteSpace(key)) continue;
                string[] values = nvc.GetValues(key);
                if (values == null) continue;
                foreach (string value in values) {
                    sb.Append(sb.Length == 0 ? "?" : "&");
                    sb.AppendFormat("{0}={1}", key, value);
                }
            }
            return sb.ToString();
        }
        public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
            if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
            var trueValues = new string[] { true.ToString(), "yes", "1" };
            if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
            var falseValues = new string[] { false.ToString(), "no", "0" };
            if (falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
            return defaultValue;
        }
        public static string GetString(this NameValueCollection collection, string key) {
            if (!collection.AllKeys.Contains(key)) return collection[key];
            return null;
        }
        public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);
        public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);
        public static T PopAt<T>(this List<T> list, int index) {
            T r = list.ElementAt<T>(index);
            list.RemoveAt(index);
            return r;
        }
        public static readonly Regex QueryRegex = new Regex(@"[?&](\w[\w.]*)=([^?&]+)");
        public static IReadOnlyDictionary<string, string> ParseQueryString(this Uri uri) {
            var match = QueryRegex.Match(uri.PathAndQuery);
            var paramaters = new Dictionary<string, string>();
            while (match.Success) {
                paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
                match = match.NextMatch();
            }
            return paramaters;
        }
        public static string GetDescription(this Enum value) {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null) {
                FieldInfo field = type.GetField(name);
                if (field != null) {
                    DescriptionAttribute attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null) {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
        public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields()) {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null) {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                } else {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            if (returnDefault) return default(T);
            else throw new ArgumentException("Not found.", "description");
        }
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task) {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;  
                } else {
                    return default(TResult);
                }
            }
        }
        public static string ToYesNo(this bool input) => input ? "Yes" : "No";
        public static string ToEnabledDisabled(this bool input) => input ? "Enabled" : "Disabled";
        public static string ToOnOff(this bool input) => input ? "On" : "Off";
        public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
            return instanceToConvert.GetType()
              .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
              .ToDictionary(
              propertyInfo => propertyInfo.Name,
              propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
        }
        public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
            Type propertyType = propertyInfo.PropertyType;
            object propertyValue = propertyInfo.GetValue(owner);
            if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
                var collectionItems = new List<Dictionary<string, object>>();
                var count = (int)propertyType.GetProperty("Count").GetValue(propertyValue);
                PropertyInfo indexerProperty = propertyType.GetProperty("Item");
                for (var index = 0; index < count; index++) {
                    object item = indexerProperty.GetValue(propertyValue, new object[] { index });
                    PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                    if (itemProperties.Any()) {
                        Dictionary<string, object> dictionary = itemProperties
                          .ToDictionary(
                            subtypePropertyInfo => subtypePropertyInfo.Name,
                            subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
                        collectionItems.Add(dictionary);
                    }
                }
                return collectionItems;
            }
            if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
                return propertyValue;
            }
            PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (properties.Any()) {
                return properties.ToDictionary(
                                    subtypePropertyInfo => subtypePropertyInfo.Name,
                                    subtypePropertyInfo => (object)Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
            }
            return propertyValue;
        }
        public static bool ExpiredSince(this DateTime dateTime, int minutes) {
            return (dateTime - DateTime.Now).TotalMinutes < minutes;
        }
        public static TimeSpan StripMilliseconds(this TimeSpan time) {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
        public static DirectoryInfo Combine(this Environment.SpecialFolder specialFolder, params string[] paths) => Combine(new DirectoryInfo(Environment.GetFolderPath(specialFolder)), paths);
        public static FileInfo CombineFile(this Environment.SpecialFolder specialFolder, params string[] paths) => CombineFile(new DirectoryInfo(Environment.GetFolderPath(specialFolder)), paths);
        public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path.ReplaceInvalidFileNameChars());
            }
            return new DirectoryInfo(final);
        }
        public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string PrintablePath(this FileSystemInfo file) => file.FullName.Replace(@"\\", @"\");
        public static FileInfo CopyTo(this FileInfo file, FileInfo destination, bool overwrite = true) {
            try { return file.CopyTo(destination.FullName, overwrite); } catch (IOException) { return destination; }
        }
        public static FileInfo Backup(this FileInfo file, bool overwrite = true, string extension = ".bak") {
            var bakPath = new FileInfo(file.FullName + extension);
            try { return file.CopyTo(bakPath, overwrite); } catch (IOException) { return bakPath; }
        }
        public static FileInfo Combine(this FileInfo file, params string[] paths) {
            var final = file.DirectoryName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string FileNameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        public static void AppendLine(this FileInfo file, string line) {
            try {
                if (!file.Exists) file.Create();
                File.AppendAllLines(file.FullName, new string[] { line });
            } catch { }
        }
        public static void WriteAllText(this FileInfo file, string text, bool overwrite = true) {
            file.Directory.Create();
            if (file.Exists && !overwrite) return;
            if (!file.Exists) file.Create().Close();
            File.WriteAllText(file.FullName, text);
        }
        public static void WriteAllBytes(this FileInfo file, byte[] bytes, bool overwrite = true) {
            file.Directory.Create();
            if (file.Exists && !overwrite) return;
            if (!file.Exists) file.Create().Close();
            File.WriteAllBytes(file.FullName, bytes);
        }
        public static string ReadAllText(this FileInfo file) => File.ReadAllText(file.FullName);
        public static List<string> ReadAllLines(this FileInfo file) => File.ReadAllLines(file.FullName).ToList();
        public static string GetDigits(this string input) {
            return new string(input.Where(char.IsDigit).ToArray());
        }
        public static string Format(this string input, params string[] args) {
            return string.Format(input, args);
        }
        public static IEnumerable<string> SplitToLines(this string input) {
            if (input == null) {
                yield break;
            }
            using (System.IO.StringReader reader = new System.IO.StringReader(input)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    yield return line;
                }
            }
        }
        public static string ToTitleCase(this string source, string langCode = "en-US") {
            return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp) {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static bool IsNullOrEmpty(this string source) {
            return string.IsNullOrEmpty(source);
        }
        public static bool IsNullOrWhiteSpace(this string source) {
            return string.IsNullOrWhiteSpace(source);
        }
        public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
            if (count != -1) return source.Split(new string[] { split }, count, options);
            return source.Split(new string[] { split }, options);
        }
        public static string Remove(this string Source, string Replace) {
            return Source.Replace(Replace, string.Empty);
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
            int place = Source.LastIndexOf(Find);
            if (place == -1)
                return Source;
            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
        public static string EscapeLineBreaks(this string source) {
            return Regex.Replace(source, @"\r\n?|\n", @"\$&");
        }
        public static string Ext(this string text, string extension) {
            return text + "." + extension;
        }
        public static string Quote(this string text) {
            return SurroundWith(text, "\"");
        }
        public static string Enclose(this string text) {
            return SurroundWith(text, "(", ")");
        }
        public static string Brackets(this string text) {
            return SurroundWith(text, "[", "]");
        }
        public static string SurroundWith(this string text, string surrounds) {
            return surrounds + text + surrounds;
        }
        public static string SurroundWith(this string text, string starts, string ends) {
            return starts + text + ends;
        }
        public static string RemoveInvalidFileNameChars(this string filename) {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }
        public static string ReplaceInvalidFileNameChars(this string filename) {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
        public static int Percentage(this int total, int part) {
            return (int)((double)part / total * 100);
        }
        public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, value);
        }
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
   => self.Select((item, index) => (item, index));
        public static string ToQueryString(this NameValueCollection nvc) {
            if (nvc == null) return string.Empty;
            StringBuilder sb = new StringBuilder();
            foreach (string key in nvc.Keys) {
                if (string.IsNullOrWhiteSpace(key)) continue;
                string[] values = nvc.GetValues(key);
                if (values == null) continue;
                foreach (string value in values) {
                    sb.Append(sb.Length == 0 ? "?" : "&");
                    sb.AppendFormat("{0}={1}", key, value);
                }
            }
            return sb.ToString();
        }
        public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
            if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
            var trueValues = new string[] { true.ToString(), "yes", "1" };
            if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
            var falseValues = new string[] { false.ToString(), "no", "0" };
            if (falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
            return defaultValue;
        }
        public static string GetString(this NameValueCollection collection, string key) {
            if (!collection.AllKeys.Contains(key)) return collection[key];
            return null;
        }
        public static string Join(this List<string> strings, string separator) {
            return string.Join(separator, strings);
        }
        public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);
        public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);
        public static T PopAt<T>(this List<T> list, int index) {
            T r = list.ElementAt<T>(index);
            list.RemoveAt(index);
            return r;
        }
        public static bool ContainsKey(this NameValueCollection collection, string key) {
            if (collection.Get(key) == null) {
                return collection.AllKeys.Contains(key);
            }
            return true;
        }
        public static void OpenIn(this Uri uri, string browser) {
            var url = uri.ToString();
            try {
                Process.Start(browser, url);
            } catch {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start \"{browser}\" {url}") { CreateNoWindow = true });
            }
        }
        public static void OpenInDefault(this Uri uri) {
            var url = uri.ToString();
            try {
                Process.Start(url);
            } catch {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
        }
        public static string GetDescription(this Enum value) {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null) {
                FieldInfo field = type.GetField(name);
                if (field != null) {
                    DescriptionAttribute attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null) {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
        public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields()) {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null) {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                } else {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            if (returnDefault) return default(T);
            else throw new ArgumentException("Not found.", "description");
        }
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task) {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;  
                } else {
                    return default(TResult);
                }
            }
        }
        public static string ToYesNo(this bool input) => input ? "Yes" : "No";
        public static string ToEnabledDisabled(this bool input) => input ? "Enabled" : "Disabled";
        public static string ToOnOff(this bool input) => input ? "On" : "Off";
        public static string GetCommandLine(this Process process) {
            if (process == null) {
                throw new ArgumentNullException(nameof(process));
            }
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
                using (var objects = searcher.Get()) {
                    var result = objects.Cast<ManagementBaseObject>().SingleOrDefault();
                    return result?["CommandLine"]?.ToString() ?? string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }
        public static IEnumerable<Cookie> GetAllCookies(this CookieContainer c) {
            Hashtable k = (Hashtable)c.GetType().GetField("m_domainTable", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(c);
            foreach (DictionaryEntry element in k) {
                SortedList l = (SortedList)element.Value.GetType().GetField("m_list", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(element.Value);
                foreach (var e in l) {
                    var cl = (CookieCollection)((DictionaryEntry)e).Value;
                    foreach (Cookie fc in cl) {
                        yield return fc;
                    }
                }
            }
        }
        public static Uri AddQuery(this Uri uri, string key, string value, bool encode = true) {
            var uriBuilder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            if (encode) {
                query[key] = value;
                uriBuilder.Query = query.ToString();
            }
            else
            {
                var queryDict = query.AllKeys.Where(k => k != null).ToDictionary(k => k, k => query[k]);
                queryDict[key] = value;
                var queryString = string.Join("&", queryDict.Select(kvp => $"{HttpUtility.UrlEncode(kvp.Key)}={(kvp.Key == key ? value : HttpUtility.UrlEncode(kvp.Value))}"));
                uriBuilder.Query = queryString;
            }
            return uriBuilder.Uri;
        }
        public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new DirectoryInfo(final);
        }
        public static bool IsEmpty(this DirectoryInfo directory) {
            return !Directory.EnumerateFileSystemEntries(directory.FullName).Any();
        }
        public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static FileInfo Combine(this FileInfo file, params string[] paths) {
            var final = file.DirectoryName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }
        public static string FileNameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        public static bool WriteAllText(this FileInfo file, string content) {
            if (file.Directory != null && !file.Directory.Exists) file.Directory.Create();
            try
            {
                File.WriteAllText(file.FullName, content);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error writing to file {file.FullName}: {ex.Message}");
                return false;
            }
            return true;
        }
        public static string ReadAllText(this FileInfo file) {
            if (file.Directory != null && !file.Directory.Exists) file.Directory.Create();
            if (!file.Exists) return string.Empty;
            return File.ReadAllText(file.FullName);
        }
}
