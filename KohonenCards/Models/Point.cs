// <copyright file="Point.cs" company="Scada International A/S">
// Copyright (c) Scada International A/S. All rights reserved.
// </copyright>

using Common.Utility;

namespace KohonenCards.Models
{
    public class Point
    {
        [KMeansValue]
        public int X { get; set; }

        [KMeansValue]
        public int Y { get; set; }

        public Point()
        {
        }

        public Point(int x, int y) => (X, Y) = (x, y);
    }
}