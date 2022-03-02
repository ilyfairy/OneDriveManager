using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ilyfairy.Config
{
    public class AppConfig
    {
        public static AppConfig Instance { get; set; } = new("config.json");
        

        private JObject config { get; set; } = new();

        public string ConfigPath { get; }

        public AppConfig(string configPath)
        {
            ConfigPath = Path.GetFullPath(configPath);
        }
        public void Init<T>(string key, T value)
        {
            if (TryGetOrDefault<T>(key, out _))
            {
                return;
            }
            else
            {
                Set<T>(key, value);
            }
        }
        public bool ReadFileConfig()
        {
            string json = "{}";
            try
            {
                json = File.ReadAllText(ConfigPath);
                var obj = JObject.Parse(json);
                //this.config = node;
                foreach (var item in obj)
                {
                    Set(item.Key, item.Value);
                    //Set(item.Key, item.Value.GetValue<object>());
                    //try
                    //{
                    //}
                    //catch (Exception)
                    //{

                    //}
                }
            }
            catch (Exception)
            {
                File.Create(ConfigPath).Close();
                return false;
            }
            return true;
        }
        public bool FileExists()
        {
            return File.Exists(ConfigPath);
        }
        public string? Get(string key, string defaultValue = "")
        {
            return Get<string>(key, defaultValue);
        }
        public T? Get<T>(string key, T defaultValue = default)
        {
            if (config.TryGetValue(key, out var tmp))
            {
                T val = default;
                try
                {
                    val = JsonConvert.DeserializeObject<T>(tmp.ToString());
                }
                catch (Exception)
                {
                    val = config.Value<T>(key);
                }
                return val;
                //return tmp.Value<T>();
            }
            else
            {
                Set<T>(key, defaultValue);
                return defaultValue;
            }
        }

        public bool Remove(string key)
        {
            if (key == null) return false;
            return config.Remove(key);
        }

        public bool TryGetOrDefault<T>(string key, out T value)
        {
            if (config.TryGetValue(key, out var tmp))
            {
                string str = tmp.ToString();
                try
                {
                    value = JsonConvert.DeserializeObject<T>(str);
                }
                catch (Exception)
                {
                    //Console.WriteLine("序列化异常");
                    value = tmp.Value<T>();
                }

                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
        public void Set(string key, string value)
        {
            Set<string>(key, value);
        }

        public void Set<T>(string key, T value)
        {
            config[key] = JToken.FromObject(value);
        }
        public bool Save()
        {
            lock (this)
            {
                var json = config.ToString();
                try
                {
                    File.WriteAllText(ConfigPath, json, Encoding.UTF8);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
