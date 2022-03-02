
using NullLib.CommandLine;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Ilyfairy.Tools
{
    public class OneDriveCommand
    {
        public ConsoleManager manager = new();
        public OneDriveCommand()
        {
            if (!manager.Login()) return;
        }

        [Command]
        public void Help()
        {
            Console.WriteLine("clear\t清屏");
            Console.WriteLine("ls\t列出目录文件");
            Console.WriteLine("cd\t跳转目录");
            Console.WriteLine("ref\t刷新当前目录文件缓存");
            Console.WriteLine("url\t获取文件链接");
            Console.WriteLine("down\t下载文件");
            Console.WriteLine("upload\t上传文件");
            Console.WriteLine("rm\t删除文件");
        }

        [Command(CommandAlias = "clear")]
        public void Clear()
        {
            Console.Clear();
        }

        [Command(CommandAlias = "ls")]
        public void ListFile()
        {
            manager.List();
        }

        [Command(CommandAlias = "cd")]
        public void ChangeDirectory(string path_or_index)
        {
            manager.ChangeDirectory(path_or_index);
        }

        [Command(CommandAlias = "url")]
        public void GetUrl(string path_or_index)
        {
            manager.GetUrl(path_or_index);
        }

        [Command(CommandAlias = "down")]
        public void Download(string path_or_index)
        {
            manager.DownloadToDir(path_or_index);
        }

        [Command(CommandAlias = "ref")]
        public void Refresh()
        {
            manager.RefFileList();
        }

        [Command(CommandAlias = "upload")]
        public void Upload(string localFile)
        {
            manager.UploadFile(localFile);
        }

        [Command(CommandAlias = "rm")]
        public void Rm(string str1, string str2 = null, string str3 = null)
        {
            bool isF = false;
            bool isR = false;
            var arr = new string[] { str1, str2, str3 }.Where(v => v != null).ToList();
            string path = null;
            foreach (var item in arr.ToArray())
            {
                if (item == "-rm")
                {
                    arr.Remove(item);
                    isF = true;
                    isR = true;
                }
                else if (item == "-r")
                {
                    arr.Remove(item);
                    isR = true;
                }
                else if (item == "-f")
                {
                    arr.Remove(item);
                    isF = true;
                }
                else
                {
                    if (path == null) path = item;
                }
            }

            var r = manager.GetIndexOrItem(path,true);
            if (r.Folder != null && !isR)
            {
                Console.WriteLine("这是一个目录 请指定-r");
                return;
            }
            if (r == null)
            {
                Console.WriteLine("找不到指定文件");
                return;
            }
            if (!isF)
            {
                Console.Write($"是否删除目录{manager.CurrentPath}/{r.Name} ? (Y/N) ");
                var isRemove = Console.ReadLine();
                if (isRemove.ToLower() != "y") return;
            }
            manager.Delete(r);
            Console.WriteLine("删除成功");
        }

    }

    public class Program
    {
        static CommandObject<OneDriveCommand> Home;

        static void Main(string[] args)
        {
            //Console.WriteLine(Path.);



            return;
            //ConsoleManager manager = new();
            //manager.Login();

            //RootCommand root = new RootCommand("说明qwq");
            //var inputopt = new Option("ls", "列出目录文件");
            
            //root.Add(inputopt);


            Home = new();

            while (true)
            {
                Home.TargetInstance.manager.ShowPath();

                string input = Console.ReadLine();
                if (input == null) continue;
                
                //root.Invoke(input);

                //CommandParser.SplitCommandLine(input, out var r);
                //string[] cmds = r.Select(v => v.Content).ToArray();
                ////执行到这里了
                //Parser.Default.ParseArguments<ListFile>(cmds)
                //    .MapResult(
                //(ls) =>
                //{
                //    Console.WriteLine($"ls: {ls.Path}");
                //    return 0;
                //}, e => 0);



                if (Home.TryExecuteCommand(input, true, out object result))
                {


                }

            }

        }


    }
}