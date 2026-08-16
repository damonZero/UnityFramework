using Framework.Log;
using R3;

namespace Framework.MVVM
{
    public interface ICompositeDisposable
    {
        CompositeDisposable DefaultDisposables { get; }
    }
}
