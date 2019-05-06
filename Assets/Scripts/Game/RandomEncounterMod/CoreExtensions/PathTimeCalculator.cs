using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallConnect.Utility;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using System;
using DaggerfallWorkshop.Game;



//So main difference between this and other, is that it uses our PathBuilder, and assigns callbacks accordingly.
public class PathTimeCalculator : TravelTimeCalculator, PathBuilder.NewLegAction
{


    int totalTime;
    int transportModifier;
    bool sleepModeInn;



    //For calculating days for interrupt and caching.
    public LinkedList<DFPosition> lastComputedPath = new LinkedList<DFPosition>();

    //Either store here for quick and dirty or let execute call a method already here that computes leg.
    int minutesForLastLeg = 0;

    void addOceanPixels()
    {
        pixelsTraveledOnOcean++;
    }

    public int computeStep(DFPosition pos)
    {
        int minutesTakenThisMove;

        MapsFile mapsFile = DaggerfallUnity.Instance.ContentReader.MapFileReader;

        int terrain = mapsFile.GetClimateIndex(pos.X, pos.Y);

        int terrainMovementIndex = 0;

        int climateIndex = terrain - (int)MapsFile.Climates.Ocean;
        terrainMovementIndex = climateIndices[climateIndex];

        //This multiplied by slope to make this accurate time, or multiplied by difference, ladder more accurate.
        minutesTakenThisMove = (((102 * transportModifier) >> 8)
            * (256 - terrainMovementModifiers[terrainMovementIndex] + 256)) >> 8;


        //This may need to be param, but for this instance should be fine.
        if (!sleepModeInn)
            minutesTakenThisMove = (300 * minutesTakenThisMove) >> 8;


        return minutesTakenThisMove;
    }


    //Could just use last computed but for clarity.
    public int CalculateTime(LinkedList<DFPosition> path, DFPosition dest)
    {
        int totalTravelTime = 0;


        DFPosition prev = null;

        foreach (DFPosition pos in path)
        {
            
            if (pos.X == dest.X && pos.Y == dest.Y)
            {
                break;
            }


            int minutesTakenThisMove = computeStep(pos);




            if (prev != null)
            {
                //Cause no skips done in x, drawing no big deal, but time must be accurate.
                if (prev.Y - pos.Y != 0)
                    minutesTakenThisMove *= Math.Abs(prev.Y - pos.Y);
            }

            totalTravelTime += minutesTakenThisMove;
        }
        return totalTravelTime;
    }

    public void Execute(List<DFPosition> leg, bool travelShip)
    {


        int minutesTakenThisLeg = 0;

        DFPosition prev = null;
        MapsFile mapsFile = DaggerfallUnity.Instance.ContentReader.MapFileReader;


        foreach (DFPosition position in leg)
        {

          

            int minutesTakenThisMove = 0;



            int terrain = mapsFile.GetClimateIndex(position.X, position.Y);


            //So the time taken needs to include destination though.
            if (terrain == (int)MapsFile.Climates.Ocean)
            {


                if (travelShip)
                {
                    //Otherwise if on ocean and travelling by ship, add this for travel cost.
                    ++pixelsTraveledOnOcean;
                    //Otherwise of on ocean and travelling by ship it is this.
                    minutesTakenThisMove = 51;
                }
            }
            else
            {
                minutesTakenThisMove = computeStep(position);
              

                if (prev != null)
                {
                    //Cause no skips done in x, drawing no big deal, but time must be accurate.
                    if (prev.Y - position.Y != 0)
                        minutesTakenThisMove *= Math.Abs(prev.Y - position.Y);
                }

                minutesTakenThisLeg += minutesTakenThisMove;

                prev = position;
            }


            //Problem is dependant on order
            this.minutesForLastLeg = minutesTakenThisLeg;

        }

        totalTime += minutesTakenThisLeg;

    }




    public override int CalculateTravelTime(DFPosition endPos,
         bool speedCautious = false,
         bool sleepModeInn = false,
         bool travelShip = false,
         bool hasHorse = false,
         bool hasCart = false)
    {


        //Then call buildPath

        //In complete version access via manger.

        transportModifier = 256;
        if (hasHorse)
            transportModifier = 128;
        else if (hasCart)
            transportModifier = 192;

        this.sleepModeInn = sleepModeInn;

        //Then before this, see if end of path and beginning path rqual to start and beginning

        DFPosition start = GetPlayerTravelPosition();


        //Interrupt won't happen though if do it like that.

        /*
        if (this.lastComputedPath != null && this.lastComputedPath.Count != 0 &&
            this.lastComputedPath.First.Value.X == start.X && this.lastComputedPath.First.Value.Y == start.Y && this.lastComputedPath.Last.Value.X == endPos.X &&
            this.lastComputedPath.Last.Value.Y == endPos.Y)
        {
            //Return the time calculated before.
            return totalTime;
        }
        */
        totalTime = 0;

        //Update to take in everything as needed later for actual pathing.

        LinkedList<DFPosition> path = DaggerfallRandomEncountersMod.RandomEncounterManager.Instance.PathBuilder.getPath(start, endPos, travelShip);



        //Not used for anything anymorer ight now, hindsight 20/20
        this.lastComputedPath = path;

        if (!speedCautious)
        {
            totalTime = totalTime >> 1;
        }



        //Primitive should be by copy.
        int timeToReturn = totalTime;

        return timeToReturn;

    }
}
