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

    public RaytracingEngine(int width, int height)
    {
        _width = width;
        _height = height;
        _pixels = new byte[width * height * 4];  // RGBA
    }

    /// <summary>
    /// Rendert die Wahrscheinlichkeitsdichte als Heatmap
    /// </summary>
    public WriteableBitmap RenderOrbitalDensity(int n, int l, int m, float scale = 50.0f)
    {
        // Finde maximale Dichte für Normalisierung
        double maxDensity = 0.0;
        double[] densities = new double[_width * _height];

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width;x++)
            {
                // Konvertiere Pixel zu Koordinaten
                double px = (x - _width / 2.0) / scale;
                double py = (y - _height / 2.0) / scale;
                double r = Math.Sqrt(px * px + py * py);
                double theta = Math.Atan2(py, px);
                double phi = 0.0;  // 2D Querschnitt

                // Berechne Dichte
                double density = OrbitalCalculator.ProbabilityDensity(n, l, m, r, theta, phi);
                densities[y * _width + x] = density;

                if (density > maxDensity)
                    maxDensity = density;
            }
        }

        // Rendere als Heatmap
        for (int i = 0; i < densities.Length; i++)
        {
            double normalizedDensity = maxDensity > 0 ? densities[i] / maxDensity : 0;
            System.Windows.Media.Color color = DensityToColor(normalizedDensity);

            int pixelIndex = i * 4;
            _pixels[pixelIndex] = color.B;      // B
            _pixels[pixelIndex + 1] = color.G;  // G
            _pixels[pixelIndex + 2] = color.R;  // R
            _pixels[pixelIndex + 3] = color.A;  // A
        }

        // Erstelle WriteableBitmap
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
    /// Konvertiert Dichte zu Farbe (Farbveranstaltung: Blau → Cyan → Grün → Gelb → Rot)
    /// </summary>
    private static System.Windows.Media.Color DensityToColor(double density)
    {
        // Viridis-ähnliche Farbkarte
        if (density < 0.25)
        {
            // Blau zu Cyan
            double t = density / 0.25;
            byte r = 0;
            byte g = (byte)(255 * t);
            byte b = 255;
            return System.Windows.Media.Color.FromArgb(255, r, g, b);
        }
        else if (density < 0.5)
        {
            // Cyan zu Grün
            double t = (density - 0.25) / 0.25;
            byte r = 0;
            byte g = 255;
            byte b = (byte)(255 * (1 - t));
            return System.Windows.Media.Color.FromArgb(255, r, g, b);
        }
        else if (density < 0.75)
        {
            // Grün zu Gelb
            double t = (density - 0.5) / 0.25;
            byte r = (byte)(255 * t);
            byte g = 255;
            byte b = 0;
            return System.Windows.Media.Color.FromArgb(255, r, g, b);
        }
        else
        {
            // Gelb zu Rot
            double t = (density - 0.75) / 0.25;
            byte r = 255;
            byte g = (byte)(255 * (1 - t));
            byte b = 0;
            return System.Windows.Media.Color.FromArgb(255, r, g, b);
        }
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
