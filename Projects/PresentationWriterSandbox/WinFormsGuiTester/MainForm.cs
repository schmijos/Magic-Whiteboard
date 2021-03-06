﻿using System;
using System.Windows.Forms;

namespace WinFormsGuiTester
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void liveButton_Click(object sender, EventArgs e)
        {
            (new LivePenTrackingForm()).ShowDialog();
        }

        private void filtersButton_Click(object sender, EventArgs e)
        {
            (new FilterCalibrationForm()).ShowDialog();
        }

        private void libraryTrackingButton_Click(object sender, EventArgs e)
        {
            (new LibraryTrackingForm()).ShowDialog();
        }

        private void liveTestButton_Click(object sender, EventArgs e)
        {
            (new LiveTestForm()).ShowDialog();
        }

        private void showGridButton_Click(object sender, EventArgs e)
        {
            (new MeasureGrid()).ShowDialog();
        }
    }
}
