using System.Collections.ObjectModel;
using System.Numerics;

namespace AtomModell_CSharp.Models;

/// <summary>
/// Simulationsengine für die Atom-Modellierung
/// </summary>
public class SimulationEngine
{
    public ObservableCollection<Atom> Atoms { get; }
    public float OrbitDistance { get; set; } = 200.0f;
    public bool IsRunning { get; set; } = true;

    private int _frameCount = 0;
    private DateTime _lastUpdateTime = DateTime.Now;

    public SimulationEngine()
    {
        Atoms = new ObservableCollection<Atom>();
    }

    /// <summary>
    /// Initialisiert die Simulation mit Standard-Atom
    /// </summary>
    public void Initialize()
    {
        Atoms.Clear();
        Atoms.Add(new Atom(Vector2.Zero, OrbitDistance));
    }

    /// <summary>
    /// Fügt ein neues Atom hinzu
    /// </summary>
    public void AddAtom(Vector2 position)
    {
        Atoms.Add(new Atom(position, OrbitDistance));
    }

    /// <summary>
    /// Aktualisiert alle Atome in der Simulation
    /// </summary>
    public void Update()
    {
        if (!IsRunning) return;

        foreach (var atom in Atoms)
        {
            atom.Update(OrbitDistance);
        }

        _frameCount++;
    }

    /// <summary>
    /// Erhöht die Energie aller Elektronen
    /// </summary>
    public void ExciteAllElectrons(double amount)
    {
        foreach (var atom in Atoms)
        {
            atom.ExciteElectrons(amount);
        }
    }

    /// <summary>
    /// Erniedrigt die Energie aller Elektronen
    /// </summary>
    public void DeexciteAllElectrons(double amount)
    {
        foreach (var atom in Atoms)
        {
            atom.DeexciteElectrons(amount);
        }
    }

    /// <summary>
    /// Gibt die Anzahl der Frames zurück
    /// </summary>
    public int GetFrameCount() => _frameCount;

    /// <summary>
    /// Berechnet die aktuelle FPS
    /// </summary>
    public double GetFramesPerSecond()
    {
        var elapsed = (DateTime.Now - _lastUpdateTime).TotalSeconds;
        if (elapsed <= 0) return 0;

        double fps = _frameCount / elapsed;
        _frameCount = 0;
        _lastUpdateTime = DateTime.Now;
        return fps;
    }

    /// <summary>
    /// Setzt die Simulation zurück
    /// </summary>
    public void Reset()
    {
        Atoms.Clear();
        Initialize();
        _frameCount = 0;
    }

    /// <summary>
    /// Generiert Samplepunkte für das aktuelle Orbital des ersten Elektrons
    /// </summary>
    public List<Vector2>? GenerateOrbitalSamples(int sampleCount = 1000)
    {
        if (Atoms.Count == 0) return null;

        var firstAtom = Atoms[0];
        var electron = firstAtom.GetElectrons().FirstOrDefault();

        if (electron == null) return null;

        return OrbitalCalculator.SamplePoints(
            electron.QuantumNumberN,
            electron.QuantumNumberL,
            electron.QuantumNumberM,
            sampleCount
        );
    }
}
