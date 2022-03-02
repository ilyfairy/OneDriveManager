using KoenZomers.OneDrive.Api.Entities;
using System.Text.RegularExpressions;

namespace Ilyfairy.Tools
{
    public class ConsoleManager
    {
        public string CurrentPath { get; set; }
        //public string CurrentItem { get; set; }
        public OneDrive drive = new();
        public List<OneDriveItem> CurrentFiles = new();
        private bool isGetCurrentFiles = false;

        private Task listTask = null;

        public ConsoleManager(string startPath = "/")
        {
            CurrentPath = startPath;
        }

        public bool Login()
        {
            try
            {
                Console.WriteLine("正在初始化");
                drive.Init();
                Console.WriteLine("正在验证...");
                drive.Authenticate();
                Console.WriteLine("验证成功");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"异常: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 显示目录前缀
        /// </summary>
        public void ShowPath()
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(CurrentPath);
            Console.ForegroundColor = old;
            Console.Write($" # ");
            Console.Title = $"OneDrive";
        }

        public static string Combine(params string[] paths)
        {
            var _c = '/';
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = paths[i].Replace('\\', _c);
            }
            bool isRoot = false;

            if (paths[0][..1] == _c.ToString())
            {
                isRoot = true;
            }
            IList<string> result = new List<string>();
            foreach (var path in paths)
            {
                foreach (var item in path.Split(_c))
                {
                    if (item == "")
                    {
                        continue;
                    }
                    result.Add(item);
                }
            }

            if (isRoot)
            {
                return $"{_c}{string.Join(_c, result)}";
            }
            else
            {
                return $"{string.Join(_c, result)}";
            }
        }

        /// <summary>
        /// 获取上一级目录
        /// </summary>
        /// <returns></returns>
        public string GetUpperPath()
        {
            var split = CurrentPath.Split('/').Where(v => v != "");
            if (split.Count() <= 1) return "/";
            return $"/{split.SkipLast(1).Last()}";
        }

        public string GetTargetPath(string path)
        {
            //if (path)
            //{

            //}
            if (path == ".") return CurrentPath;
            if (path == "..") return GetUpperPath();
            return Combine(CurrentPath, path);
        }


        /// <summary>
        /// ls 列出当前目录文件
        /// </summary>
        public void List(bool isShow = true)
        {
            var items = drive.GetDirectory(CurrentPath);
            CurrentFiles.Clear();
            int i = 0;
            if (isShow)
            {
                foreach (var item in items)
                {
                    i++;
                    CurrentFiles.Add(item);

                    char type = '-';
                    if (item.Folder != null) type = 'd';
                    string size;

                    if (item.Size > 1 * 1000 * 1000 * 1000L) size = $"{item.Size / 1000000000.0:f2}GB";
                    else if (item.Size > 1 * 1000 * 1000) size = $"{item.Size / 1000000.0:f2}MB";
                    else if (item.Size > 1 * 1000) size = $"{item.Size / 1000.0:f2}KB";
                    else size = (item.Size).ToString();
                    Co.Crate($"{type} {item.LastModifiedDateTime,-18:G}\t{size,-12}")
                        .Write($" {i,-3} ", ConsoleColor.Magenta)
                        .WriteLine($"{item.Name}").Show();
                    //Console.WriteLine($"{type} {item.LastModifiedDateTime,-18:G}\t{size,-12} {i,-3} {item.Name}");
                }
            }
            isGetCurrentFiles = true;
        }

        /// <summary>
        /// cd 修改目录
        /// </summary>
        /// <param name="path"></param>
        public void ChangeDirectory(string path)
        {
            var new_path = GetTargetPath(path);
            if (new_path == CurrentPath) return;

            CurrentPath = new_path;
            CurrentFiles.Clear();
            isGetCurrentFiles = false;
            //List(false);

            //if (path == "..")
            //{
            //    var tmp = GetUpperPath();
            //    if (CurrentPath != tmp)
            //    {
            //        CurrentFiles.Clear();
            //        isGetCurrentFiles = false;
            //        List(false);
            //    }
            //    CurrentPath = tmp;
            //    return;
            //}
            //if (path == ".") return;

            //if (!int.TryParse(path, out var index) || index <= 0 || index > CurrentFiles.Count)
            //{
            //    Console.WriteLine("索引错误");
            //    return;
            //}
            //CurrentPath = $"{(CurrentPath == "/" ? "" : CurrentPath)}/{CurrentFiles[index - 1].Name}";
            //CurrentFiles.Clear();
            //isGetCurrentFiles = false;
        }

        /// <summary>
        /// url 获取文件链接
        /// </summary>
        /// <param name="path"></param>
        public void GetUrl(string path)
        {
            if (!int.TryParse(path, out var index) || index <= 0 || index > CurrentFiles.Count)
            {
                Console.WriteLine("索引错误");
                return;
            }
            if (CurrentFiles[index - 1].Folder != null)
            {
                Console.WriteLine("这是一个目录");
                return;
            }
            var url = drive.GetDownloadUrl(CurrentFiles[index - 1]);
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"下载链接为:");
            Console.ForegroundColor = old;
            Console.WriteLine(url);
        }

        /// <summary>
        /// down 下载文件
        /// </summary>
        /// <param name="path"></param>
        public void DownloadToDir(string path)
        {
            if (!int.TryParse(path, out var index) || index <= 0 || index > CurrentFiles.Count)
            {
                Console.WriteLine("索引错误");
                return;
            }
            if (CurrentFiles[index - 1].Folder != null)
            {
                Console.WriteLine("这是一个目录");
                return;
            }
            var task = drive.DownloadToDir(CurrentFiles[index - 1], out var down, Directory.GetCurrentDirectory(), (e) =>
            {
                string speed;
                if (e.BytesPerSecondSpeed > 1000000)
                {
                    speed = $"{e.BytesPerSecondSpeed / 1000000:f2}MB/s";
                }
                else if (e.BytesPerSecondSpeed > 1000)
                {
                    speed = $"{e.BytesPerSecondSpeed / 1000:f2}KB/s";
                }
                else
                {
                    speed = $"{e.BytesPerSecondSpeed:f2}B/s";
                }
                Console.Title = $"正在下载 {speed}  {e.ProgressPercentage:f2}%";
            });

            if (task == null)
            {
                Console.WriteLine("下载失败");
                return;
            }

            bool isCache = false;
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = false;
                down.CancelAsync();
                isCache = true;
                down.Clear();
                down.Dispose();
                Console.WriteLine("取消下载");
            };
            task.Wait();

            Console.WriteLine("下载完成");
        }

        /// <summary>
        /// 刷新文件列表
        /// </summary>
        public void RefFileList()
        {
            drive.RefCache(CurrentPath);
            new Co().WriteLine("刷新缓存成功").Show();
        }

        /// <summary>
        /// upload 上传文件
        /// </summary>
        /// <param name="path"></param>
        public void UploadFile(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("文件不存在");
                return;
            }

            var r = drive.UploadFile(path, CurrentPath);
            if (r != null)
            {
                new Co().Write("上传成功", ConsoleColor.Green).WriteLine($": {CurrentPath}/{r.Name}").Show();
            }
            else
            {
                Console.WriteLine("上传失败");
            }
        }

        public OneDriveItem GetIndexOrItem(string str_index, bool allowDir = false)
        {
            OneDriveItem item = null;
            if (!isGetCurrentFiles)
            {
                List(false);
            }
            if (int.TryParse(str_index, out var index) || index <= 0 || index > CurrentFiles.Count)
            {
                item = CurrentFiles[index - 1];
            }
            else if (allowDir)
            {
                
            }
            else
            {
                Console.WriteLine("找不到文件或索引错误");
            }

            if (item.Folder != null && !allowDir)
            {
                Console.WriteLine("这是一个目录");
                return null;
            }

            return item;
        }

        /// <summary>
        /// rm 删除文件或目录
        /// </summary>
        public void Delete(OneDriveItem item)
        {
            if (item == null)
            {
                Console.WriteLine("找不到指定目录");
                return;
            }

            var su = drive.DeleteItem(item);
            if (su)
            {
                Console.WriteLine($"删除成功: {item.Name}");
            }
            else
            {
                Console.WriteLine("删除失败");
            }
        }

        /// <summary>
        /// rm 删除文件或目录
        /// </summary>
        public void Delete(string path)
        {
            var item = GetIndexOrItem(path, true);
            Delete(item);
        }
    }
}