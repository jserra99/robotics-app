using System;
using System.Collections.Generic;
using System.Text;

namespace robotics_app
{
    public class exportPoint
    {
        public double x;
        public double y;
        public double theta;
        public double d;
        public double speed;
        public double heading;
        public bool stop;
        public List<string> actions;
        public exportPoint(ControlPoint controlPoint)
        {
            x = -controlPoint.y;
            y = controlPoint.x; // make this or the other one negative
            theta = controlPoint.theta - (Math.PI / 2);
            if (theta < -Math.PI)
            {
                theta += (2*Math.PI);
            }
            d = controlPoint.d;
            speed = controlPoint.speed;
            heading = controlPoint.heading - (Math.PI / 2);
            if (heading < -Math.PI)
            {
                heading += (2*Math.PI);
            }
            stop = controlPoint.stop;
            actions = controlPoint.actions;
        }
    }
}
