using System.Collections.Generic;

namespace KohonenCards.Models
{
    public class Cluster
    {
        public List<InputDataResult> InputDataResults { get; set; }

        public Point Centroid { get; set; }
    }
}
