using System.Collections.Generic;
using ItsukiSumeragi.Models;

namespace NishinotouinYuriko.Models
{
    public class ViolationStatus
    {
        public List<TrafficChart<string, int, int>> ViolationChart { get; set; }
        public List<TrafficChart<string, int, int>> LocationChart { get; set; }
        public List<TrafficChart<string, int, int>> TargetTypeChart { get; set; }
        public List<TrafficChart<string, int, int>> CarTypeChart { get; set; }
    }
}
