using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Fusion;
using Fusion.Graphics;
using Fusion.Audio;
using Fusion.Input;
using Fusion.Content;
using Fusion.Development;
using Fusion.Mathematics;

namespace GraphVis
{
    class GeoCamera : Camera
    {

        float latitude;
        float longitude;
        float altitude;

        float angularVelocity;
        float latVelocity;
        float lonVelocity;
        float upDownVelocity;

        float EarthRadius;

        const float PI180 = (float)Math.PI / 180;

        public GeoCamera(Game game)
            : base(game)
        {

        }


        public override void Initialize()
        {

            base.Initialize();

            var rndr = Game.GetService<ParticleSystem>();

            latitude = 0;
            longitude = 0;

        }


        public override void Update(GameTime gameTime)
        {

            base.Update(gameTime);
            base.IsFreeCamEnabled = false;

            var rndr = Game.GetService<ParticleSystem>();
            var ds = Game.GetService<DebugStrings>();

            var input = Game.InputDevice;
            var vel = Config.FreeCamVelocity;
            var dt = gameTime.ElapsedSec;

            IsFreeCamEnabled = Config.FreeCamEnabled;



            input.IsMouseHidden = false;
            input.IsMouseCentered = false;
            input.IsMouseClipped = true;

        }

        public Vector3 AnglesToCoords(float lat, float lon, float radius)
        {
            lat = lat * PI180;
            lon = lon * PI180;
            float rSmall = radius * (float)(Math.Cos(lat));
            float X = (float)(rSmall * Math.Sin(lon));
            float Z = (float)(rSmall * Math.Cos(lon));
            float Y = radius * (float)(Math.Sin(lat));
            return new Vector3(X, Y, Z);
        }


    }
}
