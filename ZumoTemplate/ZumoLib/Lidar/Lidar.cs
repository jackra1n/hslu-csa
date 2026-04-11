using System;
using System.Device.Gpio;
using System.Device.Pwm;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace ZumoLib;

public class Lidar
{
    #region members
    private readonly byte[] crc8 =
    {
                0x00, 0x4d, 0x9a, 0xd7, 0x79, 0x34, 0xe3, 0xae, 0xf2, 0xbf, 0x68, 0x25, 0x8b, 0xc6, 0x11, 0x5c,
                0xa9, 0xe4, 0x33, 0x7e, 0xd0, 0x9d, 0x4a, 0x07, 0x5b, 0x16, 0xc1, 0x8c, 0x22, 0x6f, 0xb8, 0xf5,
                0x1f, 0x52, 0x85, 0xc8, 0x66, 0x2b, 0xfc, 0xb1, 0xed, 0xa0, 0x77, 0x3a, 0x94, 0xd9, 0x0e, 0x43,
                0xb6, 0xfb, 0x2c, 0x61, 0xcf, 0x82, 0x55, 0x18, 0x44, 0x09, 0xde, 0x93, 0x3d, 0x70, 0xa7, 0xea,
                0x3e, 0x73, 0xa4, 0xe9, 0x47, 0x0a, 0xdd, 0x90, 0xcc, 0x81, 0x56, 0x1b, 0xb5, 0xf8, 0x2f, 0x62,
                0x97, 0xda, 0x0d, 0x40, 0xee, 0xa3, 0x74, 0x39, 0x65, 0x28, 0xff, 0xb2, 0x1c, 0x51, 0x86, 0xcb,
                0x21, 0x6c, 0xbb, 0xf6, 0x58, 0x15, 0xc2, 0x8f, 0xd3, 0x9e, 0x49, 0x04, 0xaa, 0xe7, 0x30, 0x7d,
                0x88, 0xc5, 0x12, 0x5f, 0xf1, 0xbc, 0x6b, 0x26, 0x7a, 0x37, 0xe0, 0xad, 0x03, 0x4e, 0x99, 0xd4,
                0x7c, 0x31, 0xe6, 0xab, 0x05, 0x48, 0x9f, 0xd2, 0x8e, 0xc3, 0x14, 0x59, 0xf7, 0xba, 0x6d, 0x20,
                0xd5, 0x98, 0x4f, 0x02, 0xac, 0xe1, 0x36, 0x7b, 0x27, 0x6a, 0xbd, 0xf0, 0x5e, 0x13, 0xc4, 0x89,
                0x63, 0x2e, 0xf9, 0xb4, 0x1a, 0x57, 0x80, 0xcd, 0x91, 0xdc, 0x0b, 0x46, 0xe8, 0xa5, 0x72, 0x3f,
                0xca, 0x87, 0x50, 0x1d, 0xb3, 0xfe, 0x29, 0x64, 0x38, 0x75, 0xa2, 0xef, 0x41, 0x0c, 0xdb, 0x96,
                0x42, 0x0f, 0xd8, 0x95, 0x3b, 0x76, 0xa1, 0xec, 0xb0, 0xfd, 0x2a, 0x67, 0xc9, 0x84, 0x53, 0x1e,
                0xeb, 0xa6, 0x71, 0x3c, 0x92, 0xdf, 0x08, 0x45, 0x19, 0x54, 0x83, 0xce, 0x60, 0x2d, 0xfa, 0xb7,
                0x5d, 0x10, 0xc7, 0x8a, 0x24, 0x69, 0xbe, 0xf3, 0xaf, 0xe2, 0x35, 0x78, 0xd6, 0x9b, 0x4c, 0x01,
                0xf4, 0xb9, 0x6e, 0x23, 0x8d, 0xc0, 0x17, 0x5a, 0x06, 0x4b, 0x9c, 0xd1, 0x7f, 0x32, 0xe5, 0xa8
            };
    private LidarPoint[] Points;
    private PwmChannel pwm;
    #endregion

    public Lidar(GpioController gpio)
    {
        Gpio = gpio;
        Gpio.OpenPin(7, PinMode.Output);

        pwm = PwmChannel.Create(0, 0);

        Points = new LidarPoint[360];
        for (int i = 0; i < Points.Length; i++) Points[i] = new LidarPoint();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Com = new SerialPort("Com25", 230400);
        }
        else
        {
            Com = new SerialPort("/dev/ttyAMA4", 230400);
        }

        Com.Open();

        Thread thread = new Thread(Run);
        thread.IsBackground = true;
        thread.Start();
    }

    #region properites
    internal SerialPort Com { get; }

    internal GpioController Gpio { get; }


    /// <summary>
    /// Degree per second
    /// </summary>
    public int Speed { get; private set; }


    public LidarPoint this[int angle]
    {
        get { return Points[angle]; }
    }
    #endregion


    #region methods
    public void SetPower(bool enable)
    {
        if (enable)
        {
            Gpio.Write(7, true);
            Console.WriteLine("Power on");
            Thread.Sleep(100);
            pwm.Frequency = 1000;
            pwm.DutyCycle = 0.5;
            pwm.Start();
            Thread.Sleep(100);
            pwm.DutyCycle = 0.84;
            Thread.Sleep(100);
            pwm.Stop();
            Console.WriteLine("Lidar configured");
        }
        else
        {
            pwm.Frequency = 7000;
            pwm.DutyCycle = 0.5;
            pwm.Start();
            Thread.Sleep(100);
            pwm.Stop();

            Console.WriteLine("Power off");
            Thread.Sleep(500);
            Gpio.Write(7, false);
            Console.WriteLine("Power off!");
        }
    }

    private void Run()
    {
        byte Crc;
        byte[] data = new byte[47];

        DateTime start = DateTime.Now;
        int measuresCount = 0;
        while (true)
        {
            int read = Com.Read(data, 0, 1);
            if (data[0] != 0x54) continue;

            read = Com.Read(data, 1, 1);
            if (data[1] != 0x2C) continue;

            while (Com.BytesToRead < (45)) Thread.Sleep(1);
            read = Com.Read(data, 2, data.Length - 2);

            Crc = 0;
            for (int i = 0; i < 47; i++)
            {
                Crc = crc8[(Crc ^ data[i]) & 0xff];
            }

            if (Crc != 0)
            {
                Console.Write(".");
                continue;
            }

            Console.WriteLine("Sync");
            while (true)
            {
                while (Com.BytesToRead < 47) Thread.Sleep(3);

                Com.Read(data, 0, data.Length);
                Crc = 0;
                for (int i = 0; i < 47; i++)
                {
                    Crc = crc8[(Crc ^ data[i]) & 0xff];
                }
                if (Crc != 0)
                {
                    Console.WriteLine("CRC Error");
                    break;
                }

                Speed = (UInt16)(data[2] | data[3] << 8);
                UInt16 angleStart = (UInt16)(data[4] | data[5] << 8);
                UInt16 angleEnd = (UInt16)(data[42] | data[43] << 8);

                int step;
                if (angleStart > angleEnd)
                {
                    step = ((36000 - angleStart) + angleEnd) / (12 - 1);
                }
                else
                {
                    step = (angleEnd - angleStart) / (12 - 1);
                }

                for (int i = 0; i < 12; i++)
                {
                    UInt16 a = (UInt16)(((angleStart + (i * step)) % 36000) / 100);
                    Points[a].Distance = (UInt16)(data[(i * 3) + 6] | data[(i * 3) + 7] << 8);
                    Points[a].Intensity = data[(i * 3) + 8];
                }
            }
        }
    }
    #endregion
}
