using System.Collections.ObjectModel;
using System.Numerics;

namespace AtomModell_CSharp.Models;

/// <summary>
/// Representation eines Atoms mit Proton und Elektronenhülle
/// </summary>
public class Atom
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public ObservableCollection<Particle> Particles { get; }

    public Atom(Vector2 position, float orbitDistance = 200.0f)
    {
        Position = position;
        Particles = new ObservableCollection<Particle>
        {
            new(position, ChargeType.Proton),                                    // Proton im Zentrum
            new(new Vector2(position.X - orbitDistance, position.Y), ChargeType.Electron)  // Elektron in der Umlaufbahn
        };
    }

    /// <summary>
    /// Aktualisiert alle Partikel des Atoms
    /// </summary>
    public void Update(float orbitDistance)
    {
        foreach (var particle in Particles)
        {
            particle.Update(Position, orbitDistance);
        }
    }

    /// <summary>
    /// Gibt alle Elektronen des Atoms zurück
    /// </summary>
    public IEnumerable<Particle> GetElectrons() 
        => Particles.Where(p => p.Charge == ChargeType.Electron);

    /// <summary>
    /// Gibt alle Protonen des Atoms zurück
    /// </summary>
    public IEnumerable<Particle> GetProtons() 
        => Particles.Where(p => p.Charge == ChargeType.Proton);

    /// <summary>
    /// Erhöht die Energie aller Elektronen
    /// </summary>
    public void ExciteElectrons(double amount)
    {
        foreach (var electron in GetElectrons())
        {
            electron.Energy += amount;
            electron.Angle = 0;
        }
    }

    /// <summary>
    /// Erniedrigt die Energie aller Elektronen
    /// </summary>
    public void DeexciteElectrons(double amount)
    {
        foreach (var electron in GetElectrons())
        {
            electron.Energy -= amount;
        }
    }
}
