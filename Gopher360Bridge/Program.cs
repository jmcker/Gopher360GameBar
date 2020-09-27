using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Gopher360Bridge
{
    class Program
    {
        static void Main(string[] args)
        {
            string home = Path.Combine(Environment.GetEnvironmentVariable("HOMEDRIVE"), Environment.GetEnvironmentVariable("HOMEPATH"));
            string installation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(installation, "Assets", "Gopher.exe");
            path = "C:\\Users\\jmcke\\source\\repos\\Gopher360GameBar\\Gopher360GameBar\\Assets\\Gopher.exe";

            Console.WriteLine(home);
            Console.WriteLine(path);
            Console.WriteLine(Directory.GetCurrentDirectory());
            Console.ReadLine();

            ProcessStartInfo pi = new ProcessStartInfo(path);
            pi.WorkingDirectory = home;
            pi.WindowStyle = ProcessWindowStyle.Hidden;

            Process proc = new Process();
            proc.StartInfo = pi;
            proc.Start();
        }
    }
}
