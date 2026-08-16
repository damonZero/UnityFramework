using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Package.PSD2UGUI
{
    public enum PsdNodeEnum
    {
        Root = 1,
        Group,
        Image,
        Text
    }


    public abstract class PsdNodeBase : IPsdNodeData
    {
        public abstract PsdNodeEnum NodeType { get; }

        public string name;
        public int type;
        public float[] pos;
        public float[] size;
        public JsonArray childNodes;
        public float scale = 1;

        public PsdNodeBase Parent { get; set; }
        public List<PsdNodeBase> ChildrenNodes { get; private set; }

        public void AddChildPsdNode(PsdNodeBase childNode)
        {
            ChildrenNodes ??= new List<PsdNodeBase>();
            ChildrenNodes.Add(childNode);
        }
        
        public bool isFixedPosition;
    }
}