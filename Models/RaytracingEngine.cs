using System.Windows.Media.Imaging;

namespace AtomModell_CSharp.Models;

/// <summary>
/// Raytracing Engine für die Visualisierung der Elektronendichte
/// </summary>
public class RaytracingEngine
{
    private int _width;
    private int _height;
    private byte[] _pixels;
    private const float LIGHTING_SCALER = 700.0f;  // Wie im Original C++

    public RaytracingEngine(int width, int height)
    {
        _width = width;
        _height = height;
        _pixels = new byte[width * height * 4];  // RGBA
    }

    /// <summary>
    /// Berechnet die Pixel-Daten im Hintergrund (Thread-sicher)
    /// </summary>
    public byte[] ComputePixels(int n, int l, int m, float scale = 50.0f)
    {
        var pixels = new byte[_width * _height * 4];

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                // Konvertiere Pixel zu Weltkoordinaten (2D Querschnitt)
                // Skaliere mit n, da Orbital-Ausdehnung mit n wächst
                double effectiveScale = scale / Math.Max(1, n);
                double px = (x - _width / 2.0) / effectiveScale;
                double py = (y - _height / 2.0) / effectiveScale;
                
                // Kugelkoordinaten für 2D Querschnitt (z=0 Ebene)
                double r = Math.Sqrt(px * px + py * py);
                double theta = Math.PI / 2.0;  // Äquatorialebene
                double phi = Math.Atan2(py, px);

                // Berechne rohe Intensität wie im C++ Original (OHNE normY)
                double intensity = OrbitalCalculator.RawIntensity(n, l, m, r, theta, phi);

                // Multipliziere mit LightingScaler wie im Original
                intensity = intensity * LIGHTING_SCALER;
                intensity = Math.Max(0.0, Math.Min(1.0, intensity));  // Clamp zu [0,1]

                System.Windows.Media.Color color = DensityToColor(intensity);

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
    /// Erstellt WriteableBitmap aus Pixel-Daten (muss im UI-Thread aufgerufen werden)
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

    /// <summary>
    /// Rendert die Wahrscheinlichkeitsdichte als Heatmap (synchron, für UI-Thread)
    /// </summary>
    public WriteableBitmap RenderOrbitalDensity(int n, int l, int m, float scale = 50.0f)
    {
        var pixels = ComputePixels(n, l, m, scale);
        return CreateBitmap(pixels);
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

    /// <summary>
    /// Rendert mit Custom Colormap
    /// </summary>
    public WriteableBitmap RenderWithColormap(int n, int l, int m, float scale, Func<double, System.Windows.Media.Color> colormap)
    {
        double maxDensity = 0.0;
        double[] densities = new double[_width * _height];

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                double px = (x - _width / 2.0) / scale;
                double py = (y - _height / 2.0) / scale;
                double r = Math.Sqrt(px * px + py * py);
                double theta = Math.Atan2(py, px);

                double density = OrbitalCalculator.ProbabilityDensity(n, l, m, r, theta, 0.0);
                densities[y * _width + x] = density;

                if (density > maxDensity)
                    maxDensity = density;
            }
        }

        for (int i = 0; i < densities.Length; i++)
        {
            double normalized = maxDensity > 0 ? densities[i] / maxDensity : 0;
            System.Windows.Media.Color color = colormap(normalized);

            int pixelIndex = i * 4;
            _pixels[pixelIndex] = color.B;
            _pixels[pixelIndex + 1] = color.G;
            _pixels[pixelIndex + 2] = color.R;
            _pixels[pixelIndex + 3] = color.A;
        }

        var bitmap = new WriteableBitmap(_width, _height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        bitmap.WritePixels(
            new System.Windows.Int32Rect(0, 0, _width, _height),
            _pixels,
            _width * 4,
            0
        );

        return bitmap;
    }

    /// <summary>
    /// Gibt einen Heatmap-Snap als Grayscale zurück
    /// </summary>
    public WriteableBitmap RenderGrayscale(int n, int l, int m, float scale)
    {
        double maxDensity = 0.0;
        double[] densities = new double[_width * _height];

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                double px = (x - _width / 2.0) / scale;
                double py = (y - _height / 2.0) / scale;
                double r = Math.Sqrt(px * px + py * py);
                double theta = Math.Atan2(py, px);

                double density = OrbitalCalculator.ProbabilityDensity(n, l, m, r, theta, 0.0);
                densities[y * _width + x] = Math.Log(density + 1);  // Log-Skalierung für besseren Kontrast

                if (density > maxDensity)
                    maxDensity = density;
            }
        }

        for (int i = 0; i < densities.Length; i++)
        {
            double normalized = maxDensity > 0 ? densities[i] / maxDensity : 0;
            byte gray = (byte)(normalized * 255);

            int pixelIndex = i * 4;
            _pixels[pixelIndex] = gray;      // B
            _pixels[pixelIndex + 1] = gray;  // G
            _pixels[pixelIndex + 2] = gray;  // R
            _pixels[pixelIndex + 3] = 255;   // A
        }

        var bitmap = new WriteableBitmap(_width, _height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        bitmap.WritePixels(
            new System.Windows.Int32Rect(0, 0, _width, _height),
            _pixels,
            _width * 4,
            0
        );

        return bitmap;
    }
}
