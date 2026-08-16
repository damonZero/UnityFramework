//*****************************************************************************
//Created By Liangc on 2019/6/3
//PSD2UGUI规则类
//@Description 包含PSD节点类型规则,和导出查找规则,插件移植时根据当前项目自行修改
//*****************************************************************************

using UnityEngine;
using System.Collections.Generic;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD规则类
    /// </summary>
    public static class Psd2UguiRule
    {
        //图片导出路径
        public const string EXPORT_IMAGE_PATH = "Assets/GameRes/UI/ResPool";

        //临时图片导出路径
        public const string EXPORT_IMAGE_TEMP_PATH = "Assets/GameRes/UI/ResPool/_Temp";

        /// <summary>
        /// 美术导出的图片和预制体存放路径
        /// </summary>
        public const string EXPORT_IMAGE_UI_TEMP_PREFAB_PATH = "Assets/GameRes/UI/_TempPrefab";

        public const string EXPORT_IMAGE_UI_BG_PATH =  "Assets/GameRes/UI/ResPool/UIBg";

        //配置图片路径
        public const string CONFIG_IMAGE_PATH = "Assets/GameRes/UI/ResConfig";

        //UI预制体查找路径
        public const string PREFAB_FIND_PATH = "Assets/GameRes/UI/General";

        //Logo路径
        public const string LOGO_PATH = "Assets/Scripts/Core.Editor/PSD2UGUI/Picture/logo.png";

        //配置图片关键字
        public const string CONFIG_IMAGE_KEY = "Config";

        //临时用于比较图片导出路径
        public const string EXPORT_COMPARE_IMAGE_TEMP_PATH = EXPORT_IMAGE_TEMP_PATH + "/temp_img.png";

        //图片翻转标识
        public const string IMAGE_SYMMETRY_KEY = "@z";

        //锚点对齐层级深度
        public const int ALIGN_HIERARCHY_INDEX = 2;

        //背景图标识
        public const string IMAGE_BG_KEY = "@bg";

        //全屏背景尺寸定义(根据移动设备长宽比和1024*1024尺寸定制的分辨率)
        public static readonly Vector2 FULL_SCREEN_IMAGE_SIZE = new Vector2(750, 1800);
        
        /// <summary>
        /// 标准宽度
        /// </summary>
        public const int STANDARD_WIDTH = 750;

        /// <summary>
        /// 标准高度
        /// </summary>
        public const int STANDARD_HEIGHT = 1624;

        /// <summary>
        /// 高度适配下限
        /// </summary>
        public const int FLOOR_HEIGHT = 1334;

        //UI制作标准分辨率
        public static readonly Vector2 UI_STANDARD_SIZE =
            new Vector2(STANDARD_WIDTH, FLOOR_HEIGHT);

        //UI制作标准分辨率(新)
        public static readonly Vector2 UI_STANDARD_SIZE_NEW =
            new Vector2(STANDARD_WIDTH, STANDARD_HEIGHT);

        //标准分辨率:宽
        public static readonly int RESOLUTION_WIDTH = 750;
        //标准分辨率:高
        public static readonly int RESOLUTION_HEIGHT = 1624;

        //分辨率枚举
        public static readonly string[] RESOLUTION_CHOICE =
            {"【制作分辨率·新】全面屏：750*1624", "【制作分辨率·旧】非全面屏：750*1334"};

        //按钮最小尺寸(88*88为移动设备推荐的最小点击尺寸)
        public static readonly Vector2 BUTTON_MIN_SIZE = new Vector2(88, 88);

        //九宫格图检测连续像素(超过该像素会被剪裁)
        public const int SLICE_CONTINUE_NUM = 16;

        //九宫格剪裁预留像素(剪裁时预留的像素值)
        public const int SLICE_RESERVED_NUM = 8;

        //九宫格剪裁相似度
        public const float SLICE_CLIP_SIMILARITY = 99;

        //九宫格剪裁最低相似度
        public const float SLICE_CLIP_LOW_SIMILARITY = 99;

        //九宫格剪裁平均相似度
        public const float SLICE_CLIP_AVE_SIMILARITY = 99;

        //img前缀关键字
        public const string PER_KEY_IMG = "img";

        //t2d前缀关键字
        public const string PER_KEY_T_2D = "t2d";

        //btn前缀关键字   按钮生成走预制生成流程。
        public const string PER_KEY_BTN = "temp_btn";

        //ctn前缀关键字
        public const string PER_KEY_CTN = "temp_ctn";

        //nd前缀关键字
        public const string PER_KEY_ND = "";

        //定义类型
        private static readonly List<KeyValuePair<string, PsdNodeType>> _defineTypes =
            new List<KeyValuePair<string, PsdNodeType>>
            {
                new KeyValuePair<string, PsdNodeType>(PER_KEY_BTN, PsdNodeType.ButtonNode),
                new KeyValuePair<string, PsdNodeType>(PER_KEY_CTN, PsdNodeType.ButtonNode),
                new KeyValuePair<string, PsdNodeType>(PER_KEY_ND, PsdNodeType.CommonNode),
            };

        /// <summary>
        /// 判断PSD节点类型
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static PsdNodeType JudgePsdNodeType(string key)
        {
            foreach (var keyValue in _defineTypes)
            {
                if (key.StartsWith(keyValue.Key))
                    return keyValue.Value;
            }

            return PsdNodeType.CommonNode;
        }

        /// <summary>
        /// 获取标准分辨率
        /// </summary>
        /// <returns></returns>
        public static Vector2 GetStandardResolution(int choiceIdx)
        {
            return choiceIdx == 0 ? UI_STANDARD_SIZE_NEW : UI_STANDARD_SIZE;
        }
    }
}