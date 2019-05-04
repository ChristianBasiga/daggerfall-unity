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
        
        public int calculateTravelTime(DFPosition destination, bool speedCautious = false,
            bool sleepModeInn = false,
            bool travelShip = false,
            bool hasHorse = false,
            bool hasCart = false)
        {


            int totalTravelTime = 0;



            int transportModifier = 0;
            if (hasHorse)
                transportModifier = 128;
            else if (hasCart)
                transportModifier = 192;
            else
                transportModifier = 256;

            MapsFile mapsFile = DaggerfallUnity.Instance.ContentReader.MapFileReader;
            DFPosition nodeEndpoint; // The final position of a leg of travel
            DFPosition currPos = GetPlayerTravelPosition(); // This changes node per node
            List<DFPosition> path = new List<DFPosition>();
            bool isAtDestination = false;
            DFPosition initialPosition = currPos;

            //B tree of ocean, map of map of map of pixels, coudl instead store tuples as keys
            //but for simplicity y to ranges of x.

            if (bTreeOceanPixels == null)
            {
                initBTree();
            }
            /*
            foreach(DFPosition pixel in oceanPixels)
            {

               // Debug.LogError(string.Format("Ocean Pixel: {0}", pixel.ToString()));
            }*/

            //Debug.LogError("Number of ocean pixels " + oceanPixels.Count);


            bool rerouted = false;

            //Converting current position to world coords.
           //   currPos = MapsFile.MapPixelToWorldCoord(currPos.X, currPos.Y);
              DFPosition pixelDestination = destination;
           // destination = MapsFile.MapPixelToWorldCoord(destination.X, destination.Y);


            LinkedList<DFPosition> subPath = new LinkedList<DFPosition>();

            const double angleIncrement = 0.0436332; // 5 degrees in radians


            // Continually modifies vectors based on currPlayerPosition until an acceptable path is reached
            //If reduce is literally just halving it, it's not enough, that's why finished so fast, sometimes just 3 too, etc.
            while (!isAtDestination)
            {

                // Obtain vector created between destination point and current position
                //This is same.
                int[] cartesianVectorToDest = { destination.X - currPos.X, destination.Y - currPos.Y };

                // Optimize angle modification to make deviations hug the coastline
                // as much as possible
                int angleSign = 0;
            
               ///  const double angleIncrement = 1.745329e-5; //1 degree in radians, makes it so doesn't actually cross ocean, so angle is off.
                if (currPos.Y > destination.Y) // Counter-clockwise (ocean is below)
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
                    polarVectorToDest = new double[]{Math.Sqrt(Math.Pow(cartesianVectorToDest[0], 2) + Math.Pow(cartesianVectorToDest[1], 2)), Math.Atan((double)cartesianVectorToDest[1] / cartesianVectorToDest[0]) };
                }





                // Variables used to follow along path of vector
                int currX; // We are we along the vector now?
                double currY;
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
              
                bool crossesOcean = false;
                int minutesTakenThisLeg = 0;
                while (true)
                {
                    // Verify if ocean is crossed


                    //Cartesian updated, but not current pos

                    currX = currPos.X;
                    currY = currPos.Y;

                    xDistance = cartesianVectorToDest[0];

                    yDistance = cartesianVectorToDest[1];


                    int run = 1;
                    double rise = 0;


                    if (xDistance == 0)
                    {
                        run = 0;
                    }
                    else
                    {
                        rise = Math.Abs(((double)yDistance / xDistance));
                    }

                   
                



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





                    int pixelsAdded = 0;


                    int minutesTakenThisMove = 0;

                    //Back to false between each leg building progressions.
                    bool justSetInterrupt = false;

                    while (currX * xModifier < currPos.X + xDistance && currY * yModifier < currPos.Y + yDistance)
                    // while(true)
                    {

                        currX += run * xModifier;

                        currY += rise * yModifier;





                        //Finish cleaning up merge

                        int roundedY = (int)Math.Round(currY);

                        DFPosition mapPixel = new DFPosition(currX, roundedY);




                        int terrain = mapsFile.GetClimateIndex(mapPixel.X, mapPixel.Y);
                        int terrainMovementIndex = climateIndices[terrain - (int)MapsFile.Climates.Ocean];


                        minutesTakenThisMove = (((102 * transportModifier) >> 8)
                            * (256 - terrainMovementModifiers[terrainMovementIndex] + 256)) >> 8;


                        if (!sleepModeInn)
                            minutesTakenThisMove = (300 * minutesTakenThisMove) >> 8;


                        //Should create method that returns approriate key.

                        List<DFPosition> oceanPixelsSetToCheck;

                        int yKey = getYKey(roundedY);
                        int xKey = getXKey(currX);

                        if (bTreeOceanPixels.ContainsKey(yKey) && bTreeOceanPixels[yKey].ContainsKey(xKey))
                        {
                            oceanPixelsSetToCheck = bTreeOceanPixels[getYKey(roundedY)][getXKey(currX)];
                        }
                        else
                        {
                            oceanPixelsSetToCheck = new List<DFPosition>();
                        }








                        //Try to interrupt at this pixel, and with current time travelled so far.
                        //    tryInterrupt(mapPixel.X, mapPixel.Y, totalTravelTime + minutesTakenThisLeg);

                        bool inSubPath = false;
                        foreach (DFPosition pos in subPath)
                        {
                            //if same pixel don't include in subpath.
                            if ((pos.X == mapPixel.X && pos.Y == mapPixel.Y))
                            {
                                inSubPath = true;
                                break;
                            }

                        }

                        if (!inSubPath)
                        {
                            pixelsAdded += 1;

                            subPath.AddLast(mapPixel);


                        }

                        int prevX = mapPixel.X - (run * xModifier);
                        int prevY = mapPixel.Y - (int)Math.Round((rise * yModifier));
                        int nextX = mapPixel.X + (run * xModifier);
                        int nextY = mapPixel.Y + (int)Math.Round((rise * xModifier));

                        // If ocean, invalid vector, time to modify

                        //      if (DaggerfallUI.Instance.DfTravelMapWindow.isOnOcean(mapPixel.X, mapPixel.Y))

                        //If slope caused us to travel more than one pixel in either direction, then do bounds
                        //otherwise check exact only, checking run > 2 is same thing.
                        if (Math.Abs(mapPixel.X - prevX) > 2 || Math.Abs(mapPixel.Y - prevY) > 2)
                        {

                            //Strangely enough slope is extremely small when across island over he ocean.
                            //So it hits ocean, but it doesn't seem to draw second line.
                            //Prob cause from second end point the destination is within bounds of slope.
                            //But if i travel along slope and prev + slope is not more than 1 pixel travel time, then can do normal check.
                            if (prevX <= pixelDestination.X && pixelDestination.X <= mapPixel.X && prevY <= pixelDestination.Y && mapPixel.Y <= pixelDestination.Y)
                            {
                                isAtDestination = true;
                                break;
                            }
                        }
                        //But apparently never hits so maybe a bigger threshhold than just one.. What if slope?
                        else if (mapPixel.X == pixelDestination.X && mapPixel.Y == pixelDestination.Y)
                        {
                            isAtDestination = true;
                            break;
                        }


                        if (!travelShip)
                        {

                            //Almost there baby, just need to fucking figure out why it doesn't see ocean
                            //there seems to be some threshold distance away from ocean for destination before it hits.

                            if (terrain == (int)MapsFile.Climates.Ocean)
                            {

                                //Check within bounds of ocean pixel set to see if anything in that set is this pixel.


                                Debug.LogError("Drowning");
                                //Cartesian coordinates not updating?
                                crossesOcean = true;
                                break;
                            }
                            /*       else if (Math.Abs(mapPixel.X - prevX) > 1 || Math.Abs(mapPixel.Y - prevY) > 1)
                          //  else
                            {
                                //  else
                                // {
                                //Check ocean pixel set.


                                //So same stuff should be caught here, and it is but when caught here angle never changes?
                                foreach (DFPosition pos in oceanPixels)
                                {

                                         // Debug.LogErrorFormat("ocean pixel checking is {0}, is for sure ocean: {1}", pos.ToString(), mapsFile.GetClimateIndex(pos.X, pos.Y) == (int)MapsFile.Climates.Ocean);
                                    //Check for bounds of each position here.
                                    //Exct same concept of destination check for each ocean pixel.

                                    //Same concept as destination check, if in bounding box of slope size then was within ocean
                                    //as long as destination not also in it.
                                    //Equality check already made with climate.
                                    if ((prevX * xModifier <= pos.X && pos.X <= mapPixel.X * xModifier && prevY * yModifier <= pos.Y && mapPixel.Y * yModifier <= pos.Y))

                                   /// if ((prevX <= pos.X && pos.X <= nextX && prevY <= pos.Y && nextY <= pos.Y))
                                    {
                                        crossesOcean = true;
                                        break;
                                    }





                                }

                                if (crossesOcean)
                                {
                                    break;
                                }
                            }*/
                            //}



                        }


                        // If we've made it this far, we have yet to cross the ocean
                        crossesOcean = false;
                        // If out of bounds, backtrack one slope increment to get a point that's in bounds
                        if (mapPixel.X >= MapsFile.MaxMapPixelX || mapPixel.Y >= MapsFile.MaxMapPixelY || mapPixel.X < MapsFile.MinMapPixelX || mapPixel.Y < MapsFile.MinMapPixelY)
                        {
                            currX -= run * xModifier;
                            currY -= rise * yModifier;


                            //If out of bounds should also pop off subpath.
                            subPath.RemoveLast();

                            //currY = Math.Round(currY);
                            //Does this happen? It shouldn't with current destination.
                            break;
                        }

                        minutesTakenThisLeg += minutesTakenThisMove;



                        //For seeing if just happened this leg, to reset it if did.

                        if (interrupt == null)
                        {
                            //20% chance, but try multiple times so may overwrite so more random.
                            //Time taken to get to this position is total travle time so far but time taken this leg so far.
                            tryInterrupt(currX, roundedY, totalTravelTime + minutesTakenThisLeg);

                            //If successfully interrupted, then did it during this leg.
                            if (interrupt != null)
                            {
                                justSetInterrupt = true;
                            }
                        }
                        if (!speedCautious)
                            minutesTakenThisLeg = minutesTakenThisLeg >> 1;
                    } 

                    // If we're still crossing ocean, we need to increment the angle and try again
                    // If not, we can stop looping because we've found a viable solution
                    if (crossesOcean)
                    {
                        /*
                                      //If no edges needed it does this.
                                      foreach (DFPosition pos in subPath)
                                      {
                                          path.Add(new DFPosition(pos.X, pos.Y));
                                      }*/



                        //Reset interrupt because it was set during this leg of travel.
                        if (justSetInterrupt)
                        {
                            interrupt = null;
                        }
                       
                        //Reset time.
                        minutesTakenThisLeg = 0;


                        //Pop pixels added form last.
                        for (int i = 0; i < pixelsAdded; ++i)
                        {
                            //For drawing incorrect vecotrs the stuff popped from here add to path.
                            subPath.RemoveLast();
                        }


                        //subPath.Clear();

                        polarVectorToDest[1] += angleSign * angleIncrement;
                       
                        //Issue is getting back distance that's too big in magnitude that it doesn't make sense.
                        //Negative distance makes sense, but should not be more in magnitude than current position when origin is 0,0
                        cartesianVectorToDest[0] = (int)(polarVectorToDest[0] * Math.Cos(polarVectorToDest[1]));
                        cartesianVectorToDest[1] = (int)(polarVectorToDest[0] * Math.Sin(polarVectorToDest[1]));
                    }
                    else
                    {
                        //subPath.Clear();
                        //If valid , remove it.
                        totalTravelTime += minutesTakenThisLeg;
                        break;
                    }
                }

             
                currPos.X = currX;
                currPos.Y = (int)Math.Round(currY);

                /*
                foreach( DFPosition pos in subPath)
                {

                    path.Add(pos)
                }*/

            }
            DFPosition prev = GetPlayerTravelPosition();
            List<DFPosition> fullPath = new List<DFPosition>();


            /*
            //Otherwise add onto the path the sub path.
            //If no edges needed it does this.

        
            //Istead of using theirs, use ours to draw the path.
            List<DFPosition> fullPath = new List<DFPosition>();
            if (path.Count != 0)
            {
                String pathToString = "Path is: ";
                for (int i = 0; i < path.Count; i++)
                {
                    pathToString += "(" + path[i].X + ", " + path[i].Y + ") ";

                    //To get both time, and pixels in between end points, won't match cause not considering slope in our original.

                   // totalTravelTime += CalculateTravelTime(path[i], speedCautious, sleepModeInn, travelShip, hasHorse, hasCart, prev, fullPath);
                    prev = path[i];
                }
                Debug.LogError(pathToString);
            }
            */



            subPath.AddLast(pixelDestination);
            DaggerfallUI.Instance.DfTravelMapWindow.DrawPathOfTravel(subPath);


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

            List<DFPosition> path = new List<DFPosition>();




            
          //  truePath.Add(new DFPosition(position.X, position.Y));


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

        private void tryInterrupt(int pixelX, int pixelY, int timeTravelled)
        {

            //Maybe with this can call their algorithm strictly for time?


            //Pixel offset makes it so it is along line of travel at the very least.
            //Obviously more interrupts should be possible and also should be via the engine.
            bool doInterrupt = (UnityEngine.Random.Range(0, 101) < 20);

           
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
