using System.Collections.Generic;
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

                int total = 0, passed = 0, failed = 0, skipped = 0, inconclusive = 0;
                CountResults(result, ref total, ref passed, ref failed, ref skipped, ref inconclusive);

                // Write a custom XML summary that records the totals AND every case,
                // so an automated reader can confirm pass/fail counts (not just failures).
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    writer.WriteLine($"<test-run total=\"{total}\" passed=\"{passed}\" failed=\"{failed}\" skipped=\"{skipped}\" inconclusive=\"{inconclusive}\">");
                    WriteResult(writer, result);
                    writer.WriteLine("</test-run>");
                }
                Debug.Log($"[AutoExportTestResults] Test results automatically exported to: {filePath} (total={total} passed={passed} failed={failed} skipped={skipped})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AutoExportTestResults] Failed to export test results: {e.Message}");
            }
        }

        private static bool HasChildren(ITestResultAdaptor result)
        {
            if (result.Children == null) return false;
            foreach (var _ in result.Children) return true;
            return false;
        }

        private void CountResults(ITestResultAdaptor result,
            ref int total, ref int passed, ref int failed, ref int skipped, ref int inconclusive)
        {
            // Leaf nodes (no children) are individual test cases; parents are groups.
            if (!HasChildren(result))
            {
                total++;
                switch (result.TestStatus)
                {
                    case TestStatus.Passed: passed++; break;
                    case TestStatus.Failed: failed++; break;
                    case TestStatus.Skipped: skipped++; break;
                    default: inconclusive++; break;
                }
                return;
            }
            foreach (var child in result.Children)
            {
                CountResults(child, ref total, ref passed, ref failed, ref skipped, ref inconclusive);
            }
        }

        private void WriteResult(StreamWriter writer, ITestResultAdaptor result)
        {
            if (!HasChildren(result))
            {
                var status = result.TestStatus.ToString();
                writer.WriteLine($"  <test-case name=\"{System.Security.SecurityElement.Escape(result.Name)}\" status=\"{status}\">");
                if (result.TestStatus == TestStatus.Failed)
                {
                    writer.WriteLine($"    <message>{System.Security.SecurityElement.Escape(result.Message)}</message>");
                    writer.WriteLine($"    <stack-trace>{System.Security.SecurityElement.Escape(result.StackTrace)}</stack-trace>");
                }
                writer.WriteLine($"  </test-case>");
                return;
            }

            foreach (var child in result.Children)
            {
                WriteResult(writer, child);
            }
        }

        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
    }
}
