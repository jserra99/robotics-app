using System;
using System.Collections.Generic;
public class pathPoint
{
	public double x;
	public double y;
	public double speed;
	public double heading;
	public bool stop;
	public List<string> actions;
	private static int index_ = 0;
	public int index;

	public pathPoint(double x_, double y_, double speed_, double heading_, bool stop_, List<string> actions_)
	{
		x = x_;
		y = y_;
		speed = speed_;
		heading = heading_;
		stop = stop_;
		actions = actions_;
		index = index_;
		index_ += 1;
	}

	public string ToString()
    {
		return $"X: {x}, Y: {y}, Heading: {heading}, Speed: {speed}, Stop: {stop}, actions: PLACEHOLDER, Index: {index}";
	}
}
