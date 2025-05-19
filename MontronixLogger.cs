using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Configuration;
using System.Numerics;
using System.Threading.Tasks;
using System.Threading;

namespace MDataCollector
{
    class MontronixLogger
    {
        // private int LIMITS_COUNT = 3;  
        // "192.168.49.108";  "192.168.49.110";
        private string deviceIp;
        private int port;
        public String csvLine = "";
        public Queue<string> csvLines;
        public bool lineBreak = false;
        private String compactNull;
        private String useLogger;
        private String terminator = ";;;;;;;;";
        private int tcpAttempt = 0;
        
        public void MontronixLoggeStart()
        {

            
            deviceIp= ConfigurationManager.AppSettings.Get("sensor_ip");
            useLogger = ConfigurationManager.AppSettings.Get("create_log_file");
            if (useLogger == "true")
                terminator = "";
            port = Int32.Parse(ConfigurationManager.AppSettings.Get("sensor_port"));
            Int32 _Tsi = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Console.WriteLine("2;" + _Tsi + ";" + "Connecting to Montronix Sensor at: " + deviceIp + ":" + port + ". Please wait..." + terminator);
            this.csvLines = new Queue<string>();
            compactNull = ConfigurationManager.AppSettings.Get("compact_null");

            while (true)
            {
                try
                {
                    TcpClient client = new TcpClient(deviceIp, port);
                    Console.WriteLine("2;" + _Tsi + ";" + "Connected to Montronix Sensor at: " + deviceIp + ":" + port + terminator);
                    NetworkStream stream = client.GetStream();
                    Byte[] data = new Byte[15];
                    bool[] scenarioByte = new bool[3] { false, false, false };
                    bool[] alarm_bit = new bool[3] { false, false, false };
                    float _alarm;
                    var showNullValue = "1";

                    while (client.Connected)
                    {
                        stream.Read(data, 0, data.Length);

                        if (data.Length == 15)
                        {
                            int _value;

                            /* Begin Sensor data */

                            // Ts
                            Int32 _Ts = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                            // Limit
                            var bit1 = GetBitAtPosition(data[0], 2);
                            var bit0 = GetBitAtPosition(data[0], 1);
                            bool[] limitByte = new bool[8] { false, false, false, false, false, false, bit1, bit0 };
                            byte _Limit = ConvertBoolArrayToByte(limitByte);

                            // Max
                            _value = (data[3] << 8) + data[4];
                            var _Max = $"{_value,8:F}";

                            // Min
                            _value = data[6] + (data[5] << 8);
                            var _Min = $"{_value,8:F}";

                            // PosMax > PosMin
                            var bit = GetBitAtPosition(data[7], 1);
                            var _PosMaxUpPosMin = $"{bit,6}";                           
                            if (_PosMaxUpPosMin.Trim()  == "True"){
                                _PosMaxUpPosMin = "T";
                            } else {
                                _PosMaxUpPosMin = "F";
                            }
                            
                            // Max DSP
                                _value = data[9] + (data[8] << 8);
                            var _MaxDSP = $"{_value,8:F}";

                            // Min DSP
                            _value = data[11] + (data[10] << 8);
                            var _MinDSP = $"{_value,8:F}";

                            // Scenario
                            scenarioByte[0] = GetBitAtPosition(data[2], 6);
                            scenarioByte[1] = GetBitAtPosition(data[2], 5);
                            scenarioByte[2] = GetBitAtPosition(data[2], 4);
                            _value = (ConvertBoolArrayToByte(scenarioByte)) + 1;
                            var _Scenario = $"{_value}";

                            // Package
                            _value = data[0] - 127;
                            // _value = _value - 127;
                            var _Package = $"{_value}";

                            // Alarm
                            alarm_bit[0] = GetBitAtPosition(data[12], 0);
                            alarm_bit[1] = GetBitAtPosition(data[12], 1);
                            alarm_bit[2] = GetBitAtPosition(data[12], 2);
                            _alarm = (ConvertBoolArrayToByte(alarm_bit)) + 0;

                            /* End Sensor data */

                            var csvLineType = "0";
                            if (float.Parse(_Max) > 1 || float.Parse(_Min) > 1 || float.Parse(_MaxDSP) > 1 || float.Parse(_MinDSP) > 1)
                                csvLineType = "1";

                            if (showNullValue == "1" || csvLineType == "1")
                            {
                                this.csvLine = csvLineType + ";"
                                                 + _Ts + ";"
                                                 + _Limit + ";"
                                                 + _Max + ";"
                                                 + _Min + ";"
                                                 + _MaxDSP + ";"
                                                 + _MinDSP + ";"
                                                 + _PosMaxUpPosMin + ";"
                                                 + _Scenario + ";"
                                                 + _alarm + ";"
                                                 + _Package;


                                this.csvLines.Enqueue(this.csvLine);
                            }
                            if (compactNull == "true")
                            {
                                showNullValue = csvLineType;
                            }

                            // Console.WriteLine(this.csvLine);
                            if (_Limit == 3)
                            {
                                lineBreak = true;
                                // Console.WriteLine(" ");

                            }
                            else
                            {
                                lineBreak = false;
                            }
                        }
                        else
                        {
                            Int32 _Ts = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                            Console.WriteLine("-1;" + _Ts + ";" + "Data Length <> 15 Byte" + terminator);
                        }
                        
                    }

                }

                catch
                {
                    // Console.WriteLine(ex);
                    Int32 _Ts = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    Console.WriteLine("-1;"+ _Ts + ";" + "Unable to connect to Montronix Sensor. Retrying ..." + terminator);
                    tcpAttempt++;
                    // Console.WriteLine(tcpAttempt);
                    if (tcpAttempt > 4)
                    {
                        tcpAttempt = 0;
                        Thread.Sleep(10000);
                    }
                }
            }

        }

        /// <summary>Convert the given array of boolean values into a byte </summary>
        /// <param name="source">Array of boolean</param>
        /// <returns>The byte rapresentation</returns>
        private byte ConvertBoolArrayToByte(bool[] source)
        {
            byte result = 0;
            int index = 8 - source.Length;
            foreach (bool b in source)
            {
                if (b)
                    result |= (byte)(1 << (7 - index));
                index++;
            }
            return result;
        }

        /// <summary>Get the given bit in a specific position of a the givem byte.</summary>
        /// <param name="b">Byte to search through.</param>
        /// <param name="bitNumber">Range from 1 to 8.</param>
        /// <returns>The bit at the requested position.</returns>
        private bool GetBitAtPosition(byte b, int bitNumber)
        {
            return (b & (1 << bitNumber - 1)) != 0;
        }
    }
}
