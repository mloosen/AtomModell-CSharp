using System.Windows.Media.Imaging;

namespace AtomModell_CSharp.Models;

/// <summary>
/// 3D Raytracing Engine mit Volume Rendering für Wasserstoff-Orbitale
/// </summary>
public class RaytracingEngine3D
{
    private int _width;
    private int _height;
    private byte[] _pixels;
    private const float LIGHTING_SCALER = 700.0f;  // Wie im Original C++

    public float RotationX { get; set; } = 0.3f;
    public float RotationY { get; set; } = 0.5f;
    public float RotationZ { get; set; } = 0.0f;
    public float ClipX { get; set; } = 0.0f;
    public float ClipY { get; set; } = 0.0f;
    public float ClipZ { get; set; } = 0.0f;
    public float ColorScale { get; set; } = 1.0f;

    public RaytracingEngine3D(int width, int height)
    {
        _width = width;
        _height = height;
        _pixels = new byte[width * height * 4];
    }

    /// <summary>
    /// Berechnet die Pixel-Daten im Hintergrund (Thread-sicher)
    /// </summary>
    public byte[] ComputePixels3D(int n, int l, int m)
    {
        const float scale = 0.03f;  // Skalierungsfaktor für die Raumkoordinaten
        const int samples = 50;     // Tiefensamples pro Strahl
        var pixels = new byte[_width * _height * 4];

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                // Normalisierte Screen-Koordinaten (-1 bis 1)
                float sx = (x - _width / 2.0f) / (_width / 2.0f);
                float sy = (y - _height / 2.0f) / (_height / 2.0f);

                // Raytracing: Schießt einen Strahl durch das Volumen
                var color = CastRay(sx, sy, n, l, m, scale, samples);

                int pixelIndex = (y * _width + x) * 4;
                pixels[pixelIndex] = color.B;      // B
                pixels[pixelIndex + 1] = color.G;  // G
                pixels[pixelIndex + 2] = color.R;  // R
                pixels[pixelIndex + 3] = color.A;  // A
            }
        }

        return pixels;
    }

    /// <summary>
    /// Erstellt eine WriteableBitmap aus Pixel-Daten (muss im UI-Thread aufgerufen werden)
    /// </summary>
    public WriteableBitmap CreateBitmap(byte[] pixels)
    {
        var bitmap = new WriteableBitmap(_width, _height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        bitmap.WritePixels(
            new System.Windows.Int32Rect(0, 0, _width, _height),
            pixels,
            _width * 4,
            0
        );
        return bitmap;
    }

    public int Width => _width;
    public int Height => _height;

    /// <summary>
    /// Wirft einen Strahl durch das Volumen (Volume Rendering)
    /// </summary>
    private System.Windows.Media.Color CastRay(float sx, float sy, int n, int l, int m, float scale, int samples)
    {
        float accR = 0, accG = 0, accB = 0;
        float accAlpha = 0;

        // Strahl durch verschiedene Tiefenpositionen (von vorne nach hinten)
        for (int step = 0; step < samples; step++)
        {
            // Linear durch die Tiefe samplen
            float t = (step + 0.5f) / samples;  // 0.5/samples bis (samples-0.5)/samples

            // Volumen skaliert mit n - Orbital-Ausdehnung wächst proportional zu n
            float volumeExtent = 5.0f * n;

            // 3D Position im Weltkoordinatensystem (uniformes kubisches Volumen)
            float wx = sx * volumeExtent;
            float wy = sy * volumeExtent;
            float wz = (t - 0.5f) * 2.0f * volumeExtent;

            // Wende Rotationen an
            var rotated = RotatePoint(wx, wy, wz);

            // Prüfe Clipping - relativ zum Volumen-Extent
            if (ClipX > 0.01f && Math.Abs(rotated.x) > volumeExtent * (1.0f - ClipX)) continue;
            if (ClipY > 0.01f && Math.Abs(rotated.y) > volumeExtent * (1.0f - ClipY)) continue;
            if (ClipZ > 0.01f && Math.Abs(rotated.z) > volumeExtent * (1.0f - ClipZ)) continue;

            // Konvertiere zu Kugelkoordinaten (r, θ, φ)
            // C++ Original: r = length(pos), theta = acos(y/r), phi = atan2(z, x)
            float pr = (float)Math.Sqrt(rotated.x * rotated.x + rotated.y * rotated.y + rotated.z * rotated.z);
            
            // Verhindere Division durch Null
            if (pr < 0.01f) continue;
            
            // Theta: Winkel von Y-Achse (wie im C++ Original: acos(pos.y / r))
            float ptheta = (float)Math.Acos(Math.Max(-1.0, Math.Min(1.0, rotated.y / pr)));
            // Phi: Winkel in XZ-Ebene (wie im C++ Original: atan2(pos.z, pos.x))
            float pphi = (float)Math.Atan2(rotated.z, rotated.x);

            // Berechne rohe Intensität wie im C++ Original (OHNE normY)
            // r wird direkt verwendet - KEIN Extra-Scaling mit (5/n)
            double density = OrbitalCalculator.RawIntensity(n, l, m, pr, ptheta, pphi);
            
            // Multipliziere mit LightingScaler wie im Original (KEINE Log-Kompression hier!)
            double intensity = density * LIGHTING_SCALER * (double)ColorScale;
            intensity = Math.Max(0.0, Math.Min(1.0, intensity));  // Clamp zu [0,1]

            if (intensity > 0.01)
            {
                // Hol die Farbe für diese Dichte
                var sampleColor = DensityToColor(intensity);
                
                // Konvertiere Farbe zu Floats (0-1 Range)
                float sr = sampleColor.R / 255.0f;
                float sg = sampleColor.G / 255.0f;
                float sb = sampleColor.B / 255.0f;

                // Alpha pro Sample: höherer Faktor damit Volume Rendering 
                // sichtbare Dichte erreicht (äquivalent zu 100k Partikel-Rendering im C++)
                float sampleAlpha = (float)intensity * 8.0f / samples;
                sampleAlpha = Math.Min(sampleAlpha, 1.0f);

                // Front-to-Back Blending: compositing
                accR = accR + (1.0f - accAlpha) * sr * sampleAlpha;
                accG = accG + (1.0f - accAlpha) * sg * sampleAlpha;
                accB = accB + (1.0f - accAlpha) * sb * sampleAlpha;
                accAlpha = accAlpha + (1.0f - accAlpha) * sampleAlpha;

                // Früh rausgehen wenn genug Opazität erreicht
                if (accAlpha > 0.95f) break;
            }
        }

        // Schwarzer Hintergrund wie im C++ Original (glClearColor(0,0,0,1))
        float bgR = 0.0f;
        float bgG = 0.0f;
        float bgB = 0.0f;

        // Finale Farbe = composited color + (1 - alpha) * background
        float finalR = accR + (1.0f - accAlpha) * bgR;
        float finalG = accG + (1.0f - accAlpha) * bgG;
        float finalB = accB + (1.0f - accAlpha) * bgB;

        // Konvertiere zu Bytes
        byte r = (byte)Math.Min(255, finalR * 255);
        byte g = (byte)Math.Min(255, finalG * 255);
        byte b = (byte)Math.Min(255, finalB * 255);

        return System.Windows.Media.Color.FromArgb(255, r, g, b);
    }

    /// <summary>
    /// Rotiert einen Punkt im 3D-Raum (Euler-Winkel)
    /// </summary>
    private (float x, float y, float z) RotatePoint(float x, float y, float z)
    {
        // Rotation um X-Achse
        float sinX = (float)Math.Sin(RotationX);
        float cosX = (float)Math.Cos(RotationX);
        float y1 = y * cosX - z * sinX;
        float z1 = y * sinX + z * cosX;

        // Rotation um Y-Achse
        float sinY = (float)Math.Sin(RotationY);
        float cosY = (float)Math.Cos(RotationY);
        float x2 = x * cosY + z1 * sinY;
        float z2 = -x * sinY + z1 * cosY;

        // Rotation um Z-Achse
        float sinZ = (float)Math.Sin(RotationZ);
        float cosZ = (float)Math.Cos(RotationZ);
        float x3 = x2 * cosZ - y1 * sinZ;
        float y3 = x2 * sinZ + y1 * cosZ;

        return (x3, y3, z2);
    }

    /// <summary>
    /// Heatmap Fire Farbkarte vom Original C++
    /// Black -> Dark Purple -> Red -> Orange -> Yellow -> White
    /// </summary>
    private static System.Windows.Media.Color DensityToColor(double value)
    {
        value = Math.Max(0.0, Math.Min(1.0, value));

        // Farbstopps: Black, Dark Purple, Deep Red, Orange, Yellow, White
        var colors = new[]
        {
            (r: 0.0f, g: 0.0f, b: 0.0f),       // 0.0: Black
            (r: 0.3f, g: 0.0f, b: 0.6f),       // 0.2: Dark Purple
            (r: 0.8f, g: 0.0f, b: 0.0f),       // 0.4: Deep Red
            (r: 1.0f, g: 0.5f, b: 0.0f),       // 0.6: Orange
            (r: 1.0f, g: 1.0f, b: 0.0f),       // 0.8: Yellow
            (r: 1.0f, g: 1.0f, b: 1.0f)        // 1.0: White
        };

        // Finde in welches Segment der Wert fällt
        float scaled_v = (float)value * (colors.Length - 1);
        int i = (int)scaled_v;
        int next_i = Math.Min(i + 1, colors.Length - 1);

        // Lokale Interpolation zwischen den beiden Farben
        float local_t = scaled_v - i;

        float r = colors[i].r + local_t * (colors[next_i].r - colors[i].r);
        float g = colors[i].g + local_t * (colors[next_i].g - colors[i].g);
        float b = colors[i].b + local_t * (colors[next_i].b - colors[i].b);

        return System.Windows.Media.Color.FromArgb(255, 
            (byte)(r * 255), 
            (byte)(g * 255), 
            (byte)(b * 255));
    }
}
