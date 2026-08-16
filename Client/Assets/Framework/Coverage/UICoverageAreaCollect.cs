//**************************************************************************************
//Create By szx on 2020/12/3
//
//@Description coverage UI区域集合
//**************************************************************************************

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Coverage
{
    public class UICoverageAreaCollect : IEnumerable<IntRect>
    {
        private List<UICoverageArea> _areaList = new List<UICoverageArea>();

        public int Count => _areaList.Count;

        public void Init(IList<AreaInfo> infoList, CanvasCoverage cov, UICoverageArea.CoverageType type)
        {
            _areaList.Clear();
            foreach (var info in infoList)
            {
                var area = new UICoverageArea();
                area.Init(info, cov, type);
                _areaList.Add(area);
            }
        }

        public IEnumerator<IntRect> GetEnumerator()
        {
            for (int i = 0; i < _areaList.Count; i++)
            {
                var area = _areaList[i];
                if (area.Available)
                    yield return _areaList[i].Rect;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
