using System;

namespace Framework.DependencyInjection
{
    public interface IInjectable : IDisposable
    {
    }

    public interface IOtherInjectable : IInjectable
    {

    }
}
