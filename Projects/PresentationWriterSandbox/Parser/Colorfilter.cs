﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HSR.PresWriter.PenTracking
{
    internal struct Colorfilter
    {
        /// <summary>
        /// Red matching
        /// </summary>
        public char[] Red { get; set; }

        /// <summary>
        /// Green matching
        /// </summary>
        public char[] Green { get; set; }

        /// <summary>
        /// Blue matching
        /// </summary>
        public char[] Blue { get; set; }
    }
}
