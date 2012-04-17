﻿#region Using Statements
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Mindstep.EasterEgg.Commons;
using System;
using System.Linq;
using System.Collections.Generic;
using Mindstep.EasterEgg.MapEditor.Animations;
using Mindstep.EasterEgg.Commons.Graphics;
#endregion

namespace Mindstep.EasterEgg.MapEditor
{
    /// <summary>
    /// Example control inherits from GraphicsDeviceControl, which allows it to
    /// render using a GraphicsDevice. This control shows how to draw animating
    /// 3D graphics inside a WinForms application. It hooks the Application.Idle
    /// event, using this to invalidate the control, which will cause the animation
    /// to constantly redraw.
    /// </summary>
    class MainView : GraphicsDeviceControl
    {
        private static readonly Color SELECTED_TEXTURE_COLOR = Color.LimeGreen;

        private SpriteBatch spriteBatch;
        private Texture2D blockTexture;
        private Texture2D wireframeTexture;
        private Texture2D gridTexture;

        private MainForm mainForm;
        private bool draggingTextures;
        private bool panning;
        private System.Drawing.Point lastMouseLocation;
        private SamplerState samplerState;

        private Camera camera;
        public float Zoom { get { return camera.Zoom; } }
        private SpriteFont spriteFont;
        public bool drawTextureIndices = true;
        private Point mouseCoordAtMouseDown;
        private List<Texture2DWithDoublePos> selectedTextures = new List<Texture2DWithDoublePos>();
        private List<SaveBlock> selectedBlocks = new List<SaveBlock>();
        private ContextMenu blockContextMenu;
        private ContextMenu textureContextMenu;
        private bool mouseHasMovedSinceMouseDown = true;

        private static Dictionary<BlockType, Color> blockTypeColor;
        private bool erasingBlocks;
        private bool drawingBlocks;


        public void Initialize(MainForm mainForm)
        {
            this.mainForm = mainForm;
            camera = new Camera(new float[]{.25f, .5f, .75f, 1, 2, 4, 6, 8, 12, 16, 24, 32 }, 3, new Point(Width/2, Height/2));
            spriteBatch = new SpriteBatch(mainForm.GraphicsDevice);
            spriteFont = mainForm.Content.Load<SpriteFont>("hudFont");

            blockTexture = mainForm.Content.Load<Texture2D>("mainBlock31");
            wireframeTexture = mainForm.Content.Load<Texture2D>("mainWireframe31");
            gridTexture = mainForm.Content.Load<Texture2D>("mainGrid31");
            if (!blockTexture.Bounds.Equals(gridTexture.Bounds))
            {
                throw new Exception("mainBlock and mainGrid image size mismatch.");
            }

            blockContextMenu = new ContextMenu(new MenuItem[]{
                new MenuItem("Edit Block Details", BlockContextMenuEditDetails),
            });
            textureContextMenu = new ContextMenu(new MenuItem[]{
                new MenuItem("Bring To Front", TextureContextMenuBringToFront),
                new MenuItem("Bring Forward", TextureContextMenuBringForward),
                new MenuItem("Send Backward", TextureContextMenuSendBackward),
                new MenuItem("Send To Back", TextureContextMenuSendToBack),
                new MenuItem("-"),
                new MenuItem("Delete", TextureContextMenuDelete),
            });

            MouseDown += new MouseEventHandler(MainView_MouseDown);
            MouseUp += new MouseEventHandler(MainView_MouseUp);
            MouseMove += new MouseEventHandler(MainView_MouseMove);
            KeyDown += new KeyEventHandler(MainView_KeyDown);
            Resize += new EventHandler(MainView_Resize);

            samplerState = new SamplerState();
            samplerState.Filter = TextureFilter.PointMipLinear;
            samplerState.AddressU = TextureAddressMode.Clamp;
            samplerState.AddressV = TextureAddressMode.Clamp;

            blockTypeColor = new Dictionary<BlockType, Color>();
            blockTypeColor.Add(BlockType.SOLID, Color.Green);
            blockTypeColor.Add(BlockType.STAIRS_DOWN, Color.Blue);
            blockTypeColor.Add(BlockType.STAIRS_UP, Color.Brown);
            blockTypeColor.Add(BlockType.WALKABLE, Color.Olive);
        }



        /// <summary>
        /// Draws the control.
        /// </summary>
        protected override void Draw()
        {
            GraphicsDevice.Clear(mainForm.BackgroundColor);
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.NonPremultiplied, samplerState, null, null, null, camera.ZoomAndOffsetMatrix);
            
            BoundingBoxInt boundingBox = new BoundingBoxInt(mainForm.SaveBlocks.ToPositions());

            List<Position> tiles = new List<Position>();
            for (int x = -5; x < 10; x += 1)
            {
                for (int y = -5; y < 10; y += 1)
                {
                    Position tilePos = new Position(x, y, -1);
                    tiles.Add(tilePos);
                    boundingBox.addPos(tilePos);
                }
            }

            Position currentLayerOffset = new Position(0, 0, mainForm.CurrentLayer + 1);
            foreach (Position tilePos in tiles)
            {
                drawBlock(gridTexture, boundingBox, Color.White, tilePos, 0.01f);
                //drawBlock(grid, boundingBox, Color.Red, tilePos+currentLayerOffset);
            }

            if (mainForm.CurrentBlockDrawState != BlockDrawState.None)
            {
                Texture2D saveBlockTexture;
                if (mainForm.CurrentBlockDrawState == BlockDrawState.Solid)
                {
                    saveBlockTexture = blockTexture;
                }
                else
                {
                    saveBlockTexture = wireframeTexture;
                }
                foreach (SaveBlock saveBlock in mainForm.SaveBlocks)
                {
                    drawBlock(blockTexture, boundingBox, blockTypeColor[saveBlock.type], saveBlock.Position);
                }
            }

            float i = 0;
            foreach (Texture2DWithPos tex in mainForm.CurrentFrame.Textures.BackToFront())
            {
                float depth = (1 - i / mainForm.CurrentFrame.Textures.Count) * .1f;
                spriteBatch.Draw(tex.Texture, tex.Coord.ToVector2(), null,
                    new Color(1, 1, 1, mainForm.TextureOpacity), 0,
                    Vector2.Zero, 1, SpriteEffects.None, depth / camera.Zoom);
                if (mainForm.DrawTextureIndices)
                {
                    spriteBatch.DrawString(spriteFont, i.ToString(), tex.Coord.ToVector2(),
                        Color.Green, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                }
                i++;
            }

            foreach (Texture2DWithDoublePos selectedTexture in selectedTextures)
            {
                int borderWidth = 1;
                Rectangle r = selectedTexture.t.Bounds;
                r.Inflate(borderWidth, borderWidth);
                spriteBatch.DrawRectangle(r, SELECTED_TEXTURE_COLOR, borderWidth);
            }

            //Vector3 v = CoordinateTransform.ScreenToObjectSpace(lastMouseLocation.ToXnaPoint(), camera, 0);
            //Vector2 u = CoordinateTransform.ObjectToProjectionSpace(v);
            //spriteBatch.DrawRectangle(new Rectangle((int)u.X, (int)u.Y, 1, 1), Color.Orchid, 5);
            //spriteBatch.DrawString(spriteFont, v.ToString()+"\n"+v.ToPosition().ToString(), u, Color.Orange);
            spriteBatch.End();
        }

        private void drawBlock(Texture2D image, BoundingBoxInt boundingBox, Color color, Position pos)
        {
            drawBlock(image, boundingBox, color, pos, 0);
        }
        private void drawBlock(Texture2D image, BoundingBoxInt boundingBox, Color color, Position pos, float depthOffset)
        {
            float depth = boundingBox.getRelativeDepthOf(pos);
            Vector2 projCoords = CoordinateTransform.ObjectToProjSpace(pos);
            spriteBatch.Draw(image, projCoords + Constants.blockDrawOffset, null, color, 0, Vector2.Zero, 1, SpriteEffects.None, (depth+depthOffset) / camera.Zoom );
        }







        private void MainView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (draggingTextures) //pressing the other mouse button only cancels the dragging
                {
                    draggingTextures = false;
                }
                else
                {
                    panning = true;
                    mouseHasMovedSinceMouseDown = false;
                }
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (panning) //pressing the other mouse button only cancels the panning
                {
                    panning = false;
                }
                else
                {
                    switch (mainForm.EditingMode)
                    {
                        case EditingMode.Block:
                            if (blockAt(e.Location))
                            {
                                deleteBlockAt(e.Location);
                                drawingBlocks = false;
                                erasingBlocks = true;
                            }
                            else
                            {
                                createBlockAt(e.Location);
                                drawingBlocks = true;
                                erasingBlocks = false;
                            }
                            //Position blockPos = CoordinateTransform.ScreenToObjectSpace(
                            //    e.Location.ToXnaPoint(), camera, mainForm.CurrentLayer).ToPosition();
                            //SaveBlock hitBlock = mainForm.SaveBlocks.SingleOrDefault(b => b.Position == blockPos);
                            //if (hitBlock == null)
                            //{
                            //    mainForm.SaveBlocks.Add(new SaveBlock(blockPos));
                            //}
                            //else
                            //{
                            //    mainForm.SaveBlocks.Remove(hitBlock);
                            //}
                            break;
                        case EditingMode.Texture:
                            mouseCoordAtMouseDown = e.Location.ToXnaPoint();
                            updateSelectedTextures(e.Location, getClickOperation());
                            selectedTextures.ForEach(t => t.CoordAtMouseDown = t.t.Coord);
                            draggingTextures = selectedTextures.Count != 0;
                            break;
                    }

                }
            }
            else if (e.Button == MouseButtons.XButton1)
            {
                mainForm.CurrentLayer--;
            }
            else if (e.Button == MouseButtons.XButton2)
            {
                mainForm.CurrentLayer++;
            }
            lastMouseLocation = e.Location;
            mainForm.Updated();
        }

        private void MainView_MouseMove(object sender, MouseEventArgs e)
        {
            if (!(e.Button == System.Windows.Forms.MouseButtons.Left ||
                e.Button == System.Windows.Forms.MouseButtons.Right ||
                e.Button == System.Windows.Forms.MouseButtons.Middle))
            { //no buttons down
                return;
            }

            //since MouseMove was called, the mouse must have moved
            mouseHasMovedSinceMouseDown = true;

            Point movement = e.Location.ToXnaPoint().Subtract(lastMouseLocation.ToXnaPoint());
            if (panning)
            {
                camera.Offset = camera.Offset.Add(movement);
            }
            else if (draggingTextures)
            {
                Point changeInProjectionSpace = e.Location.ToXnaPoint().Subtract(mouseCoordAtMouseDown).Divide(camera.Zoom);
                selectedTextures.ForEach(tex => tex.t.Coord = tex.CoordAtMouseDown.Add(changeInProjectionSpace));
            }
            else if (drawingBlocks)
            {
                createBlockAt(e.Location);
            }
            else if (erasingBlocks)
            {
                deleteBlockAt(e.Location);
            }
            
            lastMouseLocation = e.Location;
            mainForm.Updated();
            //mainForm.SaveBlocks.Clear();
            //mainForm.SaveBlocks.Add(new SaveBlock(CoordinateTransform.ScreenToObjectSpace(e.Location.ToXnaPoint(), camera, mainForm.CurrentLayer).ToPosition()));
        }

        private void MainView_MouseUp(object sender, MouseEventArgs e)
        {
            panning = false;
            draggingTextures = false;
            if (!mouseHasMovedSinceMouseDown)
            {
                MainView_MouseUpWithoutMoving(sender, e);
            }
        }

        private void MainView_MouseUpWithoutMoving(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                switch (mainForm.EditingMode)
                {
                    case EditingMode.Block:
                        updateSelectedBlocks(e.Location, getClickOperation());
                        if (selectedBlocks.Count != 0)
                        {
                            blockContextMenu.Show(this, e.Location);
                        }
                        break;
                    case EditingMode.Texture:
                        updateSelectedTextures(e.Location, getClickOperation());
                        if (selectedTextures.Count != 0)
                        {
                            textureContextMenu.Show(this, e.Location);
                        }
                        break;
                }
            }
        }

        public void MainView_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                camera.ZoomIn(e.Location.ToXnaPoint(), Width, Height);
                mainForm.RefreshTitle();
            }
            else if (e.Delta < 0)
            {
                camera.ZoomOut(e.Location.ToXnaPoint(), Width, Height);
                mainForm.RefreshTitle();
            }
            mainForm.Updated();
        }

        public void MainView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                mainForm.CurrentFrame.Textures.Remove(selectedTextures.GetUnderlyingTextures2DWithDoublePos());
                selectedTextures.Clear();
                mainForm.Updated();
            }
            if (e.KeyCode == Keys.Up)
            {
                mainForm.CurrentLayer++;
            }
            if (e.KeyCode == Keys.Down)
            {
                mainForm.CurrentLayer--;
            }
        }

        #region Context menus
        private void TextureContextMenuBringToFront(object sender, EventArgs e)
        {
            mainForm.CurrentFrame.Textures.BringToFront(selectedTextures.GetUnderlyingTextures2DWithDoublePos());
            mainForm.Updated();
        }

        private void TextureContextMenuBringForward(object sender, EventArgs e)
        {
            mainForm.CurrentFrame.Textures.BringForward(selectedTextures.GetUnderlyingTextures2DWithDoublePos());
            mainForm.Updated();
        }

        private void TextureContextMenuSendBackward(object sender, EventArgs e)
        {
            mainForm.CurrentFrame.Textures.SendBackward(selectedTextures.GetUnderlyingTextures2DWithDoublePos());
            mainForm.Updated();
        }

        private void TextureContextMenuSendToBack(object sender, EventArgs e)
        {
            mainForm.CurrentFrame.Textures.SendToBack(selectedTextures.GetUnderlyingTextures2DWithDoublePos());
            mainForm.Updated();
        }

        private void TextureContextMenuDelete(object sender, EventArgs e)
        {
            mainForm.CurrentFrame.Textures.Remove(selectedTextures.GetUnderlyingTextures2DWithDoublePos());
            selectedTextures.Clear();
            mainForm.Updated();
        }

        private void BlockContextMenuEditDetails(object sender, EventArgs e)
        {
            new BlockDetailsForm(selectedBlocks, lastMouseLocation);
        }
        #endregion



        private void createBlockAt(System.Drawing.Point mouseLocation)
        {
            Position pos = posUnderPoint(mouseLocation);
            if (!mainForm.SaveBlocks.Any(block => block.Position == pos))
            {
                mainForm.SaveBlocks.Add(new SaveBlock(pos));
            }
            mainForm.Updated();
        }

        private void deleteBlockAt(System.Drawing.Point mouseLocation)
        {
            Position pos = posUnderPoint(mouseLocation);
            mainForm.SaveBlocks.RemoveAll(block => block.Position == pos);
            mainForm.Updated();
        }

        private bool blockAt(System.Drawing.Point mouseLocation)
        {
            Position pos = posUnderPoint(mouseLocation);
            return mainForm.SaveBlocks.Any(block => block.Position == pos);
        }

        private Position posUnderPoint(System.Drawing.Point mouseLocation)
        {
            return CoordinateTransform.ScreenToObjectSpace(
                mouseLocation.ToXnaPoint(), camera, mainForm.CurrentLayer).ToPosition();
        }

        private SaveBlock getHitBlockInCurrentLayer(System.Drawing.Point mouseLocation)
        {
            Point mousePosInProjSpace = CoordinateTransform.ScreenToProjSpace(mouseLocation.ToXnaPoint(), camera);

            mainForm.Updated();
            return getHitBlock(
                mainForm.SaveBlocks.Where(block => block.Position.Z == mainForm.CurrentLayer),
                mousePosInProjSpace.ToSDPoint());
        }

        private SaveBlock getHitBlock(IEnumerable<SaveBlock> outOf, System.Drawing.Point pointInProjSpace)
        {
            BoundingBoxInt boundingBox = new BoundingBoxInt(outOf.ToPositions());

            foreach (SaveBlock block in outOf.OrderBy(block => boundingBox.getRelativeDepthOf(block.Position)))
            {
                System.Drawing.Region blockRegion = BlockRegions.WholeBlock.Offset(
                    CoordinateTransform.ObjectToProjSpace(block.Position).ToXnaPoint().Add(
                    Constants.blockDrawOffset.ToXnaPoint()));
                if (blockRegion.IsVisible(pointInProjSpace))
                {
                    return block;
                }
            }
            return null;
        }

        /// <summary>
        /// Updates the field TODO "selectedBlocks" to include the first block that
        /// contains "point". If no block meet this requirement, "selectedTextures" is cleared.
        /// </summary>
        /// <param name="mousePos"></param>
        /// <param name="clickOperation">The operation to be performed on the hit block.</param>
        private void updateSelectedBlocks(System.Drawing.Point mousePos, ClickOperation clickOperation)
        {
            Position pos = CoordinateTransform.ScreenToObjectSpace(mousePos.ToXnaPoint(), camera, mainForm.CurrentLayer).ToPosition();
            Point mousePosInProjSpace = CoordinateTransform.ScreenToProjSpace(mousePos.ToXnaPoint(), camera);

            SaveBlock hitAlreadyExistingBlock = getHitBlock(
                mainForm.SaveBlocks.Where(block => block.Position.Z >= mainForm.CurrentLayer),
                mousePosInProjSpace.ToSDPoint());

            SaveBlock newBlock = new SaveBlock(pos);

            switch (clickOperation)
            {
                case ClickOperation.Add:
                    if (hitAlreadyExistingBlock == null)
                    {
                        mainForm.SaveBlocks.Add(newBlock);
                        selectedBlocks.Add(newBlock);
                    }
                    break;
                case ClickOperation.Replace:
                    selectedBlocks.Clear();
                    if (hitAlreadyExistingBlock == null)
                    {
                        mainForm.SaveBlocks.Add(newBlock);
                        selectedBlocks.Add(newBlock);
                    }
                    else
                    {
                    }
                    break;
            }
            mainForm.Updated();
        }

        /// <summary>
        /// Updates the field "selectedTextures" to include the first texture that
        /// contains "point" and isn't transparent at that pixel. If no texture meet
        /// these requirements, "selectedTextures" is cleared.
        /// </summary>
        /// <param name="mouseLocation"></param>
        /// <param name="clickOperation">The operation to be performed on the hit textures.</param>
        private void updateSelectedTextures(System.Drawing.Point mouseLocation, ClickOperation clickOperation)
        {
            Point mousePosInProjSpace = CoordinateTransform.ScreenToProjSpace(mouseLocation.ToXnaPoint(), camera);

            foreach (Texture2DWithPos hitTexture in mainForm.CurrentFrame.Textures.FrontToBack())
            {
                if (hitTexture.Bounds.Contains(mousePosInProjSpace) &&
                    hitTexture.Texture.GetPixelColor(mousePosInProjSpace.Subtract(hitTexture.Coord)).A != 0)
                {
                    Texture2DWithDoublePos hitAlreadySelectedTexture = null;
                    foreach (Texture2DWithDoublePos selectedTexture in selectedTextures)
                    {
                        if (hitTexture == selectedTexture.t)
                        {
                            hitAlreadySelectedTexture = selectedTexture;
                            break;
                        }
                    }
                    switch (clickOperation)
                    {
                        case ClickOperation.Add:
                            if (hitAlreadySelectedTexture == null)
                            {
                                selectedTextures.Add(new Texture2DWithDoublePos(hitTexture));
                            }
                            break;
                        case ClickOperation.Replace:
                            if (hitAlreadySelectedTexture == null)
                            {
                                selectedTextures.Clear();
                                selectedTextures.Add(new Texture2DWithDoublePos(hitTexture));
                            }
                            break;
                        case ClickOperation.Subtract:
                            if (hitAlreadySelectedTexture != null)
                            {
                                selectedTextures.Remove(hitAlreadySelectedTexture);
                            }
                            break;
                        case ClickOperation.Toggle:
                            if (hitAlreadySelectedTexture != null)
                            {
                                selectedTextures.Remove(hitAlreadySelectedTexture);
                            }
                            else
                            {
                                selectedTextures.Add(new Texture2DWithDoublePos(hitTexture));
                            }
                            break;
                    }
                    mainForm.Updated();
                    return;
                }
            }

            selectedTextures.Clear();
            mainForm.Updated();
        }

        private static ClickOperation getClickOperation()
        {
            if (ModifierKeys == Keys.Shift)
            {
                return ClickOperation.Toggle;
            }
            if (ModifierKeys == Keys.Control)
            {
                return ClickOperation.Copy;
            }
            return ClickOperation.Replace;
        }

        private void MainView_Resize(object sender, EventArgs e)
        {
            System.Console.WriteLine("mainview resized");
        }
    }

    class Texture2DWithDoublePos
    {
        public Texture2DWithPos t;
        public Point CoordAtMouseDown;

        public Texture2DWithDoublePos(Texture2DWithPos t)
        {
            this.t = t;
        }
    }
}
