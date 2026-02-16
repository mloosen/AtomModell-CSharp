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
    private int _lastRenderedN = -1, _lastRenderedL = -1, _lastRenderedM = -1;  // Cache für Raytracing

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

        // Raytracing Engine initialisieren
        int canvasWidth = (int)RenderCanvas.ActualWidth > 0 ? (int)RenderCanvas.ActualWidth : 800;
        int canvasHeight = (int)RenderCanvas.ActualHeight > 0 ? (int)RenderCanvas.ActualHeight : 600;
        _raytracingEngine = new RaytracingEngine(canvasWidth, canvasHeight);

        // Render Timer starten
        _renderTimer = new DispatcherTimer();
        _renderTimer.Interval = TimeSpan.FromMilliseconds(16);  // ~60 FPS
        _renderTimer.Tick += RenderTimer_Tick;
        _renderTimer.Start();

        UpdateStatus();
    }

    private void RenderTimer_Tick(object? sender, EventArgs e)
    {
        _engine.Update();
        
        // Rendere basierend auf aktuellem Tab
        if (ViewTabs.SelectedIndex == 0)
        {
            Render();  // Klassische Ansicht
        }
        else if (ViewTabs.SelectedIndex == 1)
        {
            RenderRaytracing();  // Raytracing/Densitymap
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

    private void RenderRaytracing()
    {
        if (_engine.Atoms.Count == 0) return;

        var firstAtom = _engine.Atoms[0];
        var electron = firstAtom.GetElectrons().FirstOrDefault();

        if (electron == null) return;

        // Prüfe ob Quantenzahlen sich geändert haben
        if (_lastRenderedN != electron.QuantumNumberN || 
            _lastRenderedL != electron.QuantumNumberL || 
            _lastRenderedM != electron.QuantumNumberM)
        {
            // Rendere neue Densitymap
            var bitmap = _raytracingEngine.RenderOrbitalDensity(
                electron.QuantumNumberN,
                electron.QuantumNumberL,
                electron.QuantumNumberM,
                scale: 30.0f
            );

            RaytracingImage.Source = bitmap;

            _lastRenderedN = electron.QuantumNumberN;
            _lastRenderedL = electron.QuantumNumberL;
            _lastRenderedM = electron.QuantumNumberM;
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
        var pos = e.GetPosition(RenderCanvas);
        // Kann für zukünftige Interaktionen genutzt werden
    }
}
