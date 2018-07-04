using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace HuntTheWumpus3
{

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        Camera camera;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont courierFont;
        Texture2D roomDoor;
        Texture2D pit;
        Texture2D wump;
        Texture2D arrow;
        Texture2D zubat;
        Vector2 textPos;
        Vector2 endGameText;
        Model cube;
        Model sphere;
        State currentGameState;
        int batRoom;
        int wumpRoom;
        int arrowSelectIndex = 1;
        int[] arrowRooms = new int[5];
        int[] arrowPath = new int[6];
        string arrowOutput = "";
        int selectedRooms = 0;
        int currentIndex = 0;
        Vector3 cameraPostionOffset = new Vector3(100, -100, 600);
        Vector3 mapcenter = new Vector3(300, 350, 0); //approximate for adjusting camera projection
        double timer;

        private KeyboardState oldKeyboardState, currentKeyboardState;

        Map gameBoard;
        //Room locations
        Vector2[] roomVectors;
        Texture2D lineTexture;
        Vector2 arrowVector;

        public enum State
        {
            Playing,
            Fell,
            Superbat,
            Shooting,
            ArrowSelect,
            WumpBump,
            WumpWin,
            PlayerWin,
            PlayerHit
        }
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 700;
            graphics.PreferredBackBufferWidth = 1100;
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            this.IsMouseVisible = true;
            gameBoard = new Map();
            camera = new Camera(this, new Vector3(0, 0, 150), Vector3.Zero, Vector3.Up);
            Components.Add(camera);

            currentGameState = State.Playing;
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            Mouse.WindowHandle = Window.Handle;
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            courierFont = Content.Load<SpriteFont>("GameFont");
            cube = Content.Load<Model>("cube");
            sphere = Content.Load<Model>("sphere");
            roomDoor = Content.Load<Texture2D>("room");
            pit = Content.Load<Texture2D>("pit");
            arrow = Content.Load<Texture2D>("arrow");
            wump = Content.Load<Texture2D>("wump");
            zubat = Content.Load<Texture2D>("zubat");
            lineTexture = new Texture2D(GraphicsDevice, 1, 1);
            lineTexture.SetData<Color>(new Color[] { Color.White });

            roomVectors = new Vector2[21]
            {
                new Vector2(), //dummy
                new Vector2 (220,25),
                new Vector2 (420,166),
                new Vector2 (343,403),
                new Vector2 (96,403),
                new Vector2 (20,167),
                new Vector2 (88,188),
                new Vector2 (140,120),
                new Vector2 (220,93),
                new Vector2 (303,120),
                new Vector2 (352,189),
                new Vector2 (352,279),
                new Vector2 (303,347),
                new Vector2 (219,371),
                new Vector2 (136,345),
                new Vector2 (90,275),
                new Vector2 (154,255),
                new Vector2 (181,174),
                new Vector2 (263,177),
                new Vector2 (286,254),
                new Vector2 (219,301)
            };

            textPos = new Vector2(graphics.GraphicsDevice.Viewport.Width - 190, 0);
            arrowVector = new Vector2(335, 425);
            endGameText = new Vector2(graphics.GraphicsDevice.Viewport.Width - 105, 1);


            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }
        Vector2 MoveVector(Vector2 position, Vector2 target, float speed)
        {
            double direction = Math.Atan2(target.Y - position.Y, target.X - position.X);

            Vector2 move = new Vector2(0, 0);

            move.X = (float)Math.Cos(direction) * speed;
            move.Y = (float)Math.Sin(direction) * speed;

            return move;

        }
        void playerMove(int dest)
        {
            //update player position
            gameBoard.player.pos = dest;
            if (gameBoard.Pits.Contains(dest))
                currentGameState = State.Fell; //update state
            else if (gameBoard.Bats.Contains(dest))
            {
                currentGameState = Game1.State.Playing; //update state
                batRoom = dest;
                //generate new room for player to be carried to
                int newRoom = -1;
                Random randomGenerator = new Random();
                do
                {
                    newRoom = randomGenerator.Next(1, 20);
                } while (newRoom == dest);
                playerMove(newRoom);
            }
            else if (gameBoard.wump.pos == dest)
            {
                //check if already awake
                if (gameBoard.wump.awake)
                    currentGameState = Game1.State.WumpWin;
                else //wump was sleeping
                {
                    gameBoard.wump.awake = true;
                    gameBoard.WumpusMove();
                    currentGameState = State.Playing;
                    //if wumpus didn't move then he wins
                    if (gameBoard.wump.pos == gameBoard.player.pos)
                        currentGameState = State.WumpWin;
                }
            }
            else //regular move
                currentGameState = State.Playing;
            
        }
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            oldKeyboardState = currentKeyboardState;
            currentKeyboardState = Keyboard.GetState();

            if (gameBoard.player.pos == 1 || gameBoard.player.pos == 2 || gameBoard.player.pos == 3 || gameBoard.player.pos == 4 || gameBoard.player.pos == 5)
                camera.update(new Vector3(300, 300, 800), mapcenter);
            else
                camera.update(new Vector3(roomVectors[gameBoard.player.pos], 0) + cameraPostionOffset, (new Vector3(roomVectors[gameBoard.player.pos], 0) + mapcenter) / 2);

            // OLD LOGIC
            if (gameBoard.wump.awake && gameBoard.player.pos == gameBoard.wump.pos || gameBoard.player.arrows == 0)
                currentGameState = State.WumpWin;
            if (currentGameState == State.Playing)
            {
                if (wasKeyPressed(Keys.A))
                {
                    currentGameState = State.ArrowSelect;
                    selectedRooms = 0;
                    arrowRooms = new int[5];
                    arrowSelectIndex = gameBoard.getAdjacentRooms(gameBoard.player.pos)[0];
                }
                    
                else
                {
                    int[] adjacent = gameBoard.getAdjacentRooms(gameBoard.player.pos);
                    if (wasKeyPressed(Keys.Right))
                        if (currentIndex == 2)
                            currentIndex = 0;
                        else
                            currentIndex++;
                    else if (wasKeyPressed(Keys.Left))
                        if (currentIndex == 0)
                            currentIndex = 2;
                        else
                            currentIndex--;

                    //check if enter pressed
                    if (wasKeyPressed(Keys.Enter))
                    {
                        gameBoard.WumpusMove();
                        //check if ran into bat
                        if (gameBoard.Bats.Contains(adjacent[currentIndex]))
                        {
                            currentGameState = State.Superbat;
                            batRoom = adjacent[currentIndex];
                            timer = 0;
                        }
                        //check if bumped into wump
                        else if (gameBoard.wump.pos == adjacent[currentIndex])
                        {
                            currentGameState = State.WumpBump;
                            wumpRoom = adjacent[currentIndex];
                            timer = 0;
                        }
                        else
                            gameBoard.PlayerMove(adjacent[currentIndex], ref currentGameState);
                        currentIndex = 0;
                        gameBoard.WumpusMove();
                    }
                }
                
            }
            else if (currentGameState == State.ArrowSelect)
            {
                camera.update(new Vector3(300, 300, 800), mapcenter); //change camera to view all rooms
                if (wasKeyPressed(Keys.A))
                    if (selectedRooms != 0)
                    {
                        currentGameState = State.Shooting;
                        fireArrow(ref arrowRooms);
                    }
                    else
                        currentGameState = State.Playing;

                else if (wasKeyPressed(Keys.Right))
                    if (arrowSelectIndex == 20)
                        arrowSelectIndex = 1;
                    else
                        arrowSelectIndex++;
                else if (wasKeyPressed(Keys.Left))
                    if (arrowSelectIndex == 1)
                        arrowSelectIndex = 20;
                    else
                        arrowSelectIndex--;

                else if (wasKeyPressed(Keys.Enter)) //select arrow room
                {
                    if (selectedRooms > 0)
                    {
                        if (arrowRooms[selectedRooms - 1] == arrowSelectIndex) //invalid
                            ;//do nothing 
                        else
                            arrowRooms[selectedRooms++] = arrowSelectIndex;
                    }
                    else
                        arrowRooms[selectedRooms++] = arrowSelectIndex;
                    if (selectedRooms >= 5)
                    {
                        currentGameState = State.Shooting;
                        fireArrow(ref arrowRooms);
                    }
                }
            }
            else if (currentGameState == State.Shooting)
            {
                camera.update(new Vector3(300, 300, 800), mapcenter);
                if (wasKeyPressed(Keys.Enter))
                    currentGameState = State.Playing;
            }
            else if (currentGameState == State.Superbat)
            {
                timer += gameTime.ElapsedGameTime.TotalSeconds;
                if (timer >= 3) //superbat move after drawing sprite for 3 seconds
                    playerMove(batRoom); //update state
            }
            else if (currentGameState == State.WumpBump)
            {
                timer += gameTime.ElapsedGameTime.TotalSeconds;
                if (timer >= 3) //superbat move after drawing sprite for 4 seconds
                    playerMove(wumpRoom); //update state

            }
            else if (currentGameState == State.Fell || currentGameState == State.WumpWin || currentGameState == State.PlayerWin || currentGameState == State.PlayerHit)
            {
                camera.update(new Vector3(300, 300, 800), mapcenter);
                if (Keyboard.GetState().GetPressedKeys().Contains(Keys.D1))
                {
                    gameBoard.ReplayGame();
                    selectedRooms = 0;
                    currentGameState = State.Playing;
                }
                else if (Keyboard.GetState().GetPressedKeys().Contains(Keys.D2))
                {
                    gameBoard.NewGame();
                    currentGameState = State.Playing;
                }
            }

            base.Update(gameTime);
        }
        public bool wasKeyPressed(Keys key)
        {
            return oldKeyboardState.IsKeyDown(key) && currentKeyboardState.IsKeyUp(key);
        }

        private void fireArrow(ref int[] rooms)
        {
            if (selectedRooms > 0)
            {
                gameBoard.ShootArrow(ref rooms, selectedRooms, ref currentGameState); //pass arrow rooms, func will pass back arrow path
                arrowPath = rooms; //update arrow path
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            DrawMap();
            spriteBatch.Begin();
            if (currentGameState == State.Playing)
            {
                spriteBatch.DrawString(courierFont, "Left/Right to select", endGameText - new Vector2(100, 0), Color.White);
                spriteBatch.DrawString(courierFont, "\nEnter to move", endGameText - new Vector2(30, 0), Color.White);

            }
            else if (currentGameState == State.Fell)
            {
                spriteBatch.DrawString(courierFont, "You fell!", endGameText, Color.Red);
                spriteBatch.DrawString(courierFont, "\n1 to replay board", endGameText - new Vector2(75, 0), Color.White);
                spriteBatch.DrawString(courierFont, "\n\n2 to play new board", endGameText - new Vector2(95, 0), Color.White);
            }
            else if (currentGameState == State.ArrowSelect)
            {
                spriteBatch.DrawString(courierFont, "Left/Right to select", endGameText - new Vector2(100, 0), Color.White);
                spriteBatch.DrawString(courierFont, "\nEnter to pick", endGameText - new Vector2(30, 0), Color.White);
            }
            else if (currentGameState == State.Shooting)
            {
                spriteBatch.DrawString(courierFont, gameBoard.player.arrows + " arrows left", endGameText - new Vector2(27, 0), Color.White);
            }
            else if (currentGameState == State.WumpWin)
            {
                spriteBatch.DrawString(courierFont, "Wumpus wins!", endGameText - new Vector2(5, 0), Color.Red);
                spriteBatch.DrawString(courierFont, "\n1 to replay board", endGameText - new Vector2(75, 0), Color.White);
                spriteBatch.DrawString(courierFont, "\n\n2 to play new board", endGameText - new Vector2(95, 0), Color.White);
            }
            else if (currentGameState == State.PlayerWin)
            {
                spriteBatch.DrawString(courierFont, "You win!", endGameText - new Vector2(15, 0), Color.Red);
                spriteBatch.DrawString(courierFont, "\n1 to replay board", endGameText - new Vector2(75, 0), Color.Orange);
                spriteBatch.DrawString(courierFont, "\n\n2 to play new board", endGameText - new Vector2(95, 0), Color.Orange);
            }
            else if (currentGameState == State.PlayerHit)
            {
                spriteBatch.DrawString(courierFont, "Suicide!", endGameText - new Vector2(15, 0), Color.Red);
                spriteBatch.DrawString(courierFont, "\n1 to replay board", endGameText - new Vector2(75, 0), Color.Orange);
                spriteBatch.DrawString(courierFont, "\n\n2 to play new board", endGameText - new Vector2(95, 0), Color.Orange);
            }

            spriteBatch.End();


            base.Draw(gameTime);
        }

        void DrawMap()
        {
            //draw layout (rooms, text)
            //spriteBatch.DrawString(courierFont, "Hunt The Wumpus 2D", textPos, Color.Goldenrod);
            spriteBatch.Begin();
            spriteBatch.DrawString(courierFont, gameBoard.returnHazards(), new Vector2(0, 0), Color.White);
            spriteBatch.End();
            //draw rooms
            for (int i=1; i<=20; i++)
            {
                foreach (int j in gameBoard.getAdjacentRooms(i))
                {
                    DrawConnector(i, j, Color.White);
                }

                DrawRoom(i);
            }
            if (currentGameState == State.Shooting || currentGameState == State.PlayerWin || currentGameState == State.WumpWin || currentGameState == State.PlayerHit)
            {
                for (int i = 1; i < selectedRooms+1; i++)
                    DrawConnector(arrowPath[i - 1], arrowPath[i], Color.Red);
            }
            //spriteBatch.Draw(arrow, arrowVector, Color.White);
            //spriteBatch.DrawString(courierFont, "" + gameBoard.player.arrows, arrowVector + new Vector2(arrow.Width / 2, arrow.Height + 10), Color.White);
        }

        private void DrawRoom(int i)
        {
            Vector3 position = new Vector3(roomVectors[i], 0);
            Matrix[] transforms = new Matrix[sphere.Bones.Count];
            Color color = Color.Black;

            if (gameBoard.player.pos == i)
                color = Color.Red;
            else if (currentGameState == State.Playing && i == gameBoard.getAdjacentRooms(gameBoard.player.pos)[currentIndex])
                color = Color.White;
            else if (currentGameState == State.ArrowSelect && arrowSelectIndex == i)
                color = Color.White;
            else if ((currentGameState == State.ArrowSelect || currentGameState == State.Shooting || currentGameState == State.PlayerHit) && arrowRooms.Contains(i))
                color = Color.Yellow;

            sphere.CopyAbsoluteBoneTransformsTo(transforms);
            foreach (ModelMesh mesh in sphere.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.DiffuseColor = color.ToVector3();
                    effect.SpecularColor = new Vector3(0,0,0);
                    effect.EnableDefaultLighting();
                    effect.World = transforms[mesh.ParentBone.Index] *  Matrix.CreateTranslation(1.5f*position.X, 1.5f*position.Y, position.Z);
                    effect.View = camera.view;
                    effect.Projection = camera.projection;
                }
                mesh.Draw();
                //spriteBatch.Begin();
                BasicEffect basic = new BasicEffect(GraphicsDevice)
                {
                    TextureEnabled = true,
                    VertexColorEnabled = true
                };
                position += new Vector3(-5, 5, 0);
                basic.View = camera.view;
                basic.Projection = camera.projection;
                basic.World = Matrix.CreateScale(1, -1, 1) * Matrix.CreateTranslation(1.5f * position.X, 1.5f * position.Y, position.Z);
                spriteBatch.Begin(0, null, null, null, null, basic);

                color = Color.White;
                if (currentGameState == State.Fell && gameBoard.player.pos == i)
                {
                    spriteBatch.Draw(pit, new Vector2(-20, -25), Color.White);
                }
                else if ((currentGameState == State.WumpBump || currentGameState == State.WumpWin || currentGameState == State.PlayerWin) && gameBoard.wump.pos == i)
                {
                    spriteBatch.Draw(wump, new Vector2(-20, -25), Color.White);
                }
                else if (currentGameState == State.Superbat && batRoom == i)
                {
                    spriteBatch.Draw(zubat, new Vector2(-20, -25), Color.White);
                }
                else
                    spriteBatch.DrawString(courierFont, "" + i, Vector2.Zero, Color.White);
                spriteBatch.End();
            }
        }

        private void DrawConnector(int start, int finish, Color color)
        {
            Vector2 begin = roomVectors[start] + new Vector2(0, 0);
            Vector2 end = roomVectors[finish] + new Vector2(0, 0);
            Vector2 middle = begin + end / 2;
            Vector2 diff = end - begin;
            float angle = (float)Math.Atan2(diff.Y, diff.X);
            Model m =cube;
            Matrix[] transforms = new Matrix[m.Bones.Count];
            m.CopyAbsoluteBoneTransformsTo(transforms);
            foreach (ModelMesh mesh in m.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.DiffuseColor = color.ToVector3();
                    effect.EnableDefaultLighting();
                    effect.View = camera.view;
                    effect.Projection = camera.projection;
                    effect.World = transforms[mesh.ParentBone.Index] * Matrix.CreateScale(diff.Length() /25, 2, 1) * Matrix.CreateRotationZ(angle) * Matrix.CreateTranslation(new Vector3(middle, 0));
               
                }
                mesh.Draw();
            }
        }
    }
}
