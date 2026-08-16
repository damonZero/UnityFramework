using Framework.Coverage;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public sealed class SegmentTreeTests
    {
        // 回归：SetSegEnable 的端点恰好等于线段树 mid 时（end == mid），
        // 修复前会只路由到左子而丢失 mid 这一个点（对应 REVIEW 2.10 High）。
        [Test]
        public void SetSegEnable_CoversEndpointWhenEndEqualsMid()
        {
            var tree = new SegmentTree();
            tree.Build(0, 3);

            tree.SetSegEnable(0, 2, true);

            Assert.That(tree.CheckSegIsEnable(2, 2), Is.True, "端点 end==mid 不应被丢");
            Assert.That(tree.CheckSegIsEnable(0, 2), Is.True);
            Assert.That(tree.CheckSegIsEnable(3, 3), Is.False, "未设置的点不应被覆盖");
        }
    }
}
