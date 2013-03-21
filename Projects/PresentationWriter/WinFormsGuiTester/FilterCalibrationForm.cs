﻿using AForge.Imaging;
using AForge.Imaging.Filters;
using HSR.PresentationWriter.DataSources;
using HSR.PresentationWriter.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsGuiTester
{
    public partial class FilterCalibrationForm : Form
    {
        public FilterCalibrationForm()
        {
            InitializeComponent();
        }

        private void FilterCalibrationForm_Load(object sender, EventArgs e)
        {
            updateListBox(@"c:\temp\filterimages");
        }

        private class CustomFilterStrategy : FilterStrategy
        {

        }
        private async void inputListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            double grayR = double.Parse(this.grayRTextBox.Text);
            double grayG = double.Parse(this.grayGTextBox.Text);
            double grayB = double.Parse(this.grayBTextBox.Text);
            int threshold = int.Parse(this.thresholdTextBox.Text);
            int minBlobWidth = int.Parse(this.minBlobWidthTextBox.Text);
            int minBlobHeight = int.Parse(this.minBlobHeightTextBox.Text);

            // TODO ev. IOC Pattern für Strategies
            CustomFilterStrategy strategy = new CustomFilterStrategy()
            {
                DifferenceFilter = new Difference(),
                GrayFilter = new Grayscale(grayR, grayG, grayB),
                ThresholdFilter = new Threshold(threshold),
                BlobCounter = new BlobCounter()
            };
            strategy.BlobCounter.MinWidth = minBlobWidth;
            strategy.BlobCounter.MinHeight = minBlobHeight;

            AForgePenTracker t = new AForgePenTracker(strategy);
            t.DebugPicture += t_DebugPicture;

            int i = this.inputListBox.SelectedIndex;
            if (i >= this.inputListBox.Items.Count - 1) // if index is  going to overflow
            {
                return;
            }
            VideoFrame v1 = new VideoFrame(1, new Bitmap((string)this.inputListBox.Items[i]));
            PointFrame p1 = await t.ProcessAsync(v1);
            VideoFrame v2 = new VideoFrame(1, new Bitmap((string)this.inputListBox.Items[++i]));
            PointFrame p2 = await t.ProcessAsync(v2);

            if (p2 == null)
            {
                this.foundPointXLabel.Text = "?";
                this.foundPointYLabel.Text = "?";
            }
            else
            {
                this.foundPointXLabel.Text = p2.Point.X.ToString();
                this.foundPointYLabel.Text = p2.Point.Y.ToString();
            }
        }

        private List<Bitmap> debugPictures;
        private void t_DebugPicture(object sender, DebugPictureEventArgs e)
        {
            debugPictures = e.Pictures;
            int index = 0;
            this.filterPictureBox.Click += delegate(object o, EventArgs clickE) {
                this.filterPictureBox.BeginInvoke(new MethodInvoker(delegate()
                {
                    if (index >= debugPictures.Count)
                    {
                        index = 0;
                    }
                    this.filterPictureBox.Image = debugPictures[index];
                    index++;
                }));
            };
            this.filterPictureBox.Image = debugPictures[index];
            index++;
        }

        private void folderButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog d = new FolderBrowserDialog();
            if (d.ShowDialog() == DialogResult.OK)
            {
                this.pathLabel.Text = d.SelectedPath;
                updateListBox(d.SelectedPath);
            }
        }

        private void updateListBox(string path)
        {
            this.inputListBox.Items.Clear();
            DirectoryInfo d = new DirectoryInfo(path);
            foreach (FileInfo f in d.GetFiles("*.jpg"))
            {
                this.inputListBox.Items.Add(f.FullName);
            }
        }
    }
}