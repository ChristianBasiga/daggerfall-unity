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




            //Converting current position to world coords.
            currPos = MapsFile.MapPixelToWorldCoord(currPos.X, currPos.Y);
            DFPosition pixelDestination = destination;
            destination = MapsFile.MapPixelToWorldCoord(destination.X, destination.Y);




            // Continually modifies vectors based on currPlayerPosition until an acceptable path is reached
            while (!isAtDestination)
            {

                // Obtain vector created between destination point and current position
                //This is same.
                int[] cartesianVectorToDest = { destination.X - currPos.X, destination.Y - currPos.Y };

                // Optimize angle modification to make deviations hug the coastline
                // as much as possible
                int angleSign = 0;
             //    const double angleIncrement = 0.610865; // 20 degrees in radians, 35
                //const double angleIncrement = 0.523599;//30
                // const double angleIncrement = 0.00174533;
                 const double angleIncrement = 0.0872665; // 5 degrees in radians
               ///  const double angleIncrement = 1.745329e-5; //1 degree in radians, makes it so doesn't actually cross ocean, so angle is off.
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

                double[] polarVectorToDest;
                // Convert to polar to easily modify direction of vector
                if (cartesianVectorToDest[0] == 0)
                {
                    polarVectorToDest = new double[]{Math.Sqrt(Math.Pow(cartesianVectorToDest[0], 2) + Math.Pow(cartesianVectorToDest[1], 2)), (Math.PI / 2) };
                } else
                {
                    polarVectorToDest = new double[]{Math.Sqrt(Math.Pow(cartesianVectorToDest[0], 2) + Math.Pow(cartesianVectorToDest[1], 2)), Math.Atan(cartesianVectorToDest[1] / cartesianVectorToDest[0]) };
                }



                // Variables used to follow along path of vector
                int currX; // We are we along the vector now?
                int currY;
                int xDistance; // How far is there to go in this direction?
                int yDistance;

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

                    //Cartesian updated, but not current pos
                    currX = currPos.X;
                    currY = currPos.Y;
                    Debug.LogErrorFormat("curr x {0}, curr y {1}", currX, currY);
                    if (path.Count > 0)
                    {
                        Debug.LogErrorFormat("pixel: {0}, {1}", path[path.Count - 1].X, path[path.Count - 1].Y);
                    }



                    xDistance = cartesianVectorToDest[0];
                    yDistance = cartesianVectorToDest[1];

                    if (xDistance == 0 && yDistance == 0)
                    {
                        isAtDestination = true;
                        break;
                    }
                    //Well I have the slope through getting their distaance right?

                   
                    // If we've moved as many pixels as is along the furthest vector
                    // component, then we've completely followed the vector

                    //SO at this point instead of further and short stuff, get slope and reduce it via gcd.



                    //Get slope.
                    int rise = yDistance;
                    int run = xDistance;
                    int[] reducedSlope = new int[2];

                    //0:rise, 1: run

                    /*
                    if (run == 0) {

                        reducedSlope[1] = 0;
                        reducedSlope[0] = 1;
                    }

                    if (rise == 0)
                    {

                        reducedSlope[0] = 0;
                        reducedSlope[1] = 1;
                    }
                    */

                //    if (run != 0 && rise != 0)
                    reducedSlope = getReducedFraction(rise, run);


                    //What should I do if reduced was same as rise and run? What should the increment then be?



                    //I essentially want to see amount of multiples of reduced needed to get to non reduced form.

                    //Need to determine the end condition of this now, since not furthest, well will increments of reduction to get to original?
                    //What should it be? If this is while true, it ends and eventually hits destination, meaning it doesn't ever go through water?
                    //It going more than it should may be causing it, but the simple fact that it goes through ocean is a problem.
                    int maxDifference = 1;
                    DFPosition prevPixel =  MapsFile.WorldCoordToMapPixel(currX, currY);


                    /*
                     * We want to verify if we've followed the path of travel all the way
                     * to its magnitude. However, if the distance in x or y is negative,
                     * we can't do a currX < currPos.X + xDistance check; we'd have to inverse
                     * it. Therefore, we add these ifs so that in the case that x or y distance
                     * is negative, we flip the sign of currX and / or currY so that it properly
                     * checks if curr is *less* than currPos + distance if distance is negative.
                     */
                    int xModifier = 1;
                    int yModifier = 1;
                    if(xDistance < 0)
                    {
                        xModifier = -1;
                    }
                    if(yDistance < 0)
                    {
                        yModifier = -1;
                    }
                

                    while (currX * xModifier < currPos.X + xDistance && currY * yModifier < currPos.Y +  yDistance)
                    {
                        currX += reducedSlope[1];
                        currY += reducedSlope[0];
                        DFPosition mapPixel = MapsFile.WorldCoordToMapPixel(currX, currY);

                        //Debug.LogErrorFormat("We are at map pixel {0}, {1} and we're going for map pixel {2}, {3}",
                            //mapPixel.X, mapPixel.Y, pixelDestination.X, pixelDestination.Y);

                        if (Math.Abs(mapPixel.X - prevPixel.X) > maxDifference || Math.Abs(mapPixel.Y - prevPixel.Y) > maxDifference)
                        {
                            Debug.LogError("Moved more than one pixel at time");
                            //This is problem.
                        }
                        prevPixel = mapPixel;
                        
                        // If ocean, invalid vector, time to modify
                        //It's still not seeing ocean towards the end.
                        //    if (DaggerfallUI.Instance.DfTravelMapWindow.isOnOcean(mapPixel.X, mapPixel.Y))
                        if ((mapsFile.GetClimateIndex(mapPixel.X, mapPixel.Y) == (int)MapsFile.Climates.Ocean))
                        {
                            //There process catches each ocean pixel, ours doesn't.
                            Debug.LogError("Hit ocean here");
                            crossesOcean = true;
                            break;
                        }

                        // If we've made it this far, we have yet to cross the ocean
                        crossesOcean = false;
                        // If out of bounds, backtrack one slope increment to get a point that's in bounds
                        if (currX >= MapsFile.MaxWorldCoordX || currY >= MapsFile.MaxWorldCoordZ || currX < MapsFile.MinWorldCoordX || currY < MapsFile.MinWorldCoordZ)
                        {
                            currX -= reducedSlope[1];
                            currY -= reducedSlope[0];
                            break;
                        }

                        // If we've reached our original destination, all necessary legs are computed
                        if (mapPixel.X == pixelDestination.X && mapPixel.Y == pixelDestination.Y)
                        {
                            //True test is if it moves more than one pixel at a time.

                            //rise = currY - prevY;
                            isAtDestination = true;

                            break;
                        }
                        //path.Add(mapPixel);
                        //  if (!path.Contains(mapPixel)) {
                        //  path.Add(mapPixel);
                        // }

                    }

                    // If we're still crossing ocean, we need to increment the angle and try again
                    // If not, we can stop looping because we've found a viable solution
                    if (crossesOcean)
                    {
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

                // Convert back to cartesian to obtain our new endpoint, we already did this.
                // cartesianVectorToDest[0] = (int)(polarVectorToDest[0] * Math.Cos(polarVectorToDest[1]));
                // cartesianVectorToDest[1] = (int)(polarVectorToDest[0] * Math.Sin(polarVectorToDest[1]));

                //Okay so here we update current position by distance to new end point
                // Update new endpoint, this is now a node along our journey

                //This might be issue, if what we are subtracting by is too large, we bound it in making steps
                //But then add on the potentialy too large magnitude anyway.
                //Cause current x and current y is what we're travelling to and is kept in bounds.

                // currX = Math.Max(MapsFile.MinMapPixelX, Math.Min(MapsFile.MaxMapPixelX - 1, currX));
                // currY = Math.Max(MapsFile.MinMapPixelY,Math.Min(MapsFile.MaxMapPixelY - 1, currY));


         /*       if (xDistance > 0)
                    currX = Math.Min(currX, currPos.X + xDistance);
                else
                    currX = Math.Max(currX, currPos.X + xDistance);

                if (yDistance > 0)
                    currY = Math.Min(currY, currPos.Y + yDistance);
                else
                    currY = Math.Max(currY, currPos.Y + yDistance);*/
                currPos.X = currX;
                currPos.Y = currY;


                //Only if pixel not already there if for some reason we stop at it again.
                if (!path.Contains(currPos))
                path.Add(MapsFile.WorldCoordToMapPixel(currX, currY));
            }

            int totalTravelTime = 0;
            DFPosition prev = GetPlayerTravelPosition();

            List<DFPosition> fullPath = new List<DFPosition>();
            if (path.Count != 0)
            {
                String pathToString = "Path is: ";
                for (int i = 0; i < path.Count; i++)
                {
                    pathToString += "(" + path[i].X + ", " + path[i].Y + ") ";

                    //To get both time, and pixels in between end points, won't match cause not considering slope in our original.

                    totalTravelTime += CalculateTravelTime(path[i], speedCautious, sleepModeInn, travelShip, hasHorse, hasCart, prev, fullPath);
                    prev = path[i];
                }
                Debug.LogError(pathToString);
            }

            DaggerfallUI.Instance.DfTravelMapWindow.DrawPathOfTravel(fullPath);


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
            bool hasCart = false,
            DFPosition startPosition = null,
            List<DFPosition> truePath = null
           )
        {

            //Calling our version of calculation.
            if(startPosition == null)
            {
                return calculateTravelTime(endPos, speedCautious, sleepModeInn, travelShip, hasHorse, hasCart);
            }

            int transportModifier = 0;
            if (hasHorse)
                transportModifier = 128;
            else if (hasCart)
                transportModifier = 192;
            else
                transportModifier = 256;



            DFPosition position = GetPlayerTravelPosition();

            
            position = startPosition;


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

            List<DFPosition> path = new List<DFPosition>();




            
            truePath.Add(new DFPosition(position.X, position.Y));


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




                truePath.Add(new DFPosition(playerXMapPixel, playerYMapPixel));
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
