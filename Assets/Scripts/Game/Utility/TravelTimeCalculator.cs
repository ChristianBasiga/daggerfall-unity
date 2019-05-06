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
        
        public int calculateTravelTime(DFPosition destination, bool speedCautious = false,
            bool sleepModeInn = false,
            bool travelShip = false,
            bool hasHorse = false,
            bool hasCart = false)
        {

            pixelsTraveledOnOcean = 0;

            int totalTravelTime = 0;



            int transportModifier = 256;
            if (hasHorse)
                transportModifier = 128;
            else if (hasCart)
                transportModifier = 192;

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



            //Converting current position to world coords.
           //   currPos = MapsFile.MapPixelToWorldCoord(currPos.X, currPos.Y);
              DFPosition pixelDestination = destination;
           // destination = MapsFile.MapPixelToWorldCoord(destination.X, destination.Y);


            LinkedList<DFPosition> subPath = new LinkedList<DFPosition>();

            const double angleIncrement = 0.0523599;// 3 degrees 0.0436332; // 5 degrees in radians


            int numberOfLegs = 0;

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


                angleSign = -1;

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
                    double rise = 1;


                    if (xDistance == 0)
                    {
                        run = 0;

                    }
                    else
                    {
                        rise = Math.Abs(((double)yDistance / xDistance));

                    }
                    if (Math.Abs(xDistance) >= 1 && Math.Abs(xDistance) <= 2 && yDistance != 0)
                    {
                        rise = 1;
                    }
                    





                    int xModifier = 1;
                    int yModifier = 1;


                    if (xDistance < 0)
                    {
                        xModifier = -1;
                    }
                    if (yDistance < 0)
                    {
                        yModifier = -1;
                    }





                    int pixelsAdded = 0;



                    int minutesTakenThisMove = 0;

                    //Back to false between each leg building progressions.
                    bool justSetInterrupt = false;


                    //Right now we go full magnitude at most, but what we want is to stop at the very first point where we can draw a vector that doesn't cross ocean
                    //to destination, cause right now we go past that point cause then breaks when travels magnitude.
                    //now we said overshotting doesn't matter because then we'll draw a leg again later, but that was when we only end points, we want to make sure
                    //time is accurate, we may be able to get time right despite over shooting destination, but former problem still a thing.
                    while (currX * xModifier < currPos.X + xDistance || currY * yModifier < currPos.Y + yDistance)
                    { 

                            currX += run * xModifier;


                            currY += rise * yModifier;



                        //Finish cleaning up merge

                        int roundedY = (int)Math.Round(currY);

                        DFPosition mapPixel = new DFPosition(currX, roundedY);




                        int terrain = mapsFile.GetClimateIndex(mapPixel.X, mapPixel.Y);





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
                            //Needed during world coords travel but in this case not really needed since no duplicates will happen.
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



                        if (Math.Abs(mapPixel.X - prevX) > 1 || Math.Abs(mapPixel.Y - prevY) > 1)
                        {

                            
                            if (pixelDestination.X >= prevX * xModifier && pixelDestination.X <= mapPixel.X * xModifier &&
                                pixelDestination.Y >= prevY * yModifier && pixelDestination.Y <= mapPixel.Y * yModifier) 
                            {

                                isAtDestination = true;
                                break;
                            }
                        }
                        //But apparently never hits so maybe a bigger threshhold than just one.. What if slope?
                        if (mapPixel.X == pixelDestination.X && mapPixel.Y == pixelDestination.Y)
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
                            subPath.RemoveLast();

                            //currY = Math.Round(currY);
                            //Does this happen? It shouldn't with current destination.
                            break;
                        }

                        //So the time taken needs to include destination though.
                        if (terrain == (int)MapsFile.Climates.Ocean)
                        {

                            //Check within bounds of ocean pixel set to see if anything in that set is this pixel.
                            if (!travelShip)
                            {

                                crossesOcean = true;
                                break;
                            }
                            else
                            {
                                //Otherwise if on ocean and travelling by ship, add this for travel cost.
                                ++pixelsTraveledOnOcean;

                                //Otherwise of on ocean and travelling by ship it is this.
                                minutesTakenThisMove = 51;
                            }
                        }
                        else
                        {
                            /*
                            if (!travelShip)

                                //In future not all ocea pixels just for testing doing this.
                                foreach (DFPosition position in oceanPixelsSetToCheck)
                                {
                                    if (position.X >= prevX * xModifier && position.X <= mapPixel.X * xModifier &&
                                  position.Y >= prevY * yModifier && position.Y <= mapPixel.Y * yModifier)
                                    {
                                        crossesOcean = true;
                                        break;
                                    }
                                }

                            if (crossesOcean) break;

                            */
                            //This way when we already hit destination we can include time travelled to there?
                            //Or is it saying time travelled from before. Start 0, so actually is current to next
                            //if there is no next then this doesn't need to happen, okay so this doesn't need to happen  if hit destination.
                            int terrainMovementIndex = 0;

                            int climateIndex = terrain - (int)MapsFile.Climates.Ocean;
                            terrainMovementIndex = climateIndices[climateIndex];

                            //This multiplied by slope to make this accurate time, or multiplied by difference, ladder more accurate.
                            minutesTakenThisMove = (((102 * transportModifier) >> 8)
                                * (256 - terrainMovementModifiers[terrainMovementIndex] + 256)) >> 8;

                            //Cause no skips done in x, drawing no big deal, but time must be accurate.
                            if (prevY - mapPixel.Y != 0)
                                minutesTakenThisMove *= Math.Abs(prevY - mapPixel.Y);
                        }


                        if (!sleepModeInn)
                            minutesTakenThisMove = (300 * minutesTakenThisMove) >> 8;

                      


                        // If we've made it this far, we have yet to cross the ocean
                        crossesOcean = false;
                       

                        minutesTakenThisLeg += minutesTakenThisMove;



                        //For seeing if just happened this leg, to reset it if did.

                        if (!travelShip)
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


                        minutesTakenThisLeg = 0;


                        //Pop pixels added form last.
                        for (int i = 0; i < pixelsAdded; ++i)
                        {
                            //For drawing incorrect vecotrs the stuff popped from here add to path.
                          //  subPath.RemoveLast();
                        }



                        polarVectorToDest[1] += angleSign * angleIncrement;

                        //Issue is getting back distance that's too big in magnitude that it doesn't make sense.
                        //Negative distance makes sense, but should not be more in magnitude than current position when origin is 0,0
                        cartesianVectorToDest[0] = (int)(polarVectorToDest[0] * Math.Cos(polarVectorToDest[1]));
                        cartesianVectorToDest[1] = (int)(polarVectorToDest[0] * Math.Sin(polarVectorToDest[1]));
                    }
                    else
                    {
                        numberOfLegs += 1;


                        //Reset time.
                        //Pop pixels added form last.
                        /*   for (int i = 0; i < pixelsAdded; ++i)
                           {
                               //For drawing incorrect vecotrs the stuff popped from here add to path.
                               subPath.RemoveLast();
                           }
                           */
                        //subPath.Clear();


                        //then iterate from start of subpath to end and choose first ocean
                        //or from end to start, choosing latest one that doesn't draw vector, more likely from end too.


                        //It coincidently fixes it for my test because towards end of leg is when it can make direct path,
                        //logically it should break when hits but in this case shouldn't because previous tries may not detect crossing ocean.

                        /*

                        if (!travelShip && !isAtDestination)
                        {



                            LinkedListNode<DFPosition> current = subPath.Last;
                            int i;


                            Debug.LogErrorFormat("Leg count {0} Leg size: {1}", numberOfLegs, pixelsAdded);


                            //I want to iterate number of pixels minus 1 times cause last is also a pixel.
                            for (i = 0; i < pixelsAdded && current.Previous != null; ++i)
                            {
                                current = current.Previous;
                            }

                            i = 0;
                            int amountToRemove = 0;


                            while ( i < pixelsAdded && current != null)
                            {

                                //Same slope logic.
                                int t_yDist = pixelDestination.Y - current.Value.Y;
                                int t_xDist = pixelDestination.X - current.Value.X;

                                int t_run = 1;
                                double t_rise = 1;

                                if (t_xDist == 0)
                                {
                                    t_run = 0;
                                }
                                else
                                {
                                    t_rise = (double)t_yDist / t_xDist;
                                }


                                if ((Math.Abs(t_xDist) == 1 || Math.Abs(t_xDist) == 2) && t_yDist != 0)
                                {
                                    t_rise = 1;
                                }

                                
                                int t_currX = current.Value.X;
                                double t_currY = current.Value.Y;


                                int x_modifier = (t_xDist < 0) ? -1 : 1;
                                int y_modifier = (t_yDist < 0) ? -1 : 1;

                                bool validStart = true;
                                while ((t_currX * x_modifier < t_xDist + current.Value.X || t_currY * y_modifier < t_yDist + current.Value.Y))
                                {
                                    t_currX += t_run * x_modifier;
                                    t_currY += t_rise * y_modifier;

                                    int rounded = (int)Math.Round(t_currY);

                                    int yKey = getYKey(rounded);
                                    int xKey = getXKey(t_currX);

                                    int prevX = t_currX - (t_run * x_modifier);
                                    int prevY = rounded - (int)Math.Round((t_rise * y_modifier));



                                    if (Math.Abs(t_currX - prevX) > 1 || Math.Abs(rounded - prevY) > 1)
                                    {


                                        if (pixelDestination.X >= prevX * x_modifier && pixelDestination.X <= t_currX * x_modifier &&
                                            pixelDestination.Y >= prevY * y_modifier && pixelDestination.Y <= rounded * y_modifier)
                                        {
                                            break;
                                        }
                                    }
                                    //But apparently never hits so maybe a bigger threshhold than just one.. What if slope?
                                    if (t_currX == pixelDestination.X && rounded == pixelDestination.Y)
                                    {
                                        break;
                                    }



                                    if (t_currX >= MapsFile.MaxMapPixelX || rounded >= MapsFile.MaxMapPixelY || t_currX < MapsFile.MinMapPixelX || rounded < MapsFile.MinMapPixelY)
                                    {

                                        //currY = Math.Round(currY);
                                        //Does this happen? It shouldn't with current destination.
                                        validStart = false;
                                        break;
                                    }


                                   

                                    if (mapsFile.GetClimateIndex(t_currX, rounded) == (int)MapsFile.Climates.Ocean)
                                    {

                                        validStart = false;

                                        break;
                                    }



                                }


                                ++i;


                                if (validStart)
                                {
                                    amountToRemove = pixelsAdded - i;
                                    break;
                                }



                                //Then for each position here, draw vector to destination
                                current = current.Next;


                            }

                            //Essentially if this isn't valid start or if all of hits ocean cause impossible, we still remove nothing.
                            //Real question is why does this cause to wierd angle away.
                            for (i = 0; i < amountToRemove; ++i)
                            {
                                subPath.RemoveLast();
                            }
                        }
                        */
                                

                              //Can bound magnitude but not make it more precise in path
                              totalTravelTime += minutesTakenThisLeg;
                              break;
                          }
                      }


                      //The bounding done is still relevant, since slopes may go over the total distance, as funny as that sounds.
                      //I mean why else would it keep shooting up, unless met y but never met x, which is a problem
                      currPos.X = subPath.Last.Value.X;
                      currPos.Y = subPath.Last.Value.Y;

                      /*
                      foreach( DFPosition pos in subPath)
                      {

                          path.Add(pos)
                      }*/

                                }
            DFPosition prev = GetPlayerTravelPosition();
            List<DFPosition> fullPath = new List<DFPosition>();


            Debug.LogError("Amount of legs " + numberOfLegs);
            //Otherwise add onto the path the sub path.
            //If no edges needed it does this.
            /*
        
            if (subPath.Count != 0)
            {
                String pathToString = "Path is: ";
                for (int i = 0; i < subPath.Count; i++)
                {
                    pathToString += "(" + path[i].X + ", " + path[i].Y + ") ";

                    //To get both time, and pixels in between end points, won't match cause not considering slope in our original.
                    //It drew on ocean when did this, I swear it did, it had to have, and we are storing
                   // totalTravelTime += CalculateTravelTime(path[i], speedCautious, sleepModeInn, travelShip, hasHorse, hasCart, prev, fullPath);
                    prev = path[i];
                }
                Debug.LogError(pathToString);
            }
            
            */
            if (!speedCautious)
            {
                totalTravelTime = totalTravelTime >> 1;
            }

            subPath.AddLast(pixelDestination);

          //  DaggerfallUI.Instance.DfTravelMapWindow.DrawPathOfTravel(subPath);


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
