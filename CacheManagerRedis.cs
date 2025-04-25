using NServiceKit.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace TesteRedis
{
    public static class CacheManagerRedis
    {
        public static LZOCompressor Compressor = new LZOCompressor();

        public static T getAndSet<T>(string key, Func<T> factoryFunction)
        {
            try
            {
                using (var redisClient = new RedisClient(ConfigurationManager.AppSettings["IpRedis"], Convert.ToInt32(ConfigurationManager.AppSettings["PortaRedis"])))
                {
                    var res = redisClient.Get<T>(key);

                    if (res == null)
                    {
                        res = factoryFunction();

                        redisClient.Set<T>(key, res);

                    }

                    return res;
                }

                return factoryFunction();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("getAndSet - Erro:[{0}]", ex.Message));
                return factoryFunction();
            }
        }

        public static T getAndSet<T>(string key, Func<T> factoryFunction, bool gravaNoCache)
        {
            try
            {
                using (var redisClient = new RedisClient(ConfigurationManager.AppSettings["IpRedis"], Convert.ToInt32(ConfigurationManager.AppSettings["PortaRedis"])))
                {
                    var res = redisClient.Get<T>(key);

                    if (res == null)
                    {
                        res = factoryFunction();
                        if (gravaNoCache)
                        {
                            redisClient.Set<T>(key, res);
                        }
                    }

                    return res;
                }

                return factoryFunction();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("getAndSet - Erro:[{0}]", ex.Message));
                return factoryFunction();
            }
        }

        public static T GetCompress<T>(string key, T defaultValue)
        {
            try
            {
                using (var redisClient = new RedisClient(ConfigurationManager.AppSettings["IpRedis"], Convert.ToInt32(ConfigurationManager.AppSettings["PortaRedis"])))
                {
                    var retKey = get<byte[]>(key, null);

                    if (retKey != null && retKey.Length > 0)
                    {
                        var decompressed = Compressor.Decompress(retKey);
                        string objectString = Encoding.Default.GetString(decompressed);
                        T objectDeserialized = NServiceKit.Text.JsonSerializer.DeserializeFromString<T>(objectString);

                        return objectDeserialized;
                    }
                    else
                    {
                        return defaultValue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("GetCompress - Erro:[{0}]", ex.Message));
            }

            return defaultValue;
        }

        public static void SetCompress<T>(string key, T value)
        {
            byte[] compressed = CompressValue<T>(value);

            set<byte[]>(key, compressed);
        }

        public static byte[] CompressValue<T>(T value)
        {
            try
            {
                if (value != null)
                {
                    var objString = NServiceKit.Text.JsonSerializer.SerializeToString(value);
                    byte[] arraybytes = Encoding.Default.GetBytes(objString);
                    byte[] compressed = Compressor.Compress(arraybytes);

                    return compressed;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("CompressValue - Erro:[{0}]", ex.Message));
            }

            return null;
        }

        public static void set<T>(string key, T value)
        {

            try
            {
                using (var redisClient = new RedisClient(ConfigurationManager.AppSettings["IpRedis"], Convert.ToInt32(ConfigurationManager.AppSettings["PortaRedis"])))
                {
                    redisClient.Set<T>(key, value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("set - Erro:[{0}]", ex.Message));
            }
        }

        public static void setWithExpire<T>(string key, T value, TimeSpan timeSpanExpire)
        {
            try
            {
                using (var redisClient = new RedisClient(ConfigurationManager.AppSettings["IpRedis"], Convert.ToInt32(ConfigurationManager.AppSettings["PortaRedis"])))
                {
                    var res = redisClient.Get<T>(key);

                    if (res != null)
                    {
                        redisClient.Set<T>(key, value, timeSpanExpire);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("setWithExpire - Erro:[{0}]", ex.Message));
            }
        }

        public static void remove<T>(string key)
        {
            try
            {
                using (var redisClient = new RedisClient(ConfigurationManager.AppSettings["IpRedis"], Convert.ToInt32(ConfigurationManager.AppSettings["PortaRedis"])))
                {
                    redisClient.Remove(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("remove - Erro:[{0}]", ex.Message));
            }
        }

        public static void removePattern(string pattern)
        {
            try
            {
                using (var redisClient = new RedisClient(ConfigurationManager.AppSettings["IpRedis"], Convert.ToInt32(ConfigurationManager.AppSettings["PortaRedis"])))
                {
                    var keys = redisClient.SearchKeys(pattern);
                    redisClient.RemoveAll(keys);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("removePattern - Erro:[{0}]", ex.Message));
            }
        }

        public static T get<T>(string key, T defaultValue)
        {
            try
            {
                using (var redisClient = new RedisClient(ConfigurationManager.AppSettings["IpRedis"], Convert.ToInt32(ConfigurationManager.AppSettings["PortaRedis"])))
                {
                    return redisClient.Get<T>(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("get - Erro:[{0}]", ex.Message));
            }

            return defaultValue;
        }


        public static long decrement(string key)
        {
            try
            {
                using (var redisClient = new RedisClient(ConfigurationManager.AppSettings["IpRedis"], Convert.ToInt32(ConfigurationManager.AppSettings["PortaRedis"])))
                {
                    return redisClient.DecrementValue(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("decrement - Erro:[{0}]", ex.Message));
            }

            return -1;
        }

        public static List<T> getList<T>(string key)
        {
            try
            {
                using (var redisClient = new RedisClient(ConfigurationManager.AppSettings["IpRedis"], Convert.ToInt32(ConfigurationManager.AppSettings["PortaRedis"])))
                {
                    var keys = redisClient.SearchKeys(key).ToList();
                    var res = redisClient.GetValues<T>(keys);

                    if (keys.Count == 0)//se nao achou nenhuma chave.. nao foi inserido no redis ainda, logo retorna null
                    {
                        return null;
                    }

                    return res;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("getList - Erro:[{0}]", ex.Message));
                return null;
            }
        }

        public static void removeList(string key)
        {
            try
            {
                using (var redisClient = new RedisClient(ConfigurationManager.AppSettings["IpRedis"], Convert.ToInt32(ConfigurationManager.AppSettings["PortaRedis"])))
                {
                    redisClient.RemoveAll(redisClient.SearchKeys(key).ToList());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("removeList - Erro:[{0}]", ex.Message));
            }
        }

        public static void renameKey(string keyOld, string keyNew)
        {
            try
            {
                using (var redisClient = new RedisClient(ConfigurationManager.AppSettings["IpRedis"], Convert.ToInt32(ConfigurationManager.AppSettings["PortaRedis"])))
                {
                    redisClient.RenameKey(keyOld, keyNew);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("renameKey - Erro:[{0}]", ex.Message));
            }
        }

        public static void ExpireIn(string key, TimeSpan time)
        {
            using (var redisClient = new RedisClient(ConfigurationManager.AppSettings["IpRedis"], Convert.ToInt32(ConfigurationManager.AppSettings["PortaRedis"])))
            {
                redisClient.ExpireEntryIn(key, time);
            }
        }
    }
}