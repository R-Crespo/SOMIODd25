using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMIODd25.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Creation_dt { get; set; }
        public int Parent { get; set; }
        public string Event { get; set; }
        public string Endpoint { get; set; }

    }
}