# Wasserstoff Orbital Visualizer - C# .NET 10 Edition

Eine moderne Desktop-Anwendung zur Visualisierung von Wasserstoff-Orbitalen basierend auf der Schrödinger-Gleichung, entwickelt in C# mit .NET 10 und WPF.

## Features

- **Quantenmechanische Berechnung**: Lösung der Schrödinger-Gleichung für Wasserstoff-Atome
- **Interaktive Visualisierung**: Echtzeit-Darstellung von Elektron und Proton
- **Energiesteuerung**: Anregung und Deexcitation des Elektrons mit verschiedenen Energiestufen
- **Orbitale**: Berechnung und Anzeige der Elektronenbahnen basierend auf Quantenzahlen (n, l, m)
- **WPF-UI**: Moderne native Windows-Oberfläche mit Steuerungspanel
- **Echtzeitstatistiken**: FPS-Counter und Orbital-Informationen

## Funktionsweise

### Quantenmechanisches Modell

Die Anwendung implementiert:

1. **Radiale Wellenfunktion** R_{n,l}(r)
   - Bohr-Radius a₀
   - Assoziierte Laguerre-Polynome
   - Exponentielle Dämpfung

2. **Sphärische Harmonische** Y_l^m(θ,φ)
   - Assoziierte Legendre-Polynome
   - Azimuthaler Anteil mit komplexen Exponentialen

3. **Wahrscheinlichkeitsdichte** |ψ|²
   - Für Rejection Sampling verwendet
   - Visualisierung der Elektronendichte

### Partikel-System

- **Elektron**: Bewegt sich auf quantisierten Bahnen
- **Proton**: Statisches Kernzentrum
- Dynamische Quantenzahlenberechnung basierend auf Elektronenenergie

## Technologie-Stack

- **.NET 10**: Moderne C# Laufzeitumgebung
- **WPF (Windows Presentation Foundation)**: Native grafische Oberfläche
- **C# 13**: Moderne Sprachenmerkmale

## Installation

### Voraussetzungen

- .NET 10 SDK oder Runtime
- Windows 10/11

### Build & Ausführung

```bash
# Abhängigkeiten wiederherstellen
dotnet restore

# Projekt bauen
dotnet build

# Anwendung starten
dotnet run
```

## Steuerung

### Energiesteuerung

- **+ Klein (0.01 eV)**: Kleine Anregung des Elektrons
- **- Klein (0.01 eV)**: Kleine Deexcitation
- **+ Mittel (0.1 eV)**: Mittlere Anregung
- **- Mittel (0.1 eV)**: Mittlere Deexcitation
- **+ Maximum (1.0 eV)**: Starke Anregung
- **- Maximum (1.0 eV)**: Starke Deexcitation

### Simulation

- **Play/Pause**: Startet oder pausiert die Simulation
- **Reset**: Setzt die Simulation zurück

## Projekt-Struktur

```
AtomModell-CSharp/
├── Models/
│   ├── OrbitalCalculator.cs   # Quantenmechanische Berechnungen
│   ├── Particle.cs             # Elektron/Proton Repräsentation
│   ├── Atom.cs                 # Atom mit Partikeln
│   └── SimulationEngine.cs     # Simulationslogik
├── MainWindow.xaml             # WPF UI
├── MainWindow.xaml.cs          # CodeBehind
├── App.xaml                    # WPF App-Konfiguration
└── App.xaml.cs                 # App-CodeBehind
```

## Wissenschaftliche Grundlagen

### Schrödinger-Gleichung für Wasserstoff

Die zeitunabhängige Schrödinger-Gleichung in Kugelkoordinaten:

ψ(r,θ,φ) = R_{n,l}(r) × Y_l^m(θ,φ)

### Quantenzahlen

- **n (Principal)**: 1, 2, 3, ... (Energieniveau)
- **l (Angular)**: 0, ..., n-1 (Winkelimpuls)
- **m (Magnetic)**: -l, ..., +l (Magnetisches Moment)

### Elektronenenergie

E_n = -13.6 eV / n²

## Vergleich mit C++ Original

| Feature | C++ Original | C# .NET 10 |
|---------|-------------|-----------|
| Graphics API | OpenGL | WPF Canvas |
| UI Framework | GLFW | WPF |
| Quantum Calc | Custom | OrbitalCalculator |
| Platform | Cross-platform | Windows (WPF) |
| Performance | High | Good |
| Entwicklung | Schneller | Rapid (C# Vorteile) |

## Zukünftige Erweiterungen

- [ ] 3D-Visualisierung mit helix.toolkit
- [ ] Animationen für Quantensprünge
- [ ] Export von Orbital-Daten (JSON)
- [ ] Mehrere Elektronen-Systeme
- [ ] Relativistische Korrektionen
- [ ] Mathematische Graphen-Visualisierung

## Lizenz

MIT License - Basierend auf dem Original-Projekt von Kavan

## Autor

Portiert und erweitert als .NET 10 WPF-Anwendung

## Ressourcen

- [Wasserstoff Atomorbitale - Wikipedia](https://en.wikipedia.org/wiki/Hydrogen_atom)
- [Schrödinger-Gleichung](https://en.wikipedia.org/wiki/Schrödinger_equation)
- [.NET 10 Dokumentation](https://learn.microsoft.com/en-us/dotnet/)
- [WPF Überblick](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
