// <copyright file="Point.cs" company="Scada International A/S">
// Copyright (c) Scada International A/S. All rights reserved.
// </copyright>

namespace KohonenCards.Models
{
    public class Point
    {
        public int X { get; set; }

        public int Y { get; set; }

        public Point()
        {
        }

        public Point(int x, int y) => (X, Y) = (x, y);
    }
}