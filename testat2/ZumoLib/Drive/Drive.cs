//    _____                            ____        __          __
//   /__  /  __  ______ ___  ____     / __ \____  / /_  ____  / /_
//     / /  / / / / __ `__ \/ __ \   / /_/ / __ \/ __ \/ __ \/ __/
//    / /__/ /_/ / / / / / / /_/ /  / _, _/ /_/ / /_/ / /_/ / /_
//   /____/\__,_/_/ /_/ /_/\____/  /_/ |_|\____/_.___/\____/\__/
//   (c) Hochschule Luzern T&A ========== www.hslu.ch ============
//
using System;
using System.Globalization;
using System.IO.Ports;
using System.Security.Cryptography;

namespace ZumoLib;

public class Drive : ComDevice {
    private event EventHandler DriveFinished;
    private EventWaitHandle wh = new AutoResetEvent(false);
    private string response = "start";
    private bool stop = false;

    public Drive(ICom com) : base(com, 0x24) {
        TurnCalib(115); // 100 entspricht keine Korrektur (=1.00)
    }

    public string Response { get => response; set => response = value; }

    /// <summary>
    /// Fährt eine Strecke gerade aus und wartet bis die Fahrt fertig ist.
    /// </summary>
    /// <param name="length">die zu fahrende Strecke in mm (negativer Wert => rückwärts)</param>
    /// <param name="speed"></param>
    /// <param name="acceleration"></param>
    /// <param name="offset">Korrekturfaktor in 0.1mm/s</param>
    public void Track(Int16 length, UInt16 speed, UInt16 acceleration, sbyte offset = 0) {
        if (!this.stop) {
            string msg = SetRequest($"C{length:X4}{speed:X4}{acceleration:X4}{offset:X2}");
            response = string.Concat(response, msg + "\n");
            this.wh.WaitOne();
        }
    }

    /// <summary>
    ///  Dreht an Ort und Stelle und wartet bis das Drehen fertig ist.
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="speed"></param>
    /// <param name="acceleration"></param>
    public void Turn(Int16 angle, UInt16 speed, UInt16 acceleration) {
        if (!this.stop) {
            string msg = SetRequest($"A{angle:X4}{speed:X4}{acceleration:X4}");
            response = string.Concat(response, msg + "\n");
            this.wh.WaitOne();
        }
    }

    /// <summary>
    ///  Dreht an Ort und Stelle und wartet bis das Drehen fertig ist.
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="speed"></param>
    /// <param name="acceleration"></param>
    /// <param name="factor">Korrekturfaktor Istwinkel zu Sollwinkel</param>
    public void Turn(Int16 angle, UInt16 speed, UInt16 acceleration, Int16 factor) {
        if (!this.stop) {
            string msg = SetRequest($"B{factor:X4}");
            msg = SetRequest($"A{angle:X4}{speed:X4}{acceleration:X4}");
            response = string.Concat(response, msg + "\n");
            this.wh.WaitOne();
        }
    }

    /// <summary>
    /// Setzt den Korrekturfaktor für den Fahrbefehl "An Ort drehen".
    /// 100 entspricht 1.00,
    /// 115 entspricht beispielweise einem Korrekturfaktor von 1.15 (Istwinkel zu Sollwinkel)
    /// </summary>
    /// <param name="factor">Korrekturfaktor Istwinkel zu Sollwinkel</param>
    public void TurnCalib(Int16 factor) {
        string msg = SetRequest($"B{factor:X4}");
        response = string.Concat(response, msg + "\n");
    }

    /// <summary>
    /// Liefert die restliche Distanz zurück, bis der Zumo anhält (Fahrbefehl fertig ist)
    /// </summary>
    /// <returns>Die Distanz in mm</returns>
    public int GetRemainingDistance() {
        string msg = GetRequest("2");
        int dist = int.Parse(msg.Substring(4), NumberStyles.HexNumber);
        response = string.Concat(response, msg + "\n");
        return dist;
    }

    /// <summary>
    /// Liefert True zurück, solange ein Fahrbefehl ausgeführt wird
    /// </summary>
    /// <returns>true solange der Zumo fährt</returns>
    public bool IsRunning() {
        string msg = GetRequest("7");
        bool running = byte.Parse(msg.Substring(4), NumberStyles.HexNumber) == 1;
        return running;
    }

    /// <summary>
    ///  Stoppt die Fahrt.
    /// </summary>
    public void Stop() {
        this.stop = true;
        this.wh.Set();
        string msg = SetRequest("100000000");
        response = string.Concat(response, msg + "...Stop\n");
    }

    /// <summary>
    ///  Gibt die Fahrt wieder frei.
    /// </summary>
    public void ResetStop() {
        this.stop = false;
    }

    protected override bool ProcessEvent(string message) {
        if (message == "5!24FF") {
            DriveFinished?.Invoke(this, EventArgs.Empty);
            this.wh.Set();
            return true;
        }
        return false;
    }
}
