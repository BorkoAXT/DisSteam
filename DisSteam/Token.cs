using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DisSteam
{
    public class Token
    {
        public string token { get; set; }

        public Token()
        {
            token = "";
        }

        public Token(string _token)
        {
            token = _token;
        }
    }
}
