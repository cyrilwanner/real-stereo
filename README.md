# Real Stereo

Eine Projektarbeit der ZHAW.

## Installation

1. Repository in Visual Studio clonen.
1. Projekt ausführen. Alle Dependencies werden dabei automatisch installiert.

## Zusammenfassung

Verschiedene Technologien wie Stereo- und Surround-Sound wurden erfunden, um das Hörerlebnis von verschiedenen Medien so realistisch wie möglich zu gestalten. Diese Technologien setzen aber implizit voraus, dass die verschiedenen Lautsprecher die richtigen Positionen im Raum, und vor allem eine einheitliche Distanz zum Hörer einnehmen.

In dieser Arbeit wird ein System ausgearbeitet, welches die Lautstärke der einzelnen Lautsprecher, und damit das Balancing, kontinuierlich an die Position des Hörers im Raum anpasst. Hierzu wird diskutiert, welche technischen Ansätze für die Umsetzung in Frage kommen. Die vielversprechendste Lösung wird als Software für PCs umgesetzt.

Dabei werden für die Umsetzung wichtige Grundlagen und Konzepte erarbeitet. Dazu zählen vor allem die Personenerkennung und Positionierung im zweidimensionalen Raum mittels digitalen Farbkameras, die Algorithmen, um die Lautstärken pro Lautsprecher anhand der Personenposition zu berechnen, und die Windows APIs für die Anwendung der berechneten Volumen-Werte.

Das Resultat dieser Projektarbeit ist ein Windows-Programm, welches mittels 2 USB-Webcams die Position des Hörers kontinuierlich ermittelt und die Audio-Kanäle des am Computer angeschlossenen Wiedergabegeräts anpasst, um eine uniforme Lautstärke für jeden Audio-Kanal zu ermöglichen. Die erarbeiteten Algorithmen und Lösungswege bilden eine Grundlage, auf welcher in einer weiterführenden Arbeit eine allgemeinere Lösung für den einfachen Einsatz im Alltag mit verschiedenen Audio- Geräten erarbeitet werden kann.
