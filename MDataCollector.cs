
using System;
using System.ServiceProcess;
using System.Threading;

namespace MDataCollector
{

    public class MDataCollector : ServiceBase
    {
        public Thread t;
        public MDataCollector()
        {
            ServiceName = Program.ServiceName;
        }

        protected override void OnStart(string[] args)
        {
            t = new Thread( new ThreadStart(Listener));
            t.Start();
         
        }

        private void Listener()
        {
            Program.Start();
        }

        protected override void OnStop()
        {
            t.Abort();
            Program.Stop();
        }
    }

}
