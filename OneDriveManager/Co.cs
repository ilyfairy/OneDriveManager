namespace Ilyfairy.Tools
{
    public class Co
    {
        private List<(string, ConsoleColor, ConsoleColor, bool)> list = new();

        public static Co Crate(string text, ConsoleColor force = ConsoleColor.Gray, ConsoleColor back = ConsoleColor.Black)
        {
            Co co = new Co();
            co.list.Add((text, force, back, false));
            return co;
        }

        public Co Write(string text, ConsoleColor force = ConsoleColor.Gray, ConsoleColor back = ConsoleColor.Black)
        {
            list.Add((text, force, back, false));
            return this;
        }
        public Co WriteLine(string text, ConsoleColor force = ConsoleColor.Gray, ConsoleColor back = ConsoleColor.Black)
        {
            list.Add((text, force, back, true));
            return this;
        }
        
        public void Show()
        {
            foreach (var item in list)
            {
                var old_fore = Console.ForegroundColor;
                var old_back = Console.BackgroundColor;
                Console.ForegroundColor = item.Item2;
                Console.BackgroundColor = item.Item3;
                Console.Write(item.Item1);
                if (item.Item4) Console.Write('\n');
                Console.ForegroundColor = old_fore;
                Console.BackgroundColor = old_back;
            }
            list.Clear();
        }
    }
}