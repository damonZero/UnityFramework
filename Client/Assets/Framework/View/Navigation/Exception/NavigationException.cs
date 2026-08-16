//**************************************************************************************
//Create By Liangc on 2023/11/15
//导航系统错误定义
//@Description 定义出各种错误类型，方便错误处理
//**************************************************************************************

using Framework.Log;
using System;
using System.Reflection;
using UnityEngine;
namespace Framework.View.Navigation
{
    //导航错误类型
    public class NavigationException : Exception
    {
        public NavigationException() : base("NavigationException")
        {
        }

        public NavigationException(string message) : base(message)
        {
        }

        public NavigationException(string message, Exception inner) : base(message, inner)
        {
        }

        public string LuaTrace { get; internal set; }

        public override string ToString()
        {
            return $"{base.ToString()}\r\nLuaTrace:{LuaTrace}";
        }

        /// <summary>
        /// 转换异常
        /// </summary>
        /// <param name="ex"></param>
        /// <typeparam name="TException"></typeparam>
        /// <returns></returns>
        public static TException Convert<TException>(Exception ex)
            where TException : NavigationException, new()
        {
            try
            {
                var constructor =
                    typeof(TException).GetConstructor(new[] { typeof(string), typeof(Exception) });
                return (TException)constructor.Invoke(new object[] { ex.Message, ex });
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "Navigation exception", module: "Framework.View.Navigation");
                return null;
            }
        }
    }

    //资源加载错误
    public class NavigationAssetLoadException : NavigationException
    {
        public NavigationAssetLoadException() : base("NavigationAssetLoadException")
        {
        }

        public NavigationAssetLoadException(string message) : base(message)
        {
        }

        public NavigationAssetLoadException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    //打开异常
    public class NavigationOpenException : NavigationException
    {
        public NavigationOpenException() : base("NavigationShowException")
        {
        }

        public NavigationOpenException(string message) : base(message)
        {
        }

        public NavigationOpenException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    //验证错误
    public class NavigationVerifyException : NavigationException
    {
        public NavigationVerifyException() : base("NavigationVerifyException")
        {
        }

        public NavigationVerifyException(string message) : base(message)
        {
        }

        public NavigationVerifyException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    //生命周期调用错误
    public class NavigationLifecycleException : NavigationException
    {
        public NavigationLifecycleException() : base("NavigationLifecycleException")
        {
        }

        public NavigationLifecycleException(string message) : base(message)
        {
        }

        public NavigationLifecycleException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    //事件执行错误
    public class NavigationEventException : NavigationException
    {
        public NavigationEventException() : base("NavigationEventException")
        {
        }

        public NavigationEventException(string message) : base(message)
        {
        }

        public NavigationEventException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
