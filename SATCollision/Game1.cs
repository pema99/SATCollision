using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace SATCollision
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public Texture2D Blank { get; set; }
        public Vector2[] Square { get; set; }
        public Vector2[] Triangle { get; set; }

        public CollisionResponse LastCollision { get; set; }

        public Game1()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Blank = new Texture2D(GraphicsDevice, 1, 1);
            Blank.SetData<Color>(new Color[] { Color.White });

            Triangle = new Vector2[] { new Vector2(250, 250), new Vector2(275, 320), new Vector2(300, 250) };
            Square = new Vector2[] { new Vector2(100, 100), new Vector2(200, 100), new Vector2(200, 200), new Vector2(100, 200) };
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (Keyboard.GetState().IsKeyDown(Keys.Up))
                MovePolygon(Square, new Vector2(0, -1));
            if (Keyboard.GetState().IsKeyDown(Keys.Down))
                MovePolygon(Square, new Vector2(0, 1));
            if (Keyboard.GetState().IsKeyDown(Keys.Left))
                MovePolygon(Square, new Vector2(-1, 0));
            if (Keyboard.GetState().IsKeyDown(Keys.Right))
                MovePolygon(Square, new Vector2(1, 0));

            LastCollision = GetCollisionResponse(Square, Triangle);
            
            if (LastCollision.Collides)
            {
                MovePolygon(Square, LastCollision.MTV * 0.5f);
                MovePolygon(Triangle, -LastCollision.MTV * 0.5f);
            }
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();
                
            if (LastCollision.Collides)
            {
                DrawPolygon(spriteBatch, Square, Color.Red);
                DrawPolygon(spriteBatch, Triangle, Color.Red);
            }
            else
            {
                DrawPolygon(spriteBatch, Square, Color.Black);
                DrawPolygon(spriteBatch, Triangle, Color.Black);
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        public struct CollisionResponse
        {
            public Vector2 MTVAxis { get; set; }
            public float MTVOverlap { get; set; }
            public bool Collides { get; set; }
            public Vector2 MTV { get; set; }
        }

        public void MovePolygon(Vector2[] Polygon, Vector2 Relative)
        {
            for (int i = 0; i < Polygon.Length; i++)
                Polygon[i] += Relative;
        }

        public bool TestCollision(Vector2[] Polygon1, Vector2[] Polygon2)
        {
            Vector2[] Axes1 = GetNormals(Polygon1);
            Vector2[] Axes2 = GetNormals(Polygon2);

            for (int i = 0; i < Axes1.Length; i++)
            {
                Vector2 Axis = Axes1[i];

                Axis.Normalize();
                Vector2 Projection1 = Project(Polygon1, Axis);
                Vector2 Projection2 = Project(Polygon2, Axis);

                if (GetProjectionOverlap(Projection1, Projection2) > 0)
                    return false;
            }

            for (int i = 0; i < Axes2.Length; i++)
            {
                Vector2 Axis = Axes2[i];

                Axis.Normalize();
                Vector2 Projection1 = Project(Polygon1, Axis);
                Vector2 Projection2 = Project(Polygon2, Axis);

                if (GetProjectionOverlap(Projection1, Projection2) > 0)
                    return false;
            }

            return true;
        }

        public CollisionResponse GetCollisionResponse(Vector2[] Polygon1, Vector2[] Polygon2)
        {
            Vector2 SmallestAxis = Vector2.Zero;
            float SmallestOverlap = float.PositiveInfinity;

            Vector2[] Axes1 = GetNormals(Polygon1);
            Vector2[] Axes2 = GetNormals(Polygon2);

            for (int i = 0; i < Axes1.Length; i++)
            {
                Vector2 Axis = Axes1[i];

                Axis.Normalize();
                Vector2 Projection1 = Project(Polygon1, Axis);
                Vector2 Projection2 = Project(Polygon2, Axis);

                float Overlap = GetProjectionOverlap(Projection1, Projection2);

                if (Overlap > 0)
                    return new CollisionResponse { Collides = false };
                else
                {
                    Overlap = Math.Abs(Overlap);
                    if (Overlap < SmallestOverlap)
                    {
                        SmallestOverlap = Overlap;
                        SmallestAxis = Axis;
                    }
                }
            }

            for (int i = 0; i < Axes2.Length; i++)
            {
                Vector2 Axis = Axes2[i];

                Axis.Normalize();
                Vector2 Projection1 = Project(Polygon1, Axis);
                Vector2 Projection2 = Project(Polygon2, Axis);

                float Overlap = GetProjectionOverlap(Projection1, Projection2);

                if (Overlap > 0)
                    return new CollisionResponse { Collides = false };
                else
                {
                    Overlap = Math.Abs(Overlap);
                    if (Overlap < SmallestOverlap)
                    {
                        SmallestOverlap = Overlap;
                        SmallestAxis = Axis;
                    }
                }
            }

            //Calculate MTV
            Vector2 Temp = SmallestAxis;
            Temp.Normalize();
            Vector2 TempMTV = new Vector2(Temp.X * SmallestOverlap, Temp.Y * SmallestOverlap);

            Vector2 D = GetCenter(Polygon1) - GetCenter(Polygon2);
            if (Vector2.Dot(D, TempMTV) < 0)
                TempMTV = -TempMTV;

            return new CollisionResponse { Collides = true, MTVAxis = SmallestAxis, MTVOverlap = SmallestOverlap, MTV = TempMTV };
        }

        public float GetProjectionOverlap(Vector2 Projection1, Vector2 Projection2)
        {
            //Where X is Min and Y is Max

            if (Projection1.X < Projection2.X)
            {
                return Projection2.X - Projection1.Y;
            }
            else
            {
                return Projection1.X - Projection2.Y;
            }
        }

        public Vector2 Project(Vector2[] Polygon, Vector2 Axis)
        {
            float Min = Vector2.Dot(Axis, Polygon[0]);
            float Max = Min;

            //i should be 1 or 0?
            for (int i = 0; i < Polygon.Length; i++)
            {
                // NOTE: the axis must be normalized to get accurate projections
                float P = Vector2.Dot(Polygon[i], Axis);
                if (P < Min)
                {
                    Min = P;
                }
                else if (P > Max)
                {
                    Max = P;
                }
            }

            return new Vector2(Min, Max);
        }

        public Vector2[] GetNormals(Vector2[] Polygon)
        {
            Vector2[] Axes = new Vector2[Polygon.Length];

            for (int i = 0; i < Polygon.Length; i++)
            {
                //Get 2 vertices
                Vector2 Vert1 = Polygon[i];
                Vector2 Vert2 = Polygon[i + 1 == Polygon.Length ? 0 : i + 1];

                //Subtract to get get edge vector
                Vector2 Edge = Vert1 - Vert2;

                //Perpendicular vector
                Vector2 Normal = new Vector2(-Edge.Y, Edge.X);

                Axes[i] = Normal;
            }

            return Axes;
        }

        public Vector2 GetCenter(Vector2[] Polygon)
        {
            Vector2 Total = Vector2.Zero;

            foreach (Vector2 V in Polygon)
                Total += V;

            return Total / Polygon.Length;
        }

        public void DrawPolygon(SpriteBatch spriteBatch, Vector2[] Polygon, Color color)
        {
            Point[] PointPolygon = new Point[Polygon.Length];
            for (int i = 0; i < Polygon.Length; i++)
                PointPolygon[i] = Polygon[i].ToPoint();

            for (int i = 0; i < PointPolygon.Length; i++)
            {
                var P1 = PointPolygon[i];
                var P2 = PointPolygon[(i + 1) % PointPolygon.Length];
                spriteBatch.Draw(Blank, new Rectangle(P1.X, P1.Y, (int)Math.Sqrt(Math.Pow(P1.X - P2.X, 2) + Math.Pow(P1.Y - P2.Y, 2)), 1), null, color, (float)Math.Atan2(P2.Y - P1.Y, P2.X - P1.X), new Vector2(0, 0), SpriteEffects.None, 0);
            }
        }
    }
}
