using System;
using System.Collections.Generic;



//https://docs.microsoft.com/en-us/dotnet/api/system.windows.shapes.ellipse?view=windowsdesktop-6.0


namespace robotics_app
{
    public class ControlPoint
    {

        public double x;
        public double y;
        public double theta;
        public double d;
        public double heading;
        public double speed;
        public bool stop;
        public List<string> actions;
        public double subPoint1X;
        public double subPoint1Y;
        public double subPoint2X;
        public double subPoint2Y;
        public bool selected;

        public ControlPoint(double x_, double y_, double theta_, double d_, double heading_, double speed_, bool stop_, List<string> actions_)
        {
            x = x_;
            y = y_;
            theta = theta_;
            d = d_;
            heading = heading_;
            speed = speed_;
            stop = stop_;
            actions = actions_;
            UpdateSubPoints();
        }
        public void UpdateSubPoints()
        {
            subPoint1X = (-1 * d * Math.Cos(theta)) + x;
            subPoint1Y = (-1 * d * Math.Sin(theta)) + y;
            subPoint2X = (d * Math.Cos(theta)) + x;
            subPoint2Y = (d * Math.Sin(theta)) + y;
        }

        public string ToString()
        {
            return $"X: {x}, Y: {y}, Theta: {theta}, Dval: {d}, Heading: {heading}, Speed: {speed}, Stop: {stop}, actions: PLACEHOLDER";
        }

    }

}