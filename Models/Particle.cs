using System.Numerics;

namespace AtomModell_CSharp.Models;

/// <summary>
/// Elektrische Ladungen in der Simulation
/// </summary>
public enum ChargeType
{
    None = 0,
    Electron = -1,
    Proton = 1
}

/// <summary>
/// Representation eines Partikels im Atom (Elektron/Proton)
/// </summary>
public class Particle
{
    public Vector2 Position { get; set; }
    public ChargeType Charge { get; set; }
    public double Angle { get; set; } = Math.PI;
    public double Energy { get; set; } = -13.6;  // Rydberg-Energie in eV
    public int QuantumNumberN { get; set; } = 1;
    public int QuantumNumberL { get; set; } = 0;
    public int QuantumNumberM { get; set; } = 0;
    public double ExcitationTimer { get; set; } = 0.0;

    public Particle(Vector2 position, ChargeType charge)
    {
        Position = position;
        Charge = charge;
    }

    /// <summary>
    /// Aktualisiert die Position des Partikels basierend auf dem aktuellen Orbital
    /// </summary>
    public void Update(Vector2 atomCenter, float orbitDistance)
    {
        if (Charge == ChargeType.Electron)
        {
            // Elektron bewegt sich auf elliptischer Bahn
            float baseOrbit = orbitDistance;
            float radius = baseOrbit * QuantumNumberN;

            Angle += 0.05;

            Position = new Vector2(
                (float)(Math.Cos(Angle) * radius + atomCenter.X),
                (float)(Math.Sin(Angle) * radius + atomCenter.Y)
            );
        }
        // Proton bleibt statisch
    }

    /// <summary>
    /// Erhöht die Energie des Elektrons (Anregung)
    /// </summary>
    public void ExciteSmall()
    {
        Energy += 0.01;
        Angle = 0;
    }

    /// <summary>
    /// Erniedrigt die Energie des Elektrons (Deexcitation)
    /// </summary>
    public void DeexciteSmall()
    {
        Energy -= 0.01;
    }

    /// <summary>
    /// Großzügigere Energieänderung
    /// </summary>
    public void ExciteLarge()
    {
        Energy += 0.1;
    }

    /// <summary>
    /// Großzügigere Deexcitation
    /// </summary>
    public void DeexciteLarge()
    {
        Energy -= 0.1;
    }

    /// <summary>
    /// Maximale Anregung
    /// </summary>
    public void ExciteMaximum()
    {
        Energy += 1.0;
    }

    /// <summary>
    /// Maximale Deexcitation
    /// </summary>
    public void DeexciteMaximum()
    {
        Energy -= 1.0;
    }

    /// <summary>
    /// Berechnet Quantenzahlen basierend auf der Energie
    /// </summary>
    public void UpdateQuantumNumbers()
    {
        // n = sqrt(-13.6 / E)
        if (Energy < 0)
        {
            QuantumNumberN = Math.Max(1, (int)Math.Round(Math.Sqrt(-13.6 / Energy)));
        }

        // l kann maximal n-1 sein
        QuantumNumberL = Math.Min(QuantumNumberN - 1, 2);

        // m kann zwischen -l und +l liegen
        QuantumNumberM = 0;
    }

    /// <summary>
    /// Gibt die Größe des Partikels zurück (abhängig von Ladung)
    /// </summary>
    public float GetRadius() => Charge switch
    {
        ChargeType.Electron => 5.0f,
        ChargeType.Proton => 15.0f,
        _ => 3.0f
    };

    /// <summary>
    /// Gibt die Farbe des Partikels zurück (abhängig von Ladung)
    /// </summary>
    public System.Windows.Media.Color GetColor() => Charge switch
    {
        ChargeType.Electron => System.Windows.Media.Colors.Cyan,
        ChargeType.Proton => System.Windows.Media.Colors.Red,
        _ => System.Windows.Media.Colors.Gray
    };
}
