using System;
using Fusion;
using Fusion.Mathematics;
using Fusion.Graphics;
using Fusion.Input;
using Fusion.Development;
using Fusion.UserInterface;

namespace GraphVis
{
    public class GraphVis : Game
    {
        /// <summary>
        /// GraphVis constructor
        /// 
        /// </summary>
        public GraphVis()
            : base()
        {
            //	enable object tracking :
            Parameters.TrackObjects = true;

            //	uncomment to enable debug graphics device:
            //	(MS Platform SDK must be installed)
            //	Parameters.UseDebugDevice	=	true;

            //	add services :
            AddService(new SpriteBatch(this), false, false, 0, 0);
            AddService(new DebugStrings(this), true, true, 9999, 9999);
            AddService(new DebugRender(this), true, true, 9998, 9998);
            AddService(new OrbitCamera(this), true, false, 9997, 9997);

            //	add here additional services :
            AddService(new ParticleSystem(this), true, true, 9996, 9996);
            AddService(new Directory(this), true, false, 9997, 9997);
            AddService(new UserInterface(this, "segoe40"), true, true, 5000, 5000);

            //	load configuration for each service :
            LoadConfiguration();

            //	make configuration saved on exit :
            Exiting += Game_Exiting;
        }



        //string[] filenames = System.IO.Directory.GetFiles(@"D:\DATAART\new\edges");

        /// <summary>q
        /// Initializes game :
        /// </summary>
        protected override void Initialize()
        {
            //	initialize services :
            base.Initialize();
            InputDevice.MouseScroll += InputDevice_MouseScroll;




            var cam = GetService<OrbitCamera>();

            InputDevice.IsMouseHidden = true;

            cam.Config.FreeCamEnabled = true;

            //	add keyboard handler :
            InputDevice.KeyDown += InputDevice_KeyDown;

            //	load content & create graphics and audio resources here:
        }

        void InputDevice_MouseScroll(object sender, InputDevice.MouseScrollEventArgs e)
        {
            Log.Message("...mouse scroll event : {0}", e.WheelDelta);
        }




        /// <summary>
        /// Disposes game
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //	dispose disposable stuff here
                //	Do NOT dispose objects loaded using ContentManager.
            }
            base.Dispose(disposing);
        }



        /// <summary>
        /// Handle keys
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void InputDevice_KeyDown(object sender, Fusion.Input.InputDevice.KeyEventArgs e)
        {
            if (e.Key == Keys.F1)
            {
                DevCon.Show(this);
            }

            if (e.Key == Keys.F5)
            {
                Reload();
            }

            if (e.Key == Keys.F12)
            {
                GraphicsDevice.Screenshot();
            }

            if (e.Key == Keys.Escape)
            {
                Exit();
            }

        }



        /// <summary>
        /// Saves configuration on exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Game_Exiting(object sender, EventArgs e)
        {
            SaveConfiguration();
        }

        /// <summary>
        /// Updates game
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {
            //	Update stuff here :

            var ds = GetService<DebugStrings>();

            ds.Add(Color.Orange, "FPS {0}", gameTime.Fps);
            ds.Add("F1   - show developer console");
            ds.Add("F5   - build content and reload textures");
            ds.Add("F12  - make screenshot");
            ds.Add("ESC  - exit");


            var cam = GetService<OrbitCamera>();
            var debRen = GetService<DebugRender>();

            int w = GraphicsDevice.DisplayBounds.Width;
            int h = GraphicsDevice.DisplayBounds.Height;


            var partSys = GetService<ParticleSystem>();

            partSys.setBuffers(true);



            if (InputDevice.IsKeyDown(Keys.I))
            {
                partSys.AddMaxParticles();
            }

            base.Update(gameTime);

        }

        /// <summary>
        /// Draws game
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="stereoEye"></param>
        protected override void Draw(GameTime gameTime, StereoEye stereoEye)
        {
            base.Draw(gameTime, stereoEye);

            //	Draw stuff here :
        }
    }
}
