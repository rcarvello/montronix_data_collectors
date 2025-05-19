using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDataCollector
{
    class LogFileManager
    {
        private int ChekInterval;
        private int CheckCounter = 0;
        private string LogFileName;
        public LogFileManager()
        {
            this.ChekInterval = Int32.Parse(ConfigurationManager.AppSettings.Get("log_check_interval"));
            this.LogFileName = this.GetLogFileBaseName();
        }

        public String GetLogFileBaseName()
        {
            String DeviceId = ConfigurationManager.AppSettings.Get("log_device_id");
            String LogBasePath = ConfigurationManager.AppSettings.Get("log_base_path");
            string FileBaseName = "log-" + DeviceId + "-" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            // string CurrentDir = System.Environment.CurrentDirectory;
            // Console.WriteLine("Check file name");
            return LogBasePath + "\\" + FileBaseName;
        }

        public void AddLine( String line)
        {
            this.CheckCounter++;
            if (this.CheckCounter > this.ChekInterval)
            {
                this.LogFileName = this.GetLogFileBaseName();
                this.CheckCounter = 0;
            }

            if (!File.Exists(this.LogFileName))
            {
                using (StreamWriter sw = File.CreateText(this.LogFileName))
                {
                    sw.WriteLine(line);
                    // Console.WriteLine("Create");
                }
            } else
            {
                using (StreamWriter sw = File.AppendText(this.LogFileName))
                {
                    sw.WriteLine(line);
                    // Console.WriteLine("Append");
                }
            }
        }

    }
}
