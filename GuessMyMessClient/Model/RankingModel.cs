using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuessMyMessClient.Model
{
    public class RankingModel
    {
        public int Rank { get; set; }
        public string Username { get; set; }
        public int TotalScore { get; set; }
        public bool IsCurrentUser { get; set; } 
    }
}
