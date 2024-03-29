﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static Magnetic_Raw_Data_Viewer.Raw;

namespace Magnetic_Raw_Data_Viewer
{
    public partial class Form1 : Form
    {
        List<Fm> data;
        int gridy = 10;
        int viewx = 10;
        const int intervalx = 1;
        int last_midy = 0, int_firstfix = 0, int_lastfix = 0;
        string filename = "";
        const string form_title = "Magnetic Raw Data Viewer";
        readonly string rootpath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\"
            + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "\\";
        public Form1()
        {
            InitializeComponent();
            //this.WindowState = FormWindowState.Maximized;
            var _chartArea = chart1.ChartAreas[0];

            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
            this.MouseWheel += Chart1_MouseWheel;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);

            comboBox1.SelectedIndex = 2;
            comboBox2.SelectedIndex = 2;
            comboBox3.SelectedIndex = 0;

            _chartArea.AxisX.LabelStyle.Format = "0";
            _chartArea.AxisY.LabelStyle.Format = "0";
            _chartArea.AxisX.ScrollBar.ButtonStyle = ScrollBarButtonStyles.SmallScroll;
            _chartArea.AxisX.ScrollBar.Size = 18;
            _chartArea.AxisY.ScrollBar.ButtonStyle = ScrollBarButtonStyles.SmallScroll;
            _chartArea.AxisY.ScrollBar.Size = 18;
            _chartArea.AxisX.Interval = intervalx;
            _chartArea.AxisX.ScaleView.Size = viewx;
            _chartArea.AxisY.Interval = gridy;
            _chartArea.AxisY.ScaleView.Size = gridy * 4;
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            List<string> filelist = OpenRawFiles_Dialog();
            if (filelist.Count == 1)
                Open_One_Raw_file(filelist[0]); //test
            else if (filelist.Count > 1)
                Open_Multiple_Raw_files(filelist);
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            if (data != null)
            {
                Generate_Screen_Dumps();
                MessageBox.Show($"Check the files in :\n{rootpath}", "Exported", MessageBoxButtons.OK);
            }
        }
        private void Button3_Click(object sender, EventArgs e)
        {
            if (data != null)
            {
                chart1.ChartAreas[0].AxisX.ScrollBar.Enabled = false;
                if (!System.IO.Directory.Exists(rootpath))
                    System.IO.Directory.CreateDirectory(rootpath);
                double viewMin = chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
                string ofilename = $"{rootpath}{filename}_F{viewMin:0}-F{viewMin + viewx:0}.gif";
                chart1.SaveImage(ofilename, ChartImageFormat.Gif);
                chart1.ChartAreas[0].AxisX.ScrollBar.Enabled = true;
                MessageBox.Show($"Current Chart saved at :\n{ofilename}", "Exported", MessageBoxButtons.OK);
            }
        }
        private void Chart1_MouseWheel(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Delta < 0) // Scrolled down.
                    Scroll_right(1);
                else if (e.Delta > 0) // Scrolled up.
                    Scroll_left(1);
            }
            catch { }
        }
        private void Chart1_DoubleClick(object sender, EventArgs e)
        {
            List<string> filelist = OpenRawFiles_Dialog();
            if (filelist.Count == 1)
                Open_One_Raw_file(filelist[0]); //test
            else if (filelist.Count > 1)
                Open_Multiple_Raw_files(filelist);
        }
        private void Chart1_AxisViewChanged(object sender, System.Windows.Forms.DataVisualization.Charting.ViewEventArgs e)
        {
            if (data != null) Chart1_updateYaxis();
        }
        private void Chart1_updateYaxis()
        {
            ResetAxesScale();
            double viewMin = chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
            double viewMax = chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum;

            //IEnumerable<Fm> viewdata2 = data.Where(item => (item.fix >= viewMin && item.fix <= viewMax));
            //viewdata2.OrderBy(item => item.mag);

            IOrderedEnumerable<Fm> viewdata = from item in data
                                              where item.fix >= viewMin && item.fix <= viewMax
                                              orderby item.mag
                                              select item;
            if (viewdata == null | viewdata.Count() == 0)
            {
                ResetAxesScale();
                return;
            }

            int midy = (int)viewdata.ElementAt((viewdata.Count() - 1) / 2).mag;

            if (midy != last_midy)
            {
                chart1.ChartAreas[0].AxisY.Minimum = midy - gridy * 2;
                chart1.ChartAreas[0].AxisY.Maximum = midy + gridy * 2;
                chart1.ChartAreas[0].AxisY.ScaleView.Zoom(midy - gridy * 2, midy + gridy * 2);
                last_midy = midy;
            }
        }
        private void ComboBox1_TextChanged(object sender, EventArgs e)
        {
            gridy = int.Parse(comboBox1.Text);
            chart1.ChartAreas[0].AxisY.Interval = gridy;
            chart1.ChartAreas[0].AxisY.ScaleView.Size = gridy * 4;
            chart1.ChartAreas[0].AxisY.Minimum = last_midy - gridy * 2;
            chart1.ChartAreas[0].AxisY.Maximum = last_midy + gridy * 2;
            chart1.ChartAreas[0].AxisY.ScaleView.Zoom(last_midy - gridy * 2, last_midy + gridy * 2);
        }
        private void ComboBox2_TextChanged(object sender, EventArgs e)
        {
            viewx = int.Parse(comboBox2.Text);
            chart1.ChartAreas[0].AxisX.ScaleView.Size = viewx;
        }
        private void ComboBox3_TextChanged(object sender, EventArgs e)
        {
            chart1.Series[0].BorderWidth = int.Parse(comboBox3.Text);
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!chart1.Focused | data == null)
            {
                //chart1.Focus();
                e.Handled = true;
            }
            //Console.WriteLine(e.KeyValue);
            switch (e.KeyValue)
            {
                case 65: //A
                    Scroll_left_1();
                    break;
                case 219: //[
                    Scroll_left(1); break;
                case 68: //D
                    Scroll_right_1();
                    break;
                case 221: //]
                    Scroll_right(1); break;
                case 87: //W
                    Scroll_up(); break;
                case 83: //S
                    Scroll_down(); break;
                case 33: //page up
                    Scroll_left(viewx); break;
                case 34: //page dn
                    Scroll_right(viewx); break;
                case 36: //home
                    Scroll_left(5000); break;
                case 35: //end
                    Scroll_right(5000); break;
                default:
                    break;
            }

        }
        private void Scroll_up()
        {
            if (data != null)
            {
                double viewMin = chart1.ChartAreas[0].AxisY.ScaleView.ViewMinimum + gridy;
                chart1.ChartAreas[0].AxisY.Minimum = viewMin;
                chart1.ChartAreas[0].AxisY.Maximum = viewMin + gridy * 4;
                chart1.ChartAreas[0].AxisY.ScaleView.Zoom(viewMin, viewMin + gridy * 4);
            }
        }
        private void Scroll_down()
        {
            if (data != null)
            {
                double viewMin = chart1.ChartAreas[0].AxisY.ScaleView.ViewMinimum - gridy;
                chart1.ChartAreas[0].AxisY.Minimum = viewMin;
                chart1.ChartAreas[0].AxisY.Maximum = viewMin + gridy * 4;
                chart1.ChartAreas[0].AxisY.ScaleView.Zoom(viewMin, viewMin + gridy * 4);
            }
        }
        private void Scroll_right_1()//***********WASD
        {
            if (data != null)
            {
                int viewMax = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum) + 1;
                chart1.ChartAreas[0].AxisX.Minimum = int_firstfix - viewx;
                chart1.ChartAreas[0].AxisX.Maximum = int_lastfix + viewx + 0.01;
                chart1.ChartAreas[0].RecalculateAxesScale();
                chart1.ChartAreas[0].AxisX.ScaleView.Size = viewx;
                if (viewMax - viewx <= int_lastfix)
                    chart1.ChartAreas[0].AxisX.ScaleView.Zoom(viewMax - viewx, viewMax);
            }
        }
        private void Scroll_left_1()//***********WASD
        {
            if (data != null)
            {
                int viewMin = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum) - 1;
                chart1.ChartAreas[0].AxisX.Minimum = int_firstfix - viewx;
                chart1.ChartAreas[0].AxisX.Maximum = int_lastfix + viewx + 0.01;
                chart1.ChartAreas[0].RecalculateAxesScale();
                chart1.ChartAreas[0].AxisX.ScaleView.Size = viewx;
                if (viewMin + viewx >= int_firstfix)
                    chart1.ChartAreas[0].AxisX.ScaleView.Zoom(viewMin, viewMin + viewx);
            }
        }
        private void Scroll_right(int sx)
        {
            if (data != null)
            {
                ResetAxesScale();
                double viewMax = chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum + sx;
                if (viewMax > data.Last().fix) viewMax = data.Last().fix;
                chart1.ChartAreas[0].AxisX.ScaleView.Zoom(viewMax - viewx, viewMax);
                Chart1_updateYaxis();
            }
        }
        private void Scroll_left(int sx)
        {
            if (data != null)
            {
                ResetAxesScale();
                double viewMin = chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum - sx;
                if (viewMin < data[0].fix) viewMin = data[0].fix;
                chart1.ChartAreas[0].AxisX.ScaleView.Zoom(viewMin, viewMin + viewx);
                Chart1_updateYaxis();
            }
        }
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            List<string> filelist = new List<string>();
            foreach (string file in files)
                if (file.ToUpper().EndsWith(".RAW")) filelist.Add(file);
            if (filelist.Count == 1)
                Open_One_Raw_file(filelist[0]);
            else if (filelist.Count > 1)
                Open_Multiple_Raw_files(filelist);
        }
        private void Open_One_Raw_file(string sRawfile)
        {
            List<Fm> idata = Raw_Open(sRawfile);
            if (idata != null)
            {
                data = idata;
                filename = System.IO.Path.GetFileNameWithoutExtension(sRawfile);
                this.Text = form_title + " - " + System.IO.Path.GetFileName(sRawfile);
                chart1.Titles[0].Text = filename;
                chart1.Series[0].Points.Clear();
                foreach (Fm ifm in data)
                    chart1.Series[0].Points.AddXY(ifm.fix, ifm.mag);

                int_firstfix = Convert.ToInt32(data[0].fix);
                int_lastfix = Convert.ToInt32(data.Last().fix);
                ResetAxesScale();
                chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                chart1.ChartAreas[0].AxisX.ScaleView.Size = viewx;
                Chart1_updateYaxis();

                MovingFilters();
            }
        }
        private void Open_Multiple_Raw_files(List<string> filelist)
        {
            if (MessageBox.Show("Multiple files detected. Export them to Gif images with current setting ?",
                "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                foreach (string file in filelist)
                {
                    Open_One_Raw_file(file);
                    Generate_Screen_Dumps();
                }
                data.Clear(); data = null;
                chart1.Series[0].Points.Clear();
                filename = "";
                this.Text = form_title;
                chart1.Titles[0].Text = "";
                MessageBox.Show($"Check the files in :\n{rootpath}", "Exported", MessageBoxButtons.OK);
            }
            else
            {
                MessageBox.Show($"{System.IO.Path.GetFileName(filelist[0])} will be loaded.",
                    "Confirm", MessageBoxButtons.OK);
                Open_One_Raw_file(filelist[0]);
            }
        }
        private void HScrollBar1_ValueChanged_1(object sender, EventArgs e)
        {
            comboBox2.Items[0] = this.hScrollBar1.Value;
            comboBox2.SelectedIndex = 0;
        }
        private void Generate_Screen_Dumps()
        {
            if (data != null)
            {
                if (!System.IO.Directory.Exists(rootpath))
                    System.IO.Directory.CreateDirectory(rootpath);
                Scroll_left(9999);//home
                chart1.ChartAreas[0].AxisX.ScrollBar.Enabled = false;
                double viewMax = 0, viewMin;
                while (viewMax < data.Last().fix)
                {
                    viewMin = chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
                    chart1.SaveImage($"{rootpath}{filename}_F{viewMin:0}-F{viewMin + viewx:0}.gif", ChartImageFormat.Gif);
                    Scroll_right(viewx);
                    viewMax = chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum;
                }
                viewMin = chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
                chart1.SaveImage($"{rootpath}{filename}_F{viewMin:0}-F{viewMin + viewx:0}.gif", ChartImageFormat.Gif);
                Scroll_left(9999);//home
                chart1.ChartAreas[0].AxisX.ScrollBar.Enabled = true;
            }
        }
        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                groupBox1.Enabled = true;
                if (data != null) MovingFilters();
            }
            else
            {
                groupBox1.Enabled = false;
                if (data != null)
                {
                    chart1.Series[0].Points.Clear();
                    foreach (Fm ifm in data)
                        chart1.Series[0].Points.AddXY(ifm.fix, ifm.mag);
                }
            }
        }
        private void Button4_Click(object sender, EventArgs e)
        {
            MovingFilters();
        }
        private void ResetAxesScale()
        {
            chart1.ChartAreas[0].AxisX.Minimum = int_firstfix;
            chart1.ChartAreas[0].AxisX.Maximum = int_lastfix;
            chart1.ChartAreas[0].RecalculateAxesScale();
            chart1.ChartAreas[0].AxisX.ScaleView.Size = viewx;
        }
        private void MovingFilters()
        {
            if (checkBox1.Checked && data != null)
            {
                int mawidth = (int)numericUpDown1.Value;
                if (mawidth % 2 == 0)
                {
                    mawidth++; //odd
                    numericUpDown1.Value = mawidth;
                }

                int mmwidth = (int)numericUpDown2.Value;
                if (mmwidth % 2 == 0)
                {
                    mmwidth++; //odd
                    numericUpDown2.Value = mmwidth;
                }

                //Rolling Median
                List<Fm> idata = new List<Fm>();
                foreach (Fm item in data)
                {
                    idata.Add(new Fm
                    {
                        fix = item.fix,
                        mag = item.mag
                    });
                }
                int halfIndex = mmwidth / 2;
                int iend = data.Count - halfIndex - 1;
                for (int i = halfIndex; i < iend + 1; i++)
                {
                    List<double> ldbl = new List<double>();
                    for (int j = i - halfIndex; j < i + halfIndex + 1; j++)
                        ldbl.Add(data[j].mag);
                    var sortedNumbers = ldbl.OrderBy(n => n);
                    idata[i].mag = sortedNumbers.ElementAt(halfIndex);
                }

                //Rolling Average
                List<Fm> jdata = new List<Fm>();
                foreach (Fm item in idata)
                {
                    jdata.Add(new Fm
                    {
                        fix = item.fix,
                        mag = item.mag
                    });
                }
                halfIndex = mawidth / 2;
                iend = data.Count - halfIndex - 1;
                for (int i = halfIndex; i < iend + 1; i++)
                {
                    List<double> ldbl = new List<double>();
                    for (int j = i - halfIndex; j < i + halfIndex + 1; j++)
                        ldbl.Add(idata[j].mag);
                    jdata[i].mag = ldbl.Average();
                }

                chart1.Series[0].Points.Clear();
                foreach (Fm ifm in jdata)
                    chart1.Series[0].Points.AddXY(ifm.fix, ifm.mag);
            }
        }
    }
}
