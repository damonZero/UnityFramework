//*****************************************************************************
//Created By Liangc on 2021/5/11
//
//@Description PSD节点差异展示界面
//*****************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class Psd2UguiDiffShow : EditorWindow
    {
        private int _curIndex;
        private Psd2UguiChoiceData _curData;
        private List<Psd2UguiChoiceData> _showAllData;

        private Rect _nameRect;
        private Rect _oldImageRect;
        private Rect _newImageRect;
        private Rect _diffImageRect;
        private Rect _lastBtnRect;
        private Rect _nextBtnRect;
        private readonly Vector2 _nameSize = new Vector2(500, 50);

        public static void InitShow(List<Psd2UguiChoiceData> showData, int curIndex)
        {
            Psd2UguiDiffShow diffWindows = GetWindow(typeof(Psd2UguiDiffShow)) as Psd2UguiDiffShow;
            diffWindows.titleContent = new GUIContent {text = "图片差异对比"};
            diffWindows._showAllData = showData;
            diffWindows._curIndex = curIndex;
            diffWindows._curData = showData[curIndex];
            diffWindows.minSize = new Vector2(400, 600);
            diffWindows.PreloadDiffImage();
        }

        private void OnGUI()
        {
            if (_curData.oldImage != null)
            {
                _nameRect = new Rect(0, 0, position.width, 20);
                _newImageRect = new Rect(0, 25, position.width * 0.5f - 5,
                    position.height * 0.3f - 5);
                _oldImageRect = new Rect(position.width * 0.5f + 5, 25,
                    position.width * 0.5f - 5, position.height * 0.3f - 5);
                _diffImageRect = new Rect(0, position.height * 0.3f + 5 + 25,
                    position.width, position.height * 0.7f - 30);
                _lastBtnRect = new Rect(0, position.height * 0.5f - 40, 40, 80);
                _nextBtnRect = new Rect(position.width - 40,
                    position.height * 0.5f - 40, 40, 80);

                GUI.Box(_newImageRect, _curData.originalImage);
                GUI.Label(new Rect(_newImageRect.position, _nameSize),
                    $"导入图片:\n{_curData.originalImage.width}×{_curData.originalImage.height}");

                GUI.Box(_oldImageRect, _curData.oldImage);
                GUI.Label(new Rect(_oldImageRect.position, _nameSize),
                    $"项目图片:\n{_curData.oldImage.width}×{_curData.oldImage.height}");

                GUI.Box(_diffImageRect, _curData.DiffImage);
                GUI.Label(new Rect(_diffImageRect.position, _nameSize), "对比图:");

                GUI.Box(_nameRect, $"图片名:{_curData.node.nodeName}");

                if (GUI.Button(_lastBtnRect, "<"))
                    MoveShow(false);

                if (GUI.Button(_nextBtnRect, ">"))
                    MoveShow(true);

            }
        }

        //移动显示目标
        private void MoveShow(bool isNext)
        {
            _curIndex = isNext ? _curIndex + 1 : _curIndex - 1;
            _curIndex = _curIndex > _showAllData.Count - 1 ? 0 : _curIndex;
            _curIndex = _curIndex < 0 ? _showAllData.Count - 1 : _curIndex;
            _curData = _showAllData[_curIndex];
            PreloadDiffImage();
        }

        //预加载差异图
        private void PreloadDiffImage()
        {
            int nextIdx = _curIndex + 1;
            nextIdx = nextIdx < 0 ? 0 : nextIdx;
            nextIdx = nextIdx > _showAllData.Count - 1 ? _showAllData.Count - 1 : nextIdx;
            int lastIdx = _curIndex - 1;
            lastIdx = lastIdx < 0 ? 0 : lastIdx;
            lastIdx = lastIdx > _showAllData.Count - 1 ? _showAllData.Count - 1 : lastIdx;

            _showAllData[nextIdx].InitDifferenceImage(this);
            _showAllData[lastIdx].InitDifferenceImage(this);
        }

    }
}