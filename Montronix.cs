
using System;
using System.Net.Sockets;

namespace MDataCollector
{
    public class Montronix
    {
        private int origRow;
        private int origCol;


        private void WriteAt(string s, int x, int y)
        {
            try
            {
                Console.SetCursorPosition(origCol + x, origRow + y);
                Console.Write(s);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Console.Clear();
                Console.WriteLine(e.Message);
            }
        }
        public Montronix()
        {
            const int LIMITS_COUNT = 3;
            string deviceIp = "192.168.49.108";    // default IP
            int port = 3020;                       // default port
            int colWidth = 24;
            int numCols = 4;
           
            // string _fileName = "log.csv";
            // const float VALUE_MULTIPLIER = 1.0f;

            Console.WriteLine("Starting application...");

            // Starting the connection and creating the top of data
            Console.WriteLine($"Device IP: {deviceIp}"); Console.WriteLine($"Port: {port}");
            Console.Clear();
            origRow = Console.CursorTop;
            origCol = Console.CursorLeft;

            for (int i = 0; i < numCols; i++)
            {
                WriteAt($"{i}", 12 + (i * colWidth), 0);
            }
            for (int i = 0; i < numCols; i++)
            {
                WriteAt($"max{i}", (colWidth * i), 1);
                WriteAt($"min{i}", (colWidth * i), 3);
                WriteAt($"max DSP{i}", (colWidth * i), 5);
                WriteAt($"min DSP{i}", (colWidth * i), 7);
                WriteAt($"POSmax > POSmin", (colWidth * i), 9);
                WriteAt($"SCENARIO", (colWidth * i), 11);
            }

            // Starting catching the data from the stream after connection
            while (true)
            {
                try
                {
                    TcpClient client = new TcpClient(deviceIp, port);
                    NetworkStream stream = client.GetStream();
                    Byte[] data = new Byte[15];
                    bool bit;
                    float value_to_print;
                    int packetNumber = 0;
                    int offSet = 0;
                    bool[] scenarioByte = new bool[3] { false, false, false };
                    while (client.Connected)
                    {
                        stream.Read(data, 0, data.Length);
                        Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                        try
                        {
                            if (data.Length == 15)
                            {
                                packetNumber++;
                                WriteAt($"Package no.: {data[0]}", 0, 13);
                                WriteAt($"Pachetto no.: {packetNumber} - {unixTimestamp}", 0, 16);
                                var bit1 = GetBitAtPosition(data[0], 2);
                                var bit0 = GetBitAtPosition(data[0], 1);
                                // bool[] a_bit = new bool[3] { false, false, false };

                                bool[] limitByte = new bool[8] { false, false, false, false, false, false, bit1, bit0 };
                                byte limit = ConvertBoolArrayToByte(limitByte);
                                if (limit == 0)
                                {
                                    offSet = 0;
                                }
                                if (limit == 1)
                                {
                                    offSet = 1;
                                }
                                if (limit == 2)
                                {
                                    offSet = 2;
                                }
                                if (limit == 3)
                                {
                                    offSet = 3;
                                }

                                // Writing the data into the correct position after the catch from the streaming

                                // Max
                                value_to_print = (data[3] << 8) + data[4];
                                WriteAt($"{value_to_print,8:F}", (colWidth * offSet), 2);

                                // Min
                                value_to_print = data[6] + (data[5] << 8);
                                WriteAt($"{value_to_print,8:F}", (colWidth * offSet), 4);
                                bit = GetBitAtPosition(data[7], 1);
                                WriteAt($"{bit,6}", (colWidth * offSet), 10);

                                // Max DSP
                                value_to_print = data[9] + (data[8] << 8);
                                WriteAt($"{value_to_print,8:F}", (colWidth * offSet), 6);

                                // Min DSP
                                value_to_print = data[11] + (data[10] << 8); WriteAt($"{value_to_print,8:F}", (colWidth * offSet), 8);

                                // Scenario
                                scenarioByte[0] = GetBitAtPosition(data[2], 6);
                                scenarioByte[1] = GetBitAtPosition(data[2], 5);
                                scenarioByte[2] = GetBitAtPosition(data[2], 4);
                                value_to_print = (ConvertBoolArrayToByte(scenarioByte)) + 1;
                                WriteAt($"{value_to_print}", (colWidth * offSet), 12);
                                WriteAt($"Active scenario: {GetBitAtPosition(data[2], 6)} {GetBitAtPosition(data[2], 5)} { GetBitAtPosition(data[2], 4)}", 0, 14);
                                
                                /* Alarms
                                a_bit[0] = GetBitAtPosition(data[12], 0);
                                a_bit[1] = GetBitAtPosition(data[12], 1);
                                a_bit[2] = GetBitAtPosition(data[12], 2);
                                value_to_print = (ConvertBoolArrayToByte(a_bit)) + 0;
                                WriteAt($"{value_to_print} A", (colWidth * offSet), 20);
                                */

                                if (packetNumber == LIMITS_COUNT)
                                {
                                    packetNumber = 0;
                                }
                            }
                            else
                            {
                                Console.WriteLine(" DL <> 15");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
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

        private bool[] ConvertByteToBoolArray(byte b)
        {
            // prepare the return result
            bool[] result = new bool[8];
            // check each bit in the byte. if 1 set to true, if 0 set to false
            for (int i = 0; i < 8; i++)
                result[i] = (b & (1 << i)) == 0 ? false : true;// reverse the array
            Array.Reverse(result);
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

