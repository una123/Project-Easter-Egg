﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mindstep.EasterEgg.Commons;
using SysMouse = Microsoft.Xna.Framework.Input.Mouse;
using Mindstep.EasterEgg.Engine.Interfaces;

namespace Mindstep.EasterEgg.Engine.Input
{
    public enum MouseButton { Left, Middle, Right };
    public class MouseInfo : Entity
    {
        private Point Center { get { return Engine.GraphicsDevice.Viewport.Bounds.RelativeCenter(); } }

        MouseState currentMouseState;
        MouseState previousMouseState;

        internal Point location;
        public Point Location { get { return location; } }

        private int scrollChange = 0;
        public int ScrollChange { get { return scrollChange; } }

        private Point frozenAt;
        private bool frozen = false;
        public bool Frozen { get { return frozen; } }

        private Point movement = new Point();
        public Point Movement { get { return movement; } }

        private Point locationInProjSpace;
        public Point LocationInProjSpace { get { return locationInProjSpace; } }





        public MouseInfo(EggEngine engine)
            : base(engine)
        {
            location = Center.Multiply(2*(2-1.618f)); //starting location of the mouse pointer in game
        }

        internal void Update(GameTime gameTime)
        {
            previousMouseState = currentMouseState;
            currentMouseState = SysMouse.GetState();

            if (!Engine.IsActive)
            {
                movement.X = 0;
                movement.Y = 0;
                currentMouseState = new MouseState();
            }
            else if (Frozen)
            {
                movement.X = currentMouseState.X - Center.X;
                movement.Y = currentMouseState.Y - Center.Y;
                CenterMouse();
            }
            else
            {
                movement.X = currentMouseState.X - previousMouseState.X;
                movement.Y = currentMouseState.Y - previousMouseState.Y;
            }

            location = location.Add(Movement);
            location.X = location.X.Clamp(0, Engine.GraphicsDevice.Viewport.Width - 1);
            location.Y = location.Y.Clamp(0, Engine.GraphicsDevice.Viewport.Height - 1);

            locationInProjSpace = CoordinateTransform.ScreenToProjSpace(Location, Engine.World.CurrentMap.Camera);
        }





        public bool IsButtonDown(MouseButton mouseButton)
        {
            switch (mouseButton)
            {
                case MouseButton.Left:
                    return currentMouseState.LeftButton == ButtonState.Pressed;
                case MouseButton.Middle:
                    return currentMouseState.MiddleButton == ButtonState.Pressed;
                case MouseButton.Right:
                    return currentMouseState.RightButton == ButtonState.Pressed;
            }
            return false;
        }
        
        private bool WasButtonDown(MouseButton mouseButton)
        {
            switch (mouseButton)
            {
                case MouseButton.Left:
                    return previousMouseState.LeftButton == ButtonState.Pressed;
                case MouseButton.Middle:
                    return previousMouseState.MiddleButton == ButtonState.Pressed;
                case MouseButton.Right:
                    return previousMouseState.RightButton == ButtonState.Pressed;
            }
            return false;
        }
        
        public bool ButtonPressed(MouseButton mouseButton)
        {
            return IsButtonDown(mouseButton) &&
                !WasButtonDown(mouseButton);
        }
        
        public bool ButtonReleased(MouseButton mouseButton)
        {
            return !IsButtonDown(mouseButton) &&
                WasButtonDown(mouseButton);
        }

        public void Freeze(Point at)
        {
            frozen = true;
            frozenAt = at;
            CenterMouse();
        }

        public void Unfreeze()
        {
            SysMouse.SetPosition(frozenAt.X, frozenAt.Y);
            frozen = false;
        }

        private void CenterMouse()
        {
            SysMouse.SetPosition(Center.X, Center.Y);
        }
    }
}
