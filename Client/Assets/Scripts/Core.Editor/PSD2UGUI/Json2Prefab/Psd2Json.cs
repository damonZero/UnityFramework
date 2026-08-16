using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Package.PSD2UGUI
{
    public static class Psd2Json
    {
        private static readonly string _nodeJsPath = Application.dataPath + "/../../tools/PSD/index.js";
        private static readonly string _nodeExePath;
        private static readonly string _jsonRootPath;

        static Psd2Json()
        {
            var editorPath = EditorApplication.applicationPath;
            var editorDir = Path.GetDirectoryName(editorPath);
            _nodeExePath = Path.Combine(editorDir, "Data\\Tools\\nodejs", "node.exe");
            // Unity 内置 Node.js 不存在时回退到系统 node
            if (!File.Exists(_nodeExePath))
                _nodeExePath = "node";
            _jsonRootPath = Path.Combine(Application.dataPath,
                Regex.Replace(Psd2UguiRule.EXPORT_IMAGE_UI_TEMP_PREFAB_PATH, "^Assets/", ""));
        }


        public static string Psd2JsonByNodeJs(string psdPath, bool ignoreHideLayer)
        {
            if (!File.Exists(psdPath))
            {
                Debug.LogError("Psd2JsonByNodeJs Error: psdPath not exist:" + psdPath);
                return "";
            }

            var psdName = Path.GetFileNameWithoutExtension(psdPath);
            var jsonDir = Path.Combine(_jsonRootPath, psdName);
            JsonDirCheck(jsonDir);
            var jsonPath = Path.Combine(jsonDir, psdName + ".json");

            var result = RunExe(_nodeExePath,
                _nodeJsPath + " " + psdPath + " " + jsonPath +
                (ignoreHideLayer ? " ignoreHideLayer" : ""), "UTF-8");

            if (result.StartsWith("true")) return jsonPath;

            Debug.Log("Psd2JsonByNodeJs Error:" + result);
            return "";
        }

        public static void JsonDirCheck(string jsonDir)
        {
            if (Directory.Exists(jsonDir)) return;
            try
            {
                Directory.CreateDirectory(jsonDir);
            }
            catch (Exception ex)
            {
                Debug.LogError("创建目标目录失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 同步执行进程并返回标准输出(等价 P33 Core.CmdUtil.RunExe)
        /// </summary>
        private static string RunExe(string exePath, string parameters, string encoding = "UTF-8")
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = parameters,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.GetEncoding(encoding),
                StandardErrorEncoding = Encoding.GetEncoding(encoding),
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
                Debug.LogError(error);

            return output;
        }
    }
}
