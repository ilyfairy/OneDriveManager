using Downloader;
using Ilyfairy.Config;
using KoenZomers.OneDrive.Api;
using KoenZomers.OneDrive.Api.Entities;
using System.Net;
using System.Net.Http.Headers;

namespace Ilyfairy.Tools
{
    public class OneDrive
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
        public string RefreshToken { get; set; }
        private AppConfig config = new AppConfig("config.json");
        private OneDriveApi Api;
        private DownloadConfiguration DownloadConfig;
        private Dictionary<string, OneDriveItem[]> CacheFiles = new();

        public OneDrive()
        {
            config = new AppConfig("config.json");
            config.ReadFileConfig();
            DownloadConfig = new DownloadConfiguration()
            {
                BufferBlockSize = 10240, // usually, hosts support max to 8000 bytes, default values is 8000
                ChunkCount = 8, // file parts to download, default value is 1
                MaxTryAgainOnFailover = int.MaxValue, // the maximum number of times to fail
                OnTheFlyDownload = false, // caching in-memory or not? default values is true
                ParallelDownload = true, // download parts of file as parallel or not. Default value is false
                Timeout = 1000, // timeout (millisecond) per stream block reader, default values is 1000
                RequestConfiguration = // config and customize request headers
                    {
                        Accept = "*/*",
                        CookieContainer =  new CookieContainer(), // Add your cookies
                        Headers = new WebHeaderCollection(), // Add your custom headers
                        KeepAlive = false,
                        ProtocolVersion = HttpVersion.Version11, // Default value is HTTP 1.1
                        UseDefaultCredentials = false,
                    }
            };
        }

        public void Init()
        {
            config.TryGetOrDefault("client_id", out string client_id);
            config.TryGetOrDefault("client_secret", out string client_secret);
            config.TryGetOrDefault("redirect_uri", out string redirect_uri);

            if (string.IsNullOrWhiteSpace(client_id))
            {
                Console.Write("请输入应用ID: ");
                client_id = Console.ReadLine();
                config.Set("client_id", client_id);
            }
            if (string.IsNullOrWhiteSpace(client_secret))
            {
                Console.Write("请输入应用机密: ");
                client_secret = Console.ReadLine();
                config.Set("client_secret", client_secret);
            }
            if (string.IsNullOrWhiteSpace(redirect_uri))
            {
                Console.Write("请输入重定向URL: ");
                redirect_uri = Console.ReadLine();
                config.Set("redirect_uri", redirect_uri);
            }

            config.Save();

            try
            {
                Api = new OneDriveGraphApi(client_id, client_secret);
            }
            catch (Exception)
            {
                throw new Exception("Api初始化异常");
            }
            if (Api == null)
            {
                throw new Exception("Api初始化异常 null");
            }
            Api.AuthenticationRedirectUrl = redirect_uri;
        }

        public void Authenticate()
        {
            if (!config.TryGetOrDefault("refresh_token", out string refresh_token))
            {
                Console.Clear();
                Console.WriteLine("请打开以下链接：");
                Console.WriteLine(Api.GetAuthenticationUri());
                Console.Write("请输入code: ");
                var code = Api.GetAuthorizationTokenFromUrl(Console.ReadLine());
                Console.Clear();
                Console.WriteLine($"code:\n{code}");
                var token = Api.GetAccessToken().Result;
                if (token == null)
                {

                    throw new Exception("Token验证错误");
                }

                refresh_token = Api.AccessToken.RefreshToken;
                config.Set("refresh_token", refresh_token);
                config.Save();

            }

            Api.AuthenticateUsingRefreshToken(refresh_token).Wait();
            config.Set("refresh_token", refresh_token);
            config.Save();
        }

        /// <summary>
        /// 获取目录文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public OneDriveItem[] GetDirectory(string path)
        {
            if (CacheFiles.TryGetValue(path, out var tmp))
            {
                return tmp;
            }
            path = $"/{string.Join("/", path.Split('/').Where(v => v != "").Select(v => WebUtility.UrlEncode(v)))}";
            var items = Api.GetChildrenByPath(path).Result.Collection;
            CacheFiles[path] = items;
            return items;
        }

        public string GetDownloadUrl(OneDriveItem item)
        {
            // 构造完整的 URL 来调用
            string completeUrl;
            if (item.RemoteItem != null)
            {
                // 要下载的项目是从另一个驱动器共享的
                completeUrl = string.Concat("drives/", item.RemoteItem.ParentReference.DriveId, "/items/", item.RemoteItem.Id, "/content");
            }
            else if (item.ParentReference != null && !string.IsNullOrEmpty(item.ParentReference.DriveId))
            {
                // 要下载的项目是从另一个驱动器共享的
                completeUrl = string.Concat("drives/", item.ParentReference.DriveId, "/items/", item.Id, "/content");
            }
            else
            {
                // 要下载的项目驻留在当前用户的驱动器上
                completeUrl = string.Concat("drive/items/", item.Id, "/content");
            }

            completeUrl = Api.ConstructCompleteUrl(completeUrl);
            var accessToken = Api.GetAccessToken().Result;

            // 创建一个 HTTPClient 实例与 One Drive 的 REST API 通信
            var httpClientHandler = new HttpClientHandler
            {
                UseDefaultCredentials = Api.ProxyCredential == null,
                UseProxy = Api.ProxyConfiguration != null,
                Proxy = Api.ProxyConfiguration,
                AllowAutoRedirect = false
            };

            // 检查我们是否需要代理的特定凭据
            if (Api.ProxyCredential != null && httpClientHandler.Proxy != null)
            {
                httpClientHandler.Proxy.Credentials = Api.ProxyCredential;
            }
            var client = new HttpClient(httpClientHandler);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", accessToken.AccessToken);


            // 将请求发送到 One Drive API
            var response = client.GetAsync(completeUrl).Result;

            return response.Headers.Location?.ToString();
        }

        public Task DownloadToDir(OneDriveItem file, out DownloadService down, string dir = null, Action<Downloader.DownloadProgressChangedEventArgs> progressCallback = null)
        {
            down = null;
            string url = GetDownloadUrl(file);
            if (url == null) return null;
            if (dir == null) dir = Directory.GetCurrentDirectory();

            down = new DownloadService(DownloadConfig);

            down.DownloadProgressChanged += (sender, e) => progressCallback?.Invoke(e);
            var task = down.DownloadFileTaskAsync(url, Path.Combine(dir, file.Name));
            return task;
        }

        public void RefCache(string path, bool isall = false)
        {
            if (isall) CacheFiles.Clear();
            CacheFiles.Remove(path);
        }

        public OneDriveItem GetItem(string path)
        {
            OneDriveItem item;
            if (path == "/")
            {
                item = Api.GetDriveRoot().Result;
            }
            else
            {
                item = Api.GetItem(path).Result;
            }
            if (item == null) return null;
            return item;
        }

        public OneDriveItem UploadFile(string file, string onedriveDir)
        {
            var dir = GetItem(onedriveDir);
            if (dir == null || dir.Folder == null) return null;

            return Api.UploadFileAs(file, Path.GetFileName(file), dir).Result;
        }

        public bool DeleteItem(OneDriveItem item)
        {
            return Api.Delete(item).Result;
        }

    }
}