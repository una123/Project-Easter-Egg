﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Mindstep.EasterEgg.Commons;
using Mindstep.EasterEgg.Commons.Physics;

namespace Mindstep.EasterEgg.Engine
{
    public interface IPhysicsObject
    {
        Position Position { get; }
        List<Block> Blocks { get; }
    }
}
