//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//   
using System;

namespace ZumoLib;

public enum MessageType
{
    GetRequest = 0,
    GetResponse = 1,
    SetRequest = 2,
    SetResponse = 3,
    Event = 4,
    OneWay = 5,
    Unknown = -1
}


public abstract class ComDevice
{
    public const byte DEFAULT_ADR = 5;
    private readonly NLog.Logger log;
    private string messageReceived = string.Empty;
    protected AutoResetEvent areMessageReceived;

    public ComDevice(ICom com, params byte[] sdf)
    {
        log = NLog.LogManager.GetLogger(GetType().ToString());

        Com = com;
        Com.MessageReceived += OnMessageReceived;

        SDFs = sdf;
        areMessageReceived = new AutoResetEvent(false);
    }

    internal ICom Com { get; }

    protected string AwrFlt { get; set; } = string.Empty;

    protected byte[] SDFs { get; }

    private const string MSG_TYPES = "?*><!:-";

    protected virtual void OnMessageReceived(object? sender, ComEventArgs e)
    {
        byte sdf = byte.Parse(e.Message.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        if (SDFs.Contains(sdf))
        {
            bool handled = false;
            switch (e.Message[1])
            {
                case ':':
                case '>': handled = ProcessSetRequest(e.Message); break;
                case '?': handled = ProcessGetRequest(e.Message); break;
                case '!': handled = ProcessEvent(e.Message); break;

                case '<':
                case '*':
                    if (e.Message.Substring(0, 4) == AwrFlt)
                    {
                        messageReceived = e.Message;
                        areMessageReceived.Set();
                        handled = true;
                    }
                    break;

                default: throw new InvalidDataException("Invalid MessageType: " + e.Message[1]);
            }
            if (handled) e.Handled = true;
        }
        else
        {
            if (e.Message.Substring(0, 4) == AwrFlt)
            {
                log.Warn($"Message Subdev/Function {e.Message.Substring(2, 2)} not in Constructor List {{{string.Join(", ", SDFs)}}}");
                messageReceived = e.Message;
                areMessageReceived.Set();
                e.Handled = true;
            }
        }
    }

    protected virtual bool ProcessSetRequest(string message)
    {
        log.Warn("Received DetRequest Message not handled: " + message);
        return false;
    }
    protected virtual bool ProcessGetRequest(string message)
    {
        log.Warn("Received GetRequest Message not handled: " + message);
        return false;
    }

    protected virtual bool ProcessEvent(string message)
    {
        log.Warn("Received Event Message not handled: " + message);
        return false;
    }


    protected string SendMessage(string message)
    {
        MessageType messageType;
        switch (message[1])
        {
            case '>': messageType = MessageType.SetRequest; break;
            case '<': messageType = MessageType.SetResponse; break;
            case '?': messageType = MessageType.GetRequest; break;
            case '*': messageType = MessageType.GetResponse; break;
            case '!': messageType = MessageType.Event; break;
            case ':': messageType = MessageType.OneWay; break;
            default: throw new InvalidDataException("Invalid MessageType: " + message[1]);
        }

        if (messageType == MessageType.SetRequest)
        {
            AwrFlt = message[0] + "<" + message.Substring(2, 2);
        }
        else if (messageType == MessageType.GetRequest)
        {
            AwrFlt = message[0] + "*" + message.Substring(2, 2);
        }
        else
        {
            AwrFlt = string.Empty;
        }

        areMessageReceived.Reset();
        Com.SendMessage(message);

        if (!string.IsNullOrWhiteSpace(AwrFlt))
        {
            if (areMessageReceived.WaitOne(1000))
            {
                return messageReceived;
            }
        }
        return string.Empty;
    }


    protected string SendMessage(byte adr, MessageType messageType, byte sdf, string args)
    {
        string message = $"{adr}{MSG_TYPES[(int)messageType]}{sdf:X2}{args}";

        if (messageType == MessageType.SetRequest)
        {
            AwrFlt = $"{adr}<{sdf:X2}";
        }
        else if (messageType == MessageType.GetRequest)
        {
            AwrFlt = $"{adr}*{sdf:X2}";
        }
        else
        {
            AwrFlt = string.Empty;
        }

        areMessageReceived.Reset();
        Com.SendMessage(message);

        if (!string.IsNullOrWhiteSpace(AwrFlt))
        {
            if (areMessageReceived.WaitOne(1000))
            {
                return messageReceived;
            }
        }
        return string.Empty;
    }


    protected string SetRequest(byte adr, byte sdf, string args, bool oneWay=false)
    {
        return SendMessage(adr, oneWay ? MessageType.OneWay : MessageType.SetRequest, sdf, args);
    }

    protected string SetRequest(byte sdf, string args, bool oneWay = false)
    {
        return SendMessage(DEFAULT_ADR, oneWay ? MessageType.OneWay : MessageType.SetRequest, sdf, args);
    }
    protected string SetRequest(string args, bool oneWay = false)
    {
        return SendMessage(DEFAULT_ADR, oneWay ? MessageType.OneWay : MessageType.SetRequest, SDFs[0], args);
    }


    protected string GetRequest(byte adr, byte sdf, string args)
    {
        return SendMessage(adr, MessageType.GetRequest, sdf, args);
    }

    protected string GetRequest(byte sdf, string args)
    {
        return SendMessage(DEFAULT_ADR, MessageType.GetRequest, sdf, args);
    }

    protected string GetRequest(string args)
    {
        return SendMessage(DEFAULT_ADR, MessageType.GetRequest, SDFs[0], args);
    }

    protected string GetRequest()
    {
        return SendMessage(DEFAULT_ADR, MessageType.GetRequest, SDFs[0], string.Empty);
    }

}
