namespace Package.PSD2UGUI
{
    public class PsdNodeText : PsdNodeBase
    {
        public override PsdNodeEnum NodeType => PsdNodeEnum.Text;

        public string content;
        public int fontSize;
        public float[] color;

        public float lineSpace;
        public float letterSpacing;

        public bool italic;
        public bool bold;
        public bool underline;

        public string alignment;

        // 分段颜色 [[r,g,b,a], ...](0-255) 与分段长度
        public int[][] colorRuns;
        public int[] runLengths;
    }
}