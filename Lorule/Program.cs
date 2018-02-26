using Darkages;
using Darkages.Types;
using System;
using System.Threading;

namespace Lorule
{
    class Program
    {
        public static Instance _Server;

        static void Main(string[] args)
        {
            var s = (short)MoodQualifer.Neutral;

            _Server = new Instance();
            _Server.Start();
            _Server.Report();
        }

        public class Instance : ServerContext
        {
            DateTime SystemStartTime = DateTime.Now;
            TimeSpan Uptime => (DateTime.Now - SystemStartTime);

            public Instance()
            {
                LoadConstants();
            }

            public bool IsRunning => Running;

            public void Report()
            {
                while (Running)
                {
                    Console.Title  = string.Format("Lorule - Server Uptime {0}", Uptime.ToString(@"hh\:mm\:ss"));
                    Thread.Sleep(5000);
                }
            }

            public static void Reboot(Instance instance)
            {
                if (Running)
                {
                    Game?.Abort();
                    Lobby?.Abort();
                    instance = null;
                }
                else
                {
                    instance = new Instance();
                    instance.Start();
                }
            }
        }
    }
}
