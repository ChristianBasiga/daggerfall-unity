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

                // Optimize angle modification to make deviations hug the coastline
                // as much as possible
                int angleSign = 0;
                const double angleIncrement = 0.174533; // 10 degrees in radians
                if (currPos.Y < destination.Y) // Counter-clockwise (ocean is below)
                {
                    angleSign = 1;
                }
                else if (currPos.Y == destination.Y) // Depends on map zone
                {
                    // ToDo
                }
                else // Clockwise (ocean is above)
                {
                    angleSign = -1;
                }

                // Convert to polar to easily modify direction of vector
                double[] polarVectorToDest = { Math.Sqrt(Math.Pow(cartesianVectorToDest[0], 2) + Math.Pow(cartesianVectorToDest[1], 2)),
                    Math.Atan(cartesianVectorToDest[1] / cartesianVectorToDest[0]) };

                // Variables used to follow along path of vector
                int currX; // We are we along the vector now?
                int currY;
                int xDistance; // How far is there to go in this direction?
                int yDistance;
                int furthestOfXAndY;
                int xDirection; // Negative or positive increments?
                int yDirection;
                int numMovements; // How far have we gone?

                /*
                 * This loop checks if the travel vector:
                 *  a) Crosses ocean
                 *  b) Reaches original destination
                 *  c) Goes out of map bounds
                 * If a), we must try a new vector that avoids the ocean.
                 * If b), we have a valid travel vector and can stop the whole process here.
                 * If c), we reduce the length of the vector to keep it in bounds,
                 *  resulting in a valid leg of the journey, and must compute the
                 *  other legs.
                 * If none of the above, we have a valid leg of the journey, and must
                 *  compute the other legs.
                 */
                bool crossesOcean = true;
                while (true)
                {
                    // Verify if ocean is crossed
                    currX = currPos.X;
                    currY = currPos.Y;
                    xDistance = cartesianVectorToDest[0];
                    yDistance = cartesianVectorToDest[1];
                    furthestOfXAndY = Math.Max(xDistance, yDistance);
                    // Determine whether to increment or decrement x and y

                    // Should we increase or decrease x to get to destination's x?
                    if(cartesianVectorToDest[0] >= 0)
                    {
                        xDirection = 1;
                    } else
                    {
                        xDirection = -1;
                    }
                    // Should we increase or decrease y to get to destination's y?
                    if (cartesianVectorToDest[1] >= 0)
                    {
                        yDirection = 1;
                    }
                    else
                    {
                        yDirection = -1;
                    }

                    // If we've moved as many pixels as is along the furthest vector
                    // component, then we've completely followed the vector
                    numMovements = 0;
                    while (numMovements < furthestOfXAndY)
                    {
                        numMovements++;
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

                        // If we've reached our original destination, all necessary legs are computed
                        if (currX == destination.X && currY == destination.Y)
                        {
                            isAtDestination = true;
                            break;
                        }

                        // If ocean, invalid vector, time to modify
                        if (mapsFile.GetClimateIndex(currX, currY) == (int)MapsFile.Climates.Ocean)
                        {
                            crossesOcean = true;
                            break;
                        }

                        // If we've made it this far, we have yet to cross the ocean
                        crossesOcean = false;
                        // If out of bounds, backtrack a bit to get a point that's in bounds
                        if (currX >= MapsFile.MaxMapPixelX || currY >= MapsFile.MaxMapPixelY)
                        {
                            currX -= xDirection;
                            currY -= yDirection;
                            break;
                        }
                    }

                    // If we're still crossing ocean, we need to increment the angle and try again
                    // If not, we can stop looping because we've found a viable solution
                    if (crossesOcean)
                    {
                        polarVectorToDest[1] += angleSign * angleIncrement;
                        cartesianVectorToDest[0] = (int)(polarVectorToDest[0] * Math.Cos(polarVectorToDest[1]));
                        cartesianVectorToDest[1] = (int)(polarVectorToDest[0] * Math.Sin(polarVectorToDest[1]));
                    }
                    else
                    {
                        break;
                    }
                }

                // Convert back to cartesian to obtain our new endpoint
                cartesianVectorToDest[0] = (int)(polarVectorToDest[0] * Math.Cos(polarVectorToDest[1]));
                cartesianVectorToDest[1] = (int)(polarVectorToDest[0] * Math.Sin(polarVectorToDest[1]));
                // Update new endpoint, this is now a node along our journey
                currPos.X += cartesianVectorToDest[0];
                currPos.Y += cartesianVectorToDest[1];
                path.Add(new DFPosition(currPos.X, currPos.Y));
            }

            if(path.Count != 0)
            {
                String pathToString = "Path is: ";
                for (int i = 0; i < path.Count; i++)
                {
                    pathToString += "(" + path[0].X + ", " + path[0].Y + ") ";
                }
                Debug.LogError(pathToString);
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
            return calculateTravelTime(endPos, speedCautious, sleepModeInn, travelShip, hasHorse, hasCart);

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
         //   GameObject.Instantiate(drawer);
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
