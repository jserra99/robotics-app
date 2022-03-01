using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace robotics_app
{
	public class Planner
	{
		private int pointIndex;
		private double speed;
		private double startAngle;
		private double endAngle;
		private List<ControlPoint> theShortList;
		public Planner(List<ControlPoint> controlPoints_)
		{
			theShortList = controlPoints_;
		}
		private List<pathPoint> InterpolatePath()
		{
			List<pathPoint> pointList = new List<pathPoint>();
			speed = 0.0;
			pointIndex = 0;
            double numberOfPoints = ComputeLength();
            //Trace.WriteLine($"Number of Points: {numberOfPoints}");
			for (int i = 0; i < numberOfPoints; i++)
			{
				(double x, double y, double speed, double heading, bool stop, List<string> actions) = GetPathPosition(i * (theShortList.Count - 1) / numberOfPoints);
				pointList.Add(new pathPoint(x, y, speed, heading, stop, actions));
			}
			return pointList;
		}
        private (double, double, double, double, bool, List<string>) GetPathPosition(double t)
        {
            double heading = 0;
            bool stop = false;
            List<string> actions = new List<string>();
            if (t > pointIndex)
            {
                speed = theShortList[pointIndex].speed;
                stop = theShortList[pointIndex].stop;
                actions = theShortList[pointIndex].actions;
                startAngle = theShortList[pointIndex].heading;
                endAngle = theShortList[pointIndex + 1].heading;
                pointIndex++;
            }
            int startingTime = Convert.ToInt32(Math.Floor(t));
            t = t % 1;

            heading = ((endAngle - startAngle) * t) + startAngle;

            ControlPoint startPoint = theShortList[startingTime];
            ControlPoint endPoint = theShortList[startingTime + 1];

            (double, double) P0 = (startPoint.x, startPoint.y);
            (double, double) P1 = (startPoint.subPoint2X, startPoint.subPoint2Y);
            (double, double) P2 = (endPoint.subPoint1X, endPoint.subPoint1Y);
            (double, double) P3 = (endPoint.x, endPoint.y);


            double a = Math.Pow((1 - t), 3);
            double b = 3 * Math.Pow((1 - t), 2) * (t);
            double c = 3 * (1 - t) * (Math.Pow(t, 2));
            double d = Math.Pow(t, 3);

            double xPos = (a * P0.Item1) + (b * P1.Item1) + (c * P2.Item1) + (d * P3.Item1);
            double yPos = (a * P0.Item2) + (b * P1.Item2) + (c * P2.Item2) + (d * P3.Item2);

            return (xPos, yPos, speed, heading, stop, actions);
        }

        private int ComputeLength()
            /* Returns the (general) length of the path used for interpolation */
        {
            double length = 0;
            int index = 0;
            ControlPoint point;
            ControlPoint nextPoint;

            while (index < theShortList.Count - 1)
            {
                point = theShortList[index];
                nextPoint = theShortList[index + 1];
                double differenceX = nextPoint.x - point.x;
                double differenceY = nextPoint.y - point.y;
                length += Math.Sqrt(differenceX * differenceX + differenceY * differenceY);
                index++;
            }

            return (int) length;
        }
		public List<pathPoint> GetPathPoints()
        {
            return InterpolatePath();
        }
        public void Export(List<ControlPoint> controlPoints)
        {
            // exports to a json
        }

        public void AddControlPoint(double x_, double y_, int position)
        {
            double dVal = 50.0;
            double heading_ = 0.0;

            ControlPoint newPoint = new ControlPoint(x_, y_, Math.PI / 2, dVal, heading_, 1.0, false, new List<string>());
            theShortList.Insert(position, newPoint);
        }

        public void UpdateControlPoint(double x_, double y_, double theta, double dVal_, double heading_, double speed_, bool stop_, List<string> actions_, int position)
        {
            ControlPoint targetPoint = theShortList[position];
            targetPoint.x = x_;
            targetPoint.y = y_;
            targetPoint.theta = theta;
            targetPoint.d = dVal_;
            targetPoint.heading = heading_;
            targetPoint.speed = speed_;
            targetPoint.stop = stop_;
            targetPoint.actions = actions_;
            targetPoint.UpdateSubPoints();
        }

        public List<ControlPoint> GetControlPoints()
        {
            return theShortList;
        }

        public List<pathPoint> updatePath(List<ControlPoint> controlPoints_)
        {
            theShortList = controlPoints_;
            return InterpolatePath();
        }
    }
}