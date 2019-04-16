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
using DaggerfallWorkshop.Game.UserInterfaceWindows;

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



        
        public int calculateTravelTime(DFPosition destination, bool speedCautious = false,
            bool sleepModeInn = false,
            bool travelShip = false,
            bool hasHorse = false,
            bool hasCart = false)
        {
            MapsFile mapsFile = DaggerfallUnity.Instance.ContentReader.MapFileReader;
            DFPosition nodeEndpoint; // The final position of a leg of travel
            DFPosition currPos = GetPlayerTravelPosition(); // This changes node per node
            List<DFPosition> path = new List<DFPosition>();
            
            bool isAtDestination = false;

            // Continually modifies vectors based on currPlayerPosition until an acceptable path is reached
            while (!isAtDestination)
            {
                // Obtain vector created between destination point and current position
                int[] cartesianVectorToDest = { destination.X - currPos.X, destination.Y - currPos.Y };

                /*
                 * We will increment the angle of the vector until we find a path
                 * that doesn't go through the ocean. The direction we increment
                 * the angle changes depending on what would most logically create
                 * a path that diverts a minimum amount while still avoiding the ocean.
                 */
                int angleSign = 0;
                const double angleIncrement = 0.174533; // 10 degrees in radians
                if (currPos.Y < destination.Y) // Counter-clockwise
                {
                    angleSign = 1;
                }
                else if (currPos.Y == destination.Y) // Depends on map zone
                {
                }

                else
                {
                    angleSign = -1;
                }

                double[] polarVectorToDest = { Math.Sqrt(Math.Pow(cartesianVectorToDest[0], 2) + Math.Pow(cartesianVectorToDest[1], 2)),
                    Math.Atan(cartesianVectorToDest[1] / cartesianVectorToDest[0]) };


                int currX;
                int currY;
                int xDistance;
                int yDistance;
                int furthestOfXAndY;
                int xDirection;
                int yDirection;
                int numMovements;
                // Modifies angle of travel vector until a vector that doesn't go through the ocean is found
                bool crossesOcean = true;

                //But because doing it at top, adding offset at start, so decrementing before so correct.

                while (true)
                {


                    // Verify if ocean is crossed
                    currX = currPos.X;
                    currY = currPos.Y;
                    xDistance = cartesianVectorToDest[0];
                    yDistance = cartesianVectorToDest[1];
                    furthestOfXAndY = Math.Max(xDistance, yDistance);
                    // Determine whether to increment or decrement x and y

                    //As is now if negative, results in positive, if positive results in positive.
                    //adding in negative
                    xDirection = cartesianVectorToDest[0] / cartesianVectorToDest[0];
                    yDirection = cartesianVectorToDest[1] / cartesianVectorToDest[1];
                    numMovements = 0;

                    // Once we've attained the furthest point, we know we've reached our goal
                    while (numMovements < furthestOfXAndY)
                    {
                        numMovements++;
                        // Increment distance
                        if (xDistance == furthestOfXAndY)
                        {
                            currX += xDirection;
                            if (currY != currPos.Y + yDistance)
                            {
                                currY += yDirection;
                            }
                        }
                        else
                        {
                            currY += yDirection;
                            if (currX != currPos.X + xDistance)
                            {
                                currX += xDirection;
                            }
                        }
                        // Check if we're in the ocean
                        if (mapsFile.GetClimateIndex(currX, currY) == (int)MapsFile.Climates.Ocean)
                        {
                            crossesOcean = true;
                            break;
                        }
                        // If we've made it this far, we have yet to cross the ocean
                        crossesOcean = false;
                        // Check if we're out of bounds
                        if (currX >= MapsFile.MaxMapPixelX || currY >= MapsFile.MaxMapPixelY)
                        {
                            break;
                        }
                    }

                    // If we're still crossing ocean, we need to increment the angle
                    //Only do this if still crossing ocean which isn't checked until later.
                    //so should be at top.
                    //     polarVectorToDest[1] += angleSign * angleIncrement;
                    if (crossesOcean)
                    {
                        polarVectorToDest[1] += angleSign * angleIncrement;
                    }
                    else
                    {
                        break;
                    }

                }
                // Convert back to cartesian to obtain our new endpoint
                cartesianVectorToDest[0] = (int)(polarVectorToDest[0] * Math.Cos(polarVectorToDest[1]));
                cartesianVectorToDest[1] = (int)(polarVectorToDest[0] * Math.Sin(polarVectorToDest[1]));



                currX = currPos.X;
                currY = currPos.Y;
                xDistance = cartesianVectorToDest[0];
                yDistance = cartesianVectorToDest[1];
                furthestOfXAndY = Math.Max(xDistance, yDistance);
                // Determine whether to increment or decrement x and y
                xDirection = cartesianVectorToDest[0] / cartesianVectorToDest[0];
                yDirection = cartesianVectorToDest[1] / cartesianVectorToDest[1];
                numMovements = 0;

                // Once we've attained the furthest point, we know we've reached our goal
                while (numMovements < furthestOfXAndY)
                {
                    numMovements++;
                    // Increment distance
                    if (xDistance == furthestOfXAndY)
                    {
                        currX += xDirection;
                        //Umm, we're updating the curr here, so this check not gonna work.
                        //compare to original offset by ydistance.
                        if (currY != currPos.Y + yDistance)
                        {
                            currY += yDirection;
                        }
                    }
                    else
                    {
                        currY += yDirection;
                        if (currX != currPos.X + xDistance)
                        {
                            currX += xDirection;
                        }
                    }
                    // Check if we're out of bounds
                    if (currX >= MapsFile.MaxMapPixelX || currY >= MapsFile.MaxMapPixelY)
                    {
                        // Backtrack a step to avoid being off the map
                        currX -= xDirection;
                        currY -= yDirection;
                        break;
                    }
                    if (currX == destination.X && currY == destination.Y)
                    {
                        break;
                    }
                }
            }


            int totalTravelTime = 1;
            return totalTravelTime;
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
            bool hasCart = false
            
           )
        {

            //Calling our version of calculation.





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


            List<DFPosition> pathOfTravel = new List<DFPosition>();


            pathOfTravel.Add(position);
         
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



                pathOfTravel.Add(new DFPosition(playerXMapPixel, playerYMapPixel));

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
         //   GameObject.Instantiate(drawer);
            if (!speedCautious)
                minutesTakenTotal = minutesTakenTotal >> 1;


            //Where should I do the call then?
            DaggerfallTravelMapWindow window = DaggerfallUI.Instance.DfTravelMapWindow;
            window.drawLineOfTravel(pathOfTravel);

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
