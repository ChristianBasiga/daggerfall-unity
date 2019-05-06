using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallConnect.Utility;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using System;



//So main difference between this and other, is that it uses our PathBuilder, and assigns callbacks accordingly.
public class PathTimeCalculator : TravelTimeCalculator, PathBuilder.TravelAlongSlopeAction
{


    int totalTime;
    int transportModifier;

    void addOceanPixels()
    {
        pixelsTraveledOnOcean++;
    }

    public void Execute(DFPosition mapPixel, DFPosition prev, bool travelShip)
    {

        //SO here it does the step.
        MapsFile mapsFile = DaggerfallUnity.Instance.ContentReader.MapFileReader;
        int minutesTakenThisMove = 0;

        int terrain = mapsFile.GetClimateIndex(mapPixel.X, mapPixel.Y);


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

            int terrainMovementIndex = 0;

            int climateIndex = terrain - (int)MapsFile.Climates.Ocean;
            terrainMovementIndex = climateIndices[climateIndex];

            //This multiplied by slope to make this accurate time, or multiplied by difference, ladder more accurate.
            minutesTakenThisMove = (((102 * transportModifier) >> 8)
                * (256 - terrainMovementModifiers[terrainMovementIndex] + 256)) >> 8;




            //Cause no skips done in x, drawing no big deal, but time must be accurate.
            if (prev.Y - mapPixel.Y != 0)
                minutesTakenThisMove *= Math.Abs(prev.Y - mapPixel.Y);
        }

        totalTime += minutesTakenThisMove;

    }




    public override int CalculateTravelTime(DFPosition endPos,
         bool speedCautious = false,
         bool sleepModeInn = false,
         bool travelShip = false,
         bool hasHorse = false,
         bool hasCart = false)
    {

        totalTime = 0;

        //Then call buildPath

        //In complete version access via manger.

        transportModifier = 256;
        if (hasHorse)
            transportModifier = 128;
        else if (hasCart)
            transportModifier = 192;


        //Update to take in everything as needed later for actual pathing.
        LinkedList<DFPosition> path = DaggerfallRandomEncountersMod.RandomEncounterManager.Instance.PathBuilder.getPath(GetPlayerTravelPosition(), endPos, travelShip);



        return totalTime;

    }
}
