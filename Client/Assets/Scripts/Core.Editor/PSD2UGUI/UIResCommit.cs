//*****************************************************************************
//Created By huangjj
//
//@Description 
//*****************************************************************************

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class UIResCommit
    {
        [MenuItem("美术工具/UI专用工具/提交资源", false, 2)]
        static void SVNCommit()
        {
            // 原 P33 依赖 SVNCommitTools(TortoiseProc) 提交, 本工程已剥离
            Debug.LogWarning("[UIResCommit] 本工程未接入 SVN 提交工具, 请使用版本控制客户端提交: " + GetCommitPath());
        }

        [MenuItem("美术工具/UI专用工具/提交正式资源", false, 3)]
        static void SVNCommitFormal()
        {
            Debug.LogWarning("[UIResCommit] 本工程未接入 SVN 提交工具, 请使用版本控制客户端提交: " + GetCommitFormalPath());
        }

        [MenuItem("美术工具/UI专用工具/提交字体预制样式", false, 4)]
        static void SVNCommitTMP()
        {
            Debug.LogWarning("[UIResCommit] 本工程未接入 SVN 提交工具, 请使用版本控制客户端提交: " + GetCommitTMPPath());
        }

        [MenuItem("美术工具/UI专用工具/手册/美术导入PSD手册", false, 100)]
        static void OpenWebUIPSDManual()
        {
            Application.OpenURL("https://jzyxgames.feishu.cn/wiki/TXX1wyMdpiOJmQkpnTcclROdnLg");
        }

        [MenuItem("美术工具/UI专用工具/手册/SVN UGUI使用介绍", false, 101)]
        static void OpenWebUGUITutorials()
        {
            Application.OpenURL("https://jzyxgames.feishu.cn/wiki/NzwOwqGWii8DVrkc9PAcspepnrb");
        }

        [MenuItem("美术工具/UI专用工具/手册/PS插件安装及使用", false, 101)]
        static void OpenWebPSPlugin()
        {
            Application.OpenURL("https://jzyxgames.feishu.cn/wiki/BnVyw5FhiiZWDokDm37cTfyunsd");
        }

        private static string GetCommitPath()
        {
            var rootPath = Application.dataPath;
            var pathL = new[]
            {
                rootPath + "/GameRes/UI/_TempPrefab",
                rootPath + "/GameRes/UI/ResConfig",
                rootPath + "/GameRes/UI/UIResPool"
            };
            return string.Join("*", pathL);
        }

        private static string GetCommitFormalPath()
        {
            var rootPath = Application.dataPath;
            var pathL = new[]
            {
                rootPath + "/GameRes/UI/FunctionUI",
                rootPath + "/GameRes/UI/ResConfig",
                rootPath + "/GameRes/UI/UIResPool"
            };
            return string.Join("*", pathL);
        }

        private static string GetCommitTMPPath()
        {
            var rootPath = Application.dataPath;
            var pathL = new[]
            {
                rootPath + "/GameRes/Font/FontStyleAssets",
                rootPath + "/GameRes/Config/TMPStyle",
                rootPath + "/GameRes/Scene/_TestTMP"
            };
            return string.Join("*", pathL);
        }
    }
}