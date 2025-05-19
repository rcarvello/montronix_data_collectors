using System;
using System.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Client;
using System.Threading;
using System.ServiceProcess;

namespace MDataCollector
{
    class Program
    {

        public const string ServiceName = "MontronixDataCollector-108";
      
        static void Main(string[] args)
        {
            if (!Environment.UserInteractive)
                // running as service
                using (var service = new MDataCollector())
                    ServiceBase.Run(service);
            else
            {
                // running as console app
                Start();
            }
        }

        public static MqttDataClient client;


        public static void MainDebug()
        {
            String mqtt_server = ReadSetting("mqtt_server");
            MqttDataClient client = new MqttDataClient(mqtt_server);
            /*
                client.SetMqttServer("mqtt.cloudapps.cloud");
                client.SetUserName("user");
                client.SetPassword("password");
            */
            client.MqttConnectAsync().Wait();
            var publishingTasks = new List<Task>();
            for (int i = 1; i <= 10; i++)
            {
                Task publishingTask = Task.WhenAny(client.PublishAsync("sensors/sensor1", "Hello n° " + i));
                publishingTasks.Add(publishingTask);
                Console.WriteLine("Fine ciclo del ciclo: " + i);
            }

            foreach (Task runningTask in publishingTasks)
            {
                runningTask.Wait();
            }
            client.GetMqttClient().DisconnectAsync().Wait();
            Console.ReadKey();
        }

        public static void StartMontronix()
        {
            //Montronix m = 
             new Montronix();
        }

        public static MontronixLogger StartMontronixLogger()
        {
            MontronixLogger m = new MontronixLogger();
            Task.Run(() =>
            {
                m.MontronixLoggeStart();
            });
            return m;
        }

               
        public static void Start()
        {
            String previusLine = "";
            String currentLine = "";
            string use_broker = ReadSetting("use_broker");
            int ms_sleep = Int32.Parse(ReadSetting("ms_sleep"));
            string UseLogger = ReadSetting("create_log_file");
            LogFileManager Logger = new LogFileManager();
           
            if (use_broker == "true")
            {
                String mqtt_server = ReadSetting("mqtt_server");
                client = new MqttDataClient(mqtt_server);
                client.MqttConnectAsync().Wait();
            }

            String topic = ReadSetting("topic");
            MontronixLogger m = StartMontronixLogger();
    
            while (true)
            {
      
                try
                {
                   
                    // currentLine = m.csvLine;
                    if (m.csvLines.Count >= 0)
                    {
                        currentLine = m.csvLines.Dequeue();
                    }

                    if (currentLine != previusLine && !String.IsNullOrEmpty(currentLine) )
                    {
                        
                        Console.WriteLine(currentLine);
                        if (UseLogger == "true")
                            Logger.AddLine(currentLine);
                        previusLine = currentLine;
                        
                        if (use_broker == "true")
                        {

                            if (!client.GetConnection())                         
                                client.MqttConnectAsync().Wait();
                  
                            Task publishingTask = Task.WhenAny(client.PublishAsync(topic, currentLine));

                            /* Non più usato
                            client.PublishAsync("sensors/sensor1", currentLine).Wait();
                            if (m.lineBreak)
                            {
                                Console.WriteLine("");
                            }
                            */
                        }
                     }
                } catch {
                    // Int32 _Ts = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    // Console.WriteLine("-1;" + _Ts + ";" + "Data error or null value");
                    // Console.WriteLine(e.Message);
                }
                Thread.Sleep(ms_sleep);
            }

        }


        public static void Stop()
        {
            Console.WriteLine("Montronix Data Collector Terminated");
        }
        public static String ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key] ?? "Not Found";
                return result;
            }
            catch (ConfigurationErrorsException)
            {
                Int32 _Ts = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                Console.WriteLine("-1;" + _Ts + ";" + "Error reading app settings" + ";;;;;;;;");
                return "";
            }
        }

    }
}
