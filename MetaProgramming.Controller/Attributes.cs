using System;

namespace MetaProgramming.Controller
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class HttpControllerAttribute : Attribute
    {
        public HttpControllerAttribute(string root)
        {
            Root = root;
        }

        public string Root { get; }
    }

    public abstract class HttpRequestAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class HttpGetAttribute : HttpRequestAttribute
    {
        public HttpGetAttribute(string root)
        {
            Root = root;
        }

        public string Root { get; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class HttpPostAttribute : HttpRequestAttribute
    {
        public HttpPostAttribute(string root)
        {
            Root = root;
        }

        public string Root { get; }
    }
}
