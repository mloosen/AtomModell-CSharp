using System;
using System.Numerics;

namespace AtomModell_CSharp.Models;

/// <summary>
/// Berechnet Wasserstoff-Orbitale basierend auf der Schrödinger-Gleichung
/// Korrekte Implementierung nach dem C++ Original
/// </summary>
public class OrbitalCalculator
{
    private const double A0 = 1.0;  // Bohr radius in atomic units
    private const double PI = Math.PI;

    /// <summary>
    /// Berechnet den Radialteil der Wellenfunktion R_{n,l}(r)
    /// Vollständige Implementierung mit korrekten Laguerre-Polynomen
    /// </summary>
    public static double RadialWaveFunction(int n, int l, double r)
    {
        double rho = 2.0 * r / (n * A0);
        
        // Assoziierte Laguerre-Polynome L_{n-l-1}^{2l+1}(rho)
        int k = n - l - 1;
        int alpha = 2 * l + 1;

        double L = 1.0;
        if (k == 1)
        {
            L = 1.0 + alpha - rho;
        }
        else if (k > 1)
        {
            double Lm2 = 1.0;
            double Lm1 = 1.0 + alpha - rho;
            for (int j = 2; j <= k; j++)
            {
                L = ((2 * j - 1 + alpha - rho) * Lm1 - (j - 1 + alpha) * Lm2) / j;
                Lm2 = Lm1;
                Lm1 = L;
            }
        }

        // Normalisierungskonstante mit Gamma-Funktion
        double norm = Math.Pow(2.0 / (n * A0), 3) * 
                     Gamma(n - l) / 
                     (2.0 * n * Gamma(n + l + 1));

        double R = Math.Sqrt(norm) * Math.Exp(-rho / 2.0) * Math.Pow(rho, l) * L;
        return R;
    }

    /// <summary>
    /// Berechnet die Wahrscheinlichkeitsdichte |ψ|²
    /// </summary>
    public static double ProbabilityDensity(int n, int l, int m, double r, double theta, double phi)
    {
        // Radial part |R(r)|²
        double R = RadialWaveFunction(n, l, r);
        double radial = R * R;

        // Angular part |P_l^m(cosθ)|²
        double cosTheta = Math.Cos(theta);
        double Plm = AssociatedLegendre(l, Math.Abs(m), cosTheta);
        double angular = Plm * Plm;

        // Spherical harmonic normalization (simplified for |Y|²)
        double normY = (2 * l + 1) * Gamma(l - Math.Abs(m) + 1) / 
                      (4.0 * PI * Gamma(l + Math.Abs(m) + 1));

        double intensity = radial * angular * normY;
        return intensity;
    }

    /// <summary>
    /// Berechnet die rohe Intensität wie im C++ Original (inferno-Funktion)
    /// OHNE sphärische Harmonische Normalisierung normY
    /// intensity = radial * angular (direkt)
    /// </summary>
    public static double RawIntensity(int n, int l, int m, double r, double theta, double phi)
    {
        // Radial part |R(r)|² - identisch zum C++ inferno()
        double rho = 2.0 * r / (n * A0);

        int k = n - l - 1;
        int alpha = 2 * l + 1;

        double L = 1.0;
        if (k == 1)
        {
            L = 1.0 + alpha - rho;
        }
        else if (k > 1)
        {
            double Lm2 = 1.0;
            double Lm1 = 1.0 + alpha - rho;
            for (int j = 2; j <= k; j++)
            {
                L = ((2 * j - 1 + alpha - rho) * Lm1 - (j - 1 + alpha) * Lm2) / j;
                Lm2 = Lm1;
                Lm1 = L;
            }
        }

        double norm = Math.Pow(2.0 / (n * A0), 3) * Gamma(n - l) / (2.0 * n * Gamma(n + l + 1));
        double R = Math.Sqrt(norm) * Math.Exp(-rho / 2.0) * Math.Pow(rho, l) * L;
        double radial = R * R;

        // Angular part |P_l^m(cosθ)|² - identisch zum C++
        double x = Math.Cos(theta);
        int absM = Math.Abs(m);

        double Pmm = 1.0;
        if (absM > 0)
        {
            double somx2 = Math.Sqrt((1.0 - x) * (1.0 + x));
            double fact = 1.0;
            for (int j = 1; j <= absM; j++)
            {
                Pmm *= -fact * somx2;
                fact += 2.0;
            }
        }

        double Plm;
        if (l == absM)
        {
            Plm = Pmm;
        }
        else
        {
            double Pm1m = x * (2 * absM + 1) * Pmm;
            if (l == absM + 1)
            {
                Plm = Pm1m;
            }
            else
            {
                double Pll = 0;
                for (int ll = absM + 2; ll <= l; ll++)
                {
                    Pll = ((2 * ll - 1) * x * Pm1m - (ll + absM - 1) * Pmm) / (ll - absM);
                    Pmm = Pm1m;
                    Pm1m = Pll;
                }
                Plm = Pm1m;
            }
        }

        double angular = Plm * Plm;

        // Direkt radial * angular - KEINE normY Normalisierung (wie C++ Original)
        return radial * angular;
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
    /// Sphärische Harmonische Y_l^m(θ,φ)
    /// </summary>
    public static Complex SphericalHarmonic(int l, int m, double theta, double phi)
    {
        // Normalisierung
        double norm = Math.Sqrt((2 * l + 1) * Gamma(l - Math.Abs(m) + 1) / 
                                (4 * PI * Gamma(l + Math.Abs(m) + 1)));

        // Assoziiertes Legendre-Polynom
        double legendre = AssociatedLegendre(l, Math.Abs(m), Math.Cos(theta));

        // Azimuthaler Teil
        double realPart = Math.Cos(m * phi);
        double imagPart = Math.Sin(m * phi);

        double amplitude = norm * legendre;
        return new Complex(amplitude * realPart, amplitude * imagPart);
    }

    /// <summary>
    /// Assoziierte Legendre-Polynome P_l^m(x) - Vollständige Implementierung
    /// </summary>
    private static double AssociatedLegendre(int l, int m, double x)
    {
        // P_m^m(x)
        double Pmm = 1.0;
        if (m > 0)
        {
            double somx2 = Math.Sqrt((1.0 - x) * (1.0 + x));
            double fact = 1.0;
            for (int j = 1; j <= m; j++)
            {
                Pmm *= -fact * somx2;
                fact += 2.0;
            }
        }

        if (l == m)
            return Pmm;

        // P_m+1^m(x)
        double Pm1m = x * (2 * m + 1) * Pmm;
        if (l == m + 1)
            return Pm1m;

        // Rekursion für höhere l
        double Pll = 0.0;
        for (int ll = m + 2; ll <= l; ll++)
        {
            Pll = ((2 * ll - 1) * x * Pm1m - (ll + m - 1) * Pmm) / (ll - m);
            Pmm = Pm1m;
            Pm1m = Pll;
        }

        return Pm1m;
    }

    /// <summary>
    /// Gamma-Funktion Approximation (für positive Integer)
    /// </summary>
    private static double Gamma(double x)
    {
        if (x <= 0) return 1.0;
        
        // Für Integer: Γ(n) = (n-1)!
        if (Math.Abs(x - Math.Round(x)) < 1e-10)
        {
            int n = (int)Math.Round(x);
            if (n <= 1) return 1.0;
            
            double result = 1.0;
            for (int i = 2; i < n; i++)
                result *= i;
            return result;
        }
        
        // Stirling-Approximation für nicht-Integer
        return Math.Sqrt(2 * PI / x) * Math.Pow(x / Math.E, x);
    }

    /// <summary>
    /// Berechnet Fakultät
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
    /// Sample Points mit Rejection Sampling (für klassische Visualisierung)
    /// </summary>
    public static List<Vector2> SamplePoints(int n, int l, int m, int sampleCount)
    {
        var points = new List<Vector2>();
        var random = new Random(42);  // Fixer Seed für Reproduzierbarkeit

        int maxAttempts = sampleCount * 100;
        int attempts = 0;

        while (points.Count < sampleCount && attempts < maxAttempts)
        {
            attempts++;

            // Sample sphärische Koordinaten
            double r = random.NextDouble() * 20.0 * n;  // Skaliert mit n
            double theta = random.NextDouble() * Math.PI;
            double phi = random.NextDouble() * 2 * Math.PI;

            // Berechne Wahrscheinlichkeitsdichte
            double density = ProbabilityDensity(n, l, m, r, theta, phi);

            // Rejection sampling
            double threshold = random.NextDouble() * 0.01;  // Max-Dichte Schätzung
            if (density > threshold)
            {
                // Konvertiere zu kartesischen Koordinaten (2D Projektion)
                float x = (float)(r * Math.Sin(theta) * Math.Cos(phi));
                float y = (float)(r * Math.Sin(theta) * Math.Sin(phi));
                points.Add(new Vector2(x, y));
            }
        }

        return points;
    }
}
