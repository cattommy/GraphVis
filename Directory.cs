using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Graphics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Fusion.Audio;
using Fusion.Input;
using Fusion.Content;
using Fusion.Development;
using System.IO;
using Fusion.Mathematics;
using System.Globalization;
using System.Threading;

namespace GraphVis
{
	class Directory : GameService
	{
				public class Configure
		{



			string baseDataDir;

			[Category("Base Directory")]
			[Description("Path to edge list data")]
			public string BaseDirectory { get { return baseDataDir; } set { baseDataDir = value; } }

			public Configure()
			{
				baseDataDir = @"D:\clustering\";
			}

		}


		[Config]
		public Configure cfg { get; set; }

		public Directory(Game game)
			: base(game)
		{
			cfg = new Configure();
		}



		public override void Initialize()
		{
			base.Initialize();
		}


	}
}
