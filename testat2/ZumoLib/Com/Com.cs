//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//   
using System;
using System.IO.Ports;

namespace ZumoLib;

public class Com : ICom
{
    private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

    public event EventHandler<ComEventArgs>? MessageReceived;

    public Com()
    {
        SerialPort = new SerialPort("/dev/ttyAMA3", 115200);
        SerialPort.NewLine = "\n";
        SerialPort.Open();

        Thread thread = new Thread(Run);
        thread.IsBackground = true;
        thread.Start();
    }

    internal SerialPort SerialPort { get; }

    public void SendMessage(string message)
    {
        log.Trace(message);
        SerialPort.WriteLine(message);
    }

    private void Run()
    {
        string msg;
        while (true)
        {
            msg = SerialPort.ReadLine();
            log.Trace(msg);
            ComEventArgs e = new ComEventArgs(msg);
            try
            {
                MessageReceived?.Invoke(this, e);
                if (!e.Handled)
                {
                    log.Warn($"Received Message not handled: {msg}");
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "Invalid Message received: " + msg);
                e.Handled = true;
            }
        }
    }

}

