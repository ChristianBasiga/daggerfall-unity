using DaggerfallConnect.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;


//This will be a template pattern, builds path same way, but what changes
//are what we do at every step, maybe instead of that have list of delegates to callback instead.
public class PathBuilder {


    //Could just be delegates in builder but for sake of matching pattern exactly.
    //For these actions we'd like to know slope and current position, and travel options
    public interface TravelAlongSlopeAction
    {
       
        void Execute(DFPosition mapPixel, DFPosition prev, bool travelShip);

    }


    //So actions to do during reroute process, ocean check loop, do something with slope.
    //For calculator can use to get prev, and calculate time at each step, so basically storing in two places.
    public interface PathRerouteAction
    {
        void Execute(int run, double rise);
    }


    //In the while !isAtDestination loop.
    public interface NewLegAction
    {
        //Not sure what other params this needs, for calculation really just adds taken taken this leg to full. If keep the taken this leg variable.
        void Execute(List<DFPosition> leg, bool travelShip);

    }

    //When we've hit the destination.
    public interface PathBuiltAction
    {

        void Execute(LinkedList<DFPosition> fullPath, bool travelShip);
    }


    LinkedList<NewLegAction> newLegActions;
    LinkedList<PathRerouteAction> pathRerouteActions;
    LinkedList<TravelAlongSlopeAction> travelAlongSlopeActions;
    LinkedList<PathBuiltAction> pathBuiltActions;


    public PathBuilder()
    {

        newLegActions = new LinkedList<NewLegAction>();
        pathRerouteActions = new LinkedList<PathRerouteAction>();
        travelAlongSlopeActions = new LinkedList<TravelAlongSlopeAction>();
        pathBuiltActions = new LinkedList<PathBuiltAction>();
    }


    public void addNewLegAction(NewLegAction action)
    {

        newLegActions.AddLast(action);
    }

    public void addPathRereouteAction(PathRerouteAction action)
    {

        pathRerouteActions.AddLast(action);
    }


    public void addtravelAlongSlopeAction(TravelAlongSlopeAction action)
    {

        travelAlongSlopeActions.AddLast(action);
    }

    public void addPathBuiltAction(PathBuiltAction action)
    {

        pathBuiltActions.AddLast(action);

    }

    public LinkedList<DFPosition> getPath(DFPosition start, DFPosition destination, bool travelShip)
    {

        //Maybe make list instead for interrupt? then just append to full path
        //so for speed contemplate switching back to subPath then fullpath two diff lists, for quick access on interrupt.


        LinkedList<DFPosition> path = new LinkedList<DFPosition>();
        bool isAtDestination = false;
        MapsFile mapsFile = DaggerfallUnity.Instance.ContentReader.MapFileReader;


        DFPosition currPos = start;
        int pixelsAdded = 0;
        const double angleIncrement = 0.0523599;// 3 degrees 0.0436332; // 5 degrees in radians

        List<DFPosition> leg = new List<DFPosition>();
        while (!isAtDestination)
        {


            foreach (NewLegAction action in newLegActions)
            {
                action.Execute(leg, travelShip);
            }


            int[] cartesianVectorToDest = new int[2] { destination.X - currPos.X, destination.Y - currPos.Y };
            double[] polarVectorToDest;


            // Optimize angle modification to make deviations hug the coastline
            // as much as possible
            int angleSign = 0;




            //This may change over time.

            ///  const double angleIncrement = 1.745329e-5; //1 degree in radians, makes it so doesn't actually cross ocean, so angle is off.
            if (currPos.Y > destination.Y) // Counter-clockwise (ocean is below)
            {
                //On way back to daggerfall region this should be happenning, which may be too much?
                angleSign = 1;
            }
            else if (currPos.Y == destination.Y) // Depends on map zone
            {
                //Oh it may this case actually.
                // ToDo
                Debug.LogError("This is happening");
            }
            else // Clockwise (ocean is above)
            {
                angleSign = -1;
            }



            // Convert to polar to easily modify direction of vector
            if (cartesianVectorToDest[0] == 0)
            {
                polarVectorToDest = new double[] { Math.Sqrt(Math.Pow(cartesianVectorToDest[0], 2) + Math.Pow(cartesianVectorToDest[1], 2)), (Math.PI / 2) };
            }
            else
            {
                polarVectorToDest = new double[] { Math.Sqrt(Math.Pow(cartesianVectorToDest[0], 2) + Math.Pow(cartesianVectorToDest[1], 2)), Math.Atan((double)cartesianVectorToDest[1] / cartesianVectorToDest[0]) };
            }




            bool crossesOcean = false;

            while (true)
            {


                int run = 1;
                double rise = 1;


                int absoluteXDist = Math.Abs(cartesianVectorToDest[0]);


                if (cartesianVectorToDest[0] == 0)
                {
                    run = 0;

                }
                else
                {
                    rise = Math.Abs(((double)cartesianVectorToDest[1] / cartesianVectorToDest[0]));

                }
                if ((Math.Abs(absoluteXDist) == 2 || Math.Abs(absoluteXDist) == 2) && cartesianVectorToDest[1] != 0)
                {
                    rise = 1;
                }






                int xModifier = 1;
                int yModifier = 1;


                if (cartesianVectorToDest[0] < 0)
                {
                    xModifier = -1;
                }
                if (cartesianVectorToDest[1] < 0)
                {
                    yModifier = -1;
                }

                foreach (BuildingSteps.PathRerouteAction action in pathRerouteActions)
                {

                    action.Execute(run, rise);
                }



                int currX = currPos.X;
                double currY = currPos.Y;

                while (currX * xModifier < currPos.X + cartesianVectorToDest[0] || currY * yModifier < currPos.Y + cartesianVectorToDest[1])
                {

                    currX += run * xModifier;


                    currY += rise * yModifier;



                    //Finish cleaning up merge

                    int roundedY = (int)Math.Round(currY);

                    DFPosition mapPixel = new DFPosition(currX, roundedY);




                    int terrain = mapsFile.GetClimateIndex(mapPixel.X, mapPixel.Y);







                    path.AddLast(mapPixel);
                    leg.Add(mapPixel);

                    pixelsAdded += 1;



                    int prevX = mapPixel.X - (run * xModifier);
                    int prevY = mapPixel.Y - (int)Math.Round((rise * yModifier));
                    int nextX = mapPixel.X + (run * xModifier);
                    int nextY = mapPixel.Y + (int)Math.Round((rise * xModifier));



                    if (Math.Abs(mapPixel.X - prevX) > 1 || Math.Abs(mapPixel.Y - prevY) > 1)
                    {


                        if (destination.X >= prevX * xModifier && destination.X <= mapPixel.X * xModifier &&
                            destination.Y >= prevY * yModifier && destination.Y <= mapPixel.Y * yModifier)
                        {

                            isAtDestination = true;
                            break;
                        }
                    }
                    //But apparently never hits so maybe a bigger threshhold than just one.. What if slope?
                    if (mapPixel.X == destination.X && mapPixel.Y == destination.Y)
                    {

                        isAtDestination = true;
                        break;
                    }


                    if (mapPixel.X >= MapsFile.MaxMapPixelX || mapPixel.Y >= MapsFile.MaxMapPixelY || mapPixel.X < MapsFile.MinMapPixelX || mapPixel.Y < MapsFile.MinMapPixelY)
                    {
                        currX -= run * xModifier;
                        currY -= rise * yModifier;


                        //If out of bounds should also pop off subpath.
                        pixelsAdded--;
                        path.RemoveLast();

                        //currY = Math.Round(currY);
                        //Does this happen? It shouldn't with current destination.
                        break;
                    }

                    //So the time taken needs to include destination though.
                    if (terrain == (int)MapsFile.Climates.Ocean)
                    {
                        //No interrupts at all on ocean.
                        leg.Clear();

                        //Check within bounds of ocean pixel set to see if anything in that set is this pixel.
                        if (!travelShip)
                        {

                            crossesOcean = true;
                            break;
                        }


                    }

                    crossesOcean = false;

                }





                // If we're still crossing ocean, we need to increment the angle and try again
                // If not, we can stop looping because we've found a viable solution
                if (crossesOcean)
                {



                    //Pop pixels added form last.
                    for (int i = 0; i < pixelsAdded; ++i)
                    {
                        //For drawing incorrect vecotrs the stuff popped from here add to path.
                        path.RemoveLast();
                    }



                    polarVectorToDest[1] += angleSign * angleIncrement;

                    //Issue is getting back distance that's too big in magnitude that it doesn't make sense.
                    //Negative distance makes sense, but should not be more in magnitude than current position when origin is 0,0
                    cartesianVectorToDest[0] = (int)(polarVectorToDest[0] * Math.Cos(polarVectorToDest[1]));
                    cartesianVectorToDest[1] = (int)(polarVectorToDest[0] * Math.Sin(polarVectorToDest[1]));
                }
                else
                {
                    break;
                }
            }


            currPos.X = path.Last.Value.X;
            currPos.Y = path.Last.Value.Y;
        }


        foreach(PathBuiltAction action in pathBuiltActions)
        {
            action.Execute(path, travelShip);
        }


        return path;

    }

}
