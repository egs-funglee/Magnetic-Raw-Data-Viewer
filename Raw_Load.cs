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
            openFileDialog.Filter = "All Files (*.*)|*.*|RAW Files (*.RAW)|*.RAW";
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
            int fid = -1; int mid = -1; int navstrlen = -1;
            int firsti = -1, headi = -1, lasti = -1; double step = 0;
            char[] chars = new[] { ' ', '$', ':', ',' };
            //string[] outf = new string[data.Count];

            string lastfix = "-1";

            //search mag index on 1st MAG line, when a number is over 9999
            foreach (string line in sRaw) 
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

            //search nav index on 1st NAV line
            foreach (string line in sRaw) 
            {
                if (line.StartsWith("NAV"))
                {
                    string[] s = line.Split(',');

                    //CVIEW_NAVSTR (Contains space)
                    if (s[1].ToUpper().Contains("CVIEW_NAVSTR") && s[2].Trim().Length > 0)
                    {
                        switch (s[2].Trim())
                        {
                            case "1":
                                fid = 12;
                                if (s.Length == 13) navstrlen = 13;
                                else navstrlen = fid;
                                break;

                            case "2": //ver 2
                            case "3": //ver 3
                                fid = 12;
                                if (s.Length == 15) navstrlen = 15;
                                else navstrlen = fid;
                                break;

                            case "11":
                                fid = 12;
                                if (s.Length == 15) navstrlen = 15;
                                else navstrlen = fid;
                                break;

                            case "12": //ver 12
                            case "13": //ver 13
                                fid = 12;
                                if (s.Length == 17) navstrlen = 17;
                                else navstrlen = fid;
                                break;

                            default:
                                break;
                        }
                    }

                    //CView Nav Fix String "F FixNo GPSX GPSY HHMMSS.0 GYRO"
                    if (s.Length == 2)
                    {
                        s = line.Split(chars, StringSplitOptions.RemoveEmptyEntries);
                        if (s.Length == 7)
                        {
                            if (s[1] == "F")
                            {
                                navstrlen = 7;
                                fid = 2;
                            }
                        }
                    }

                    //break when got the #
                    if (fid > 0 && navstrlen > 0) break; 
                }
            }

            //read the file to list of Fm
            foreach (string line in sRaw)
            {
                if (line.StartsWith("NAV"))
                {
                    string[] s = line.Split(',');

                    if (s.Length == 2)
                        s = line.Split(chars, StringSplitOptions.RemoveEmptyEntries);

                    if (s.Length >= navstrlen && s[fid].Length > 0 && index > 0 && lastfix != s[fid])
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

            //on error return null
            if (data.Count == 0)
            {
                MessageBox.Show($"Cannot read {sRawfile}", "Error", MessageBoxButtons.OK);
                return null;
            }

            //interpolate fix number for chart
            for (int i = 0; i < data.Count; i++)
            {
                //debug
                //outf[i] = $"{i}\t{data[i].fix:F3}\t{data[i].mag:F3}";

                if (data[i].fix > 0)
                {
                    //init index#: first/last = scope ubound/lbound,
                    //headi = first index with valid fix# in the file, for backward interpolate in next stage
                    if (firsti == -1) { firsti = i; headi = i; }
                    else lasti = i; //if got first index and fix>0 update last index

                    //if got both first and last index, interpolate the fix in between
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

            //fix tail, fill dummy fix with last step size
            if (data.Count > firsti)
            {
                int k = 1;
                for (int j = firsti + 1; j < data.Count; j++)
                {
                    data[j].fix = k * step + data[firsti].fix;
                    k++;
                }
            }

            //fix head, fill dummy fix with last step size
            if (headi > 0)
            {
                int k = 1;
                for (int j = headi - 1; j >= 0; j--)
                {
                    data[j].fix = data[headi].fix - k * step;
                    k++;
                }
            }

            //add dummy at the end if last fix is not integer
            if (data[data.Count - 1].fix % 1 != 0)
            {
                Fm dummy = new Fm
                {
                    fix = (int)data[data.Count - 1].fix + 1,
                    mag = data[data.Count - 1].mag
                };
                data.Add(dummy);
            }

            //insert dummy at the beginning when first fix is not integer
            if (data[0].fix % 1 != 0)
            {
                Fm dummy = new Fm
                {
                    fix = (int)data[0].fix,
                    mag = data[0].mag
                };
                data.Insert(0,dummy);
            }

            /*            
            data.RemoveAll(item => item.fix == 0);
            System.IO.File.WriteAllLines(@"C:\EGS\testoutb.txt", outf);
            */

            /*
            string[] outf = new string[data.Count];
            for (int i = 0; i < data.Count; i++)
                outf[i] = data[i].ToString();            
            System.IO.File.WriteAllLines(@"C:\EGS\testout.txt", outf);
            */

            return data;
        }
        internal class Fm : IComparable<Fm>
        {
            public double fix; //interpolated after reading
            public double mag; //same as raw mag data density
            public int CompareTo(Fm other)//sort with mag
            {
                if (this.mag > other.mag) return 1;
                else if (this.mag < other.mag) return -1;
                else return 0;
            }
            public override string ToString()
            {
                return $"{this.fix:F3}\t{this.mag:F3}";
            }            
        }
    }
}
