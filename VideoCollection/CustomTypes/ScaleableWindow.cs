﻿using System.Windows;

namespace VideoCollection.CustomTypes
{
    public interface ScaleableWindow
    {
        public double WidthScale { get; set; }
        public double HeightScale { get; set; }
        public double HeightToWidthRatio { get; set; }

        // Scale the window based on the parent window
        public void scaleWindow(Window parent);
    }
}
