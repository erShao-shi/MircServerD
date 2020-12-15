using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Zhaoxi.MicroService.ServiceInstanceAuthen.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private IConfiguration _iConfiguration;

        public TestController(ILogger<TestController> logger, IConfiguration configuration)
        {
            _logger = logger;
            this._iConfiguration = configuration;
        }

        [HttpGet]
        [Route("Index")]
        public IActionResult Index()
        {
            Console.WriteLine($"This is TestController  {this._iConfiguration["port"]} Invoke");

            return new JsonResult(new
            {
                message = "This is TestControllerIndex",
                Port = this._iConfiguration["port"],
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")
            });
        }

        [Authorize]
        [HttpGet]
        [Route("IndexA")]
        public IActionResult IndexA()
        {
            Console.WriteLine($"This is TestController  {this._iConfiguration["port"]} Invoke");

            return new JsonResult(new
            {
                message = "This is TestControllerIndex",
                Port = this._iConfiguration["port"],
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")
            });
        }
    }
}