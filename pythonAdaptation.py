from enum import auto
import pygame as pg
import math
import os
import pandas as pd
import time
# import wpimath.trajectory as trajectory
# trajectory. 

'''
TODO:
- Ben: Make a method that takes the interpolated points and generates a lookup table. 
    Use this lookup table to generate points based on distance instead of time. 
    Use integral of arc length function for distance.
    Learn pygame to help Joe
- Joe:
- read the csv..
- add ways to type in coordinates
- background grid representing realistic measurements
- add a "simulation" of the robot going on the path based on constraints such as max velocity and time going up to the autonomous max time
- add a way to edit the d value
- add a way to edit the slope: add an interactive tangent line which has two manupulatable circles on the end to change the slope as well as the d-value
- add circles and line functionality
- add a way to add certain arbitrary auxilary robot functions into the commands

'''

class autonomous:
    def __init__(self, theBigPlay, mode):
        self.theBigPlay = theBigPlay
        # self.loadPlayBook()
        self.previousPoint = [None, None]
        self.selectedPoint = [None, None]
        if mode == "editor":
            pg.init()
            self.size = width, height = 750, 750
            self.screen = pg.display.set_mode(self.size)
            self.loadEditor()

    def loadPlayBook(self):
        filePath = f"{os.getcwd()}./autonomous_algorithms/{self.theBigPlay}.csv"
        df = pd.read_csv(filePath)
        self.points = []
        for row in df.itertuples(index=False, name=None):
            self.points.append(list(row))

    def loadEditor(self):
        self.pointList = []
        for point in self.theBigPlay:
            self.pointList.append([point.x + (self.size[0] / 2), -point.y + (self.size[1] / 2)])
        print(self.pointList)
        self.cubicPath = path.cubicBSpline(self.theBigPlay)
        self.updateCanvas()
        self.editor()

    def updateCanvas(self):
        self.screen.fill("black")
        self.cubicPath = path.cubicBSpline(self.theBigPlay)
        self.pointList = []
        for point in self.theBigPlay:
            self.pointList.append([point.x + (self.size[0] / 2), -point.y + (self.size[1] / 2)])
        numberOfPoints = len(self.pointList)
        pointIndex = 0
        self.pointRadius = 10
        for point in self.pointList:
            pg.draw.circle(self.screen, (255-(255*pointIndex/numberOfPoints), 118, 0), (point[0], point[1]), self.pointRadius)
            pointIndex += 1
        pathCoordinates = self.cubicPath.interpolatePath(500)
        langth = len(pathCoordinates)
        i = 1
        for coordinate in pathCoordinates:
            if i == langth:
                break
            pg.draw.line(self.screen, (127, 255, 255), (coordinate[0] + (self.size[0] / 2 ), -coordinate[1] + (self.size[1] / 2)), (pathCoordinates[i][0] + (self.size[0] / 2 ), -pathCoordinates[i][1] + (self.size[1] / 2)))
            i += 1
        pg.display.update()

    def createCirclePath(self, coordinates, radius, circumferencePercentage, cumulativeAngle, directionInverted):
        circumference = 2 * math.pi * radius
        individualLineLength = circumference / 360
        startingCoordinate = coordinates
        if directionInverted:
            cumulativeAngle = -cumulativeAngle
        for i in range(circumferencePercentage):
            endingCoordinate = (startingCoordinate[0] + (math.cos(math.radians(cumulativeAngle + 1)) * individualLineLength), startingCoordinate[1] - (math.sin(math.radians(cumulativeAngle + 1)) * individualLineLength))
            adjustedStart, adjustedEnd = (startingCoordinate[0] + self.size[0] / 2, -startingCoordinate[1] + self.size[1] / 2), (endingCoordinate[0] + self.size[0] / 2, -endingCoordinate[1] + self.size[1] / 2)
            pg.draw.line(self.screen, (255 - i / 2, 255 - i / 2, 255 - i / 2), adjustedStart, adjustedEnd, 5)
            # pg.draw.circle(self.screen, (255, 255, 255), adjustedStart, 5)
            print(str(adjustedStart) + ", " + str(adjustedEnd))
            startingCoordinate = endingCoordinate
            cumulativeAngle += 1

        pg.display.update()
        self.editor()



    def cursorTarget(self, mousePOS):
        for point in self.pointList:
            pointHitbox = [[point[0] - self.pointRadius, point[0] + self.pointRadius], [point[1] - self.pointRadius, point[1] + self.pointRadius]]
            if (mousePOS[0] >= pointHitbox[0][0] and mousePOS[0] <= pointHitbox[0][1]) and (mousePOS[1] >= pointHitbox[1][0] and mousePOS[1] <= pointHitbox[1][1]):
                print("you have violated a point")
                self.selectedPoint = [point, self.pointList.index(point)]
    def updatePoint(self, mousePOS):
        self.theBigPlay[self.selectedPoint[1]].x, self.theBigPlay[self.selectedPoint[1]].y = (mousePOS[0] - (self.size[0] / 2), -mousePOS[1] + (self.size[1] / 2))
        self.theBigPlay[self.selectedPoint[1]].updateSubPoints()
        self.updateCanvas()

    def editor(self):
        while True:
            time.sleep(0.001)
            for event in pg.event.get():
                if event.type == pg.QUIT:
                    pg.quit()
                    quit()
                elif pg.mouse.get_pressed() == (1, 0, 0):
                    mousePosition = pg.mouse.get_pos()
                    print(mousePosition)
                    self.previousPoint = self.selectedPoint
                    self.cursorTarget(mousePosition)
                    if self.previousPoint[1] == self.selectedPoint[1]:
                        self.updatePoint(mousePosition)
                '''elif pg.mouse.get_pressed() == (0, 0, 1):
                    mousePosition = pg.mouse.get_pos()
                    print(mousePosition)
                    self.cursorTarget(mousePosition)'''
                        




class path:
    class controlPoint:
        def __init__(self, x, y, theta, d):
            self.x = x
            self.y = y
            self.theta = theta
            self.d = d

            self.subPoint1X = -1*self.d*math.cos(self.theta) + self.x
            self.subPoint1Y = -1*self.d*math.sin(self.theta) + self.y
            self.subPoint2X = self.d*math.cos(self.theta) + self.x
            self.subPoint2Y = self.d*math.sin(self.theta) + self.y

        def updateSubPoints(self):
            self.subPoint1X = -1*self.d*math.cos(self.theta) + self.x
            self.subPoint1Y = -1*self.d*math.sin(self.theta) + self.y
            self.subPoint2X = self.d*math.cos(self.theta) + self.x
            self.subPoint2Y = self.d*math.sin(self.theta) + self.y

        def setX(self, x):
            self.x = x

        def setY(self, y):
            self.y = y

        def setCoords(self, x, y):
            self.x = x
            self.y = y

        def setAngle(self, theta):
            self.theta = theta
            self.updateSubPoints()

        def setDistanceValue(self, distance):
            self.d = distance
            self.updateSubPoints()


    class cubicBSpline:
        def __init__(self, points):
            self.points = points

        def interpolatePath(self, numberOfPoints):
            self.interpolatedList = []
            for i in range(0,numberOfPoints):
                self.interpolatedList.append(self.getPathPosition(i*(len(self.points)-1)/numberOfPoints))
            return(self.interpolatedList)

        def getPathPosition(self, t):
            if t > (len(self.points) - 1):
                return(None)
            startingPoint = int(t) #Takes the floor of t

            t = t % 1

            P0 = self.points[startingPoint]
            P3 = self.points[startingPoint + 1]
            P1 = [P0.subPoint2X, P0.subPoint2Y]
            P2 = [P3.subPoint1X, P3.subPoint1Y]
            P0 = [self.points[startingPoint].x, self.points[startingPoint].y]
            P3 = [self.points[startingPoint + 1].x, self.points[startingPoint + 1].y]

            a = (1-t)**3
            b = 3*((1-t)**2)*(t)
            c = 3*(1-t)*((t)**2)
            d = (t)**3

            x = a*P0[0] + b*P1[0] + c*P2[0] + d*P3[0]
            y = a*P0[1] + b*P1[1] + c*P2[1] + d*P3[1]

            return(x, y)

robotPath1 = [path.controlPoint(-200,-200,math.pi/2,0), path.controlPoint(0,0,math.pi/2,0), path.controlPoint(200, 200, math.pi/2, 55), path.controlPoint(300,300,0,55)]
pathMaker = autonomous(robotPath1, 'editor')
#circleThing = autonomous("yolo", "editor")
# circleThing.createCirclePath((0,0), 50, 270, 180, True)
