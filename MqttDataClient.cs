
using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace MDataCollector
{

    class MqttDataClient
    {
        private IMqttClient mqttClient;
        private string mqttServer;
        private string clientId;
        private string userName;
        private string password;
        private bool isConnected = false;
        private String useLogger;
        private String terminator = ";;;;;;;;";


        public void SetMqttServer(String server)
        {
            this.mqttServer = server;
        }

        public void SetClientId()
        {
            this.clientId = System.Guid.NewGuid().ToString();
        }

        public void SetUserName(String name)
        {
            this.userName = name;
        }

        public void SetPassword(String password)
        {
            this.password = password;
        }

        public IMqttClient GetMqttClient()
        {
            return this.mqttClient;
        }

        public String GetClientID()
        {
            return this.clientId;
        }
        public MqttDataClient()
        {
            this.SetClientId();
            useLogger = ConfigurationManager.AppSettings.Get("create_log_file");
            if (useLogger == "true")
                terminator = "";
        }

        public MqttDataClient(String server)
        {
            this.SetMqttServer(server);
            this.SetClientId();
            useLogger = ConfigurationManager.AppSettings.Get("create_log_file");
            if (useLogger == "true")
                terminator = "";
        }

        public async Task MqttConnectAsync()
        {
            var mqttFactory = new MqttFactory();
            this.mqttClient = mqttFactory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                 .WithClientId(this.clientId)
                 .WithTcpServer(this.mqttServer)
                 .WithCredentials(this.userName, this.password)
                 // .WithTls()
                 .WithCleanSession()
                 .Build();
            try
            {
                this.mqttClient.UseApplicationMessageReceivedHandler(e => { MessageRecieved(e); });
                this.mqttClient.UseDisconnectedHandler(e => { ConnectionClosed(); });
                this.mqttClient.UseConnectedHandler(e => { ConnectionOpened(); });

                await this.mqttClient.ConnectAsync(options, CancellationToken.None);
                this.isConnected = true;
            }
            catch (Exception e)
            {
                Int32 Ts = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                Console.WriteLine("-1;" + Ts + ";" + "Connection failed." + terminator);
                Console.WriteLine("-1;" + Ts + ";" + e.Message + terminator);
                this.isConnected = false;
            }
        }

        public async Task PublishAsync(String topic, String userMessage)
        {
            var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(userMessage)
            .WithExactlyOnceQoS()
            .Build();
            try
            {
                await this.mqttClient.PublishAsync(message, CancellationToken.None);
                Int32 _Ts = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string output = userMessage.Replace(";", "|");
                Console.WriteLine("2;" + _Ts + ";" + "Publish to: " + topic + " - Message:[" + output + "]" + terminator);

            }
            catch (Exception e)
            {
                Int32 _Ts = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                Console.WriteLine("-1;" + _Ts + ";" + "Unable to publish message to " + topic + " on " + this.mqttServer + " " + e.Message + terminator);
               
            }
        }

        public bool GetConnection()
        {
            return this.isConnected;
        }

        private void MessageRecieved(MqttApplicationMessageReceivedEventArgs e)
        {
            Int32 Ts = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Console.WriteLine("2;" + Ts + ";" + e.ClientId + ";;;;;;;;");
        }

        private void ConnectionClosed()
        {
            Int32 _Ts = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Console.WriteLine("2;" + _Ts + ";" + "Connection closed from " + this.mqttServer + terminator);
        }

        private void ConnectionOpened()
        {
            Int32 _Ts = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Console.WriteLine("2;" + _Ts + ";" + "Connection opened to " + this.mqttServer + terminator);
        }
    }
}
