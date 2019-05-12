// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2018 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Lypyl (lypyl@dfworkshop.net)
// Contributors:    Gavin Clayton (interkarma@dfworkshop.net)
// 
// Notes:
//

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallConnect.Utility;
using DaggerfallConnect.Arena2;

namespace DaggerfallWorkshop.Game.Utility
{
    /// <summary>
    /// Helper to calculate overland travel time for travel map and Clock resource.
    /// Travel time needs to be coordinated between these systems for quests to provide a
    /// realistic amount of time for player to complete quest.
    /// 
    /// </summary>
    public class TravelTimeCalculator
    {
        #region Fields

        // Gives index to use with terrainMovementModifiers[]. Indexed by terrain type, starting with Ocean at index 0.
        // Also used for getting climate-related indices for dungeon textures.
        public static byte[] climateIndices = { 0, 0, 0, 1, 2, 3, 4, 5, 5, 5 };

        // Gives movement modifiers used for different terrain types.
        protected byte[] terrainMovementModifiers = { 240, 220, 200, 200, 230, 250 };

        // Taverns only accept gold pieces, compute those separately
        protected int piecesCost = 0;
        protected int totalCost = 0;

        // Used in calculating travel cost
        protected int pixelsTraveledOnOcean = 0;

        #endregion
        Dictionary<int, Dictionary<int, List<DFPosition>>> bTreeOceanPixels;
        List<DFPosition> oceanPixels;
        #region Public Methods


        public class InterruptFastTravel
        {

            public DFPosition interruptPosition;
            public int daysTaken;

        }

        InterruptFastTravel interrupt;

        public InterruptFastTravel Interrupt
        {

            get
            {
                return interrupt;
            }
        }

        public void useInterrupt()
        {

            interrupt = null;
        }



        public int getGCD(int a, int b)
        {


            if (a == 0)
                return b;

            return getGCD(b % a, a);
        }

        public int[] getReducedFraction(int numer, int denom)
        {

            int[] reduced = new int[2];

            int gcd = getGCD(Math.Abs(numer), Math.Abs(denom));


            reduced[0] = numer / gcd;
            reduced[1] = denom / gcd;


            return reduced;
        }





        void initBTree()
        {
            bTreeOceanPixels = new Dictionary<int, Dictionary<int, List<DFPosition>>>();

            //Initialize layers.
            //Loop through all pixels to gather ocean pixles, in future just do this once at start.
            oceanPixels = new List<DFPosition>();
            MapsFile mapsFile = DaggerfallUnity.Instance.ContentReader.MapFileReader;


            for (int i = 0; i < MapsFile.MaxMapPixelY; ++i)
            {

                int yKey = getYKey(i);

                for (int j = 0; j < MapsFile.MaxMapPixelX; ++j)
                {


                    //Only do this if ocean ideally, but to compute same one across multiple xes

                    //If ocean, store in respective list on b tree.
                    if (mapsFile.GetClimateIndex(j, i) == (int)MapsFile.Climates.Ocean)
                    {
                        // Debug.LogError("here");

                        //Could do this just once, but need to do this only if ocean.
                        //hmm


                        int xKey = getXKey(j);


                        //Lazy load it as amy not need some lists in be tree.

                        if (!bTreeOceanPixels.ContainsKey(yKey))
                        {
                            bTreeOceanPixels[yKey] = new Dictionary<int, List<DFPosition>>();
                        }

                        if (!bTreeOceanPixels[yKey].ContainsKey(xKey))
                        {

                            bTreeOceanPixels[yKey][xKey] = new List<DFPosition>();
                        }

                        oceanPixels.Add(new DFPosition(j, i));
                        bTreeOceanPixels[yKey][xKey].Add(new DFPosition(j, i));

                    }
                }
            }

        }

        public int getYKey(int pixel)
        {

            int yKey = 400;
            //Store within ranges.
            if (pixel <= 50)
            {
                yKey = 0;
            }
            else if (pixel <= 100)
            {
                yKey = 50;
            }
            else if (pixel <= 150)
            {
                yKey = 100;
            }
            else if (pixel <= 200)
            {
                yKey = 150;
            }
            else if (pixel <= 250)
            {
                yKey = 200;
            }
            else if (pixel <= 300)
            {
                yKey = 250;
            }
            else if (pixel <= 350)
            {
                yKey = 300;
            }
            else if (pixel <= 400)
            {
                yKey = 350;
            }

            return yKey;
        }

        public int getXKey(int pixel)
        {

            int xKey = 950;
            //Then x in ranges fo 200
            //ranges of 100 instead, there is prob smarter way than else ifs.
            if (pixel <= 50)
            {
                xKey = 0;
            }
            else if (pixel <= 100)
            {
                xKey = 50;
            }
            else if (pixel <= 150)
            {
                xKey = 100;
            }
            else if (pixel <= 200)
            {
                xKey = 150;
            }
            else if (pixel <= 250)
            {
                xKey = 200;
            }
            else if (pixel <= 300)
            {
                xKey = 250;
            }
            else if (pixel <= 350)
            {
                xKey = 300;
            }
            else if (pixel <= 400)
            {
                xKey = 350;
            }
            else if (pixel <= 450)
            {
                xKey = 400;
            }
            else if (pixel <= 500)
            {
                xKey = 450;
            }
            else if (pixel <= 550)
            {
                xKey = 500;
            }
            else if (pixel <= 600)
            {
                xKey = 550;
            }
            else if (pixel <= 650)
            {
                xKey = 600;
            }
            else if (pixel <= 700)
            {
                xKey = 650;
            }
            else if (pixel <= 750)
            {
                xKey = 700;
            }
            else if (pixel <= 800)
            {
                xKey = 750;
            }
            else if (pixel <= 850)
            {
                xKey = 800;
            }
            else if (pixel <= 900)
            {
                xKey = 850;
            }
            else if (pixel <= 950)
            {
                xKey = 900;
            }
            else if (pixel <= 1000)
            {
                xKey = 950;
            }


            return xKey;
        }

       

        /// <summary>Gets current player position in map pixels for purposes of travel</summary>
        public static DFPosition GetPlayerTravelPosition()
        {
            DFPosition position = new DFPosition();
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
            TransportManager transportManager = GameManager.Instance.TransportManager;
            if (playerGPS && !transportManager.IsOnShip())
                position = playerGPS.CurrentMapPixel;
            else
                position = MapsFile.WorldCoordToMapPixel(transportManager.BoardShipPosition.worldPosX, transportManager.BoardShipPosition.worldPosZ);
            return position;
        }

        /// <summary>
        /// Creates a path from player's current location to destination and
        /// returns minutes taken to travel.
        /// </summary>
        /// <param name="endPos">Endpoint in map pixel coordinates.</param>
        public virtual int CalculateTravelTime(DFPosition endPos,
            bool speedCautious = false,
            bool sleepModeInn = false,
            bool travelShip = false,
            bool hasHorse = false,
            bool hasCart = false
           //   DFPosition startPosition = null,
           //  List<DFPosition> truePath = null
           )
        {

            //Calling our version of calculation.
            /*if(startPosition == null)
            {
            }*/


            //If travel by ship just do there own, unless we want to add that check for ours as well.
            //Since theirs doesn't travel along slope.
            //but time wise travel slope or not, their algorithm gets to similiar value.





            int transportModifier = 0;
            if (hasHorse)
                transportModifier = 128;
            else if (hasCart)
                transportModifier = 192;
            else
                transportModifier = 256;



            DFPosition position = GetPlayerTravelPosition();




            int playerXMapPixel = position.X;
            int playerYMapPixel = position.Y;
            int distanceXMapPixels = endPos.X - playerXMapPixel;
            int distanceYMapPixels = endPos.Y - playerYMapPixel;
            int distanceXMapPixelsAbs = Mathf.Abs(distanceXMapPixels);
            int distanceYMapPixelsAbs = Mathf.Abs(distanceYMapPixels);
            int furthestOfXandYDistance = 0;


            if (distanceXMapPixelsAbs <= distanceYMapPixelsAbs)
                furthestOfXandYDistance = distanceYMapPixelsAbs;
            else
                furthestOfXandYDistance = distanceXMapPixelsAbs;

            int xPixelMovementDirection = (distanceXMapPixels >= 0) ? 1 : -1;
            int yPixelMovementDirection = (distanceYMapPixels >= 0) ? 1 : -1;

            int numberOfMovements = 0;
            int shorterOfXandYDistanceIncrementer = 0;

            int minutesTakenThisMove = 0;
            int minutesTakenTotal = 0;

            MapsFile mapsFile = DaggerfallUnity.Instance.ContentReader.MapFileReader;
            pixelsTraveledOnOcean = 0;

            LinkedList<DFPosition> path = new LinkedList<DFPosition>();







            //Basically only if we've moved less than the furthest distance.
            while (numberOfMovements < furthestOfXandYDistance)
            {
                if (furthestOfXandYDistance == distanceXMapPixelsAbs)
                {
                    playerXMapPixel += xPixelMovementDirection;
                    shorterOfXandYDistanceIncrementer += distanceYMapPixelsAbs;

                    if (shorterOfXandYDistanceIncrementer > distanceXMapPixelsAbs)
                    {
                        shorterOfXandYDistanceIncrementer -= distanceXMapPixelsAbs;
                        playerYMapPixel += yPixelMovementDirection;
                    }
                }
                else
                {
                    playerYMapPixel += yPixelMovementDirection;
                    shorterOfXandYDistanceIncrementer += distanceXMapPixelsAbs;

                    if (shorterOfXandYDistanceIncrementer > distanceYMapPixelsAbs)
                    {
                        shorterOfXandYDistanceIncrementer -= distanceYMapPixelsAbs;
                        playerXMapPixel += xPixelMovementDirection;
                    }
                }





                //Debug.log(positionX);


                int terrainMovementIndex = 0;
                int terrain = mapsFile.GetClimateIndex(playerXMapPixel, playerYMapPixel);

                if (terrain == (int)MapsFile.Climates.Ocean)
                {
                    //After going through our algo it shouldn't do this anymore.
                    //So here in this process it knows it's hitting ocean
                    //but in second run of ours it does not know.
                    Debug.LogError("ocean");
                    //So instead of just increasing time if ocean tile, should redirect to a new path.
                    ++pixelsTraveledOnOcean;
                    if (travelShip)
                        minutesTakenThisMove = 51;
                    else
                        minutesTakenThisMove = 255;
                }
                else
                {



                    terrainMovementIndex = climateIndices[terrain - (int)MapsFile.Climates.Ocean];
                    minutesTakenThisMove = (((102 * transportModifier) >> 8)
                        * (256 - terrainMovementModifiers[terrainMovementIndex] + 256)) >> 8;
                }



                if (!sleepModeInn)
                    minutesTakenThisMove = (300 * minutesTakenThisMove) >> 8;


                minutesTakenTotal += minutesTakenThisMove;
                ++numberOfMovements;





                //Only if interrupt not set yet by random chance, try again.
                //Or should I try infinitely?
                //if (interrupt == null)
                //  tryInterrupt(position, endPos, playerXMapPixel, playerYMapPixel, minutesTakenTotal);

            }



            //   GameObject.Instantiate(drawer);
            if (!speedCautious)
                minutesTakenTotal = minutesTakenTotal >> 1;

            return minutesTakenTotal;
        }


        //Oo how rough, lol time travelled IS needed here tho. Hmmmmmm
        //Well if it interrupted I can say did interrupt, then access the last done addition in calculator.
        private void tryInterrupt(int pixelX, int pixelY, int timeTravelled)
        {

            //Maybe with this can call their algorithm strictly for time?


            //Pixel offset makes it so it is along line of travel at the very least.
            //Obviously more interrupts should be possible and also should be via the engine.
            bool doInterrupt = (UnityEngine.Random.Range(0, 101) < 5);


            DaggerfallWorkshop.Utility.ContentReader.MapSummary mapSumm = new DaggerfallWorkshop.Utility.ContentReader.MapSummary();
            bool hasLocation = DaggerfallUnity.Instance.ContentReader.HasLocation(pixelX, pixelY, out mapSumm);

            Debug.LogError(String.Format("There is location at end pos {0}, {1}", hasLocation, mapSumm.ToString()));

            //if (hasLocation) return;


            if (doInterrupt)
            {
                if (interrupt == null)
                {
                    interrupt = new InterruptFastTravel();
                    interrupt.interruptPosition = new DFPosition();
                }



                interrupt.interruptPosition.X = pixelX;
                interrupt.interruptPosition.Y = pixelY;
                // Players can have fast travel benefit from guild memberships
                timeTravelled = GameManager.Instance.GuildManager.FastTravel(timeTravelled);

                int travelTimeDaysTotal = (timeTravelled / 1440);

                // Classic always adds 1. For DF Unity, only add 1 if there is a remainder to round up.
                if ((timeTravelled % 1440) > 0)
                    travelTimeDaysTotal += 1;

                interrupt.daysTaken = travelTimeDaysTotal;
                return;
            }

            /*
            DFRegion region = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetRegion(mapSumm.RegionIndex);
        


            int randomLocationIndex = UnityEngine.Random.Range(0, region.MapNames.Length);


            //Need to get locations in current region, then filter only those whose pixels are within
            //start and ending positions.
            DFLocation newLocation = new DFLocation();
          //  DaggerfallUnity.Instance.ContentReader.GetLocation(mapSumm.RegionIndex, 5, out newLocation);

            DFPosition[] alongPathOfTravel = new DFPosition[region.LocationCount];
            int found = 0;
           
            for (int i = 0; i < region.LocationCount; ++i)
            {

                DFPosition mapPixel = MapsFile.LongitudeLatitudeToMapPixel(newLocation.MapTableData.Longitude, newLocation.MapTableData.Latitude);

                //Check if within x.
                Was really overthnking this lmao.


                //check if within y.

            }
            

            if (newLocation.Loaded)
            {

                Debug.LogError("From DF Location using terrain data location name " + newLocation.Name);
                Debug.LogError("From DF Location using terrain data region name" + newLocation.RegionName);

                DFPosition mapPixel = MapsFile.LongitudeLatitudeToMapPixel(newLocation.MapTableData.Longitude, newLocation.MapTableData.Latitude);

                Debug.LogError("New map pixel " + mapPixel.ToString());
                this.interruptPosition = mapPixel;


                //Instead of teleporting here, does teleport after they click travel.
                //GameManager.Instance.StreamingWorld.TeleportToCoordinates(mapPixel.X, mapPixel.Y, StreamingWorld.RepositionMethods.DirectionFromStartMarker);
            }
            else
            {

                Debug.LogError("Invalid location");
            }
           */

        }

        public void CalculateTripCost(int travelTimeInMinutes, bool sleepModeInn, bool hasShip, bool travelShip)
        {
            int travelTimeInHours = (travelTimeInMinutes + 59) / 60;
            piecesCost = 0;
            if (sleepModeInn && !GameManager.Instance.GuildManager.GetGuild(FactionFile.GuildGroups.KnightlyOrder).FreeTavernRooms())
            {
                piecesCost = 5 * ((travelTimeInHours - pixelsTraveledOnOcean) / 24);
                if (piecesCost < 0)     // This check is absent from classic. Without it travel cost can become negative.
                    piecesCost = 0;
                piecesCost += 5;        // Always at least one stay at an inn
            }
            totalCost = piecesCost;
            if ((pixelsTraveledOnOcean > 0) && !hasShip && travelShip)
                totalCost += 25 * (pixelsTraveledOnOcean / 24 + 1);
        }

        public int PiecesCost { get { return piecesCost; } }
        public int TotalCost { get { return totalCost; } }
        #endregion
    }
}
