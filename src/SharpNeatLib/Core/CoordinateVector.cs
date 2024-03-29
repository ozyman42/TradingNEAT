/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2016 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software; you can redistribute it and/or modify
 * it under the terms of The MIT License (MIT).
 *
 * You should have received a copy of the MIT License
 * along with SharpNEAT; if not, see https://opensource.org/licenses/MIT.
 */
using System.Collections.Generic;
using System.Diagnostics;

namespace SharpNeat.Core
{
    /// <summary>
    /// General purpose representation of a point in a multidimensional space. A vector of coordinates, 
    /// each coordinate defining the position within a dimension/axis defined by an ID.
    /// </summary>
    public class CoordinateVector
    {
        readonly KeyValuePair<ulong,double>[] _coordElemArray;

        #region Constructor

        /// <summary>
        /// Constructs a CoordinateVector using the provided array of ID/coordinate pairs.
        /// CoordinateVector elements must be sorted by ID.
        /// </summary>
        public CoordinateVector(KeyValuePair<ulong,double>[] coordElemArray)
        {
            Debug.Assert(IsSorted(coordElemArray), "CoordinateVector elements must be sorted by ID.");
            _coordElemArray = coordElemArray;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets an array containing the ID/coordinate pairs.
        /// CoordinateVector elements are sorted by ID
        /// </summary>
        public KeyValuePair<ulong,double>[] CoordArray
        {
            get { return _coordElemArray; }
        }

        #endregion

        #region Static Methods [Debugging]

        private static bool IsSorted(KeyValuePair<ulong,double>[] coordElemArray)
        {
            if(0 == coordElemArray.Length) {
                return true;
            }

            ulong prevId = coordElemArray[0].Key;
            for(int i=1; i<coordElemArray.Length; i++)
            {   // <= also checks for duplicates as well as sort order.
                if(coordElemArray[i].Key <= prevId) {
                    return false;
                }
            }
            return true;
        }

        #endregion
    }
}
