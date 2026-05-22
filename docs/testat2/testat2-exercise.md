# CSA Übung 9 – Testataufgabe: Fernsteuerung Zumo Roboter (FS 2026)

## Lernziele
- Fahrbefehle basierend auf einem Eingabebefehl ausführen
- Datenkommunikation via WLAN mit TCP/IP auf der Roboterplattform umsetzen
- Bestehenden Programmcode erweitern und neue Anforderungen implementieren

## Aufgabenstellung

1. **Streckenauswahl via WLAN**  
   Ein Client (Telnet/Netcat/PowerShell) sendet die gewünschte Fahrtstrecke an den Zumo Roboter.  
   Drei Strecken sind vorgegeben, eigene Strecken können zusätzlich definiert werden.

2. **Streckenabfahrt**  
   - Der Roboter fährt die gewählte Strecke ab und stoppt am Ende.  
   - Bei Hindernis (z. B. durch Lidar erkannt) stoppt der Roboter sofort.

3. **Protokollierung**  
   - Während der Fahrt werden Bestätigungen der Fahrbefehle protokolliert.  
   - Die Protokolldaten werden als Textdatei gespeichert.  
   - Aus dem Protokoll muss hervorgehen, ob die Fahrt normal endete oder durch ein Hindernis abgebrochen wurde.

4. **Bereitstellung der Protokolldatei**  
   - Über einen HTTP-Dateiserver werden die Protokolldaten am Ende der Fahrt per WLAN bereitgestellt.  
   - Format siehe unten.

5. **Wiederholung**  
   - Nach einer abgeschlossenen Fahrt (mit Protokollierung) kann eine neue (oder gleiche) Strecke gewählt werden.  
   - Der Ablauf beginnt wieder bei Schritt 1.

## Abgabekriterien & Termin
- **Letzter Abgabetermin:** Montag, 25. Mai 2026 – 21:00 Uhr  
- **Erfüllungskriterien:**  
  - Zwei Protokolldateien (eine Fahrt mit Hindernis, eine ohne Hindernis)  
  - Vollständiges Projekt als ZIP-Datei in ILIAS hochgeladen  
- **Kein Projektbericht erforderlich**

## Format der Protokolldatei
- **Dateiformat:** ASCII-Text, keine Leerzeilen, keine Fußzeilen  
- **Kopfzeile:** Name des/der Studierenden + aktuelles Datum und Uhrzeit  
- **1. Zeile:** Antwort auf einen Verbindungscheck (`5<D1Ping`)  
- **Ab 2. Zeile:** Antworten der Fahrbefehle nach deren Ausführung

**Beispiel (ohne Hindernis):**  
```
// Roger Diehl // 04/05/2026 11:02:39
5<D1Ping
5<24C01F40064006400
5<24A008700640064
5<24C02BC0064006400
5<24AFF7900640064
5<24C01F40064006400
5<24AFF7900640064
5<24C02BC0064006400
5<24C01F40064006400
```

**Beispiel (mit Hindernis / Stop):**  
```
// Roger Diehl // 04/05/2026 11:05:12
5<D1Ping
5<24C01F40064006400
5<24A008700640064
5<24C02BC0064006400
5<24A008700640064
5<24C01F40064006400
5<24A008700640064
5<24C02BC0064006400
5<24100000000...Stop
```

## Erforderliche Implementierungen
- **Fahrtstrecken definieren** (vorgegebene aus `ZumoDrives` oder eigene)  
- **Hindernisstopp** (unter Verwendung der Klasse `Zumolidar`)  
- **Zumo Server** (iterativer Server) für:  
  - Auswahl der Fahrtstrecke  
  - Ausführung der Fahrt  
  - Speicherung der Protokolldaten in eine Datei  
- **HTTP-Dateiserver** (basierend auf Übung 8 – einfacher HTTP Fileserver) zur Bereitstellung der Protokolldatei  
- **Starten beider Prozesse** (Zumo Server + HTTP-Fileserver)

## Tipps & Hinweise
- Nutze das bereitgestellte **Testat Template (TestatTemplate2.zip)** und vorhandene Lösungen  
- Im Template enthalten: `ZumoApp` und `Zumolib` – speziell für diese Aufgabe angepasst  
- **Lidar-Distanz** nicht zu groß einstellen, aber groß genug für rechtzeitiges Stoppen  
- **Lidar nach Ende der Fahrt unbedingt ausschalten**
