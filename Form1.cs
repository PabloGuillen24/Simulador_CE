using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;

namespace Simulador_CE
{
    public partial class Form1 : Form
    {
        private Panel simulationPanel;
        private Button btnAddPositive, btnAddNegative, btnAddSensor, btnClear;
        private CheckBox cbVectors, cbVoltage, cbValues;
        private List<Carga> cargas = new List<Carga>();
        public List<Sensor> sensores = new List<Sensor>();
        private Carga cargaSeleccionada;
        private Sensor sensorSeleccionado;
        private Point offset;
        private bool arrastrando;

        public Form1()
        {
            this.ClientSize = new Size(900, 600);
            this.Text = "Simulador de Campo Eléctrico";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
            InicializarInterfaz();
        }

        private void InicializarInterfaz()
        {
            simulationPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            simulationPanel.GetType()
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(simulationPanel, true, null);
            simulationPanel.Paint += SimulationPanel_Paint;
            simulationPanel.MouseDown += SimulationPanel_MouseDown;
            simulationPanel.MouseMove += SimulationPanel_MouseMove;
            simulationPanel.MouseUp += SimulationPanel_MouseUp;
            this.Controls.Add(simulationPanel);

            FlowLayoutPanel controls = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10),
                BackColor = Color.LightGray
            };

            btnAddPositive = new Button { Text = "Cargar +", Width = 80 };
            btnAddPositive.Click += (s, e) => { cargas.Add(new Carga(CentroPanel(), 1e-6)); simulationPanel.Invalidate(); };
            btnAddNegative = new Button { Text = "Cargar -", Width = 80 };
            btnAddNegative.Click += (s, e) => { cargas.Add(new Carga(CentroPanel(), -1e-6)); simulationPanel.Invalidate(); };
            btnAddSensor = new Button { Text = "Sensor", Width = 80 };
            btnAddSensor.Click += (s, e) => { sensores.Add(new Sensor(CentroPanel())); simulationPanel.Invalidate(); };
            btnClear = new Button { Text = "Limpiar", Width = 80 };
            btnClear.Click += (s, e) => { cargas.Clear(); sensores.Clear(); simulationPanel.Invalidate(); };

            cbVectors = new CheckBox { Text = "Vectores", Checked = true };
            cbVectors.CheckedChanged += (s, e) => simulationPanel.Invalidate();
            cbVoltage = new CheckBox { Text = "Voltajes" };
            cbVoltage.CheckedChanged += (s, e) => simulationPanel.Invalidate();
            cbValues = new CheckBox { Text = "Valores" };
            cbValues.CheckedChanged += (s, e) => simulationPanel.Invalidate();

            controls.Controls.AddRange(new Control[]
            {
                btnAddPositive,
                btnAddNegative,
                btnAddSensor,
                btnClear,
                cbVectors,
                cbVoltage,
                cbValues
            });
            this.Controls.Add(controls);
        }

        private Point CentroPanel() => new Point(simulationPanel.Width / 2, simulationPanel.Height / 2);

        private void SimulationPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (cbVectors.Checked)
                DibujarVectores(g);

            foreach (var c in cargas)
                c.Dibujar(g);

            foreach (var s in sensores)
                s.Dibujar(g, cargas, cbValues.Checked);
        }

        private void DibujarVectores(Graphics g)
        {
            int espaciamiento = 40;
            float longitud = 20;
            float tamanoPunta = 6;
            using (Pen pen = new Pen(Color.White, 2))
            {
                pen.CustomEndCap = new AdjustableArrowCap(tamanoPunta, tamanoPunta, true);
                for (int x = espaciamiento / 2; x < simulationPanel.Width; x += espaciamiento)
                {
                    for (int y = espaciamiento / 2; y < simulationPanel.Height; y += espaciamiento)
                    {
                        var vec = CalcularCampo(new Point(x, y));
                        double mag = Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y);
                        if (mag == 0) continue;
                        float dx = (float)(vec.X / mag);
                        float dy = (float)(vec.Y / mag);
                        g.DrawLine(pen, x, y, x + dx * longitud, y + dy * longitud);
                    }
                }
            }
        }

        private Vector CalcularCampo(Point p)
        {
            Vector campo = new Vector(0, 0);
            foreach (var c in cargas)
            {
                double dx = p.X - c.Posicion.X;
                double dy = p.Y - c.Posicion.Y;
                double d2 = dx * dx + dy * dy;
                if (d2 < 25) continue;
                double d = Math.Sqrt(d2);
                double magnitud = c.Valor / d2;
                campo.X += magnitud * dx / d;
                campo.Y += magnitud * dy / d;
            }
            return campo;
        }

        private void SimulationPanel_MouseDown(object sender, MouseEventArgs e)
        {
            foreach (var c in cargas)
            {
                if (Distancia(e.Location, c.Posicion) <= 20)
                {
                    cargaSeleccionada = c;
                    offset = new Point(e.X - c.Posicion.X, e.Y - c.Posicion.Y);
                    arrastrando = true;
                    return;
                }
            }
            foreach (var s in sensores)
            {
                if (Distancia(e.Location, s.Posicion) <= 10)
                {
                    sensorSeleccionado = s;
                    offset = new Point(e.X - s.Posicion.X, e.Y - s.Posicion.Y);
                    arrastrando = true;
                    return;
                }
            }
        }

        private void SimulationPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!arrastrando) return;
            if (cargaSeleccionada != null)
            {
                cargaSeleccionada.Posicion = new Point(e.X - offset.X, e.Y - offset.Y);
            }
            else if (sensorSeleccionado != null)
            {
                int nx = e.X - offset.X;
                int ny = e.Y - offset.Y;
                nx = Math.Max(0, Math.Min(simulationPanel.Width, nx));
                ny = Math.Max(0, Math.Min(simulationPanel.Height, ny));
                sensorSeleccionado.Posicion = new Point(nx, ny);
            }
            simulationPanel.Invalidate();
        }

        private void SimulationPanel_MouseUp(object sender, MouseEventArgs e)
        {
            arrastrando = false;
            cargaSeleccionada = null;
            sensorSeleccionado = null;
        }

        private double Distancia(Point a, Point b)
            => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }

    public class Carga
    {
        public Point Posicion;
        public double Valor;
        public Carga(Point pos, double val)
        {
            Posicion = pos;
            Valor = val;
        }
        public void Dibujar(Graphics g = null)
        {
            int r = 20;
            Brush b = Valor > 0 ? Brushes.Red : Brushes.Blue;
            g.FillEllipse(b, Posicion.X - r / 2, Posicion.Y - r / 2, r, r);
            g.DrawEllipse(Pens.White, Posicion.X - r / 2, Posicion.Y - r / 2, r, r);
            string sim = Valor > 0 ? "+" : "–";
            using (Font f = new Font("Arial", 14, FontStyle.Bold))
            {
                var sz = g.MeasureString(sim, f);
                g.DrawString(sim, f, Brushes.White, Posicion.X - sz.Width / 2, Posicion.Y - sz.Height / 2);
            }
        }
    }

    public class Sensor
    {
        public Point Posicion;

        public Sensor(Point pos) => Posicion = pos;

        public void Dibujar(Graphics g, List<Carga> cargas, bool mostrarValores)
        {
            Rectangle rect = new Rectangle(Posicion.X - 5, Posicion.Y - 5, 10, 10);
            g.FillEllipse(Brushes.Yellow, rect);
            g.DrawEllipse(Pens.Black, rect);

            Vector campo = new Vector(0, 0);
            const double k = 8.99e9;
            foreach (var carga in cargas)
            {
                double dx = Posicion.X - carga.Posicion.X;
                double dy = Posicion.Y - carga.Posicion.Y;
                double r2 = dx * dx + dy * dy;
                if (r2 < 100) r2 = 100; 
                double r = Math.Sqrt(r2);
                double E = k * carga.Valor / r2;
                campo.X += E * dx / r;
                campo.Y += E * dy / r;
            }

            double magnitud = Math.Sqrt(campo.X * campo.X + campo.Y * campo.Y);
            if (magnitud < 1e-5) return;

            float dirX = (float)(campo.X / magnitud);
            float dirY = (float)(campo.Y / magnitud);

            float escala = 150; 
            float longitud = (float)(magnitud * escala);
            longitud = Math.Max(10f, Math.Min(250f, longitud));

            using (Pen pen = new Pen(Color.Red, 3))
            {
                PointF inicio = new PointF(Posicion.X, Posicion.Y);
                PointF fin = new PointF(Posicion.X + dirX * longitud, Posicion.Y + dirY * longitud);
                g.DrawLine(pen, inicio, fin);
            }

            if (mostrarValores)
            {
                double angulo = Math.Atan2(-dirY, dirX) * 180 / Math.PI;
                string texto = $"{angulo:F1}°\n{magnitud:E2} V/m";
                using (Font fuente = new Font("Arial", 10, FontStyle.Bold))
                {
                    g.DrawString($"{angulo:F1}°", fuente, Brushes.Yellow, Posicion.X + 10, Posicion.Y - 25);
                    g.DrawString($"{magnitud:E2} V/m", fuente, Brushes.Yellow, Posicion.X + 10, Posicion.Y - 10);
                }
            }
        }




    }







    public class Vector
    {
        public double X, Y;
        public Vector(double x, double y) { X = x; Y = y; }
    }
}
