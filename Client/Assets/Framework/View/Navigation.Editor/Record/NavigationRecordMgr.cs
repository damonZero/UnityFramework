using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
namespace Framework.View.Navigation.Editor
{
    public class NavigationRecordMgr
    {
        //单例
        private static NavigationRecordMgr _instance;
        public static NavigationRecordMgr Instance => _instance ??= new NavigationRecordMgr();

        //记录路径
        private string _recordPath;

        //当前操作记录
        public List<NavigationOperateRecordData> Records { get; private set; } = new();

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            // NavigationFactory.GroupPool = new ObjectPool<EditorNavigationGroup>();
            // NavigationFactory.Factory= new NavigationAbstractFactory<EditorNavigationGroup, EditorNavigationFormLoader, EditorNavigationSceneLoader>();
        }

        /// <summary>
        /// 添加记录
        /// </summary>
        /// <param name="operateObj"></param>
        /// <param name="operateType"></param>
        /// <param name="operateData"></param>
        public void AddRecord(NavigationBehaviour operateObj,
            NavigationStateType operateType, object operateData = null)
        {
            NavigationOperateRecordData record = new NavigationOperateRecordData
            {
                operateObjType = operateObj.GetType(),
                operateObjName = operateObj.Name,
                operateObj = operateObj,
                operateType = operateType,
                operateData = operateData,
                operateTime = DateTime.Now,
                frame = UnityEngine.Time.frameCount,
                operateCSharpStack = Environment.StackTrace,
            };
            AddRecord(record);
        }

        /// <summary>
        /// 添加记录
        /// </summary>
        /// <param name="record"></param>
        public void AddRecord(NavigationOperateRecordData record)
        {
            Records.Add(record);
        }

        /// <summary>
        /// 移除记录
        /// </summary>
        /// <param name="removeCb"></param>
        public void RemoveRecord(Predicate<NavigationOperateRecordData> removeCb)
        {
            Records.RemoveAll(removeCb);
        }

        /// <summary>
        /// 记录数据
        /// </summary>
        /// <param name="records"></param>
        /// <param name="append"></param>
        public void WriteRecord(List<NavigationOperateRecordData> records, bool append = false)
        {
            if (!File.Exists(_recordPath))
                File.Create(_recordPath);

            var writeStream = File.Open(_recordPath, append ? FileMode.Append : FileMode.CreateNew);
            foreach (var record in records)
            {
                //新建序列化对象
                DataContractJsonSerializer jsonData =
                    new DataContractJsonSerializer(typeof(NavigationOperateRecordData));

                //进行序列化
                jsonData.WriteObject(writeStream, record);
            }

            writeStream.Close();
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <returns></returns>
        public List<NavigationOperateRecordData> ReadRecord()
        {
            if (!File.Exists(_recordPath))
                return null;

            FileStream readStream = new FileStream(_recordPath, FileMode.Open);
            DataContractJsonSerializer jsonData =
                new DataContractJsonSerializer(typeof(NavigationOperateRecordData));
            List<NavigationOperateRecordData> records = new List<NavigationOperateRecordData>();
            while (readStream.Position != readStream.Length)
            {
                NavigationOperateRecordData record = (NavigationOperateRecordData)jsonData.ReadObject(readStream);
                records.Add(record);
            }

            readStream.Close();
            return records;
        }
    }
}
