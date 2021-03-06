﻿using System.Drawing;

namespace HSR.PresWriter.Common.Containers
{
    public class VideoFrame : Frame
    {
        /// <summary>
        /// Linked Picture</summary>
        public Bitmap Bitmap { get; set; }

        /// <summary>
        /// Initializing a frame.
        /// If no timestamp is given, we set one as soon the frame is ready</summary>
        /// <param name="number">
        /// Frame Index</param>
        /// <param name="image">
        /// Linked Picture</param>
        /// <param name="timestamp">
        /// Time when the object was captured in milliseconds</param>
        public VideoFrame(int number, Bitmap image, long timestamp = 0)
            : base(number, timestamp)
        {
            Bitmap = image;
        }
    }
}
