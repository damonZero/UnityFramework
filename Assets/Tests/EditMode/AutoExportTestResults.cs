using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Tests.EditMode
{
    [InitializeOnLoad]
    public static class AutoExportTestResultsRegister
    {
        static AutoExportTestResultsRegister()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new AutoExportTestResultsCallback());
        }
    }

    public class AutoExportTestResultsCallback : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            try
            {
                var projectPath = Directory.GetParent(Application.dataPath).FullName;
                var filePath = Path.Combine(projectPath, "TestResults.xml");
                
                // Write a simple custom XML summary to easily read the failed tests
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    writer.WriteLine("<test-run>");
                    WriteResult(writer, result);
                    writer.WriteLine("</test-run>");
                }
                Debug.Log($"[AutoExportTestResults] Test results automatically exported to: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AutoExportTestResults] Failed to export test results: {e.Message}");
            }
        }

        private void WriteResult(StreamWriter writer, ITestResultAdaptor result)
        {
            if (result.TestStatus == TestStatus.Failed)
            {
                writer.WriteLine($"  <test-case name=\"{System.Security.SecurityElement.Escape(result.Name)}\" status=\"Failed\">");
                writer.WriteLine($"    <message>{System.Security.SecurityElement.Escape(result.Message)}</message>");
                writer.WriteLine($"    <stack-trace>{System.Security.SecurityElement.Escape(result.StackTrace)}</stack-trace>");
                writer.WriteLine($"  </test-case>");
            }
            
            if (result.Children != null)
            {
                foreach (var child in result.Children)
                {
                    WriteResult(writer, child);
                }
            }
        }

        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
    }
}
