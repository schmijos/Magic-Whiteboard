﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using HSR.PresWriter.IO.Events;
using HSR.PresWriter.PenTracking;
using HSR.PresWriter.IO;
using Visualizer;
using Point = System.Drawing.Point;

namespace HSR.PresWriter.PenTracking
{

    class AForgeDiffCalibrator : ICalibrator
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
        private const int BDiff = 10;
        private const int GDiff = 20;
        private Bitmap actImg;
        private double _absoluteNeighbourDist = 10;
        private double _relativeNeighbourDist = 2.1;
        private double _topDist = 1.1;


        public AForgeDiffCalibrator(IPictureProvider provider, IVisualizerControl visualizer)
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
            if (_calibrationStep == CalibrationFrames)
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
                        _drawing = false;
                        _vs.Clear();
                        FillRects();
                        diffFilter.OverlayImage = e.Frame.Bitmap;
                    }
                    else
                    {
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
                                                     new IntRange((int)stats.GreenWithoutBlack.Mean + GDiff, 255),
                                                     new IntRange(0, 255));
                        var bcf = new ColorFiltering(new IntRange(0, 255),
                                                     new IntRange(0, 255),
                                                     new IntRange((int)stats.BlueWithoutBlack.Mean + BDiff, 255));
                        //Debug.WriteLine("Green: " + stats.GreenWithoutBlack.Median + " Blue: " + stats.BlueWithoutBlack.Median);
                        //Debug.WriteLine("Green: " + stats.GreenWithoutBlack.Mean + " Blue: " + stats.BlueWithoutBlack.Mean);
                        var bf = new Difference(gcf.Apply(d));
                        bcf.ApplyInPlace(d);
                        bf.ApplyInPlace(d);
                        d.ToManagedImage().Save(@"C:\temp\daforge\diff\img" + _calibrationStep + ".jpg", ImageFormat.Jpeg);
                        stats = new ImageStatistics(d);
                        gcf = new ColorFiltering(new IntRange(0, 255),
                                                     new IntRange((int)stats.GreenWithoutBlack.Mean - GDiff, 255),
                                                     new IntRange(0, 255));
                        bcf = new ColorFiltering(new IntRange(0, 255),
                                                     new IntRange(0, 255),
                                                     new IntRange((int)stats.BlueWithoutBlack.Mean - BDiff, 255));
                        var bbm = bcf.Apply(d);
                        bbm.ToManagedImage().Save(@"C:\temp\daforge\bimg\img" + _calibrationStep + ".jpg", ImageFormat.Jpeg);
                        gcf.ApplyInPlace(d);
                        d.ToManagedImage().Save(@"C:\temp\daforge\gimg\img" + _calibrationStep + ".jpg", ImageFormat.Jpeg);
                        var gblobCounter = new BlobCounter
                            {
                                ObjectsOrder = ObjectsOrder.YX,
                                MaxHeight = 30,
                                MinHeight = 15,
                                MaxWidth = 30,
                                MinWidth = 15,
                                FilterBlobs = true,
                                CoupledSizeFiltering = false
                            };
                        gblobCounter.ProcessImage(d);
                        var bblobCounter = new BlobCounter
                            {
                                ObjectsOrder = ObjectsOrder.YX,
                                MaxHeight = 30,
                                MinHeight = 15,
                                MaxWidth = 30,
                                MinWidth = 15,
                                FilterBlobs = true,
                                CoupledSizeFiltering = false
                            };
                        bblobCounter.ProcessImage(bbm);
                        var ggb = ProcessBlobs(gblobCounter, 0);
                        var bgb = ProcessBlobs(bblobCounter, 1);
                        ggb.VerifyWith(new List<GridBlobs> { bgb }, new List<GridBlobs>());
                        bgb.VerifyWith(new List<GridBlobs> { ggb }, new List<GridBlobs>());
                        ggb.Insert();
                        bgb.Insert();

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
                                GridBlobs.InPlaceSort(corners);
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

        private Bitmap PartiallyApplyAvgFilter(Bitmap src, Channels channel, int x, int y, int diff)
        {
            var parts = new UnmanagedImage[x, y];
            var res = new Bitmap(src.Width, src.Height);
            int width = src.Width / x;
            int height = src.Height / y;
            using (var g = Graphics.FromImage(res))
            {
                for (int i = 0; i < x; i++)
                {
                    for (int j = 0; j < y; j++)
                    {
                        //var crop = src.Clone(new Rectangle(i*width, j*height, width, height), src.PixelFormat)
                        UnmanagedImage crop;
                        BitmapData imageData = src.LockBits(
                            new Rectangle(i * width, j * height, width, height),
                            ImageLockMode.ReadWrite, src.PixelFormat);

                        try
                        {
                            crop = new UnmanagedImage(imageData);
                            // apply several routines to the unmanaged image
                        }
                        finally
                        {
                            src.UnlockBits(imageData);
                        }

                        var s = new ImageStatistics(crop);
                        var cf = new ColorFiltering(new IntRange(0, 255), new IntRange(0, 255), new IntRange(0, 255));
                        switch (channel)
                        {
                            case Channels.Red:
                                cf.Red = new IntRange((int)s.Red.Mean + diff, 255);
                                break;
                            case Channels.Green:
                                cf.Green = new IntRange((int)s.Green.Mean + diff, 255);
                                cf.Blue = new IntRange(0, (int)s.Blue.Mean + 10);
                                break;
                            case Channels.Blue:
                                cf.Blue = new IntRange((int)s.Blue.Mean + diff, 255);
                                cf.Green = new IntRange(0, (int)s.Green.Mean + 10);
                                break;
                            case Channels.GreenAndBlue:
                                cf.Green = new IntRange((int)s.Green.Mean + diff, 255);
                                cf.Blue = new IntRange((int)s.Blue.Mean + diff, 255);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("channel");
                        }
                        cf.ApplyInPlace(crop);
                        g.DrawImageUnscaled(crop.ToManagedImage(), i * width, j * height);
                    }
                }
                g.Flush();
            }
            return res;
        }

        private GridBlobs ProcessBlobs(BlobCounter blobCounter, int offset)
        {
            blobCounter.FilterBlobs = true;
            var blobs = blobCounter.GetObjectsInformation().ToList();
            // remove when outside
            blobs.RemoveAll(x => !Grid.Contains(x.CenterOfGravity));
            var gb = new GridBlobs(blobCounter, this);
            //find topleft
            //var top = blobs.Where(x => x.CenterOfGravity.X == blobs.GetRange(0,Rowcount).
            //    Min(y => y.CenterOfGravity.X)).
            //    OrderBy(z=>z.CenterOfGravity.Y).First();
            var top = blobs.Where(x => IsNextTo(x.CenterOfGravity, Grid.TopLeft, 0, 0, _topDist + offset))
                .OrderBy(x => x.Area).ToList();
            while (!top.Any() && _topDist < 2.5)
            {
                _topDist += 0.05;
                top = blobs.Where(x => IsNextTo(x.CenterOfGravity, Grid.TopLeft, 0, 0, _topDist + offset))
                    .OrderBy(x => x.Area).ToList();
            }
            while (top.Count > 1)
            {
                _topDist -= 0.05;
                top = blobs.Where(x => IsNextTo(x.CenterOfGravity, Grid.TopLeft, 0, 0, _topDist + offset))
                    .OrderBy(x => x.Area).ToList();
            }
            if (top.Any())
            {
                gb.SetPosition(top.First(),new Point(0,0));
                if (ProcessBlobs(gb, top.First(), offset, offset) < Rowcount*Columncount*0.75)
                    _errors++;
                return gb;
            }
            _errors++;
            return null;
        }

        private int ProcessBlobs(GridBlobs gb, Blob act, int x, int y)
        {
            if (x < 0 || y < 0 || y > Columncount || x > Rowcount)
            {
                //throw new ArgumentException("X: "+x+ " Y: " + y + " isn't a valid blob position");
                Debug.WriteLine("X: " + x + " Y: " + y + " isn't a valid blob position");
                return 0;
            }
            //gb.SetPosition(act, new Point(x,y));
            gb.SetIterated(act);
            //find neighbours
            var n = gb.TagDirectNeighbours(act, x, y);
            n = n.Where(b => !gb.IsIterated(b));
            int res = n.Count();
            foreach (var b in n)
            {
                var xdiff = b.CenterOfGravity.X - act.CenterOfGravity.X;
                var ydiff = b.CenterOfGravity.Y - act.CenterOfGravity.Y;
                if (Math.Abs(xdiff) > Math.Abs(ydiff))
                {
                    if (xdiff > 0 && x + 2 < Rowcount)
                    {
                        res += ProcessBlobs(gb, b, x + 2, y);
                    }
                    else if (x > 1)
                    {
                        res += ProcessBlobs(gb, b, x - 2, y);
                    }
                }
                else
                {
                    if (ydiff > 0 && y + 2 < Columncount)
                    {
                        res += ProcessBlobs(gb, b, x, y + 2);
                    }
                    else if (y > 1)
                    {
                        res += ProcessBlobs(gb, b, x, y - 2);
                    }
                }
            }
            return res;
        }

        private double Distance(AForge.Point p1, AForge.Point p2)
        {
            var diff = (p1 - p2);
            return Math.Sqrt(diff.X*diff.X + diff.Y*diff.Y);
        }

        private bool IsNextTo(AForge.Point p1, AForge.Point p2, int rownr, int colnr, double maxDist = 1.5, bool diagonal = true)
        {
            return IsNextTo(p1, new Point((int)p2.X, (int)p2.Y), rownr, colnr, maxDist, diagonal);
        }



        private bool IsNextTo(AForge.Point p1, AForge.Point p2, double absoluteDist)
        {
            return Distance(p1, new AForge.Point(p2.X, p2.Y)) < absoluteDist;
        }

        private bool IsNextTo(AForge.Point p1, Point p2, int rownr, int colnr, double maxDist = 1.5, bool diagonal = true)
        {
            var rowDistance = PredictRowDistance(colnr);
            var columnDistance = PredictColumnDistance(rownr);
            var nextTo = Math.Abs(p1.X - p2.X) < rowDistance*maxDist && Math.Abs(p1.Y - p2.Y) < columnDistance*maxDist;
            if (diagonal)
            {
                return nextTo;
            }
            //Debug.WriteLine("Dist: " + Distance(p1, new AForge.Point(p2.X, p2.Y)) + " diag: " + Math.Sqrt(columnDistance * columnDistance + rowDistance * rowDistance));
            return nextTo &&
                   Distance(p1, new AForge.Point(p2.X, p2.Y)) <
                   maxDist*Math.Sqrt(columnDistance * columnDistance + rowDistance * rowDistance);
        }

        private int PredictRowDistance(int columnnr)
        {
            var dist = (Grid.TopRight.X - Grid.TopLeft.X) * (1.0 - (double)columnnr / Columncount) +
                       (Grid.BottomRight.X - Grid.BottomLeft.X) * ((double)columnnr / Columncount);
            return (int)Math.Round(dist / Rowcount);
        }

        private int PredictColumnDistance(int rownr)
        {

            var dist = (Grid.BottomLeft.Y - Grid.TopLeft.Y) * (1.0 - (double)rownr / Rowcount) +
                       (Grid.BottomRight.Y - Grid.TopRight.Y) * ((double)rownr / Rowcount);
            return (int)Math.Round(dist / Columncount);
        }

        /// <summary>
        /// Sorting the whole list
        /// </summary>
        /// <remarks>Rects have to be sorted</remarks>
        /// <param name="rectlist"></param>
        //private void InPlaceSort(List<List<IntPoint>> rectlist)
        //{
        //    var tmp = new List<List<IntPoint>>();
        //    rectlist = rectlist.OrderBy(x => x.First().Y).ToList();
        //    for (int i = 0; i < Rowcount; i++)
        //    {
        //        var tmp2 = new List<List<IntPoint>>();
        //        tmp2.AddRange(rectlist.GetRange(0, Columncount));
        //    }
        //}

        //private void InPlaceSort(List<IntPoint> rect)
        //{
        //    if (rect == null || rect.Count != 4)
        //        throw new ArgumentException("Not a rectancle");
        //    var tmp = new List<IntPoint>(rect);
        //    rect.Clear();
        //    var tl = tmp.First(x => x.Y == tmp.Min(y => y.Y));
        //    tmp.Remove(tl);
        //    rect.Add(tl);
        //    tl = tmp.First(x => x.Y == tmp.Min(y => y.Y));
        //    tmp.Remove(tl);
        //    rect.Add(tl);
        //    rect.Sort((x, y) => x.X.CompareTo(y.X));
        //    tmp = tmp.OrderBy(x => x.X).ToList();
        //    //rect.AddRange(tmp);
        //    rect.Add(tmp[0]);
        //    rect.Add(tmp[1]);
        //}

        private void FillRects()
        {

            double xOff = ((_calibrationStep - 3) % (int)Math.Sqrt(CalibrationFrames - 3)) *
                _sqrwidth / Math.Sqrt(CalibrationFrames - 3);
            double yOff = Math.Floor((_calibrationStep - 3) / Math.Sqrt(CalibrationFrames - 3)) *
                _sqrwidth / Math.Sqrt(CalibrationFrames - 3);
            for (int y = 0; y < Columncount; y++)
            {
                for (int x = -1; x < Rowcount + 3; x++)
                {
                    if (y % 2 == 0 && x % 4 == 2 && _drawing)
                    {
                        _vs.AddRect((int)((x - 3) * _sqrwidth + xOff), (int)(y * _sqrheight + yOff),
                            (int)(_sqrwidth * 3.0), (int)_sqrheight,
                        Color.FromArgb(255, 0, 255, 0));
                    }

                    if (y % 2 == 0 && x % 4 == 0 && !_drawing)
                    {
                        _vs.AddRect((int)((x - 3) * _sqrwidth + xOff), (int)(y * _sqrheight + yOff),
                            (int)(_sqrwidth * 3.0), (int)_sqrheight,
                        Color.FromArgb(255, 0, 255, 0));
                    }
                    if (y % 2 == 1 && (x % 4 == 3 && _drawing || x == 0))
                    {
                        _vs.AddRect((int)((x - 3) * _sqrwidth + xOff), (int)(y * _sqrheight + yOff),
                            (int)(_sqrwidth * 3.0), (int)_sqrheight,
                        Color.FromArgb(255, 0, 0, 255));
                    }
                    if (y % 2 == 1 && x % 4 == 1 && !_drawing)
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

        internal class BlobPositions
        {
            public BlobPositions()
            {
                Iterated = false;
                Points = new List<Point>();
                VerifiedPosition = new Point(-1, -1);
            }

            public bool Iterated { get; set; }

            public List<Point> Points { get; set; }

            public Point VerifiedPosition { get; set; }
        }

        internal class GridBlobs
        {
            internal class BlobComparer : IEqualityComparer<Blob>
            {
                public bool Equals(Blob x, Blob y)
                {
                    return x.ID == y.ID && x.Rectangle == y.Rectangle && x.CenterOfGravity == y.CenterOfGravity &&
                           x.Area == y.Area && x.Fullness == y.Fullness && x.ColorMean == y.ColorMean &&
                           x.ColorStdDev == y.ColorStdDev;
                }

                public int GetHashCode(Blob obj)
                {
                    long i = obj.ID*obj.Area*obj.ColorMean.R*obj.ColorMean.G*obj.ColorMean.B*obj.ColorStdDev.R*
                             obj.ColorStdDev.G*obj.ColorStdDev.B*obj.Rectangle.Left*obj.Rectangle.Top*
                             obj.Rectangle.Right* obj.Rectangle.Bottom;
                    i = i%UInt32.MaxValue;
                    i += Int32.MinValue;
                    return (int) i;
                }
            }

            private Dictionary<Blob, BlobPositions> _blobPositions;
            private BlobCounter _bc;
            private AForgeDiffCalibrator _adc;

            public IEnumerable<Blob> Blobs { get { return _blobPositions.Keys; } }

            public GridBlobs(BlobCounter bc, AForgeDiffCalibrator adc)
            {
                _blobPositions = new Dictionary<Blob, BlobPositions>(new BlobComparer());
                _bc = bc;
                _adc = adc;
                var blobs = bc.GetObjectsInformation().Where(x => adc.Grid.Contains(
                    new Point((int) x.CenterOfGravity.X, (int) x.CenterOfGravity.Y))).ToList();
                foreach (var blob in blobs)
                {
                    _blobPositions.Add(blob, new BlobPositions());
                }
            }

            public bool IsIterated(Blob b)
            {
                return _blobPositions[b].Iterated;
            }

            public void SetIterated(Blob b)
            {
                _blobPositions[b].Iterated = true;
            }

            public Point GetPosition(Blob b)
            {
                var res = new Dictionary<Point, int>();
                foreach (var p in _blobPositions[b].Points)
                {
                    if (!res.Keys.Contains(p))
                        res.Add(p, 1);
                    else
                        res[p]++;
                }
                if (!_blobPositions[b].Points.Any() || res.Values.Max() * 1.0 < 0.75 * _blobPositions[b].Points.Count)
                    return new Point(-1, -1);
                return res.First(x => x.Value == res.Values.Max()).Key;
            }

            public List<Point> GetUniquePositions(Blob b)
            {
                var res = new List<Point>();
                foreach (var blobPosition in _blobPositions[b].Points)
                {
                    if (!res.Contains(blobPosition))
                        res.Add(blobPosition);
                }
                return res;
            }

            public void SetPosition(Blob b, Point p)
            {
                //if (!_blobPositions.ContainsKey(b))
                //    _blobPositions.Add(b, new BlobPositions());
                _blobPositions[b].Points.Add(p);
            }

            public List<IntPoint> FindCorners(Blob b)
            {
                var res = PointsCloud.FindQuadrilateralCorners(_bc.GetBlobsEdgePoints(b));
                InPlaceSort(res);
                return res;
            }

            public IEnumerable<Blob> UnMapped { get { return _blobPositions.Where(x => !x.Value.Iterated).Select(x => x.Key); } }

            public static void InPlaceSort(List<IntPoint> rect)
            {
                if (rect == null || rect.Count != 4)
                    throw new ArgumentException("Not a rectancle");
                var tmp = new List<IntPoint>(rect);
                rect.Clear();
                var tl = tmp.First(x => x.Y == tmp.Min(y => y.Y));
                tmp.Remove(tl);
                rect.Add(tl);
                tl = tmp.First(x => x.Y == tmp.Min(y => y.Y));
                tmp.Remove(tl);
                rect.Add(tl);
                rect.Sort((x, y) => x.X.CompareTo(y.X));
                tmp = tmp.OrderBy(x => x.X).ToList();
                //rect.AddRange(tmp);
                rect.Add(tmp[0]);
                rect.Add(tmp[1]);
            }

            public Dictionary<Point, double> FindPreference(Blob act, List<Point> possibilities, bool identicalChannel)
            {
                var maxNeigbours = identicalChannel?new IntRange(5,5):new IntRange(4,4);
                var res = new Dictionary<Point, int>();
                if (possibilities.Any())
                {
                    foreach (var point in possibilities)
                    {
                        if (!res.ContainsKey(point))
                            res.Add(point, 1);
                        else
                            res[point]++;
                    }
                    var p = res.First(x => x.Value == res.Values.Max()).Key;
                    if (possibilities.Any(x => (p.X == 0 || p.X == Rowcount)))
                    {
                        maxNeigbours.Max -= 1;
                        maxNeigbours.Min -= 1;
                    }
                    if (possibilities.Any(x => (p.Y == 0 || p.Y == Columncount)))
                    {
                        maxNeigbours.Max -= 1;
                        maxNeigbours.Min -= 1;
                    }
                }
                else
                {
                    maxNeigbours.Min -= 2;
                }
                var n =
                        _blobPositions.Where(m => _adc.IsNextTo(act.CenterOfGravity, m.Key.CenterOfGravity,
                                                                _adc._absoluteNeighbourDist/2)).Select(m => m.Key).ToList();
                    while (n.Count() > maxNeigbours.Max)
                    {
                        //Debug.WriteLine("too many neighbours at " + p.X + "," + p.Y + " decreasing distance");
                        _adc._absoluteNeighbourDist -= 0.2;
                        n =
                            _blobPositions.Where(
                                m => _adc.IsNextTo(act.CenterOfGravity, m.Key.CenterOfGravity,
                                                   _adc._absoluteNeighbourDist/2)).Select(m => m.Key).ToList();
                    }
                while (n.Count() < maxNeigbours.Min && _adc._absoluteNeighbourDist < 50)
                {
                    //Debug.WriteLine("no neighbours at " + p.X + "," + p.Y + " increasing distance");
                    _adc._absoluteNeighbourDist += 0.2;
                    n =
                        _blobPositions.Where(
                            m => _adc.IsNextTo(act.CenterOfGravity, m.Key.CenterOfGravity, 
                                               _adc._absoluteNeighbourDist/2)).Select(m => m.Key).ToList();
                }
                int maxConfidance = res.Sum(x => x.Value);
                if (identicalChannel)
                {
                    var id =
                        n.OrderBy(
                            x =>
                            Math.Abs(x.CenterOfGravity.X - act.CenterOfGravity.X) + 
                            Math.Abs(x.CenterOfGravity.Y - act.CenterOfGravity.Y));
                    foreach (var position in _blobPositions[id.First()].Points)
                    {
                        if (res.ContainsKey(position))
                            res[position] += possibilities.Count(x => x.Equals(position));
                        else
                            res.Add(position, 1);
                    }
                    n.Remove(id.First());
                }
                foreach (var b in n)
                {
                    var xdiff = b.CenterOfGravity.X - act.CenterOfGravity.X;
                    var ydiff = b.CenterOfGravity.Y - act.CenterOfGravity.Y;
                    if (!identicalChannel)
                    {
                        if (ydiff > 0)
                        {
                            if (xdiff > 0)
                            {
                                foreach (var position in _blobPositions[b].Points)
                                {
                                    var calc = new Point(position.X - 1, position.Y - 1);
                                    if (res.ContainsKey(calc))
                                        res[calc] += possibilities.Count(x => x.Equals(calc));
                                    else
                                        res.Add(calc, 1);
                                }
                                maxConfidance += _blobPositions[b].Points.Count*possibilities.Count;
                            }
                            else
                            {
                                foreach (var position in _blobPositions[b].Points)
                                {
                                    var calc = new Point(position.X + 1, position.Y - 1);
                                    if (res.ContainsKey(calc))
                                        res[calc] += possibilities.Count(x => x.Equals(calc));
                                    else
                                        res.Add(calc, 1);
                                }
                                maxConfidance += _blobPositions[b].Points.Count*possibilities.Count;
                            }
                        }
                        else
                        {
                            if (xdiff > 0)
                            {
                                foreach (var position in _blobPositions[b].Points)
                                {
                                    var calc = new Point(position.X - 1, position.Y + 1);
                                    if (res.ContainsKey(calc))
                                        res[calc] += possibilities.Count(x => x.Equals(calc));
                                    else
                                        res.Add(calc, 1);
                                }
                                maxConfidance += _blobPositions[b].Points.Count*possibilities.Count;
                            }
                            else
                            {
                                foreach (var position in _blobPositions[b].Points)
                                {
                                    var calc = new Point(position.X + 1, position.Y + 1);
                                    if (res.ContainsKey(calc))
                                        res[calc] += possibilities.Count(x => x.Equals(calc));
                                    else
                                        res.Add(calc, 1);
                                }
                                maxConfidance += _blobPositions[b].Points.Count*possibilities.Count;
                            }
                        }
                    }
                    else
                    {
                        if (Math.Abs(xdiff) > Math.Abs(ydiff))
                        {
                            if (xdiff > 0)
                            {
                                foreach (var position in _blobPositions[b].Points)
                                {
                                    var calc = new Point(position.X - 1, position.Y);
                                    if (res.ContainsKey(calc))
                                        res[calc] += possibilities.Count(x => x.Equals(calc));
                                    else
                                        res.Add(calc, 1);
                                }
                                maxConfidance += _blobPositions[b].Points.Count * possibilities.Count;
                            }
                            else
                            {
                                foreach (var position in _blobPositions[b].Points)
                                {
                                    var calc = new Point(position.X + 1, position.Y);
                                    if (res.ContainsKey(calc))
                                        res[calc] += possibilities.Count(x => x.Equals(calc));
                                    else
                                        res.Add(calc, 1);
                                }
                                maxConfidance += _blobPositions[b].Points.Count * possibilities.Count;
                            }
                        }
                        else
                        {
                            if (ydiff > 0)
                            {
                                foreach (var position in _blobPositions[b].Points)
                                {
                                    var calc = new Point(position.X, position.Y - 1);
                                    if (res.ContainsKey(calc))
                                        res[calc] += possibilities.Count(x => x.Equals(calc));
                                    else
                                        res.Add(calc, 1);
                                }
                                maxConfidance += _blobPositions[b].Points.Count * possibilities.Count;
                            }
                            else
                            {
                                foreach (var position in _blobPositions[b].Points)
                                {
                                    var calc = new Point(position.X, position.Y + 1);
                                    if (res.ContainsKey(calc))
                                        res[calc] += possibilities.Count(x => x.Equals(calc));
                                    else
                                        res.Add(calc, 1);
                                }
                                maxConfidance += _blobPositions[b].Points.Count * possibilities.Count;
                            }
                        }
                    }
                }
#if DEBUG
                using (var fs = new StreamWriter(new FileStream(@"C:\Temp\daforge\blobs.csv", FileMode.Append, FileAccess.Write)))
                {
                    foreach (var point in res)
                    {
                        var s = act.CenterOfGravity.X + "," + act.CenterOfGravity.Y + "," + point.Key.X +
                                "," + point.Key.Y + "," + 100.0*point.Value/maxConfidance + ",";
                        fs.WriteLine(s);
                    }
                    fs.Flush();
                }
#endif
                return new Dictionary<Point, double>(res.ToDictionary(x => x.Key, x => (double)x.Value/maxConfidance));
            }

            public IEnumerable<Blob> TagDirectNeighbours(Blob act, int x, int y)
            {
                int maxNeigbours = 4;
                if (x == 0 || x == Rowcount)
                    maxNeigbours -= 1;
                if (y == 0 || y == Columncount)
                    maxNeigbours -= 1;
                if (!_blobPositions[act].Points.Any(p => p.X == x && p.Y == y))
                    //throw new InvalidOperationException("Position not stored");
                    Debug.WriteLine("Pos: " + x + "/" + y + " not stored");
                var n = _blobPositions.Where(m => _adc.IsNextTo(act.CenterOfGravity, m.Key.CenterOfGravity, x, y, 
                    _adc._relativeNeighbourDist, false)).Select(m => m.Key);
                while (n.Count() > maxNeigbours)
                {
                    Debug.WriteLine("too many neighbours at " + x + "," + y + " decreasing distance");
                    _adc._relativeNeighbourDist -= 0.05;
                    n = _blobPositions.Where(m => _adc.IsNextTo(act.CenterOfGravity, m.Key.CenterOfGravity, x, y, 
                        _adc._relativeNeighbourDist, false)).Select(m => m.Key);
                }
                while (n.Count() < maxNeigbours && _adc._relativeNeighbourDist < 3.0)
                {
                    Debug.WriteLine("no neighbours at " + x + "," + y + " increasing distance");
                    _adc._relativeNeighbourDist += 0.05;
                    n = _blobPositions.Where(m => _adc.IsNextTo(act.CenterOfGravity, m.Key.CenterOfGravity, x, y, 
                        _adc._relativeNeighbourDist, false)).Select(m => m.Key);
                }
                foreach (var b in n)
                {
                    var xdiff = b.CenterOfGravity.X - act.CenterOfGravity.X;
                    var ydiff = b.CenterOfGravity.Y - act.CenterOfGravity.Y;
                    if (Math.Abs(xdiff) > Math.Abs(ydiff))
                    {
                        if (xdiff > 0 && x + 2 < Rowcount)
                        {
                            _blobPositions[b].Points.Add(new Point(x + 2, y));
                        }
                        else if (x > 1)
                        {
                            _blobPositions[b].Points.Add(new Point(x - 2, y));
                        }
                    }
                    else
                    {
                        if (ydiff > 0 && y + 2 < Columncount)
                        {
                            _blobPositions[b].Points.Add(new Point(x, y + 2));
                        }
                        else if (y > 1)
                        {
                            _blobPositions[b].Points.Add(new Point(x, y - 2));
                        }
                    }
                }
                return n;
            }

            internal void VerifyWith(List<GridBlobs> otherChannels, List<GridBlobs> sameChannel)
            {
                foreach (var blob in _blobPositions.Keys)
                {
                    var probabilities = new Dictionary<Point, double>();
                    foreach (var gridBlob in otherChannels)
                    {
                        foreach (var pref in gridBlob.FindPreference(blob, _blobPositions[blob].Points, false))
                        {
                            if (!probabilities.ContainsKey(pref.Key))
                            {
                                probabilities.Add(pref.Key, pref.Value);
                            }
                            else
                            {
                                probabilities[pref.Key] += pref.Value/otherChannels.Count();
                            }
                        }
                    }
                    foreach (var gridBlob in sameChannel)
                    {
                        foreach (var pref in gridBlob.FindPreference(blob, _blobPositions[blob].Points, true))
                        {
                            if (!probabilities.ContainsKey(pref.Key))
                            {
                                probabilities.Add(pref.Key, pref.Value);
                            }
                            else
                            {
                                probabilities[pref.Key] += pref.Value/otherChannels.Count();
                            }
                        }
                    }
#if DEBUG
                    using (
                        var fs =
                            new StreamWriter(new FileStream(@"C:\Temp\daforge\blobs.csv", FileMode.Append,
                                                            FileAccess.Write)))
                    {
                        foreach (var point in probabilities)
                        {
                            var s = blob.CenterOfGravity.X + "," + blob.CenterOfGravity.Y + "," + point.Key.X +
                                    "," + point.Key.Y + "," + 100.0*point.Value + ",";
                            fs.WriteLine(s);
                        }
                        fs.Flush();
                    }
#endif
                    if (probabilities.Any())
                    {
                        _blobPositions[blob].VerifiedPosition =
                            probabilities.First(x => x.Value == probabilities.Values.Max()).Key;
                    }
                }
            }

            internal void Insert()
            {
                foreach (var act in _blobPositions.Where(x => x.Value.VerifiedPosition.X != -1 &&
                                                              x.Value.VerifiedPosition.Y != -1))
                {
                    var corners = FindCorners(act.Key);
                    int x = act.Value.VerifiedPosition.X;
                    int y = act.Value.VerifiedPosition.Y;
#if DEBUG
                    using (var g = Graphics.FromImage(_adc.actImg))
                    {
                        g.DrawString(x + "," + y, new Font(FontFamily.GenericSansSerif, 8.0f), new SolidBrush(Color.White),
                            act.Key.CenterOfGravity.X, act.Key.CenterOfGravity.Y);
                        g.Flush();
                        //Debug.WriteLine("wrote to image");
                    }
#endif
                    if (corners.Count == 4)
                    {
                        double xOff = ((_adc._calibrationStep - 3) % (int)Math.Sqrt(CalibrationFrames - 3)) * _adc._sqrwidth /
                                      (int) Math.Sqrt(CalibrationFrames - 3);
                        double yOff = Math.Floor((_adc._calibrationStep - 3) / Math.Sqrt(CalibrationFrames - 3)) * _adc._sqrwidth /
                                      (int) Math.Sqrt(CalibrationFrames - 3);
                        _adc.Grid.AddPoint((int)(x * _adc._sqrwidth + xOff), (int)(y * _adc._sqrheight + yOff), corners[0].X,
                                      corners[0].Y);
                        _adc.Grid.AddPoint((int)((x + 1) * _adc._sqrwidth + xOff), (int)(y * _adc._sqrheight + yOff), corners[1].X,
                                      corners[1].Y);
                        _adc.Grid.AddPoint((int)(x * _adc._sqrwidth + xOff), (int)((y + 1) * _adc._sqrheight + yOff), corners[2].X,
                                      corners[2].Y);
                        _adc.Grid.AddPoint((int)((x + 1) * _adc._sqrwidth + xOff), (int)((y + 1) * _adc._sqrheight + yOff), corners[3].X,
                                      corners[3].Y);
                    }
                }
            }
        }
    }
}
