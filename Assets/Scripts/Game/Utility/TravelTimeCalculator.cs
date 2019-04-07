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
        byte[] terrainMovementModifiers = { 240, 220, 200, 200, 230, 250 };

        // Taverns only accept gold pieces, compute those separately
        protected int piecesCost = 0;
        protected int totalCost = 0;

        // Used in calculating travel cost
        int pixelsTraveledOnOcean = 0;

        #endregion

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



        //get path of Travel.
        //Overflow doesn't happen so base case is hit, but just takes too long
        //Will wait till completes to see.
        public int calculateTravelTime(DFPosition destination, bool speedCautious = false,
            bool sleepModeInn = false,
            bool travelShip = false,
            bool hasHorse = false,
            bool hasCart = false)
        {

            DFPosition playerPosition = GetPlayerTravelPosition();
            Stack<DFPosition> pathOfTravel = new Stack<DFPosition>();

            bool[,] invalid = new bool[MapsFile.MaxMapPixelX, MapsFile.MaxMapPixelY];


            List<DFPosition> simplePath = new List<DFPosition>();

            CalculateTravelTime(destination, speedCautious, sleepModeInn, travelShip, hasHorse, hasCart, simplePath);


            //Okay, then use simple path to hone in on path.


            GetPathOfTravelUtil(simplePath[0], destination, pathOfTravel, invalid);


            //Once get it here, can re use this calculateTravelTime, passing in each position here.

            int totalTravelTime = 0;

            foreach (DFPosition pos in pathOfTravel)
            {

                //Since the positons will be adjacent to each other loop in travel time calculation should only iterate once.
                totalTravelTime += CalculateTravelTime(pos, speedCautious, sleepModeInn, travelShip, hasHorse, hasCart, null);


                DaggerfallWorkshop.Utility.ContentReader.MapSummary mapSumm = new DaggerfallWorkshop.Utility.ContentReader.MapSummary();

                bool hasLocation = DaggerfallUnity.Instance.ContentReader.HasLocation(pos.X, pos.Y, out mapSumm);

                if (hasLocation)
                {

                    //To Confirm we actually get to destination.
                    Debug.LogError("Location currently looking at " + mapSumm.ToString());
                }


            }

            Debug.LogError("Total Travel time " + totalTravelTime);

            return totalTravelTime;




        }

        
        //Nothing should need to change here, just what starting at,
        //May need to divide and conquer this still.
        /*
         * Algorithm to divide and conquer with threads.
         * Create ThreadPool that will be used to invoke attempts in different directions.
         * A thread disposes of itself as soon as made invalid move.
         *
         * Each parent thread will sleep after spawning respective thread, then when a thread
         * either hits a base case, interrupt method will be called and parent notified.
         *
         *
         */
        public static bool GetPathOfTravelUtil(DFPosition current, DFPosition destination, Stack<DFPosition> pathOfTravel, bool [,] invalid)
        {

            //Recursively finds path of travel, when hit ocean return and try different path of travel.


            //If way over it gotta cnvery system first.
            Debug.LogError("Looking at position " + current.ToString());


            //Should check if offset is even a thing.

            pathOfTravel.Push(current);

            int terrain = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetClimateIndex(current.X, current.Y);

            //If on ocean, then not traversable path, pop and return.
            if (terrain == (int)MapsFile.Climates.Ocean || invalid[current.X,current.Y] || 
                (current.X < MapsFile.MinMapPixelX || current.X > MapsFile.MaxMapPixelX ||  current.Y < MapsFile.MinMapPixelY  || current.Y > MapsFile.MaxMapPixelY))
            {

                //Maybe get into world coords first then make the check.

                //The bounds checking should fix stack overflow, fuck.

              
                invalid[current.X,current.Y] = true;

                pathOfTravel.Pop();
                return false;

            }
            else if (current == destination)
            {

                return true;
            }
            else
            {

                pathOfTravel.Push(current);

                //Otherwise this is good spot, return from here.

                //try all directions, this probably needs to be modified, since pretty fucking brute force.
                //Should find a path based on naive path of travel, or do dijkstras instead of just brute force.



                    
                //Conver all these to threads.
                DFPosition toRight = new DFPosition(current.X + 1, current.Y);
                DFPosition toLeft = new DFPosition(current.X - 1, current.Y);
                DFPosition forward = new DFPosition(current.X, current.Y + 1);
                DFPosition backward = new DFPosition(current.X, current.Y - 1);

                DFPosition FR = new DFPosition(toRight.X, forward.Y);
                DFPosition FL = new DFPosition(toLeft.X, forward.Y);

                DFPosition BR = new DFPosition(toRight.X, backward.Y);
                DFPosition BL = new DFPosition(toLeft.X, backward.Y);



                //Okay, so create tasks invoking for each possible move.

                //Then call a wait any, so as soon as a child thread finishes, let's say if was a path
                //that made it. Then using the result method in task we check if true. If true, then cancel
                //the other threads, cause no reason.

                //But if was invalid path, then let rest of threads continue by invoking wait any again
                //So essentially waitany until all child threads dead.

                //Also for shortest path down line, need to figure out adding weight to the different nodes.
                //perhaps they already have weight established I could use that to consider smaller subset.
              
                return GetPathOfTravelUtil(toRight, destination, pathOfTravel, invalid) ||
                GetPathOfTravelUtil(toLeft, destination, pathOfTravel, invalid) ||
                GetPathOfTravelUtil(forward, destination, pathOfTravel, invalid) ||
                GetPathOfTravelUtil(backward, destination, pathOfTravel, invalid) ||
                GetPathOfTravelUtil(BR, destination, pathOfTravel, invalid) ||
                GetPathOfTravelUtil(BL, destination, pathOfTravel, invalid) ||
                GetPathOfTravelUtil(FR, destination, pathOfTravel, invalid) ||
                GetPathOfTravelUtil(FL, destination, pathOfTravel, invalid);


            }

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
        public int CalculateTravelTime(DFPosition endPos,
            bool speedCautious = false,
            bool sleepModeInn = false,
            bool travelShip = false,
            bool hasHorse = false,
            bool hasCart = false,
            
            List<DFPosition> simplePath = null)
        {

            //Calling our version of calculation.

            //Quick & Dirty
            if (simplePath == null)
            {
                return calculateTravelTime(endPos, speedCautious, sleepModeInn, travelShip, hasCart, hasCart);
            }

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


                DFPosition offsetPos = new DFPosition();

                offsetPos.X = playerXMapPixel;
                offsetPos.Y = playerYMapPixel;

                simplePath.Add(offsetPos);

                //Debug.log(positionX);


                int terrainMovementIndex = 0;
                int terrain = mapsFile.GetClimateIndex(playerXMapPixel, playerYMapPixel);

                //Need to update this so doesn't just do direct distance from point A to point B, but considers travelling around the coast.
                //not through the ocean.
                if (terrain == (int)MapsFile.Climates.Ocean)
                {

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

            if (!speedCautious)
                minutesTakenTotal = minutesTakenTotal >> 1;

            return minutesTakenTotal;
        }

        private void tryInterrupt(DFPosition start, DFPosition end, int pixelX, int pixelY, int timeTravelled)
        {

            //Pixel offset makes it so it is along line of travel at the very least.
            bool doInterrupt = (UnityEngine.Random.Range(1, 101) & 1) == 0;

           
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
