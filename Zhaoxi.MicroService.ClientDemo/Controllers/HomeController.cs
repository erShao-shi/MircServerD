using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Consul;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zhaoxi.MicroService.ClientDemo.Models;
using Zhaoxi.MicroService.Interface;
using Zhaoxi.MicroService.Model;

namespace Zhaoxi.MicroService.ClientDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserService _iUserService = null;
        public HomeController(ILogger<HomeController> logger, IUserService userService)
        {
            _logger = logger;
            this._iUserService = userService;
        }

        public IActionResult Index()
        {

            string url = null;

            //base.ViewBag.Users = this._iUserService.UserAll();
            //代码的一小步，架构的一大步
            //string url = "http://localhost:5726/api/users/all";
            //string url = "http://localhost:5727/api/users/all";
            //string url = "http://localhost:5728/api/users/all";

            #region Nginx
            //url = "http://localhost:8080/api/users/all";//只知道nginx地址
            #endregion

            #region Consul
            url = "http://ZhaoxiService/api/users/all";//客户端得知道调用啥服务，啥名字---consul就是个DNS

            ConsulClient client = new ConsulClient(c =>
            {
                c.Address = new Uri("http://localhost:8500/");
                c.Datacenter = "dc1";
            });
            var response = client.Agent.Services().Result.Response;
            //foreach (var item in response)
            //{
            //    Console.WriteLine("***************************************");
            //    Console.WriteLine(item.Key);
            //    var service = item.Value;
            //    Console.WriteLine($"{service.Address}--{service.Port}--{service.Service}");
            //    Console.WriteLine("***************************************");
            //}

            Uri uri = new Uri(url);
            string groupName = uri.Host;
            AgentService agentService = null;

            var serviceDictionary = response.Where(s => s.Value.Service.Equals(groupName, StringComparison.OrdinalIgnoreCase)).ToArray();//找到的全部服务
            //{
            //    agentService = serviceDictionary[0].Value;//直接拿的第一个
            //    //这里有三个服务或者服务实例，只需要选择一个调用，那么这个选择的方案，就叫 负载均衡策略
            //}
            //{
            //    //轮询策略 也是平均，但是太僵硬了
            //    agentService = serviceDictionary[iIndex++ % 3].Value;
            //}
            //{
            //    //平均策略--随机获取索引--相对就平均
            //    agentService = serviceDictionary[new Random(iIndex++).Next(0, serviceDictionary.Length)].Value;
            //}
            {
                //权重策略--能给不同的实例分配不同的压力---注册时提供权重
                List<KeyValuePair<string, AgentService>> pairsList = new List<KeyValuePair<string, AgentService>>();
                foreach (var pair in serviceDictionary)
                {
                    int count = int.Parse(pair.Value.Tags?[0]);//1   5   10
                    for (int i = 0; i < count; i++)
                    {
                        pairsList.Add(pair);
                    }
                }
                //16个  
                agentService = pairsList.ToArray()[new Random(iIndex++).Next(0, pairsList.Count())].Value;
            }
            url = $"{uri.Scheme}://{agentService.Address}:{agentService.Port}{uri.PathAndQuery}";
            #endregion

            string content = InvokeApi(url);
            base.ViewBag.Users = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<User>>(content);

            Console.WriteLine($"This is {url} Invoke");
            return View();
        }

        private static int iIndex = 0;//暂不考虑线程安全

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public static string InvokeApi(string url)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                HttpRequestMessage message = new HttpRequestMessage();
                message.Method = HttpMethod.Get;
                message.RequestUri = new Uri(url);
                var result = httpClient.SendAsync(message).Result;
                string content = result.Content.ReadAsStringAsync().Result;
                return content;
            }
        }
    }
}
