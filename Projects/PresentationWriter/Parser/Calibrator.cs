﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using HSR.PresentationWriter.Parser.Events;
using HSR.PresentationWriter.Parser.Images;
using DColor = System.Drawing.Color;
using WFVisuslizer;

namespace HSR.PresentationWriter.Parser
{
    internal class Calibrator
    {
        private CameraConnector _cc;
        private const byte GreyDiff = 20;
        private const int Blocksize = 10;
        private const int Blockfill = 80; // Number of pixels needed for a Block to be valid. Depends on Blocksize.
        private const int CalibrationFrames = 300; // Number of Frames used for calibration. Divide by 10 to get Time for calibration.
        private ThreeChannelBitmap _blackImage;
        private VisualizerControl _vs = new VisualizerControl();
        private int _calibrationStep;
        private int _errors = 0;
        private Rect[] rects = new Rect[3];

        public Calibrator(CameraConnector cc)
        {
            _cc = cc;
            this.Grid = new Grid(0,0);
            //var thread = new Thread(() => _vs = new CalibratorWindow());
            //thread.SetApartmentState(ApartmentState.STA);
            //thread.Start();
            //thread.Join();
            Calibrate();
            CalibrateColors();
        }

        public void CalibrateColors()
        {
        }

        public void Calibrate()
        {
            _calibrationStep = 0;
            _errors = 0;
            _cc.NewImage += BaseCalibration;
        }

        private void BaseCalibration(object sender, Events.NewImageEventArgs e)
        {
            //var thread = new Thread(() => CalibThread(e));
            //thread.SetApartmentState(ApartmentState.STA);
            //thread.Start();
            //thread.Join();
            //_vs.Dispatcher.Invoke(() => CalibThread(e));
            //_vs.Dispatcher.InvokeAsync(() => 
            //Debug.WriteLine("Calibrating"),DispatcherPriority.Send);
            //Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
            //           new Action(() => { }));
            //_vs.Dispatcher.InvokeAsync(() => CalibThread(e));
            CalibThread(e);
        }


        private void CalibThread(NewImageEventArgs e)
        {
            Debug.WriteLine("Calibrating");
            if (_errors > 100)
            {
                //calibration not possible
                return;
            }
            if (_calibrationStep == CalibrationFrames || !Grid.DataNeeded)
            {
                _cc.NewImage -= BaseCalibration;
                Grid.Calculate();
                _vs.Close();
            }
                //else if (_calibrationStep > CalibrationFrames/2)
                //{
                
                //}
            else if (_calibrationStep > 2)
            {
                _vs.ClearRects();
                FillRandomRects();
                for (int j = 0; j < 3; j++)
                {
                    OneChannelBitmap diff = new OneChannelBitmap();
                    switch (j)
                    {
                        case 0:
                            diff = _blackImage.RChannelBitmap - e.NewImage.RChannelBitmap;
                            break;
                        case 1:
                            diff = _blackImage.GChannelBitmap - e.NewImage.GChannelBitmap;
                            break;
                        case 2:
                            diff = _blackImage.GChannelBitmap - e.NewImage.GChannelBitmap;
                            break;
                    }
                    var topLeftCorner = GetTopLeftCorner(diff);
                    var topRightCorner = GetTopRightCorner(diff);
                    var bottomLeftCorner = GetBottomLeftCorner(diff);
                    var bottomRightCorner = GetBottomRightCorner(diff);
                    if (topLeftCorner.X < topRightCorner.X && topLeftCorner.Y > bottomLeftCorner.Y
                        && topRightCorner.Y < bottomRightCorner.Y && topRightCorner.Y < bottomLeftCorner.Y
                        && topLeftCorner.Y < bottomRightCorner.Y && bottomLeftCorner.X < bottomRightCorner.X
                        && topLeftCorner.X < bottomRightCorner.X && bottomLeftCorner.X < topRightCorner.X
                        && IsValid(topLeftCorner) && IsValid(topRightCorner) && IsValid(bottomLeftCorner) &&
                        IsValid(bottomRightCorner))
                    {
                        Grid.AddPoint(rects[j].TopLeft, topLeftCorner);
                        Grid.AddPoint(rects[j].TopRight, topRightCorner);
                        Grid.AddPoint(rects[j].BottomLeft, bottomLeftCorner);
                        Grid.AddPoint(rects[j].BottomRight, bottomRightCorner);
                    }
                    else
                    {
                        _errors++;
                    }
                }
            }
            else
                switch (_calibrationStep)
                {
                    case 2:
                        var diff = (_blackImage - e.NewImage).GetGrayscale();
                        Grid.TopLeft = GetTopLeftCorner(diff);
                        Grid.TopRight = GetTopRightCorner(diff);
                        Grid.BottomLeft = GetBottomLeftCorner(diff);
                        Grid.BottomRight = GetBottomRightCorner(diff);
                        if (Grid.TopLeft.X < Grid.TopRight.X && Grid.TopLeft.X < Grid.BottomRight.X &&
                            Grid.BottomLeft.X < Grid.TopRight.X && Grid.BottomLeft.X < Grid.BottomRight.X &&
                            Grid.TopLeft.Y < Grid.BottomLeft.Y && Grid.TopLeft.Y < Grid.BottomRight.Y &&
                            Grid.TopRight.Y < Grid.BottomLeft.Y && Grid.TopRight.Y < Grid.BottomRight.Y &&
                            IsValid(Grid.TopLeft) && IsValid(Grid.TopRight) && IsValid(Grid.BottomLeft) &&
                            IsValid(Grid.BottomRight))
                        {
                            _calibrationStep++;
                            _vs.ClearRects();
                            FillRandomRects();
                        }
                        else
                        {
                            _calibrationStep = 0;
                            _errors ++;
                        }
                        break;
                    case 1:
                        _blackImage = e.NewImage;
                        _vs.AddRect(0, 0, (int) _vs.Width, (int) _vs.Height, DColor.FromArgb(255,255, 255, 255));
                        _calibrationStep++;
                        break;
                    case 0:
                        Grid = new Grid(e.NewImage.Width, e.NewImage.Height);
                        var thread = new Thread(() =>
                            {
                                _vs.Show();
                                _vs.AddRect(0, 0, (int) _vs.Width, (int) _vs.Height, DColor.FromArgb(255, 0, 0, 0));
                            });
                        thread.SetApartmentState(ApartmentState.STA);
                        thread.Start();
                        thread.Join();
                        _calibrationStep++;
                        break;
                }
            return;
        }

        private bool IsValid(Point point)
        {
            return point.X > 0 && point.Y > 0;
        }

        private void FillRandomRects()
        {
            var r = new Random();
            var tl = new Point(r.Next((int)_vs.Width), r.Next((int)_vs.Height));
            rects[0] = new Rect(tl, new Point(r.Next((int)tl.X, (int)_vs.Width), r.Next((int)tl.Y, (int)_vs.Height)));
            tl = new Point(r.Next((int)_vs.Width), r.Next((int)_vs.Height));
            rects[1] = new Rect(tl, new Point(r.Next((int)tl.X, (int)_vs.Width), r.Next((int)tl.Y, (int)_vs.Height)));
            tl = new Point(r.Next((int)_vs.Width), r.Next((int)_vs.Height));
            rects[2] = new Rect(tl, new Point(r.Next((int)tl.X, (int)_vs.Width), r.Next((int)tl.Y, (int)_vs.Height)));
            _vs.AddRect(rects[0].TopLeft, rects[0].BottomRight, DColor.FromArgb(255, 255, 0, 0));
            _vs.AddRect(rects[1].TopLeft, rects[1].BottomRight, DColor.FromArgb(255, 0, 255, 0));
            _vs.AddRect(rects[2].TopLeft, rects[2].BottomRight, DColor.FromArgb(255, 0, 0, 255));
            CheckIntersections();
        }

        private void CheckIntersections()
        {
            if (rects[0].IntersectsWith(rects[1]))
            {
                var rect = rects[1];
                rect.Intersect(rects[0]);
                _vs.AddRect(rect, DColor.FromArgb(255, 255, 255, 0));
            }
            if (rects[0].IntersectsWith(rects[2]))
            {
                var rect = rects[2];
                rect.Intersect(rects[0]);
                _vs.AddRect(rect, DColor.FromArgb(255, 255, 0, 255));
            }
            if (rects[1].IntersectsWith(rects[2]))
            {
                var rect = rects[2];
                rect.Intersect(rects[1]);
                _vs.AddRect(rect, DColor.FromArgb(255, 0, 255, 255));
            }
        }

        private Point GetBottomRightCorner(OneChannelBitmap diff)
        {
            for (int i = diff.Width; i >= 0; i--)
            {
                for (int j = 0; j <= i; j++)
                {
                    if (CheckBlock(diff, i + j - Blocksize, diff.Height - j - Blocksize))
                    {
                        return new Point {X = i + j, Y = diff.Height - j};
                    }
                }
            }
            return new Point();
        }

        private Point GetBottomLeftCorner(OneChannelBitmap diff)
        {
            for (int i = 0; i < diff.Width; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    if (CheckBlock(diff, i - j, diff.Height - j - Blocksize))
                    {
                        return new Point {X = i - j, Y = diff.Height - j};
                    }
                }
            }
            return new Point();
        }

        private Point GetTopRightCorner(OneChannelBitmap diff)
        {
            for (int i = diff.Width - 1; i >= 0; i--)
            {
                for (int j = 0; j < diff.Width - i; j++)
                {
                    if (CheckBlock(diff, i + j - Blocksize, j))
                    {
                        return new Point {X = i + j, Y = j};
                    }
                }
            }
            return new Point();
        }

        private Point GetTopLeftCorner(OneChannelBitmap diff)
        {
            for (int i = 0; i < diff.Width; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    if (CheckBlock(diff, i - j, j))
                    {
                        return new Point {X = i - j, Y = j};
                    }
                }
            }
            return new Point();
        }

        private bool CheckBlock(OneChannelBitmap diff, int p, int j)
        {
            int sum = 0;
            if (p < 0 || j < 0)
            {
                return false;
            }
            for (int i = p; i <= p+Blocksize && i < diff.Width; i++)
            {
                for (int k = j; k <= j+Blocksize && k < diff.Height; k++)
                {
                    if (diff.Channel[i,k]>=GreyDiff)
                    {
                        if (++sum>=Blockfill)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public int CheckCalibration()
        {
            // TODO dummy
            return 0;
        }

        public Grid Grid { get; private set; }

        public Colorfilter ColorFilter { get; private set; }
    }
}
