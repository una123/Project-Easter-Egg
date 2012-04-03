﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mindstep.EasterEgg.Engine.Physics;
using Mindstep.EasterEgg.Commons;
using Mindstep.EasterEgg.Engine.Game;

namespace GameTests
{
    [TestClass]
    public class FindPathUnitTest
    {
        PhysicsManager physics;

        Node[][][] worldMatrix1;
        Node[][][] worldMatrix2;

        public FindPathUnitTest()
        {
            GameMap map = new GameMap();
            physics = new PhysicsManager();

            worldMatrix1 = new Node[6][][];
            worldMatrix2 = new Node[6][][];

            for(int i = 0; i < 6; i++)
            {
                worldMatrix1[i] = new Node[6][];
                worldMatrix2[i] = new Node[6][];
                for(int j = 0; j < 6; j++)
                {
                    worldMatrix1[i][j] = new Node[1];
                    worldMatrix2[i][j] = new Node[1];

                    Node n = new Node(0, new Position(i, j, 0));
                    worldMatrix1[i][j][0] = n;

                    if ((i == 2 && j == 2) ||
                       (i == 3 && j == 2) ||
                       (i == 3 && j == 3) ||
                       (i == 3 && j == 4))
                    {
                        n = new Node(1, new Position(i, j, 0));
                    }
                    else
                    {
                        n = new Node(0, new Position(i, j, 0));
                    }

                    worldMatrix2[i][j][0] = n;


                }
            }
            
            map.WorldMatrix = worldMatrix1;
            physics.CurrentMap = map;
        }

        [TestMethod]
        public void FindPathTestMethod1()
        {
            physics.CurrentMap.WorldMatrix = worldMatrix1;

            Node n = new Node(0, new Position(1, 1, 0));
            Node end = new Node(0, new Position(4, 4, 0));
            Path path = physics.FindPath(n, end);

            Assert.AreEqual(end.Position, path.LastStep.Position);
        }

        [TestMethod]
        public void FindPathTestMethod2()
        {
            physics.CurrentMap.WorldMatrix = worldMatrix2;

            Node n = new Node(0, new Position(1, 1, 0));
            Node end = new Node(0, new Position(4, 4, 0));
            Path path = physics.FindPath(n, end);

            Assert.AreEqual(end.Position, path.LastStep.Position);
        }
    }
}
