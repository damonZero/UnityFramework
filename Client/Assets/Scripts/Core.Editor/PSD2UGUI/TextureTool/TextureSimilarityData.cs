//*****************************************************************************
//Created By cd_liangc
//
//@Description 纹理相似度数据
//*****************************************************************************

using System.Collections.Generic;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class TextureSimilarityData
    {
        public string hash;
        public Texture2D tex;
        public string path;
        public List<TextureSimilarityCompare> simTextures;
        public bool expand;
    }

    public class TextureSimilarityCompare
    {
        public TextureSimilarityData data;
        public bool choice;
        public float similarValue;
    }
}