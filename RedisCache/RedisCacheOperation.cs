using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using StackExchange.Redis;

namespace SimpleEchoBot.RedisCache
{
    public class RedisCacheOperation
    {
        #region【定数】Azure Redis Cache接続用
        /// <summary>
        /// Azure Redis Cache接続用定数
        /// </summary>
        public static IDatabase Connection
        {
            get
            {
                return ConnectionMultiplexer.Connect(ConfigurationManager.ConnectionStrings["RedisCache"].ConnectionString).GetDatabase();
            }
        }
        #endregion
    }
}