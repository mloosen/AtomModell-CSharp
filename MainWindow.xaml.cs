using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Numerics;
using AtomModell_CSharp.Models;

namespace AtomModell_CSharp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private SimulationEngine _engine = null!;
    private DispatcherTimer _renderTimer = null!;
    private Vector2 _canvasCenter;
    private const float SCALE_FACTOR = 0.5f;  // Skalierungsfaktor für Visualisierung
    private RaytracingEngine _raytracingEngine = null!;
    private RaytracingEngine3D _raytracingEngine3D = null!;
    private int _lastRenderedN = -1, _lastRenderedL = -1, _lastRenderedM = -1;  // Cache für Raytracing
    private int _last3DN = -1, _last3DL = -1, _last3DM = -1;  // Cache für 3D Raytracing
    
    // Mouse-Interaktion für 3D Rotation
    private bool _isDragging = false;
    private Point _lastMousePos;
    
    // Async Rendering Flags
    private bool _isRendering2D = false;
    private bool _isRendering3D = false;

    public MainWindow()
    {
        InitializeComponent();
        InitializeSimulation();
    }

    private void InitializeSimulation()
    {
        _engine = new SimulationEngine();
        _engine.OrbitDistance = 80.0f;  // Kleinerer Orbit für bessere Sicht
        _engine.Initialize();

        // Raytracing Engines initialisieren
        int canvasWidth = (int)RenderCanvas.ActualWidth > 0 ? (int)RenderCanvas.ActualWidth : 800;
        int canvasHeight = (int)RenderCanvas.ActualHeight > 0 ? (int)RenderCanvas.ActualHeight : 600;
        _raytracingEngine = new RaytracingEngine(canvasWidth, canvasHeight);
        _raytracingEngine3D = new RaytracingEngine3D(canvasWidth, canvasHeight);

        // Render Timer starten
        _renderTimer = new DispatcherTimer();
        _renderTimer.Interval = TimeSpan.FromMilliseconds(16);  // ~60 FPS
        _renderTimer.Tick += RenderTimer_Tick;
        _renderTimer.Start();

        // Initialisiere die Slider-Anzeigen
        ValueN.Text = SliderN.Value.ToString();
        ValueL.Text = SliderL.Value.ToString();
        ValueM.Text = SliderM.Value.ToString();
        ValueColorScale.Text = SliderColorScale.Value.ToString("F2");

        UpdateStatus();
    }

    private async void RenderTimer_Tick(object? sender, EventArgs e)
    {
        _engine.Update();
        
        // Rendere basierend auf aktuellem Tab
        if (ViewTabs.SelectedIndex == 0)
        {
            Render();  // Klassische Ansicht
        }
        else if (ViewTabs.SelectedIndex == 1)
        {
            await RenderRaytracingAsync();  // Raytracing/Densitymap
        }
        else if (ViewTabs.SelectedIndex == 2)
        {
            await RenderRaytracing3DAsync();  // 3D Raytracing
        }
        
        UpdateStatus();
    }

    private void Render()
    {
        RenderCanvas.Children.Clear();

        // Berechne Canvas-Mitte neu (für responsive Anpassung)
        _canvasCenter = new Vector2((float)RenderCanvas.ActualWidth / 2, (float)RenderCanvas.ActualHeight / 2);

        foreach (var atom in _engine.Atoms)
        {
            DrawAtom(atom);
        }
    }

    private void DrawAtom(Atom atom)
    {
        // Zeichne zuerst die Orbits (damit sie hinter den Partikeln liegen)
        foreach (var particle in atom.Particles)
        {
            if (particle.Charge == ChargeType.Electron)
            {
                DrawOrbit(atom.Position, particle.QuantumNumberN);
            }
        }

        // Dann zeichne die Partikel
        foreach (var particle in atom.Particles)
        {
            DrawParticle(particle);
        }
    }

    private void DrawParticle(Particle particle)
    {
        var radius = particle.GetRadius() * SCALE_FACTOR;
        var color = particle.GetColor();
        
        // Canvas-Koordinaten: atom.Position ist relativ zu (0,0), wir zentrieren auf _canvasCenter
        var pos = particle.Position + _canvasCenter;

        var circle = new Ellipse
        {
            Width = radius * 2,
            Height = radius * 2,
            Fill = new SolidColorBrush(color),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 1.5
        };

        Canvas.SetLeft(circle, pos.X - radius);
        Canvas.SetTop(circle, pos.Y - radius);
        RenderCanvas.Children.Add(circle);

        // Beschriftung für Proton
        if (particle.Charge == ChargeType.Proton)
        {
            var label = new TextBlock
            {
                Text = "p+",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                TextAlignment = System.Windows.TextAlignment.Center
            };
            Canvas.SetLeft(label, pos.X - 8);
            Canvas.SetTop(label, pos.Y - 6);
            RenderCanvas.Children.Add(label);
        }
        else if (particle.Charge == ChargeType.Electron)
        {
            var label = new TextBlock
            {
                Text = "e−",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                TextAlignment = System.Windows.TextAlignment.Center
            };
            Canvas.SetLeft(label, pos.X - 5);
            Canvas.SetTop(label, pos.Y - 5);
            RenderCanvas.Children.Add(label);
        }
    }

    private void DrawOrbit(Vector2 atomCenter, int n)
    {
        float orbitRadius = _engine.OrbitDistance * n * SCALE_FACTOR;
        const int segments = 360;

        var pathFigure = new System.Windows.Media.PathFigure
        {
            IsClosed = true,
            IsFilled = false
        };

        // Zeichne Orbit als Kreis
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)(2 * Math.PI * i / segments);
            float x = atomCenter.X + (float)Math.Cos(angle) * orbitRadius;
            float y = atomCenter.Y + (float)Math.Sin(angle) * orbitRadius;
            
            var point = new Point(x + _canvasCenter.X, y + _canvasCenter.Y);
            
            if (i == 0)
                pathFigure.StartPoint = point;
            else
                pathFigure.Segments.Add(new System.Windows.Media.LineSegment { Point = point });
        }

        var pathGeometry = new System.Windows.Media.PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        var path = new System.Windows.Shapes.Path
        {
            Data = pathGeometry,
            Stroke = new SolidColorBrush(Colors.DarkGray),
            StrokeThickness = 1.0,
            Opacity = 0.7
        };

        RenderCanvas.Children.Add(path);
    }

    private async Task RenderRaytracingAsync()
    {
        if (_engine.Atoms.Count == 0 || _isRendering2D) return;

        var firstAtom = _engine.Atoms[0];
        var electron = firstAtom.GetElectrons().FirstOrDefault();

        if (electron == null) return;

        // Prüfe ob Quantenzahlen sich geändert haben
        if (_lastRenderedN != electron.QuantumNumberN || 
            _lastRenderedL != electron.QuantumNumberL || 
            _lastRenderedM != electron.QuantumNumberM)
        {
            _isRendering2D = true;
            Overlay2D.Visibility = System.Windows.Visibility.Visible;
            
            int n = electron.QuantumNumberN;
            int l = electron.QuantumNumberL;
            int m = electron.QuantumNumberM;

            try
            {
                // Berechne Pixel im Background-Thread, erstelle Bitmap im UI-Thread
                var pixels = await Task.Run(() => _raytracingEngine.ComputePixels(n, l, m, scale: 30.0f));
                var bitmap = _raytracingEngine.CreateBitmap(pixels);
                RaytracingImage.Source = bitmap;

                _lastRenderedN = n;
                _lastRenderedL = l;
                _lastRenderedM = m;
            }
            finally
            {
                _isRendering2D = false;
                Overlay2D.Visibility = System.Windows.Visibility.Collapsed;
            }
        }
    }

    private async Task RenderRaytracing3DAsync()
    {
        if (_isRendering3D) return;

        // Lese Quantum-Zahlen aus den Schiebereglern
        int n = (int)SliderN.Value;
        int l = (int)SliderL.Value;
        int m = (int)SliderM.Value;

        // Prüfe ob die Werte sich geändert haben oder ob initial gerendert werden muss
        if (_last3DN != n || _last3DL != l || _last3DM != m)
        {
            _isRendering3D = true;
            Overlay3D.Visibility = System.Windows.Visibility.Visible;
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"Rendering 3D: n={n}, l={l}, m={m}");
                
                // Berechne Pixel im Background-Thread, erstelle Bitmap im UI-Thread
                var pixels = await Task.Run(() => _raytracingEngine3D.ComputePixels3D(n, l, m));
                var bitmap = _raytracingEngine3D.CreateBitmap(pixels);
                Raytracing3DImage.Source = bitmap;

                _last3DN = n;
                _last3DL = l;
                _last3DM = m;
                
                System.Diagnostics.Debug.WriteLine("3D Rendering completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"3D Rendering error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
            }
            finally
            {
                _isRendering3D = false;
                Overlay3D.Visibility = System.Windows.Visibility.Collapsed;
            }
        }
    }

    private void UpdateStatus()
    {
        if (_engine.Atoms.Count == 0) return;

        var firstAtom = _engine.Atoms[0];
        var electron = firstAtom.GetElectrons().FirstOrDefault();

        if (electron != null)
        {
            electron.UpdateQuantumNumbers();
            OrbitalNText.Text = $"n = {electron.QuantumNumberN}";
            OrbitalLText.Text = $"l = {electron.QuantumNumberL}";
            OrbitalMText.Text = $"m = {electron.QuantumNumberM}";
            EnergyText.Text = $"E = {electron.Energy:F2} eV";
        }

        StatusText.Text = $"Atome: {_engine.Atoms.Count}\nFrames: {_engine.GetFrameCount()}";
        FpsText.Text = $"FPS: {_engine.GetFramesPerSecond():F1}";
    }

    // Event Handler für Buttons
    private void BtnExciteSmall_Click(object sender, RoutedEventArgs e)
    {
        _engine.ExciteAllElectrons(0.01);
    }

    private void BtnDeexciteSmall_Click(object sender, RoutedEventArgs e)
    {
        _engine.DeexciteAllElectrons(0.01);
    }

    private void BtnExciteLarge_Click(object sender, RoutedEventArgs e)
    {
        _engine.ExciteAllElectrons(0.1);
    }

    private void BtnDeexciteLarge_Click(object sender, RoutedEventArgs e)
    {
        _engine.DeexciteAllElectrons(0.1);
    }

    private void BtnExciteMax_Click(object sender, RoutedEventArgs e)
    {
        _engine.ExciteAllElectrons(1.0);
    }

    private void BtnDeexciteMax_Click(object sender, RoutedEventArgs e)
    {
        _engine.DeexciteAllElectrons(1.0);
    }

    private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
    {
        _engine.IsRunning = !_engine.IsRunning;
        BtnPlayPause.Content = _engine.IsRunning ? "⏸ Pause" : "▶ Play";
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        _engine.Reset();
        UpdateStatus();
    }

    private void RenderCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        // Kann für zukünftige Interaktionen genutzt werden
    }

    // 3D Raytracing Slider Event Handler
    private void SliderN_ValueChanged(object sender, MouseButtonEventArgs e)
    {
        int n = (int)SliderN.Value;
        // Stelle sicher, dass l <= n-1 ist
        if (SliderL.Value > n - 1)
            SliderL.Value = n - 1;
        SliderL.Maximum = n - 1;
        ValueN.Text = n.ToString();
    }

    private void SliderL_ValueChanged(object sender, MouseButtonEventArgs e)
    {
        int l = (int)SliderL.Value;
        // Stelle sicher, dass m zwischen -l und +l liegt
        if (SliderM.Value > l)
            SliderM.Value = l;
        if (SliderM.Value < -l)
            SliderM.Value = -l;
        SliderM.Minimum = -l;
        SliderM.Maximum = l;
        ValueL.Text = l.ToString();
    }

    private void SliderM_ValueChanged(object sender, MouseButtonEventArgs e)
    {
        int m = (int)SliderM.Value;
        ValueM.Text = m.ToString();
    }

    private void SliderColorScale_ValueChanged(object sender, MouseButtonEventArgs e)
    {
        double colorScale = SliderColorScale.Value;
        _raytracingEngine3D.ColorScale = (float)colorScale;
        ValueColorScale.Text = colorScale.ToString("F2");
    }

    private void SliderClipX_ValueChanged(object sender, MouseButtonEventArgs e)
    {
        double clipX = SliderClipX.Value;
        _raytracingEngine3D.ClipX = (float)clipX;
        ValueClipX.Text = clipX.ToString("F2");
    }

    private void SliderClipY_ValueChanged(object sender, MouseButtonEventArgs e)
    {
        double clipY = SliderClipY.Value;
        _raytracingEngine3D.ClipY = (float)clipY;
        ValueClipY.Text = clipY.ToString("F2");
    }

    private void SliderClipZ_ValueChanged(object sender, MouseButtonEventArgs e)
    {
        double clipZ = SliderClipZ.Value;
        _raytracingEngine3D.ClipZ = (float)clipZ;
        ValueClipZ.Text = clipZ.ToString("F2");
    }

    // 3D Mouse Rotation Event Handlers
    private void Raytracing3DImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _lastMousePos = e.GetPosition(Raytracing3DImage);
        Raytracing3DImage.CaptureMouse();
    }

    private void Raytracing3DImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        Point currentPos = e.GetPosition(Raytracing3DImage);
        double deltaX = currentPos.X - _lastMousePos.X;
        double deltaY = currentPos.Y - _lastMousePos.Y;

        // Update Rotation basierend auf Mausbewegung
        // deltaX → Rotation um Y-Achse (horizontal)
        // deltaY → Rotation um X-Achse (vertikal)
        _raytracingEngine3D.RotationY += (float)(deltaX * 0.01);
        _raytracingEngine3D.RotationX += (float)(deltaY * 0.01);

        // Erzwinge Neurendering durch Cache-Reset
        _last3DN = -1;

        _lastMousePos = currentPos;
    }

    private void Raytracing3DImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        Raytracing3DImage.ReleaseMouseCapture();
    }
}
