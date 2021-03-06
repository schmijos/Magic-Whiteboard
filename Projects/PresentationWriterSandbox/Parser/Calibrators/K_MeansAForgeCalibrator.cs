﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using HSR.PresWriter.IO.Events;
using HSR.PresWriter.IO;
using Visualizer;
using Point = System.Drawing.Point;

namespace HSR.PresWriter.PenTracking
{

    class KMeansAForgeCalibrator : ICalibrator
    {
        private IPictureProvider _cc;
        private int _calibrationStep;
        private int _errors;
        private IVisualizerControl _vs;
        private const int CalibrationFrames = 12; //must be n^2+3
        private Difference diffFilter = new Difference();
        private const int Rowcount = 20;
        private const int Columncount = 15;
        private SemaphoreSlim _sem;
        private Task _t;
        private double _sqrheight;
        private double _sqrwidth;
        private bool _drawing;
        private Bitmap actImg;


        public KMeansAForgeCalibrator(IPictureProvider provider, IVisualizerControl visualizer)
        {
            _cc = provider;
            _vs = visualizer;
            //var thread = new Thread(() => _vs = new CalibratorWindow());
            //thread.SetApartmentState(ApartmentState.STA);
            //thread.Start();
            //thread.Join();
        }

        // TODO Calibrate Funktionen aufräumen
        public void StartCalibration()
        {
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
            _sqrwidth = ((double)_vs.Width) / Rowcount;
            _sqrheight = ((double)_vs.Height) / Columncount;
            _sem = new SemaphoreSlim(1, 1);
            _cc.FrameReady += BaseCalibration; // TODO
        }

        public int VisibleColumns { get { return Columncount - ((Math.Floor((_calibrationStep - 3) / Math.Sqrt(CalibrationFrames - 3)) == 0.0) ? 0 : 1); } }

        public int VisibleRows { get { return Rowcount - ((((_calibrationStep - 3)%(int) Math.Sqrt(CalibrationFrames - 3)) == 0) ? 0 : 1); } }

        public int CheckCalibration()
        {
            return 0;
        }

        private void BaseCalibration(object sender, FrameReadyEventArgs e)
        {
            if (_t != null && _t.Exception != null)
            {
                throw _t.Exception;
            }
            if (_sem.CurrentCount >= 1)//_t.Status != TaskStatus.Running)
            {
                _sem.Wait();
                Task.Factory.StartNew(() => _t = CalibThread(e));
            }
        }


        private async Task CalibThread(FrameReadyEventArgs e)
        {
            Debug.WriteLine("Calibrating " + _calibrationStep + " " + _drawing);
            e.Frame.Bitmap.Save(@"C:\temp\daforge\src\img" + (_calibrationStep<10?"0":"") + _calibrationStep + "-" + (_drawing?"1":"0") + "-" + _errors + ".jpg", ImageFormat.Jpeg);
            if (_errors > 100)
            {
                //calibration not possible
                return;
            }
            if (_calibrationStep == CalibrationFrames && !_drawing)
            {
                _cc.FrameReady -= BaseCalibration; // TODO
                //Grid.Calculate();
                _vs.Close();
                CalibrationCompleted(this, new EventArgs());
            }
            else
            {
                if (_calibrationStep > 2)
                {
                    if (_drawing)
                    {
                        // draw diffimage
                        _drawing = false;
                        _vs.Clear();
                        FillRects();
                        diffFilter.OverlayImage = e.Frame.Bitmap;
                    }
                    else
                    {
                        // analyse diffimage
                        _calibrationStep++;
                        _drawing = true;
                        _vs.Clear();
                        FillRects();
                        _calibrationStep--;
                        var gbm = diffFilter.Apply(e.Frame.Bitmap);
                        gbm.Save(@"C:\temp\daforge\diff\img" + _calibrationStep + ".jpg", ImageFormat.Jpeg);
#if DEBUG
                        actImg = (Bitmap) gbm.Clone();
#endif
                        var d = UnmanagedImage.FromManagedImage(gbm);
                        var stats = new ImageStatistics(d);
                        var gcf = new ColorFiltering(new IntRange(0, 255),
                                                     new IntRange((int)(stats.GreenWithoutBlack.Mean + stats.GreenWithoutBlack.StdDev + 5), 255),
                                                     new IntRange(0, 255));
                        var bcf = new ColorFiltering(new IntRange(0, 255),
                                                     new IntRange(0, 255),
                                                     new IntRange((int)(stats.BlueWithoutBlack.Mean + stats.BlueWithoutBlack.StdDev), 255));
                        //Debug.WriteLine("Green: " + stats.GreenWithoutBlack.Median + " Blue: " + stats.BlueWithoutBlack.Median);
                        //Debug.WriteLine("Green: " + stats.GreenWithoutBlack.Mean + " Blue: " + stats.BlueWithoutBlack.Mean);
                        var bf = new Difference(gcf.Apply(d));
                        bcf.ApplyInPlace(d);
                        bf.ApplyInPlace(d);
                        d.ToManagedImage().Save(@"C:\temp\daforge\diff\img" + _calibrationStep + ".jpg", ImageFormat.Jpeg);
                        stats = new ImageStatistics(d);
                        gcf = new ColorFiltering(new IntRange(0, 255),
                                                     new IntRange((int)stats.GreenWithoutBlack.Mean, 255),
                                                     new IntRange(0, 255));
                        bcf = new ColorFiltering(new IntRange(0, 255),
                                                     new IntRange(0, 255),
                                                     new IntRange((int)stats.BlueWithoutBlack.Mean, 255));
                        // split channels
                        var bbm = bcf.Apply(d);
                        bbm.ToManagedImage().Save(@"C:\temp\daforge\bimg\img" + _calibrationStep + ".jpg", ImageFormat.Bmp);
                        gcf.ApplyInPlace(d);
                        d.ToManagedImage().Save(@"C:\temp\daforge\gimg\img" + _calibrationStep + ".jpg", ImageFormat.Bmp);
                        var gblobCounter = new BlobCounter
                            {
                                ObjectsOrder = ObjectsOrder.YX,
                                MaxHeight = 60,
                                MinHeight = 15,
                                MaxWidth = 60,
                                MinWidth = 15,
                                FilterBlobs = true,
                                CoupledSizeFiltering = false
                            };
                        gblobCounter.ProcessImage(d);
                        var bblobCounter = new BlobCounter
                            {
                                ObjectsOrder = ObjectsOrder.YX,
                                MaxHeight = 60,
                                MinHeight = 15,
                                MaxWidth = 60,
                                MinWidth = 15,
                                FilterBlobs = true,
                                CoupledSizeFiltering = false
                            };
                        bblobCounter.ProcessImage(bbm);
                        ProcessBlobs(gblobCounter, 0);
                        ProcessBlobs(bblobCounter, 1);
#if DEBUG
                        actImg.Save(@"C:\temp\daforge\squares\img" + _calibrationStep + ".jpg", ImageFormat.Jpeg);
#endif
                        _calibrationStep++;
                    }
                }
                else
                    switch (_calibrationStep)
                    {
                        case 2:
                            var bm = UnmanagedImage.FromManagedImage(e.Frame.Bitmap);
                            //diffFilter.OverlayImage.Save(@"C:\temp\daforge\diff\src" + _errors + ".jpg", ImageFormat.Jpeg);
                            bm = diffFilter.Apply(bm);
                            var gf = new GaussianBlur(9.0, 3);
                            gf.ApplyInPlace(bm);
                            var cf = new ColorFiltering(new IntRange(10, 255), new IntRange(20, 255),
                                                        new IntRange(20, 255));
                            cf.ApplyInPlace(bm);
                            var blobCounter = new BlobCounter { ObjectsOrder = ObjectsOrder.Size, BackgroundThreshold = Color.FromArgb(255, 15, 20, 20), FilterBlobs = true };
                            blobCounter.ProcessImage(bm);
                            bm.ToManagedImage().Save(@"C:\temp\daforge\diff\img" + _calibrationStep + "-" + _errors + ".jpg", ImageFormat.Jpeg);
                            var blobs = blobCounter.GetObjectsInformation();
                            if (blobs.Any())
                            {
                                int i = 0;
                                List<IntPoint> corners;
                                do
                                {
                                    corners =
                                        PointsCloud.FindQuadrilateralCorners(blobCounter.GetBlobsEdgePoints(blobs[i++]));
                                } while (corners.Count != 4);
                                RecursiveAForgeCalibrator.GridBlobs.InPlaceSort(corners);
                                Grid.TopLeft = new Point(corners[0].X, corners[0].Y);
                                Grid.TopRight = new Point(corners[1].X, corners[1].Y);
                                Grid.BottomLeft = new Point(corners[2].X, corners[2].Y);
                                Grid.BottomRight = new Point(corners[3].X, corners[3].Y);
                                if (Grid.TopLeft.X > 10 && Grid.TopRight.X < _vs.Width - 5 && Grid.BottomLeft.X > 5 &&
                                    Grid.BottomRight.X < _vs.Width - 5 && Grid.TopLeft.Y > 10 && Grid.TopRight.Y > 5 &&
                                    Grid.BottomLeft.Y < _vs.Height - 5 && Grid.BottomRight.Y < _vs.Height - 5 &&
                                    Grid.TopLeft.X < Grid.BottomRight.X && blobs[i - 1].Area > 60000 &&
                                    Grid.BottomLeft.X < Grid.TopRight.X && Grid.BottomLeft.X < Grid.BottomRight.X &&
                                    Grid.TopLeft.Y < Grid.BottomLeft.Y && Grid.TopLeft.Y < Grid.BottomRight.Y &&
                                    Grid.TopRight.Y < Grid.BottomLeft.Y && Grid.TopRight.Y < Grid.BottomRight.Y)
                                {
                                    _calibrationStep++;
                                    _drawing = true;
                                    _vs.Clear();
                                    FillRects();
                                    Grid.AddPoint(new Point(), new Point(corners[0].X, corners[0].Y));
                                    Grid.AddPoint(new Point(0, _vs.Height), new Point(corners[1].X, corners[1].Y));
                                    Grid.AddPoint(new Point(_vs.Width, 0), new Point(corners[2].X, corners[2].Y));
                                    Grid.AddPoint(new Point(_vs.Width, _vs.Height),
                                                  new Point(corners[3].X, corners[3].Y));
                                }
                                else
                                {
                                    _calibrationStep = 0;
                                    _errors++;
                                }
                            }
                            else
                            {
                                _calibrationStep = 0;
                                _errors++;
                                _vs.Draw();
                            }
                            break;
                        case 1:
                            diffFilter.OverlayImage = e.Frame.Bitmap;
                            //diffFilter.OverlayImage.Save(@"C:\temp\daforge\diff\srcf" + _errors + ".jpg", ImageFormat.Jpeg);
                            //_vs.AddRect(0, 0, (int) _vs.Width, (int) _vs.Height, Color.FromArgb(255, 255, 255, 255));
                            _vs.Clear();
                            for (int y = 0; y < Columncount; y++)
                            {
                                for (int x = 0; x < Rowcount; x++)
                                {
                                    if (!(y % 2 == 0 && x % 2 == 0 || y % 2 == 1 && x % 2 == 1))
                                    {
                                        _vs.AddRect((int)(x * _sqrwidth), (int)(y * _sqrheight),
                                            (int)_sqrwidth, (int)_sqrheight,
                                        Color.FromArgb(255, 255, 255, 255));
                                    }
                                }
                            }
                            _vs.Draw();
                            _calibrationStep++;
                            break;
                        case 0:
                            Grid = new Grid(e.Frame.Bitmap.Width, e.Frame.Bitmap.Height);
                            Grid.ScreenSize = new Rectangle(0, 0, _vs.Width, _vs.Height);
                            var thread = new Thread(() =>
                            {
                                _vs.Show();
                                _vs.AddRect(0, 0, _vs.Width, _vs.Height, Color.FromArgb(255, 0, 0, 0));
                            });
                            thread.SetApartmentState(ApartmentState.STA);
                            thread.Start();
                            thread.Join();
                            for (int y = 0; y < Columncount; y++)
                            {
                                for (int x = 0; x < Rowcount; x++)
                                {
                                    if (y % 2 == 0 && x % 2 == 0 || y % 2 == 1 && x % 2 == 1)
                                    {
                                        _vs.AddRect((int)(x * _sqrwidth), (int)(y * _sqrheight),
                                            (int)_sqrwidth, (int)_sqrheight,
                                        Color.FromArgb(255, 255, 255, 255));
                                    }
                                }
                            }
                            _vs.Draw();
                            _calibrationStep++;
                            break;
                    }
            }
            Debug.WriteLine("Releasing");
            await Task.Delay(500);
            _sem.Release();
        }

        /// <summary>
        /// calculate the blob's position based on the grids position
        /// </summary>
        /// <param name="b"></param>
        /// <param name="offset"></param>
        private void ProcessBlobs(BlobCounter b, int offset)
        {
            var blobs = new Dictionary<Blob, Point>(new BlobComparer());
            double xOff = ((_calibrationStep - 3) % (int)Math.Sqrt(CalibrationFrames - 3)) * _sqrwidth /
                            (int)Math.Sqrt(CalibrationFrames - 3);
            double yOff = Math.Floor((_calibrationStep - 3) / Math.Sqrt(CalibrationFrames - 3)) * _sqrwidth /
                            (int)Math.Sqrt(CalibrationFrames - 3);
            foreach (var blob in b.GetObjectsInformation())
            {
                blobs.Add(blob, new Point(-1, -1));
            }
            var actVect = new Vector(Grid.BottomLeft.X - Grid.TopLeft.X, Grid.BottomLeft.Y - Grid.TopLeft.Y);
            var actStart = new AForge.Point(Grid.TopLeft.X, Grid.TopLeft.Y);
            for (int i = offset; i < VisibleRows; i+=2)
            {
                var nb = blobs.Where(x => x.Value.X == -1 && x.Value.Y == -1)
                                  .Select(x => x.Key)
                                  .OrderBy(x => DistPToVect(x, actVect, actStart))
                                  .ToList();
                if (nb.Count > Columncount)
                    nb = nb.GetRange(0, Columncount);
                var nearby = nb.ToDictionary(x => x, x => DistPToVect(x, actVect, actStart));
                if (nearby.Count == 0)
                    continue;
                var stddev = StdDev(nearby.Values.ToList());
                var mean = nearby.Values.Average();
                var toRemove = new List<Blob>();
                foreach (var blob in nearby)
                {
                    if (blob.Value > mean + stddev)
                        toRemove.Add(blob.Key);
                    else
                    {
                        var t = blobs[blob.Key];
                        t.X = i;
                        blobs[blob.Key] = t;
                    }
                }
                foreach (var blob in toRemove)
                {
                    nearby.Remove(blob);
                }
                var top = GetCrossingPoint(actStart, actVect, Grid.BottomLeft,
                                           new Vector(Grid.BottomRight.X - Grid.BottomLeft.X,
                                                      Grid.BottomRight.Y - Grid.BottomLeft.Y));
                var height = top.Y - GetCrossingPoint(actStart, actVect, Grid.TopLeft,
                                              new Vector(Grid.TopRight.X - Grid.TopLeft.X,
                                                         Grid.TopRight.Y - Grid.TopLeft.Y)).Y;
                var pred = height/Math.Round(VisibleColumns/2.0);
                foreach (var blob in nearby)
                {
                    var t = blobs[blob.Key];
                    t.Y = (int)Math.Floor((blob.Key.CenterOfGravity.Y - top.Y)/pred) + offset;
                    blobs[blob.Key] = t;
                }
                actStart = nearby.First(x => x.Key.CenterOfGravity.Y == nearby.Keys.Min(y => y.CenterOfGravity.Y)).Key.CenterOfGravity;
                var bott = nearby.First(x => x.Key.CenterOfGravity.Y == nearby.Keys.Max(y => y.CenterOfGravity.Y)).Key.CenterOfGravity;
                actVect = new Vector(bott.X - actStart.X, bott.Y - actStart.Y);
            }
            foreach (var blob in blobs)
            {
#if DEBUG
                using (var g = Graphics.FromImage(actImg))
                {
                    g.DrawString(blob.Value.X + "," + blob.Value.Y, new Font(FontFamily.GenericSansSerif, 8.0f),
                                 new SolidBrush(Color.White),
                                 blob.Key.CenterOfGravity.X, blob.Key.CenterOfGravity.Y);
                    g.Flush();
                    //Debug.WriteLine("wrote to image");
                }
#endif
                var corners = PointsCloud.FindQuadrilateralCorners(b.GetBlobsEdgePoints(blob.Key));
                var xPos = blob.Value.X;
                var yPos = blob.Value.Y;
                if (corners.Count == 4)
                {
                    RecursiveAForgeCalibrator.GridBlobs.InPlaceSort(corners);
                    Grid.AddPoint((int) (xPos*_sqrwidth + xOff), (int) (yPos*_sqrheight + yOff), corners[0].X,
                                  corners[0].Y);
                    Grid.AddPoint((int) ((xPos + 1)*_sqrwidth + xOff), (int) (yPos*_sqrheight + yOff),
                                  corners[1].X,
                                  corners[1].Y);
                    Grid.AddPoint((int) (xPos*_sqrwidth + xOff), (int) ((yPos + 1)*_sqrheight + yOff),
                                  corners[2].X,
                                  corners[2].Y);
                    Grid.AddPoint((int) ((xPos + 1)*_sqrwidth + xOff), (int) ((yPos + 1)*_sqrheight + yOff),
                                  corners[3].X,
                                  corners[3].Y);
                }
            }
        }

        private Point GetCrossingPoint(AForge.Point s1, Vector v1, Point s2, Vector v2)
        {
            if ((v2.Y == 0 && v1.Y == 0) || ((v2.Y != 0 && v1.Y != 0 && (v2.X/v2.Y == v1.X/v1.Y))))
                throw new ArgumentException("Doesn't cross");
            var r = (s1.X*v1.Y - s1.Y*v1.X + s2.Y*v1.X - s2.X*v1.Y) / (v2.Y*v1.X - v1.Y*v2.X);
            return new Point((int) r, (int) ((s1.X - s2.X + r*v2.X) / v1.X));
        }

        private double StdDev(List<double> selection)
        {
            var mean = selection.Average(x => x);
            var sum = 0.0;
            foreach (var d in selection)
            {
                var diff = d - mean;
                sum += diff*diff;
            }
            return Math.Sqrt(sum/selection.Count);
        }

        private double DistPToVect(Blob x, Vector vect, AForge.Point start)
        {
            vect.Normalize();
            var diff = start - x.CenterOfGravity;
            var t = new AForge.Point((float) (diff.X*vect.X*vect.X), (float) (diff.Y*vect.Y*vect.Y));
            return (diff - t).EuclideanNorm();
        }

        /// <summary>
        /// Draw in a way, that a difference results in an expected result
        /// </summary>
        private void FillRects()
        {

            double xOff = ((_calibrationStep - 3) % (int)Math.Sqrt(CalibrationFrames - 3)) *
                _sqrwidth / Math.Sqrt(CalibrationFrames - 3);
            double yOff = Math.Floor((_calibrationStep - 3) / Math.Sqrt(CalibrationFrames - 3)) *
                _sqrwidth / Math.Sqrt(CalibrationFrames - 3);
            for (int y = 0; y < ((Math.Round(yOff) != 0.0)?Columncount-1:Columncount); y++)
            {
                for (int x = -1; x < Rowcount + 3; x++)
                {
                    if (y % 2 == 0 && (x % 4 == 2 && _drawing))
                    {
                        _vs.AddRect((int)((x - 3) * _sqrwidth + xOff), (int)(y * _sqrheight + yOff),
                            (int)(_sqrwidth * 3.0), (int)_sqrheight,
                        Color.FromArgb(255, 0, 255, 0));
                    }

                    if (y % 2 == 0 && (x % 4 == 0 && !_drawing))
                    {
                        _vs.AddRect((int)((x - 3) * _sqrwidth + xOff), (int)(y * _sqrheight + yOff),
                            (int)(_sqrwidth * 3.0), (int)_sqrheight,
                        Color.FromArgb(255, 0, 255, 0));
                    }
                    if (y % 2 == 1 && (x % 4 == 3 && _drawing || x == 0 || (Math.Round(xOff) != 0.0 && x > Rowcount)))
                    {
                        _vs.AddRect((int)((x - 3) * _sqrwidth + xOff), (int)(y * _sqrheight + yOff),
                            (int)(_sqrwidth * 3.0), (int)_sqrheight,
                        Color.FromArgb(255, 0, 0, 255));
                    }
                    if (y % 2 == 1 && (x % 4 == 1 && !_drawing || (Math.Round(xOff) != 0.0 && x > Rowcount)))
                    {
                        _vs.AddRect((int)((x - 3) * _sqrwidth + xOff), (int)(y * _sqrheight + yOff),
                            (int)(_sqrwidth * 3.0), (int)_sqrheight,
                        Color.FromArgb(255, 0, 0, 255));
                    }
                }
            }
            _vs.Draw();
        }

        public Grid Grid { get; set; }
        public Colorfilter ColorFilter { get; private set; }

        public event EventHandler CalibrationCompleted;
    }
}
