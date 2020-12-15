using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;

using Ocelot.Cache.CacheManager;
using Ocelot.Cache;
using Ocelot.Provider.Polly;
using IdentityServer4.AccessTokenValidation;

namespace Zhaoxi.MicroService.GatewayDemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            #region Ids4
            var authenticationProviderKey = "UserGatewayKey";
            services.AddAuthentication("Bearer")
               .AddIdentityServerAuthentication(authenticationProviderKey, options =>
               {
                   options.Authority = "http://localhost:7200";
                   options.ApiName = "UserApi";
                   options.RequireHttpsMetadata = false;
                   options.SupportedTokens = SupportedTokens.Both;
               });
            #endregion

            services
                .AddOcelot()
                .AddConsul()
                .AddCacheManager(x =>
                {
                    x.WithDictionaryHandle();//默认字典存储
                })
                .AddPolly();
            //services.AddControllers();

            services.AddSingleton<IOcelotCache<CachedResponse>, CustomCache>();
            //这里的IOcelotCache<CachedResponse>是默认缓存的约束--准备替换成自定义的
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseOcelot();//整个进程的管道换成Ocelot

            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}

            //app.UseHttpsRedirection();

            //app.UseRouting();

            //app.UseAuthorization();

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapControllers();
            //});
        }
    }
    /// <summary>
    /// 自定义的Cache 实现IOcelotCache<CachedResponse>
    /// </summary>
    public class CustomCache : IOcelotCache<CachedResponse>
    {
        private class CacheDataModel
        {
            public CachedResponse CachedResponse { get; set; }
            public DateTime Timeout { get; set; }
            public string Region { get; set; }
        }

        private static Dictionary<string, CacheDataModel> CustomCacheDictionary = new
             Dictionary<string, CacheDataModel>();

        /// <summary>
        /// 没做过期清理  所以需要
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="ttl"></param>
        /// <param name="region"></param>
        public void Add(string key, CachedResponse value, TimeSpan ttl, string region)
        {
            Console.WriteLine($"This is {nameof(CustomCache)}.{nameof(Add)}");
            //CustomCacheDictionary.Add(key, new CacheDataModel()
            //{
            //    CachedResponse = value,
            //    Region = region,
            //    Timeout = DateTime.Now.Add(ttl)
            //});
            CustomCacheDictionary[key] = new CacheDataModel()
            {
                CachedResponse = value,
                Region = region,
                Timeout = DateTime.Now.Add(ttl)
            };
        }

        public void AddAndDelete(string key, CachedResponse value, TimeSpan ttl, string region)
        {
            Console.WriteLine($"This is {nameof(CustomCache)}.{nameof(AddAndDelete)}");
            CustomCacheDictionary[key] = new CacheDataModel()
            {
                CachedResponse = value,
                Region = region,
                Timeout = DateTime.Now.Add(ttl)
            };
        }

        public void ClearRegion(string region)
        {
            Console.WriteLine($"This is {nameof(CustomCache)}.{nameof(ClearRegion)}");
            var keyList = CustomCacheDictionary.Where(kv => kv.Value.Region.Equals(region)).Select(kv => kv.Key);
            foreach (var key in keyList)
            {
                CustomCacheDictionary.Remove(key);
            }
        }

        public CachedResponse Get(string key, string region)
        {
            Console.WriteLine($"This is {nameof(CustomCache)}.{nameof(Get)}");
            if (CustomCacheDictionary.ContainsKey(key) && CustomCacheDictionary[key] != null
                && CustomCacheDictionary[key].Timeout > DateTime.Now
                && CustomCacheDictionary[key].Region.Equals(region))
            {
                return CustomCacheDictionary[key].CachedResponse;
            }
            else
                return null;
        }
    }
}
