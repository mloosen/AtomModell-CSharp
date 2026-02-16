using System;
using System.Numerics;

namespace AtomModell_CSharp.Models;

/// <summary>
/// Berechnet Wasserstoff-Orbitale basierend auf der Schrödinger-Gleichung
/// </summary>
public class OrbitalCalculator
{
    private const double A0 = 1.0;  // Bohr radius in atomic units
    private const double PI = Math.PI;

    /// <summary>
    /// Berechnet den Radialteil der Wellenfunktion R_{n,l}(r)
    /// </summary>
    public static double RadialWaveFunction(int n, int l, double r)
    {
        double rho = 2.0 * r / (n * A0);
        
        // Normalisierungskonstante
        double norm = Math.Sqrt(
            Math.Pow(2.0 / (n * A0), 3) * 
            Factorial(n - l - 1) / 
            (2 * n * Factorial(n + l))
        );

        // Assoziierte Laguerre-Polynome (vereinfachte Implementierung)
        double laguerre = AssociatedLaguerre(rho, n - l - 1, 2 * l + 1);

        return norm * Math.Exp(-rho / 2.0) * Math.Pow(rho, l) * laguerre;
    }

    /// <summary>
    /// Berechnet die vollständige komplexe Wellenfunktion ψ(r,θ,φ)
    /// </summary>
    public static Complex WaveFunction(int n, int l, int m, double r, double theta, double phi)
    {
        double radial = RadialWaveFunction(n, l, r);
        Complex spherical = SphericalHarmonic(l, m, theta, phi);
        return radial * spherical;
    }

    /// <summary>
    /// Berechnet die Wahrscheinlichkeitsdichte |ψ|²
    /// </summary>
    public static double ProbabilityDensity(int n, int l, int m, double r, double theta, double phi)
    {
        Complex psi = WaveFunction(n, l, m, r, theta, phi);
        double magnitude = psi.Magnitude;  // |ψ|
        return magnitude * magnitude;       // |ψ|²
    }

    /// <summary>
    /// Sphärische Harmonische Y_l^m(θ,φ)
    /// </summary>
    public static Complex SphericalHarmonic(int l, int m, double theta, double phi)
    {
        // Vereinfachte Version für kleine l-Werte
        double normalisation = Math.Sqrt((2 * l + 1) * Factorial(l - Math.Abs(m)) / 
                                        (4 * PI * Factorial(l + Math.Abs(m))));

        // Assoziiertes Legendre-Polynom
        double legendre = AssociatedLegendre(l, Math.Abs(m), Math.Cos(theta));

        // Azimuthaler Teil
        double realPart = Math.Cos(m * phi);
        double imagPart = Math.Sin(m * phi);

        double amplitude = normalisation * legendre;
        return new Complex(amplitude * realPart, amplitude * imagPart);
    }

    /// <summary>
    /// Assoziierte Legendre-Polynome P_l^m(x)
    /// </summary>
    private static double AssociatedLegendre(int l, int m, double x)
    {
        if (l == 0 && m == 0) return 1.0;
        if (l == 1 && m == 0) return x;
        if (l == 1 && m == 1) return -Math.Sqrt(1 - x * x);
        if (l == 2 && m == 0) return 0.5 * (3 * x * x - 1);
        if (l == 2 && m == 1) return -3 * x * Math.Sqrt(1 - x * x);
        if (l == 2 && m == 2) return 3 * (1 - x * x);
        
        return 0.0;
    }

    /// <summary>
    /// Assoziierte Laguerre-Polynome L_n^k(x)
    /// </summary>
    private static double AssociatedLaguerre(double x, int n, int k)
    {
        if (n == 0) return 1.0;
        if (n == 1) return 1.0 + k - x;
        
        // Rekursionsformel
        double prev2 = 1.0;
        double prev1 = 1.0 + k - x;

        for (int i = 2; i <= n; i++)
        {
            double curr = ((2 * i + k - 1 - x) * prev1 - (i + k - 1) * prev2) / i;
            prev2 = prev1;
            prev1 = curr;
        }

        return prev1;
    }

    /// <summary>
    /// Fakultät
    /// </summary>
    private static double Factorial(int n)
    {
        if (n <= 1) return 1.0;
        double result = 1.0;
        for (int i = 2; i <= n; i++)
            result *= i;
        return result;
    }

    /// <summary>
    /// Generiert Samplepunkte mit Rejection Sampling
    /// </summary>
    public static List<(double x, double y, double z, double probability)> SamplePoints(
        int n, int l, int m, int sampleCount = 5000, Random? random = null)
    {
        random ??= new Random();
        var points = new List<(double, double, double, double)>();

        double maxProb = 0.0;

        // Erste Pass: Maximum finden
        for (int i = 0; i < sampleCount; i++)
        {
            double r = -n * n * Math.Log(random.NextDouble());  // Exponentialverteilung
            double theta = Math.Acos(2 * random.NextDouble() - 1);
            double phi = 2 * PI * random.NextDouble();

            double prob = ProbabilityDensity(n, l, m, r, theta, phi);
            if (prob > maxProb) maxProb = prob;
        }

        // Zweite Pass: Rejection Sampling
        int accepted = 0;
        while (accepted < sampleCount)
        {
            double r = -n * n * Math.Log(random.NextDouble());
            double theta = Math.Acos(2 * random.NextDouble() - 1);
            double phi = 2 * PI * random.NextDouble();

            double prob = ProbabilityDensity(n, l, m, r, theta, phi);

            if (random.NextDouble() < prob / maxProb)
            {
                // Kartesische Koordinaten
                double x = r * Math.Sin(theta) * Math.Cos(phi);
                double y = r * Math.Sin(theta) * Math.Sin(phi);
                double z = r * Math.Cos(theta);

                points.Add((x, y, z, prob));
                accepted++;
            }
        }

        return points;
    }
}
