using NServiceKit.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace TesteRedis
{
    public class CacheManagerRedisPersistente
    {

        private static CacheManagerRedisPersistente instance;

        private CacheManagerRedisPersistente()
        {
            try
            {
                RedisProvider.Pool = new PooledRedisClientManager(new[] { ConfigurationManager.AppSettings["IpRedis"] + ":" + ConfigurationManager.AppSettings["PortaRedis"] });
            }
            catch
            {
            }
        }

        public static CacheManagerRedisPersistente Instance
        {
            get
            {
                try
                {
                    if (instance == null)
                    {
                        instance = new CacheManagerRedisPersistente();
                    }
                    return instance;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        public T getAndSet<T>(string key, Func<T> factoryFunction)
        {
            try
            {
                using (var redis = RedisProvider.GetClient())
                {
                    var res = redis.Get<T>(key);

                    if (res == null)
                    {
                        res = factoryFunction();

                        redis.Set<T>(key, res);
                    }

                    return res;
                }
            }
            catch
            {
                return factoryFunction();
            }
        }

        public T getAndSet<T>(string key, Func<T> factoryFunction, bool gravaNoCache)
        {
            try
            {
                using (var redis = RedisProvider.GetClient())
                {
                    var res = redis.Get<T>(key);

                    if (res == null)
                    {
                        res = factoryFunction();

                        if (gravaNoCache)
                        {
                            redis.Set<T>(key, res);
                        }
                    }

                    return res;
                }
            }
            catch
            {
                return factoryFunction();
            }
        }

        public void set<T>(string key, T value)
        {
            try
            {
                using (var redis = RedisProvider.GetClient())
                {
                    redis.Set<T>(key, value);
                }
            }
            catch
            {
            }
        }

        public void setWithExpire<T>(string key, T value, TimeSpan timeSpanExpire)
        {
            try
            {
                using (var redis = RedisProvider.GetClient())
                {
                    var res = redis.Get<T>(key);

                    if (res != null)
                    {
                        redis.Set<T>(key, value, timeSpanExpire);
                    }
                }
            }
            catch
            {
            }
        }

        public void remove<T>(string key)
        {
            try
            {
                using (var redis = RedisProvider.GetClient())
                {
                    redis.Remove(key);
                }
            }
            catch
            {
            }
        }

        public void removePattern(string pattern)
        {
            try
            {
                using (var redis = RedisProvider.GetClient())
                {
                    var keys = redis.SearchKeys(pattern);
                    redis.RemoveAll(keys);
                }
            }
            catch
            {
            }
        }

        public T get<T>(string key, T defaultValue)
        {
            try
            {
                using (var redis = RedisProvider.GetClient())
                {
                    var res = redis.Get<T>(key);

                    if (res != null)
                    {
                        return res;
                    }
                    else
                    {
                        return defaultValue;
                    }
                }
            }
            catch
            {
            }
            return defaultValue;
        }

        public long decrement(string key)
        {
            try
            {
                using (var redis = RedisProvider.GetClient())
                {
                    return redis.DecrementValue(key);
                }
            }
            catch
            {
            }
            return -1;
        }

        public List<T> getList<T>(string key)
        {
            try
            {
                using (var redis = RedisProvider.GetClient())
                {
                    var keys = redis.SearchKeys(key).ToList();
                    var res = redis.GetValues<T>(keys);

                    if (keys.Count == 0)//se nao achou nenhuma chave.. nao foi inserido no redis ainda, logo retorna null
                    {
                        return null;
                    }

                    return res;
                }
            }
            catch
            {
                return null;
            }
        }

        public void removeList(string key)
        {
            try
            {
                using (var redis = RedisProvider.GetClient())
                {
                    redis.RemoveAll(redis.SearchKeys(key).ToList());
                }
            }
            catch
            {
            }
        }

        public void renameKey(string keyOld, string keyNew)
        {
            try
            {
                using (var redis = RedisProvider.GetClient())
                {
                    redis.RenameKey(keyOld, keyNew);
                }
            }
            catch
            {
            }
        }

        public void ExpireIn(string key, TimeSpan time)
        {
            using (var redis = RedisProvider.GetClient())
            {
                redis.ExpireEntryIn(key, time);
            }
        }

        public static Configuration retornaAppConfig()
        {
            ExeConfigurationFileMap exefMap = new ExeConfigurationFileMap();
            string dirPath = AppDomain.CurrentDomain.BaseDirectory.ToString();
            exefMap.ExeConfigFilename = dirPath + "\\" + "App.config";
            Configuration conf = null;

            if (File.Exists(exefMap.ExeConfigFilename))
            {
                conf = ConfigurationManager.OpenMappedExeConfiguration(exefMap, ConfigurationUserLevel.None);
            }
            else
            {
                string[] files = Directory.GetFiles(dirPath);

                for (int i = 0; i < files.Length; i++)
                {
                    if (files[i].EndsWith(".config"))
                    {
                        exefMap.ExeConfigFilename = files[i];
                        conf = ConfigurationManager.OpenMappedExeConfiguration(exefMap, ConfigurationUserLevel.None);
                        break;
                    }
                }
            }
            return conf;
        }
    }

    public static class RedisProvider
    {
        public static IRedisClientCacheManager Pool { get; set; }

        public static RedisClient GetClient()
        {
            return (RedisClient)Pool.GetClient();
        }
    }
}