using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MetaProgramming.Controller
{
    public class RootController : IDisposable
    {
        private enum HttpType
        {
            Get,
            Post
        }

        private readonly string _rootRoot = "http://localhost:8085/";
        private readonly HttpListener _listener = new HttpListener();
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private readonly Dictionary<string, Dictionary<(string, HttpType), (Type type, MethodInfo method)>> _roots =
            new Dictionary<string, Dictionary<(string, HttpType), (Type type, MethodInfo method)>>();

        public RootController()
        {
            var stackTrace = new StackTrace();
            var callingMethod = stackTrace.GetFrame(1).GetMethod();
            var callingAssembly = callingMethod.DeclaringType.Assembly;

            var allTypsInAssembly = callingAssembly.GetTypes();

            foreach (var controllerType in allTypsInAssembly)
            {
                if (!(controllerType.GetCustomAttributes(typeof(HttpControllerAttribute), false).SingleOrDefault() is HttpControllerAttribute httpController))
                    continue;

                _roots.Add(httpController.Root, new Dictionary<(string, HttpType), (Type, MethodInfo)>());

                foreach (var method in controllerType.GetMethods())
                {
                    if (!(method.GetCustomAttributes().SingleOrDefault(x => x is HttpRequestAttribute) is HttpRequestAttribute requestAttribute))
                        continue;

                    switch (requestAttribute)
                    {
                        case HttpGetAttribute get:
                            _roots[httpController.Root].Add((get.Root, HttpType.Get), (controllerType, method));
                            break;
                        case HttpPostAttribute post:
                            _roots[httpController.Root].Add((post.Root, HttpType.Post), (controllerType, method));
                            break;
                    }
                }
            }

            _listener.Prefixes.Add(_rootRoot);
        }

        public void Start()
        {
            _listener.Start();
            _ = HandleRequestAsync();
        }

        public void Stop()
        {
            _tokenSource.Cancel();
            _listener.Stop();
        }

        public void Dispose()
        {
            ((IDisposable)_listener).Dispose();
        }

        private object DeserializeObject<T>(string json) => JsonConvert.DeserializeObject<T>(json);

        private async Task HandleRequestAsync()
        {
            var context = await Task.Factory.StartNew(() => _listener.GetContext(), _tokenSource.Token);
            var request = context.Request;
            var response = context.Response;
            var root = request.Url;

            HttpType getHttpType()
            {
                switch (request.HttpMethod)
                {
                    case "GET": return HttpType.Get;
                    case "POST": return HttpType.Post;
                    default: throw new NotSupportedException();
                }
            }

            string readRequestData()
            {
                using (Stream receiveStream = request.InputStream)
                    using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                        return readStream.ReadToEnd();
            }

            void writeRequest(string value)
            {
                byte[] bufferdResult = Encoding.UTF8.GetBytes(value);

                response.ContentLength64 = bufferdResult.Length;

                Stream output = response.OutputStream;
                output.Write(bufferdResult, 0, bufferdResult.Length);
                output.Close();
            }

            var type = getHttpType();
            var localPath = root.LocalPath;

            var segments = localPath.Split('/').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            if (_roots.TryGetValue(segments[0], out var subRoots))
            {
                if (subRoots.TryGetValue((segments[1], type), out var values))
                {
                    var contoller = Activator.CreateInstance(values.type);
                    var methodInfo = values.method;

                    switch (type)
                    {
                        case HttpType.Get:
                            var result = methodInfo.Invoke(contoller, new object[0]).ToString();
                            writeRequest(result);
                            break;

                        case HttpType.Post:
                            var requestData = readRequestData();
                            var argsType = methodInfo.GetParameters().First().ParameterType;

                            var deserializeObject =
                                typeof(RootController)
                                .GetMethod("DeserializeObject", BindingFlags.Instance | BindingFlags.NonPublic)
                                .MakeGenericMethod(argsType);

                            var args = deserializeObject.Invoke(this, new object [] { requestData });
                            
                            methodInfo.Invoke(contoller, new [] { args });
                            writeRequest(string.Empty);
                            break;
                    }

                    if (contoller is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }

            await HandleRequestAsync().ConfigureAwait(false);
        }
    }
}
