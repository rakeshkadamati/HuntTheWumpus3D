﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HuntTheWumpus3
{

    class Wumpus
    {
        public int pos = -1;
        public bool awake; 
        public Wumpus(int pos)
        {
            this.pos = pos;
            awake = false;
        }
    }
}
