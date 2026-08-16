namespace Package.PSD2UGUI
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class PsdNodeImage : PsdNodeBase
    {
        public override PsdNodeEnum NodeType => PsdNodeEnum.Image;

        public string assetPath;

        // 图层不透明度(0-1, 1=完全不透明)
        public float opacity = 1;
    }
}