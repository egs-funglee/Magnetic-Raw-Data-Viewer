using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Magnetic_Raw_Data_Viewer
{
    class Raw
    {
        internal static List<string> OpenRawFiles_Dialog()
        {
            List<string> fileslist = new List<string>();
            System.IO.Stream myStream = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            System.Collections.IEnumerable datfile;

            openFileDialog.Title = "Open RAW Files";
            openFileDialog.Filter = "All Files (*.*)|*.*|RAW Files (*.RF*)|*.RAW*";
            openFileDialog.Multiselect = true;
            openFileDialog.FilterIndex = 2;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    myStream = openFileDialog.OpenFile();
                    datfile = openFileDialog.FileNames;
                    if ((myStream != null))
                    {
                        foreach (string filename in datfile)
                        {
                            if (System.IO.Path.GetExtension(filename).ToUpper().StartsWith(".RAW"))
                                fileslist.Add(filename);
                        }
                    }
                }
                catch (Exception Ex)
                {
                    MessageBox.Show("Cannot read file from disk. Original error: " + Ex.Message);
                }
                finally
                {
                    if ((myStream != null)) myStream.Close();
                }
            }
            return fileslist;
        }
        internal static List<Fm> Raw_Open(string sRawfile)
        {
            string[] sRaw = System.IO.File.ReadAllLines(sRawfile);
            List<Fm> data = new List<Fm>();
            int index = 0;
            const int fid = 12; int mid = -1;
            int firsti = -1, headi = -1, lasti = -1; double step = 0;
            char[] chars = new[] { ' ', '$', ':', ',' };
            //string[] outf = new string[data.Count];

            string lastfix = "-1";

            foreach (string line in sRaw) //search mag index on 1st MAG line
            {
                if (line.StartsWith("MAG"))
                {
                    string[] s = line.Split(chars, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 1; j < s.Length; j++)
                    {
                        if (Double.TryParse(s[j], out double number))
                            if (number > 9999)
                            {
                                mid = j; break;
                            }
                    }
                    break;
                }
            }

            foreach (string line in sRaw)
            {
                if (line.StartsWith("NAV"))
                {
                    string[] s = line.Split(',');
                    if (s.Length == 15 && s[fid].Length > 0 && index > 0 && lastfix != s[fid])
                    {
                        data[index - 1].fix = double.Parse(s[fid]);
                        lastfix = s[fid];
                    }
                }
                if (line.StartsWith("MAG"))
                {
                    string[] s = line.Split(chars, StringSplitOptions.RemoveEmptyEntries);
                    Fm ifm = new Fm
                    {
                        fix = 0,
                        mag = double.Parse(s[mid])
                    };
                    data.Add(ifm);
                    index++;
                }
            }

            for (int i = 0; i < data.Count; i++)
            {
                //outf[i] = $"{i}\t{data[i].fix:F3}\t{data[i].mag:F3}";

                if (data[i].fix > 0)
                {
                    if (firsti == -1) { firsti = i; headi = i; }
                    else lasti = i;

                    if (firsti >= 0 && lasti >= 0)
                    {
                        step = (data[lasti].fix - data[firsti].fix) / (lasti - firsti);
                        int k = 1;
                        for (int j = firsti + 1; j < lasti; j++)
                        {
                            data[j].fix = k * step + data[firsti].fix;
                            k++;
                        }
                        firsti = lasti;
                        lasti = -1;
                    }
                }
            }
            if (data.Count > firsti)//fix tail, fill dummy fix with last step size
            {
                int k = 1;
                for (int j = firsti + 1; j < data.Count; j++)
                {
                    data[j].fix = k * step + data[firsti].fix;
                    k++;
                }
            }
            if (headi > 0)//fix head, fill dummy fix with last step size
            {
                int k = 1;
                for (int j = headi - 1; j >= 0; j--)
                {
                    data[j].fix = data[headi].fix - k * step;
                    k++;
                }
            }

            if (data.Count == 0)
            {
                MessageBox.Show($"Cannot read {sRawfile}", "Error", MessageBoxButtons.OK);
                return null;
            }

            return data;

            /*data.RemoveAll(item => item.fix == 0);            
            System.IO.File.WriteAllLines(@"C:\EGS\testoutb.txt", outf);
            outf = new string[data.Count];
            for (i = 0; i < data.Count; i++)
            {
                outf[i] = $"{i}\t{data[i].fix:F3}\t{data[i].mag:F3}";
            }
            System.IO.File.WriteAllLines(@"C:\EGS\testout.txt", outf);
            */
        }
        internal class Fm : IComparable<Fm>
        {
            public double fix;
            public double mag;

            public int CompareTo(Fm other)//sort with mag
            {
                if (this.mag > other.mag) return 1;
                else if (this.mag < other.mag) return -1;
                else return 0;
            }
            public override string ToString()
            {
                return $"{this.fix}\t{this.mag}";
            }
        }
    }
}
