using System;
using System.Windows.Forms;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.WinForms;

namespace LFCharts
{
    public partial class Form1 : Form
    {

        private Data data;
        private const int SensorsNumber = 12;
        private float[] Average;

        public Form1()
        {
            InitializeComponent();
            cartesianChart1.DataTooltip = null;
            cartesianChart1.ScrollMode = ScrollMode.X;
            cartesianChart1.Zoom = ZoomingOptions.X;
            cartesianChart1.DisableAnimations = true;
            cartesianChart1.Hoverable = false;
            cartesianChart1.LegendLocation = LegendLocation.Left;
            SetNumericUpDownIncrement(numericUpDownKp, 0.1m);
            SetNumericUpDownIncrement(numericUpDownKd, 0.1m);
            SetNumericUpDownIncrement(numericUpDownKi, 0.1m);
            cartesianChart1.AxisY.Add(new Axis
            {
                Foreground = System.Windows.Media.Brushes.DodgerBlue,
                Title = "PWM"
            });
            cartesianChart1.AxisY.Add(new Axis
            {
                Foreground = System.Windows.Media.Brushes.IndianRed,
                Title = "Dane",
                Position = AxisPosition.RightTop
            });
        }

        private void SetNumericUpDownIncrement(NumericUpDown numeric, decimal value)
        {
            MethodInvoker methodInvokerDelegate = delegate () { numeric.Increment = value; };
            if (this.InvokeRequired)
                this.Invoke(methodInvokerDelegate);
            else
                methodInvokerDelegate();
        }

        private void toolStripMenuItemOpen_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = "c:\\";
                dialog.Filter = "Pliki RoChN V3 (*.lf3)|*.lf3|Wszystkie pliki|*.*";
                dialog.FilterIndex = 1;
                dialog.RestoreDirectory = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Cursor = Cursors.WaitCursor;
                    var fileStream = dialog.OpenFile();
                    using (System.IO.BinaryReader Reader = new System.IO.BinaryReader(fileStream))
                    {
                        data = new Data(Reader);
                        Init();
                    }
                    if (!data.Ok)
                    {
                        MessageBox.Show("Nie można odczytać pliku!");
                    }
                    fileStream.Dispose();
                    Cursor = Cursors.Arrow;
                }
            }
        }

        private void Init()
        {
            NumericUpDown[] numerics = new NumericUpDown[SensorsNumber + 2];
            for (int i = 0; i < SensorsNumber + 2; i++)
            {
                int n = Convert.ToInt32(panel1.Controls[i].Name.Substring(10));
                SetNumericUpDownValue(panel1.Controls[i] as NumericUpDown, Convert.ToDecimal(data.Weights[n]));
            }
            Average = new float[data.Size / 2];
            int last = 0;
            for (int i = 0; i < data.Size / 2; i++)
            {
                Average[i] = 0;
                int a = 0;
                for (int j = 0; j < 12; j++)
                {
                    if ((data.Values[i] & (1 << j)) > 0)
                        a++;
                    Average[i] += ((data.Values[i] & (1 << j)) >> j) * data.Weights[j];
                }
                if (a == 0)
                {
                    if (last > 0)
                        Average[i] = data.Weights[13];
                    else
                        if (last < 0)
                        Average[i] = data.Weights[0];
                }
                else
                {
                    Average[i] /= a;
                    last = Convert.ToInt32(Average[i]);
                }
            }
            SetNumericUpDownValue(numericUpDownKp, Convert.ToDecimal(data.Kp));
            SetNumericUpDownValue(numericUpDownKd, Convert.ToDecimal(data.Kd));
            SetNumericUpDownValue(numericUpDownKi, Convert.ToDecimal(data.Ki));
        }

        private void toolStripMenuItemClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void buttonPlot_Click(object sender, EventArgs e)
        {
            foreach (var series in cartesianChart1.Series)
                cartesianChart1.Series.Remove(series);
            Cursor = Cursors.WaitCursor;
            if (checkBoxPlotData.Checked)
            {
                cartesianChart1.Series.Add(new LineSeries
                {
                    Values = new ChartValues<float>(Average),
                    Title = "Dane",
                    Fill = System.Windows.Media.Brushes.Transparent,
                    PointGeometry = null,
                    LineSmoothness = 0,
                    ScalesYAt = 1,
                    PointForeground = System.Windows.Media.Brushes.Black
                });
            }
            if (checkBoxPIDValue.Checked)
            {
                int[] Val = new int[data.Size / 2];
                int errPre = 0;
                for (int i = 0; i < data.Size / 2; i++)
                {
                    if (data.Values[i] == 0)
                    {
                        if (errPre < 0)
                        {
                            errPre = Convert.ToInt32(nudWeights0.Value);
                        }
                        else
                        if (errPre > 0)
                        {
                            errPre = Convert.ToInt32(nudWeights11.Value);
                        }
                    }
                    int err = Convert.ToInt32(Average[i] * -1);
                    int Change = Convert.ToInt32(numericUpDownKp.Value * err + numericUpDownKd.Value * (err - errPre));
                    Val[i] = Change;
                }
                cartesianChart1.Series.Add(new LineSeries
                {
                    Values = new ChartValues<int>(Val),
                    Title = "Wart. zmiany",
                    Fill = System.Windows.Media.Brushes.Transparent,
                    PointGeometry = null,
                    LineSmoothness = 0,
                    PointForeground = System.Windows.Media.Brushes.Yellow
                });
            }
            if (checkBoxPIDPwm.Checked)
            {
                int[] Left = new int[data.Size / 2];
                int[] Right = new int[data.Size / 2];
                int errPre = 0;
                for (int i = 0; i < data.Size / 2; i++)
                {
                    if (data.Values[i] == 0)
                    {
                        if (errPre < 0)
                        {
                            errPre = Convert.ToInt32(nudWeights0.Value);
                        }
                        else
                        if (errPre > 0)
                        {
                            errPre = Convert.ToInt32(nudWeights11.Value);
                        }
                    }
                    int err = Convert.ToInt32(Average[i] * -1);
                    int Change = Convert.ToInt32(numericUpDownKp.Value * err + numericUpDownKd.Value * (err - errPre) + numericUpDownKi.Value * (err - errPre));
                    if (data.Values[i] != 0)
                        errPre = err;
                    int tempPwm;
                    tempPwm = Convert.ToInt32(numericUpDownPWM.Value + Change);
                    if (tempPwm > 255)
                        tempPwm = 255;
                    else
                        if (tempPwm < 0)
                        tempPwm = 0;
                    Left[i] = tempPwm;
                    tempPwm = Convert.ToInt32(numericUpDownPWM.Value - Change);
                    if (tempPwm > 255)
                        tempPwm = 255;
                    else
                        if (tempPwm < 0)
                        tempPwm = 0;
                    Right[i] = tempPwm;
                }
                cartesianChart1.Series.Add(new LineSeries
                {
                    Values = new ChartValues<int>(Left),
                    Title = "Lewy PWM",
                    Fill = System.Windows.Media.Brushes.Transparent,
                    PointGeometry = null,
                    LineSmoothness = 0,
                    PointForeground = System.Windows.Media.Brushes.Red
                });
                cartesianChart1.Series.Add(new LineSeries
                {
                    Values = new ChartValues<int>(Right),
                    Title = "Prawy PWM",
                    Fill = System.Windows.Media.Brushes.Transparent,
                    PointGeometry = null,
                    LineSmoothness = 0,
                    PointForeground = System.Windows.Media.Brushes.Blue
                });
            }
            Cursor = Cursors.Arrow;
        }

        private void SetNumericUpDownValue(NumericUpDown numeric, decimal value)
        {
            MethodInvoker methodInvokerDelegate = delegate () { numeric.Value = value; };
            if (this.InvokeRequired)
                this.Invoke(methodInvokerDelegate);
            else
                methodInvokerDelegate();
        }

        private void buttonSetDefaults_Click(object sender, EventArgs e)
        {
            SetNumericUpDownValue(numericUpDownKp, Convert.ToDecimal(data.Kp));
            SetNumericUpDownValue(numericUpDownKd, Convert.ToDecimal(data.Kd));
            SetNumericUpDownValue(numericUpDownKi, Convert.ToDecimal(data.Ki));
        }
    }

    class Data
    {
        public readonly bool Ok;
        public readonly short[] Weights;
        public readonly float Kp, Kd, Ki;
        public readonly uint Size;
        public readonly int[] Values;

        public Data(System.IO.BinaryReader reader)
        {
            string header;
            try
            {
                header = reader.ReadString();
            }
            catch
            {
                Ok = false;
                return;
            }
            if (header != "lf3")
            {
                Ok = false;
                return;
            }
            Weights = new short[14];
            for (int i = 0; i < 14; i++)
                Weights[i] = reader.ReadInt16();
            Kp = reader.ReadSingle();
            Kd = reader.ReadSingle();
            Ki = reader.ReadSingle();
            Size = reader.ReadUInt32();
            for (int i = 0; i < 8; i++)
            {
                reader.ReadUInt16();
            }
            Values = new int[Size / 2];
            for (int i = 0; i < Size / 2; i++)
            {
                Values[i] = Convert.ToInt32((reader.ReadByte() << 8) + reader.ReadByte() >> 4);
            }
            try
            {
                header = reader.ReadString();
            }
            catch
            {
                Ok = false;
                return;
            }
            if (header != "lf3")
            {
                Ok = false;
                return;
            }
            Ok = true;
            return;
        }
    }
}