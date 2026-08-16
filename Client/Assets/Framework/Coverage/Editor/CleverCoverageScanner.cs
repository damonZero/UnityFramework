//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description 智能区域扫描识别
//**************************************************************************************

using System;
using System.Collections.Generic;
using Framework.Coverage;
using UnityEngine;
using UnityEngine.UI;

namespace Framework.Coverage.Editor
{
    /// <summary>
    /// 智能区域扫描
    /// 用于扫描一个RectTransform根节点下适合作为显示区域和遮挡区域的节点
    /// </summary>
    public static class CleverCoverageScanner
    {
        /// <summary>
        /// 扫描区域
        /// </summary>
        /// <param name="isShow"></param>
        /// <returns></returns>
        public static List<AreaInfo> ScanCoverages(RectTransform root, bool isShow)
        {
            var list = new List<AreaInfo>();
            if (isShow)
                ScanShowCoverages(root, list);
            else
                ScanCoverCoverages(root, list);
            return RejectRectTransform(list);
        }

        /// <summary>
        /// 扫描显示区域
        /// 目前扫描根节点下所有MaskableGraphic节点，如果image的alpha>0，则加入显示列表
        /// 需要后续优化 TODO
        /// </summary>
        /// <returns></returns>
        private static void ScanShowCoverages(RectTransform root, List<AreaInfo> rtList)
        {
            foreach (Transform trans in root)
            {
                if (trans is RectTransform rt)
                    ScanShowCoverages(rt, rtList);
            }

            if (!root.gameObject.activeInHierarchy)
                return;

            var img = root.GetComponent<MaskableGraphic>();
            if (img != null)
            {
                if (img.color.a > 0)
                    rtList.Add(new AreaInfo(root));
            }
        }


        /// <summary>
        /// 扫描遮挡区域
        /// 目前扫描根节点下所有Image节点，如果image的alpha==1，则加入遮挡列表
        /// 需要后续优化 TODO
        /// </summary>
        /// <returns></returns>
        private static void ScanCoverCoverages(RectTransform root, List<AreaInfo> rtList)
        {
            foreach (Transform trans in root)
            {
                if (trans is RectTransform rt)
                    ScanCoverCoverages(rt, rtList);
            }

            if (!root.gameObject.activeInHierarchy)
                return;

            var img = root.GetComponent<Image>();
            if (img != null)
            {
                if (Math.Abs(img.color.a - 1) < 0.00001f)
                    rtList.Add(new AreaInfo(root));
            }
        }

        /// <summary>
        /// 剔除不必要的节点
        /// 如果某个节点的区域被其他节点完全包围，则剔除
        /// </summary>
        /// <param name="infoList"></param>
        /// <returns></returns>
        private static List<AreaInfo> RejectRectTransform(List<AreaInfo> infoList)
        {
            if (infoList.Count < 1)
                return new List<AreaInfo>();

            var rootCanvas = infoList[0].anchorTrans.GetComponentInParent<Canvas>();
            var width = ((RectTransform) rootCanvas.transform).rect.width * rootCanvas.transform.lossyScale.x;
            var height = ((RectTransform) rootCanvas.transform).rect.height * rootCanvas.transform.lossyScale.y;
            infoList.Sort(Comparer);
            var retList = new List<AreaInfo>();
            var ctx = RectCheckContext.Take(new IntRect(0, 0, width, height));
            //这里还有个缺陷，没有考虑到小的多个包围大的情况，后面再来优化 TODO
            for (int i = infoList.Count - 1; i >= 0; --i)
            {
                if (!infoList[i].anchorTrans.gameObject.activeInHierarchy)
                    continue;
                if (retList.Count < 1)
                {
                    retList.Add(infoList[i]);
                    continue;
                }

                var rt = infoList[i + 1].anchorTrans;
                var pos = CoverageUtil.RectTransToUIPointWithoutAnchor(rt);
                var lastRect = new IntRect(pos.x, pos.y, rt.rect.width * rt.lossyScale.x,
                    rt.rect.height * rt.lossyScale.y);

                rt = infoList[i].anchorTrans;
                pos = CoverageUtil.RectTransToUIPointWithoutAnchor(rt);
                var rect = new IntRect(pos.x, pos.y, rt.rect.width * rt.lossyScale.x, rt.rect.height * rt.lossyScale.y);
                if (!CoverageUtil.RectIsCoveredByCtx(rect, new[] {lastRect}, ctx))
                {
                    retList.Add(infoList[i]);
                }
            }

            RectCheckContext.Cache(ctx);

            return retList;
        }


        private static int Comparer(AreaInfo info1, AreaInfo info2)
        {
            var rt1 = info1.anchorTrans;
            var rt2 = info2.anchorTrans;
            var area1 = rt1.rect.width * rt1.rect.height * rt1.lossyScale.x * rt1.lossyScale.y;
            var area2 = rt2.rect.width * rt2.rect.height * rt2.lossyScale.x * rt2.lossyScale.y;
            return (int) ((area1 - area2) * 100);
        }
    }
}
