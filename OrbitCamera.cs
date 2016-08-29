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
	class OrbitCamera : Camera
	{

		float latitude;
		float longitude;
		float altitude;

		bool camScrolling = false;
		Vector3 cameraLocation;
		float angularVelocity;
		float latVelocity;
		float lonVelocity;
		float upDownVelocity;
		Vector3 target;
		float zeroRadius;

		const float PI180		= (float)Math.PI / 180;

		public float Altitude
		{
			get { return altitude; }
		}
		public float Latitude
		{
			get { return latitude; }
		}
		public float Longitude
		{
			get { return longitude; }
		}


		public OrbitCamera( Game game ) : base(game)
		{
			
		}


		public override void Initialize()
		{

			base.Initialize();
			
			var rndr = Game.GetService<ParticleSystem>();

			zeroRadius = 0.0f;
			latitude	= 0;
			longitude	= 0;
			altitude	= 150.0f;

			target = new Vector3(0, 1, 0);

			angularVelocity = 0.005f;
			latVelocity		= 0.005f;
			lonVelocity		= 0.005f;
			upDownVelocity	= 1;

			
		}

		void InputDevice_MouseScroll(object sender, InputDevice.MouseScrollEventArgs e)
		{
			Log.Message("...mouse scroll event : {0}", e.WheelDelta);
			if (e.WheelDelta > 0)
				altitude -= 0.1f;
			else
			{
				Console.WriteLine("down");
				altitude++;
			}
		}

		public override void Update(GameTime gameTime)
		{		
			Config.FreeCamEnabled	= false;
			
			var rndr	= Game.GetService<ParticleSystem>();
			var ds		= Game.GetService<DebugStrings>();

			angularVelocity	= 0.25f;
			upDownVelocity = 0.7f;

			if ( Game.InputDevice.IsKeyDown( Keys.LeftShift ) ) {
				angularVelocity *= 3;
				upDownVelocity	*= 3;
			}

			Game.InputDevice.MouseScroll += InputDevice_MouseScroll;

			if (camScrolling) {

				altitude -= 1.5f;

				if (altitude < 1f)
				{
					//target = new Vector3(0, 0, 0);
					altitude = 150;
					camScrolling = false;
				}

			}

			//Console.WriteLine(target);

			latVelocity = angularVelocity;
			lonVelocity = angularVelocity;

			if ( Game.InputDevice.IsKeyDown( Keys.W ) ) {
				latitude += latitude > 85 ? 0 : latVelocity * gameTime.Elapsed.Milliseconds;
				
			}
			if ( Game.InputDevice.IsKeyDown( Keys.S ) ) {
				latitude -= latitude < -85 ? 0 : latVelocity * gameTime.Elapsed.Milliseconds;
			}
			if ( Game.InputDevice.IsKeyDown( Keys.A ) ) {
				longitude -= lonVelocity * gameTime.Elapsed.Milliseconds;
			}
			if ( Game.InputDevice.IsKeyDown( Keys.D ) ) {
				longitude += lonVelocity * gameTime.Elapsed.Milliseconds;
			}

			if ( Game.InputDevice.IsKeyDown( Keys.Space ) ) {
				altitude += upDownVelocity * gameTime.Elapsed.Milliseconds / 3;
			}


			if ( Game.InputDevice.IsKeyDown( Keys.C ) ) {
				altitude -= upDownVelocity * gameTime.Elapsed.Milliseconds / 3;
			}
			if (Game.InputDevice.IsKeyDown(Keys.H))
			{
				//float distance = 100.0f;
				//for (float i = 0; i < 1000; i += 0.1f)
				//{
				////	if (i % 1000 == 0)
				////		altitude -= upDownVelocity * gameTime.Elapsed.Milliseconds / 3;
				//	altitude = altitude - i;
				//}
				camScrolling = true;



				//altitude = 0;
			}

			if (Game.InputDevice.IsKeyDown(Keys.K))
			{
				//zeroRadius = 100;
				target = new Vector3(0, 0, 0);
			}

			if ( altitude < 0.1f ) {
				altitude = 0.1f;
				camScrolling = false;
			}

			var input = Game.InputDevice;
			input.IsMouseHidden = false;
			input.IsMouseCentered = false;
			input.IsMouseClipped = true;

			cameraLocation = anglesToCoords( latitude, longitude, (zeroRadius + altitude) );
//			base.SetPose( cameraLocation, 0, 0, 0 );
//			base.LookAt( cameraLocation, new Vector3(0, 0, 0), new Vector3( 0, 1, 0) );
			base.SetupCamera( cameraLocation, new Vector3(0, 0, 0), target, new Vector3(0, 0, 0),
				65.0f, base.Config.FreeCamZNear,base.Config.FreeCamZFar, 0, 0 );
			//base.

			//ds.Add( "Altitude = " + altitude + " m" );
			//ds.Add( "Longitude = " + longitude );
			//ds.Add( "Latitude = " + latitude );
			
			base.Update(gameTime);
		}


		Vector3 anglesToCoords( float lat, float lon, float radius )
		{
			lat = lat * PI180;
			lon = lon * PI180;
			float rSmall	= radius * (float)( Math.Cos( lat ) );
			float X = (float)( rSmall * Math.Sin( lon ) );
 			float Z = (float)( rSmall * Math.Cos( lon ) );
			float Y = radius * (float)( Math.Sin( lat ) );
			return new Vector3( X, Y, Z );
		}

		public void scrolling()
		{
			camScrolling = true;
		}


	}
}
