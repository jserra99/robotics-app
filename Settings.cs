using System;
using System.Collections.Generic;
using System.Text;

namespace robotics_app
{
    public class Settings
    {
        public double robotWidth;
        public double robotLength;

        public Settings(double robotWidth_, double robotLength_)
        {
            robotWidth = robotWidth_;
            robotLength = robotLength_;
        }
    }
}
