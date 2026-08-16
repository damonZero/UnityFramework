// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

using System.Threading;
using Cysharp.Threading.Tasks;
namespace Framework.View
{
    public interface IViewManager : IViewLifeCycleExecutor
    {
        /// <summary>
        /// 打开一个View
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        UniTask<ViewBase> OpenAsync(IViewOptions options, CancellationToken cancellationToken = default);
    }
}
