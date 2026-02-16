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

    public MainWindow()
    {
        InitializeComponent();
        InitializeSimulation();
    }

    private void InitializeSimulation()
    {
        _engine = new SimulationEngine();
        _engine.Initialize();

        _canvasCenter = new Vector2((float)RenderCanvas.ActualWidth / 2, (float)RenderCanvas.ActualHeight / 2);

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
        Render();
        UpdateStatus();
    }

    private void Render()
    {
        RenderCanvas.Children.Clear();

        foreach (var atom in _engine.Atoms)
        {
            DrawAtom(atom);
        }
    }

    private void DrawAtom(Atom atom)
    {
        foreach (var particle in atom.Particles)
        {
            DrawParticle(particle);

            // Zeichne die Umlaufbahn für Elektronen
            if (particle.Charge == ChargeType.Electron)
            {
                DrawOrbit(atom.Position, particle.QuantumNumberN);
            }
        }
    }

    private void DrawParticle(Particle particle)
    {
        var radius = particle.GetRadius();
        var color = particle.GetColor();
        var pos = particle.Position + _canvasCenter;

        var circle = new Ellipse
        {
            Width = radius * 2,
            Height = radius * 2,
            Fill = new SolidColorBrush(color),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 1
        };

        Canvas.SetLeft(circle, pos.X - radius);
        Canvas.SetTop(circle, pos.Y - radius);
        RenderCanvas.Children.Add(circle);

        // Beschriftung
        if (particle.Charge == ChargeType.Proton)
        {
            var label = new TextBlock
            {
                Text = "p+",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 10,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(label, pos.X - 8);
            Canvas.SetTop(label, pos.Y - 6);
            RenderCanvas.Children.Add(label);
        }
    }

    private void DrawOrbit(Vector2 center, int n)
    {
        float orbitRadius = _engine.OrbitDistance * n;
        const int segments = 500;

        var points = new System.Windows.Media.PointCollection();

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)(2 * Math.PI * i / segments);
            float x = center.X + (float)Math.Cos(angle) * orbitRadius + _canvasCenter.X;
            float y = center.Y + (float)Math.Sin(angle) * orbitRadius + _canvasCenter.Y;
            points.Add(new Point(x, y));
        }

        var polyline = new Polyline
        {
            Points = points,
            Stroke = new SolidColorBrush(Colors.Gray),
            StrokeThickness = 0.5,
            Opacity = 0.5
        };

        RenderCanvas.Children.Add(polyline);
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
