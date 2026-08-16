using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework.View.Editor
{
    public static class VarBindTool
    {
        private const string VAR_AUTO_BIND_FIELD_START = "#region AUTO_BIND_FIELD";
        private const string VAR_AUTO_BIND_FIELD_END = "#endregion";

        private const string VAR_AUTO_BIND_USING_START = "#region AUTO_BIND_USING";
        private const string VAR_AUTO_BIND_USING_END = "#endregion";

        private const string VARY_TEXT_ALIAS_START = "#region VARY_TEXT_ALIAS";
        private const string VARY_TEXT_ALIAS_END = "#endregion";

        private static readonly Dictionary<Type, MonoScript> _scriptCache = new();

        private static readonly Regex _extractFieldsBlockRegex = new(
            $@"{Regex.Escape(VAR_AUTO_BIND_FIELD_START)}\s*(.*?)\s*{Regex.Escape(VAR_AUTO_BIND_FIELD_END)}",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex _matchSingleFieldRegex = new(
            @"protected\s+[\w<>\[\]]+\s+(?<field>_\w+)\s*=>\s*GetBindField<[^>]+>\(nameof\(\k<field>\)\);",
            RegexOptions.Compiled);

        public static void TraverseTrans(Transform current, Action<Transform> handler, Transform bindRoot)
        {
            if (current != bindRoot)
            {
                handler?.Invoke(current);

                // 遇到挂载了 ViewObject 的节点，停止继续遍历其子节点（避免嵌套 ViewObject 干扰）
                var flag = current.TryGetComponent<ViewObject>(out _);
                if (flag) return;
            }

            foreach (Transform child in current)
                TraverseTrans(child, handler, bindRoot);
        }

        public static bool CollectVarBind(Transform current, Dictionary<string, Type> matchDict,
            out string varName, out Type varType, out Object obj)
        {
            varName = current.name;
            foreach (var (prefix, type) in matchDict)
            {
                if (!current.name.StartsWith(prefix, StringComparison.Ordinal)) continue;

                obj = type switch
                {
                    _ when type == typeof(GameObject) => current.gameObject,
                    _ when type == typeof(Object) => current.gameObject,
                    _ => current.GetComponent(type)
                };

                if (!obj) continue;

                // Node 直接使用子类，其他用配置类型
                var isNode = typeof(INode).IsAssignableFrom(type);
                varType = isNode ? obj.GetType() : type;

                return true;
            }

            varName = string.Empty;
            varType = null;
            obj = null;
            return false;
        }

        public static void EnsureClassIsPartial(string assetPath, string className)
        {
            try
            {
                var fileContent = File.ReadAllText(assetPath);

                var classPattern =
                    $@"(?:^\s*|\b)(?:(?:public|internal|protected|private|sealed|static|abstract|partial)\s+)+\bclass\s+{Regex.Escape(className)}\b(?:\s*<[^>]+>)?(?:\s*:\s*[^{{]+)?\s*\{{";

                var match = Regex.Match(fileContent, classPattern, RegexOptions.Multiline);
                if (!match.Success)
                    throw new Exception($"无法找到类 {className} 的定义");

                var fullClassLine = match.Value;

                if (Regex.IsMatch(fullClassLine, @"\bpartial\b"))
                    return;

                var classIndex = fullClassLine.IndexOf("class", StringComparison.Ordinal);
                if (classIndex == -1)
                    throw new FormatException("无效的类定义格式");

                var hasAttributes = fullClassLine.Contains('[');
                var partialInsertion = hasAttributes ? " partial" : "partial ";

                var modifiedClassLine = fullClassLine.Insert(classIndex, partialInsertion);

                fileContent = fileContent.Replace(fullClassLine, modifiedClassLine);
                File.WriteAllText(assetPath, fileContent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"EnsureClassIsPartial 错误: {ex.Message}\n文件: {assetPath}\n类: {className}");
            }
        }

        public static (string code, HashSet<string> usings) ParseBindingFields(
            List<VarBindData> binds, HashSet<string> excludedFieldNames = null)
        {
            using var sb = ZString.CreateStringBuilder();
            var usingNamespaces = new HashSet<string>();

            var validBinds = binds.Where(b => !string.IsNullOrEmpty(b.Name))
                .Where(b => excludedFieldNames == null || !excludedFieldNames.Contains(b.Name))
                .ToList();

            foreach (var bindData in validBinds)
            {
                if (!string.IsNullOrEmpty(bindData.Type.Namespace) &&
                    !bindData.Type.Namespace.StartsWith("System"))
                {
                    usingNamespaces.Add(bindData.Type.Namespace);
                }

                var realType = bindData.IsMultiple ? $"{bindData.Type.Name}[]" : bindData.Type.Name;
                sb.AppendLine(
                    $"protected {realType} {bindData.Name} => GetBindField<{realType}>(nameof({bindData.Name}));");
                sb.AppendLine();
            }

            return (sb.ToString().TrimEnd(), usingNamespaces);
        }

        public static HashSet<string> CollectParentBindingFieldNames(Type type)
        {
            var parentBindingFields = new HashSet<string>();
            var currentType = type.BaseType;

            while (currentType is { IsAbstract: false })
            {
                var bindingPath = GetBindingFilePathForType(currentType);
                if (bindingPath == null)
                {
                    currentType = currentType.BaseType;
                    continue;
                }

                foreach (var fieldName in ParseBindingFields(bindingPath))
                {
                    parentBindingFields.Add(fieldName);
                }

                currentType = currentType.BaseType;
            }

            return parentBindingFields;
        }

        public static IEnumerable<string> ParseBindingFields(string filePath)
        {
            var content = File.ReadAllText(filePath);

            var blockMatch = _extractFieldsBlockRegex.Match(content);
            if (!blockMatch.Success)
                yield break;

            var fieldsBlock = blockMatch.Groups[1].Value;

            var fieldMatches = _matchSingleFieldRegex.Matches(fieldsBlock);
            foreach (Match match in fieldMatches)
            {
                yield return match.Groups["field"].Value;
            }
        }

        public static void InsertFileContent(string bindingFilePath, string className,
            string namespaceName, string bindingFields, HashSet<string> usingNamespaces)
        {
            if (!File.Exists(bindingFilePath))
            {
                var fileContent = GenerateBindingFileContent(className, namespaceName,
                    bindingFields, usingNamespaces);
                FileWrite(bindingFilePath, fileContent);
                return;
            }

            var content = File.ReadAllText(bindingFilePath);

            if (content.Contains(VAR_AUTO_BIND_FIELD_START) &&
                content.Contains(VAR_AUTO_BIND_FIELD_END))
            {
                content = ProcessUsingStatements(content, usingNamespaces);
                content = ProcessFieldBindings(content, bindingFields);
                FileWrite(bindingFilePath, content);
                return;
            }

            InsertBindingFieldsAtClassStart(bindingFilePath, className, namespaceName, bindingFields, usingNamespaces);
        }

        private static string ProcessUsingStatements(string content, HashSet<string> usingNamespaces)
        {
            if (usingNamespaces == null || usingNamespaces.Count == 0)
                return content;

            var existingUsings = ExtractExistingUsingStatements(content);
            var newUsings = usingNamespaces.Where(ns => !existingUsings.Contains(ns)).ToHashSet();

            if (newUsings.Count == 0)
                return content;

            var usingContent = string.Join("\n", newUsings.OrderBy(ns => ns).Select(ns => $"using {ns};"));

            if (content.Contains(VAR_AUTO_BIND_USING_START) && content.Contains(VAR_AUTO_BIND_USING_END))
            {
                var pattern =
                    $@"{Regex.Escape(VAR_AUTO_BIND_USING_START)}[\s\S]*?{Regex.Escape(VAR_AUTO_BIND_USING_END)}";
                var replacement = $"{VAR_AUTO_BIND_USING_START}\n{usingContent}\n{VAR_AUTO_BIND_USING_END}";
                return Regex.Replace(content, pattern, replacement, RegexOptions.Singleline);
            }

            var lines = content.Split('\n');

            var insertTargetIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("namespace ") || line.StartsWith("public class ") ||
                    line.StartsWith("internal class ") || line.StartsWith("class "))
                {
                    insertTargetIndex = i;
                    break;
                }
            }

            if (insertTargetIndex == -1)
                return content;

            var usingBlockLines = new List<string>
            {
                VAR_AUTO_BIND_USING_START,
                usingContent,
                VAR_AUTO_BIND_USING_END,
                ""
            };

            var resultLines = new List<string>(lines);
            resultLines.InsertRange(insertTargetIndex, usingBlockLines);

            return string.Join("\n", resultLines);
        }

        private static HashSet<string> ExtractExistingUsingStatements(string content)
        {
            var existingUsings = new HashSet<string>();

            var lines = content.Split('\n');
            bool inAutoBindRegion = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.Contains(VAR_AUTO_BIND_USING_START))
                {
                    inAutoBindRegion = true;
                    continue;
                }

                if (trimmedLine.Contains(VAR_AUTO_BIND_USING_END))
                {
                    inAutoBindRegion = false;
                    continue;
                }

                if (!inAutoBindRegion && trimmedLine.StartsWith("using ") && trimmedLine.EndsWith(";"))
                {
                    var namespaceName = trimmedLine.Substring(6, trimmedLine.Length - 7).Trim();

                    if (!namespaceName.Contains("="))
                    {
                        existingUsings.Add(namespaceName);
                    }
                }
            }

            return existingUsings;
        }

        private static string ProcessFieldBindings(string content, string bindingFields)
        {
            var startIndex = content.IndexOf(VAR_AUTO_BIND_FIELD_START, StringComparison.Ordinal);
            var lineStart = content.LastIndexOf('\n', Math.Max(0, startIndex - 1)) + 1;
            var currentIndent = content.Substring(lineStart, startIndex - lineStart);

            var replacement = CreateFieldRegionBlock(currentIndent, bindingFields);

            var pattern =
                $@"{Regex.Escape(currentIndent)}{Regex.Escape(VAR_AUTO_BIND_FIELD_START)}[\s\S]*?{Regex.Escape(currentIndent)}{Regex.Escape(VAR_AUTO_BIND_FIELD_END)}";

            return Regex.Replace(content, pattern, replacement, RegexOptions.Singleline);
        }

        private static void InsertBindingFieldsAtClassStart(string filePath, string className,
            string namespaceName, string bindingFields, HashSet<string> usingNamespaces)
        {
            var content = File.ReadAllText(filePath);

            content = ProcessUsingStatements(content, usingNamespaces);

            var classPattern =
                $@"(?<indent>\s*)(?:(?:public|internal|protected|private|sealed|static|abstract|partial)\s+)*class\s+{Regex.Escape(className)}\s*(?:<[^>]+>)?(?:\s*:\s*[^{{]+)?\s*{{";
            var match = Regex.Match(content, classPattern, RegexOptions.Multiline);

            if (match.Success)
            {
                var baseIndent = match.Groups["indent"].Value;
                var fieldIndent = baseIndent + "    ";

                var insertion = "\n" + CreateFieldRegionBlock(fieldIndent, bindingFields);

                var insertIndex = match.Index + match.Length;
                content = content.Insert(insertIndex, insertion);
                FileWrite(filePath, content);
            }
            else
            {
                var baseIndent = string.IsNullOrEmpty(namespaceName) ? "" : "    ";
                var fieldIndent = baseIndent + "    ";

                var insertion = "\n" + CreateFieldRegionBlock(fieldIndent, bindingFields);

                File.AppendAllText(filePath, insertion);
            }
        }

        private static string GenerateBindingFileContent(string className, string namespaceName,
            string bindingFields, HashSet<string> usingNamespaces)
        {
            using var sb = ZString.CreateStringBuilder();

            sb.AppendLine("// This file is auto-generated. you can modify it.");
            sb.AppendLine();

            var baseIndent = string.IsNullOrEmpty(namespaceName) ? "" : "    ";
            var fieldIndent = baseIndent + "    ";

            sb.AppendLine(VAR_AUTO_BIND_USING_START);
            sb.AppendLine(VAR_AUTO_BIND_USING_END);
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"{baseIndent}public partial class {className}");
            sb.AppendLine($"{baseIndent}{{");

            var regionBlock = CreateFieldRegionBlock(fieldIndent, baseIndent);
            sb.AppendLine(regionBlock);

            sb.AppendLine($"{baseIndent}}}");

            if (!string.IsNullOrEmpty(namespaceName)) sb.AppendLine("}");

            var content = sb.ToString();

            content = ProcessUsingStatements(content, usingNamespaces);
            content = ProcessFieldBindings(content, bindingFields);

            return content;
        }

        public static void CleanBindingFields(string filePath)
        {
            if (!File.Exists(filePath)) return;

            var content = File.ReadAllText(filePath);
            var originalContent = content;

            var fieldPattern =
                $@"({Regex.Escape(VAR_AUTO_BIND_FIELD_START)})([\s\S]*?)(\s*{Regex.Escape(VAR_AUTO_BIND_FIELD_END)})";
            content = Regex.Replace(content, fieldPattern,
                m => m.Groups[1].Value + "\n" + m.Groups[3].Value, RegexOptions.Singleline);

            var usingPattern =
                $@"({Regex.Escape(VAR_AUTO_BIND_USING_START)})([\s\S]*?)(\s*{Regex.Escape(VAR_AUTO_BIND_USING_END)})";
            content = Regex.Replace(content, usingPattern,
                m => m.Groups[1].Value + "\n" + m.Groups[3].Value, RegexOptions.Singleline);

            if (content != originalContent)
            {
                FileWrite(filePath, content);
            }
        }

        private static string GetFormattedFields(string baseIndent, string bindingFields)
        {
            var cleanedFields = Regex.Replace(bindingFields, @"(\r?\n){2,}", "\n").Trim();
            return string.Join("\n",
                cleanedFields.Split('\n')
                    .Select(line => string.IsNullOrWhiteSpace(line) ? "" : $"{baseIndent}{line.Trim()}"));
        }

        private static MonoScript FindMonoScriptByType(Type type)
        {
            if (_scriptCache.TryGetValue(type, out var monoScript))
                return monoScript;

            var guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
            var mono = guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
                .FirstOrDefault(script => script && script.GetClass() == type);
            _scriptCache[type] = mono;
            return mono;
        }

        public static void CleanSerializedBindings(ViewObject target)
        {
            if (target == null) return;

            target.ClearBinding();
            EditorUtility.SetDirty(target);
            var bindingFile = GetBindingFilePathForType(target.GetType());
            if (string.IsNullOrEmpty(bindingFile)) return;

            CleanBindingFields(bindingFile);
        }

        public static string GetBindingFilePathForType(Type type)
        {
            if (type == null) return null;

            var script = FindMonoScriptByType(type);
            if (script == null)
            {
                return null;
            }

            var scriptPath = AssetDatabase.GetAssetPath(script);
            if (string.IsNullOrEmpty(scriptPath)) return null;

            var bindingPath = Path.Combine(
                Path.GetDirectoryName(scriptPath) ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(scriptPath)}.Binding.cs");

            bindingPath = bindingPath.Replace('\\', Path.AltDirectorySeparatorChar);
            return File.Exists(bindingPath) ? bindingPath : null;
        }

        public static bool IsFieldInTypeHierarchy(string fieldName, Type type)
        {
            var currentType = type;
            while (currentType != null && currentType != typeof(MonoBehaviour) && currentType != typeof(object))
            {
                if (IsFieldInType(fieldName, currentType))
                    return true;
                currentType = currentType.BaseType;
            }

            return false;
        }

        private static string CreateFieldRegionBlock(string regionIndent, string bindingFields)
        {
            var indentedFields = GetFormattedFields(regionIndent, bindingFields);
            return
                $"{regionIndent}{VAR_AUTO_BIND_FIELD_START}\n{indentedFields}\n{regionIndent}{VAR_AUTO_BIND_FIELD_END}";
        }

        private static void FileWrite(string path, string content)
        {
            try
            {
                File.WriteAllText(path, content);
            }
            catch (IOException ex)
            {
                Debug.LogError($"文件写入失败: {ex.Message}");
            }
        }

        public static bool IsFieldInType(string fieldName, Type type)
        {
            var bindingPath = GetBindingFilePathForType(type);
            if (bindingPath == null)
                return false;

            return ParseBindingFields(bindingPath).Contains(fieldName);
        }

        /// <summary>
        /// 生成 VaryText 别名常量到对应的 .Binding.cs 文件（对应参考项目 TMP VaryText 代码生成）。
        /// </summary>
        public static void GenerateVaryTextAliasConstants(Component component, List<string> varyTextAliases, string fieldName)
        {
            if (component == null)
                return;

            var viewObject = component.GetComponentInParent<ViewObject>();
            if (viewObject == null)
            {
                Debug.LogWarning($"[VaryText] 未找到父级 ViewObject，无法生成别名常量: {fieldName}");
                return;
            }

            var bindingPath = GetBindingFilePathForType(viewObject.GetType());
            if (string.IsNullOrEmpty(bindingPath))
            {
                Debug.LogWarning($"[VaryText] 未找到对应的 Binding 文件: {viewObject.GetType().Name}");
                return;
            }

            var content = File.ReadAllText(bindingPath);

            if (varyTextAliases == null || varyTextAliases.Count == 0)
            {
                content = RemoveVaryTextAliasRegion(content, fieldName);
                FileWrite(bindingPath, content);
                return;
            }

            var aliasCode = GenerateVaryTextAliasCode(varyTextAliases, fieldName);
            if (string.IsNullOrEmpty(aliasCode))
                return;

            content = InsertOrUpdateVaryTextAliasRegion(content, aliasCode, fieldName);
            FileWrite(bindingPath, content);
        }

        private static string GenerateVaryTextAliasCode(List<string> varyTextAliases, string fieldName)
        {
            if (varyTextAliases == null || varyTextAliases.Count == 0)
                return string.Empty;

            var pascalName = ToPascalCase(fieldName);
            var enumName = $"VaryTextAlias{pascalName}";
            using var sb = ZString.CreateStringBuilder();
            sb.AppendLine($"public enum {enumName}");
            sb.AppendLine("{");
            sb.AppendLine("    None = -1,");
            for (int i = 0; i < varyTextAliases.Count; i++)
            {
                var alias = varyTextAliases[i];
                if (!string.IsNullOrEmpty(alias))
                {
                    sb.AppendLine($"    {alias.ToUpperInvariant()} = {i},");
                }
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static string ToPascalCase(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return "Unknown";

            var name = fieldName;
            if (name.StartsWith("_"))
                name = name.Substring(1);
            if (name.StartsWith("m_"))
                name = name.Substring(2);

            if (string.IsNullOrEmpty(name))
                return "Unknown";

            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        private static string ToUpperCase(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return "UNKNOWN";

            var name = fieldName;
            if (name.StartsWith("_"))
                name = name.Substring(1);
            if (name.StartsWith("m_"))
                name = name.Substring(2);

            return name.ToUpperInvariant();
        }

        private static string InsertOrUpdateVaryTextAliasRegion(string content, string aliasCode, string fieldName)
        {
            var upperName = ToUpperCase(fieldName);
            var regionStart = $"#region VARY_TEXT_ALIAS_{upperName}";
            var regionEnd = "#endregion";
            var baseIndent = "        ";
            var regionBlock = $"{baseIndent}{regionStart}\n{baseIndent}{aliasCode.Replace("\n", $"\n{baseIndent}")}\n{baseIndent}{regionEnd}";

            if (content.Contains(regionStart))
            {
                var pattern = $@"{Regex.Escape(regionStart)}[\s\S]*?{Regex.Escape(regionEnd)}";
                return Regex.Replace(content, pattern, regionBlock.TrimStart(), RegexOptions.Singleline);
            }

            var classEndPattern = @"(\s*)(\})\s*(\})\s*$";
            var match = Regex.Match(content, classEndPattern);
            if (match.Success)
            {
                var indent = match.Groups[1].Value.Contains("\n")
                    ? match.Groups[1].Value.Substring(match.Groups[1].Value.LastIndexOf('\n') + 1)
                    : match.Groups[1].Value;
                var classEndIndex = match.Groups[2].Index;
                content = content.Insert(classEndIndex, $"\n\n{regionBlock}\n{indent}");
            }
            else
            {
                var lastBraceIndex = content.LastIndexOf('}');
                if (lastBraceIndex > 0)
                {
                    var secondLastBraceIndex = content.LastIndexOf('}', lastBraceIndex - 1);
                    if (secondLastBraceIndex > 0)
                    {
                        content = content.Insert(secondLastBraceIndex, $"\n\n{regionBlock}\n    ");
                    }
                    else
                    {
                        content = content.Insert(lastBraceIndex, $"\n\n{regionBlock}\n");
                    }
                }
            }

            return content;
        }

        private static string RemoveVaryTextAliasRegion(string content, string fieldName)
        {
            var upperName = ToUpperCase(fieldName);
            var regionStart = $"#region VARY_TEXT_ALIAS_{upperName}";
            var regionEnd = "#endregion";

            if (!content.Contains(regionStart))
                return content;

            var pattern = $@"[\r\n]+\s*{Regex.Escape(regionStart)}[\s\S]*?{Regex.Escape(regionEnd)}[\r\n]*";
            content = Regex.Replace(content, pattern, "\n", RegexOptions.Singleline);

            return content;
        }
    }
}
