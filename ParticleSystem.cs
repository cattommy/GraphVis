using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Graphics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Fusion.Input;
using Fusion.Mathematics;
using Fusion.UserInterface;

namespace GraphVis
{



    public enum IntegratorType
    {
        EULER = 0x8,
        RUNGE_KUTTA = 0x8 << 1
    }

    public class ParticleConfig
    {


        float maxParticleMass;
        float minParticleMass;
        float rotation;
        IntegratorType iType;

        [Category("Particle mass")]
        [Description("Largest particle mass")]
        public float Max_mass { get { return maxParticleMass; } set { maxParticleMass = value; } }

        [Category("Particle mass")]
        [Description("Smallest particle mass")]
        public float Min_mass { get { return minParticleMass; } set { minParticleMass = value; } }

        [Category("Integrator type")]
        [Description("Integrator type")]
        public IntegratorType IType { get { return iType; } set { iType = value; } }

        [Category("Initial rotation")]
        [Description("Rate of initial rotation")]
        public float Rotation { get { return rotation; } set { rotation = value; } }

        public ParticleConfig()
        {
            minParticleMass = 0.001f;
            maxParticleMass = 0.001f;
            rotation = 2.6f;
            iType = IntegratorType.RUNGE_KUTTA;
        }
    }


    public class ParticleSystem : GameService
    {


        [Config]
        public ParticleConfig cfg { get; set; }

        Texture2D texture;
        Ubershader shader;

        public State state;

        static float distance = 0.99f;

        const int BlockSize = 512;

        const int MaxInjectingParticles = 53;
        const int MaxSimulatedParticles = MaxInjectingParticles;

        float MaxParticleMass;
        float MinParticleMass;
        float spinRate;
        float linkSize;

        public static int graphTimer;
        public static bool graphPlay;

        int[,] linkArr;

        int injectionCount = 0;
        Particle3d[] injectionBufferCPU;

        StructuredBuffer simulationBufferSrc;
        float ParentSize;
        List<List<int>> graphEdges;
        SpriteFont font;
        SpriteFont calibri;
        StructuredBuffer simulationBufferDst;
        StructuredBuffer linksPtrBuffer;
        LinkId[] linksPtrBufferCPU;
        List<Tuple<int, int>> newEdges = new List<Tuple<int, int>>();
        StructuredBuffer linksBuffer;
        Link[] linksBufferCPU;

        bool[] listForDfs;

        int[] linkCount;
        int maxLinkCount;

        ConstantBuffer paramsCB;
        List<List<int>> linkPtrLists;


        List<Link> linkList;
        public List<Particle3d> ParticleList;

        int[,] linkData;

        string[] dstrings;
        public static int[,] data;
        public double[,] dataSt;

        // Particle in 3d space:
        [StructLayout(LayoutKind.Explicit)]
        public struct Particle3d
        {
            [FieldOffset(0)]
            public Vector3 Position;
            [FieldOffset(12)]
            public Vector3 Velocity;
            [FieldOffset(24)]
            public Vector4 Color0;
            [FieldOffset(40)]
            public float Size0;
            [FieldOffset(44)]
            public float TotalLifeTime;
            [FieldOffset(48)]
            public float LifeTime;
            [FieldOffset(52)]
            public int linksPtr;
            [FieldOffset(56)]
            public int linksCount;
            [FieldOffset(60)]
            public Vector3 Acceleration;
            [FieldOffset(72)]
            public float Mass;
            [FieldOffset(76)]
            public float Charge;
            [FieldOffset(80)]
            public int Id;


            public override string ToString()
            {
                return string.Format("life time = {0}/{1}", LifeTime, TotalLifeTime);
            }

        }

        // link between 2 particles:
        [StructLayout(LayoutKind.Explicit)]
        struct Link
        {
            [FieldOffset(0)]
            public int par1;
            [FieldOffset(4)]
            public int par2;
            [FieldOffset(8)]
            public float length;
            [FieldOffset(12)]
            public float force2;
            [FieldOffset(16)]
            public Vector3 orientation;
        }


        [StructLayout(LayoutKind.Explicit)]
        struct LinkId
        {
            [FieldOffset(0)]
            public int id;
        }

        enum Flags
        {
            // for compute shader: 
            INJECTION = 0x1,
            SIMULATION = 0x1 << 1,
            MOVE = 0x1 << 2,
            EULER = 0x1 << 3,
            RUNGE_KUTTA = 0x1 << 4,
            // for geometry shader:
            POINT = 0x1 << 5,
            LINE = 0x1 << 6,
            COLOR = 0x1 << 7
        }

        public enum State
        {
            RUN,
            PAUSE
        }

        [StructLayout(LayoutKind.Explicit, Size = 160)]
        struct Params
        {
            [FieldOffset(0)]
            public Matrix View;
            [FieldOffset(64)]
            public Matrix Projection;
            [FieldOffset(128)]
            public int MaxParticles;
            [FieldOffset(132)]
            public float DeltaTime;
            [FieldOffset(136)]
            public float LinkSize;
            [FieldOffset(140)]
            public float MouseX;
            [FieldOffset(144)]
            public float MouseY;
        }

        Params param = new Params();

        Random rand = new Random();

        public bool linkptr;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="game"></param>
        public ParticleSystem(Game game)
            : base(game)
        {
            cfg = new ParticleConfig();
        }

        StateFactory factory;


        /// <summary>
        /// 
        /// </summary>
        public override void Initialize()
        {
            texture = Game.Content.Load<Texture2D>("smaller");
            shader = Game.Content.Load<Ubershader>("shaders");
            font = Game.Content.Load<SpriteFont>("segoe40");
            calibri = Game.Content.Load<SpriteFont>("Calibri");
            factory = new StateFactory(shader, typeof(Flags), (ps, i) => Enum(ps, (Flags)i));

            int w = Game.GraphicsDevice.DisplayBounds.Width;
            int h = Game.GraphicsDevice.DisplayBounds.Height;

            var inpdevice = Game.InputDevice;

            inpdevice.KeyDown += InputDevice_KeyDown;


            var ui = Game.GetService<UserInterface>();


            ui.RootFrame = new Frame(Game, 0, 0, w, h, "", Color.Transparent)
            {
                Border = 1
            };


            maxLinkCount = MaxSimulatedParticles * MaxSimulatedParticles;
            paramsCB = new ConstantBuffer(Game.GraphicsDevice, typeof(Params));

            var Dirs = Game.GetService<Directory>();


            MaxParticleMass = cfg.Max_mass;
            MinParticleMass = cfg.Min_mass;
            spinRate = cfg.Rotation;
            linkSize = 1.0f;

            linkptr = true;
            listForDfs = new bool[MaxInjectingParticles];
            graphEdges = new List<List<int>>();

            linkList = new List<Link>();
            ParticleList = new List<Particle3d>();
            linkPtrLists = new List<List<int>>();

            graphTimer = 0;
            graphPlay = false;

            linkArr = new int[MaxInjectingParticles, MaxInjectingParticles];

            linkCount = new int[MaxInjectingParticles];
            state = State.RUN;

            base.Initialize();
        }




        void Enum(PipelineState ps, Flags flag)
        {
            ps.Primitive = Primitive.PointList;
            ps.RasterizerState = RasterizerState.CullNone;
            ps.BlendState = BlendState.Additive;
            ps.DepthStencilState = DepthStencilState.Readonly;
        }


        void return_Click(object sender, Frame.MouseEventArgs e)
        {
            AddMaxParticles();
        }

        void minus_Click(object sender, Frame.MouseEventArgs e)
        {
            distance -= 0.1f;

            if (distance < 0.1f)
                distance = 0.1f;
        }

        void plus_Click(object sender, Frame.MouseEventArgs e)
        {
            distance += 0.1f;

        }

        public void Pause()
        {
            if (state == State.RUN)
            {
                state = State.PAUSE;
            }
            else
            {
                state = State.RUN;
            }
        }


        /// <summary>
        /// Returns random radial vector
        /// </summary>
        /// <returns></returns>
        Vector3 RadialRandomVector()
        {
            Vector3 r;
            do
            {
                r = rand.NextVector3(-Vector3.One, Vector3.One);
            } while (r.Length() > 1);

            r.Normalize();

            return r;
        }


        public void AddMaxParticles(int N = MaxInjectingParticles)
        {
            if (simulationBufferSrc != null)
            {

                simulationBufferSrc.Dispose();
                simulationBufferSrc = null;
            }

            ParticleList.Clear();
            linkList.Clear();
            linkPtrLists.Clear();

            addChain(N);

        }

        void addParticle(Vector3 pos, float lifeTime, float size0, int id, float colorBoost = 1)
        {
            float ParticleMass = rand.NextFloat(MinParticleMass, MaxParticleMass); //mass

            ParticleList.Add(new Particle3d
            {
                Position = pos,
                Velocity = Vector3.Zero,
                Color0 = rand.NextVector4(Vector4.Zero, Vector4.One) * colorBoost,
                Size0 = size0,
                TotalLifeTime = lifeTime,
                LifeTime = 0,
                Acceleration = Vector3.Zero,
                Mass = ParticleMass,
                Charge = 0.1f,
                Id = id,

            }
            );

            linkPtrLists.Add(new List<int>());
            graphEdges.Add(new List<int>());

        }

        public void addLink(int end1, int end2)
        {

            if (linkArr[end1, end2] == 0)
            {
                int linkNumber = linkList.Count;
                linkList.Add(new Link
                {
                    par1 = end1,
                    par2 = end2,
                    length = linkSize,
                    force2 = 100,
                    orientation = Vector3.Zero
                }
                );


                graphEdges[end1].Add(end2);

                if (linkPtrLists.ElementAtOrDefault(end1) == null)
                {

                    linkPtrLists.Insert(end1, new List<int>());
                }
                linkPtrLists[end1].Add(linkNumber);

                if (linkPtrLists.ElementAtOrDefault(end2) == null)
                {

                    linkPtrLists.Insert(end1, new List<int>());
                }
                linkPtrLists[end2].Add(linkNumber);

            }

        }


        void dfs(int node)
        {
            listForDfs[node] = true;

            for (int i = 0; i < graphEdges[node].Count; i++)
            {
                int nod = graphEdges[node][i];
                if (listForDfs[nod] == false)
                    dfs(nod);
            }
        }

        void addChain(int N)
        {
            Vector3 pos = new Vector3(0, 0, 0);

            for (int i = 0; i <= N; ++i)
            {

                addParticle(pos, 9999, 6.0f, i, 1.0f);
                pos += RadialRandomVector() * linkSize;
            }

        }


        public void setBuffers(bool switcher)
        {
            injectionBufferCPU = new Particle3d[ParticleList.Count];
            int iter = 0;

            if (simulationBufferSrc != null)
            {
                simulationBufferSrc.GetData(injectionBufferCPU);
                simulationBufferSrc.Dispose();

                foreach (var p in ParticleList)
                {
                    injectionBufferCPU[iter].Color0 = p.Color0;
                    if (switcher)
                    {
                        injectionBufferCPU[iter].Position = p.Position;
                    }
                    injectionBufferCPU[iter].Size0 = p.Size0;
                    ++iter;
                }
            }
            else
            {

                foreach (var p in ParticleList)
                {
                    injectionBufferCPU[iter] = p;
                    ++iter;
                }
            }
            linksBufferCPU = new Link[linkList.Count];
            iter = 0;
            foreach (var l in linkList)
            {
                linksBufferCPU[iter] = l;
                ++iter;
            }
            if (linkptr == true)
            {
                linksPtrBufferCPU = new LinkId[linkList.Count * 2];
                iter = 0;
                int lpIter = 0;
                foreach (var ptrList in linkPtrLists)
                {

                    int blockSize = 0;
                    injectionBufferCPU[iter].linksPtr = lpIter;
                    if (ptrList != null)
                    {
                        foreach (var linkPtr in ptrList)
                        {
                            linksPtrBufferCPU[lpIter] = new LinkId { id = linkPtr };
                            ++lpIter;
                            ++blockSize;
                        }
                    }
                    injectionBufferCPU[iter].linksCount = blockSize;
                    ++iter;
                }
            }


            if (linksBuffer != null)
            {
                linksBuffer.Dispose();
            }

            if (linksPtrBuffer != null)
            {
                linksPtrBuffer.Dispose();
            }

            if (injectionBufferCPU.Length != 0)
            {
                simulationBufferSrc = new StructuredBuffer(Game.GraphicsDevice, typeof(Particle3d), injectionBufferCPU.Length, StructuredBufferFlags.Counter);
                simulationBufferSrc.SetData(injectionBufferCPU);
            }
            if (linksBufferCPU.Length != 0)
            {
                linksBuffer = new StructuredBuffer(Game.GraphicsDevice, typeof(Link), linksBufferCPU.Length, StructuredBufferFlags.Counter);
                linksBuffer.SetData(linksBufferCPU);
            }
            if (linksPtrBufferCPU.Length != 0)
            {
                linksPtrBuffer = new StructuredBuffer(Game.GraphicsDevice, typeof(LinkId), linksPtrBufferCPU.Length, StructuredBufferFlags.Counter);
                linksPtrBuffer.SetData(linksPtrBufferCPU);
            }

        }

        /// <summary>
        /// Makes all particles wittingly dead
        /// </summary>
        void ClearParticleBuffer()
        {
            for (int i = 0; i < MaxInjectingParticles; i++)
            {
                injectionBufferCPU[i].TotalLifeTime = -999999;

            }
            injectionCount = 0;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                paramsCB.Dispose();

                simulationBufferSrc.Dispose();
                linksBuffer.Dispose();
                linksPtrBuffer.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            var ui = Game.GetService<UserInterface>();

            int w = Game.GraphicsDevice.DisplayBounds.Width;
            int h = Game.GraphicsDevice.DisplayBounds.Height;

        }

        void InputDevice_KeyDown(object sender, Fusion.Input.InputDevice.KeyEventArgs e)
        {
            if (Game.InputDevice.IsKeyDown(Keys.LeftButton))
            {

                if (ParticleList.Count > 0)
                {
                    var cam = Game.GetService<OrbitCamera>();

                    StereoEye stereoEye = Fusion.Graphics.StereoEye.Mono;

                    var viewMtx = cam.GetViewMatrix(stereoEye);
                    var projMtx = cam.GetProjectionMatrix(stereoEye);


                    var inp = Game.InputDevice;

                    int w = Game.GraphicsDevice.DisplayBounds.Width;
                    int h = Game.GraphicsDevice.DisplayBounds.Height;

                    param.MouseX = 2.0f * (float)inp.MousePosition.X / (float)w - 1.0f;
                    param.MouseY = -2.0f * (float)inp.MousePosition.Y / (float)h + 1.0f;

                    simulationBufferSrc.GetData(injectionBufferCPU);
                    foreach (var part in injectionBufferCPU)
                    {
                        var worldPos = new Vector4(part.Position, 1);
                        var viewPos = Vector4.Transform(worldPos, viewMtx);
                        var projPos = Vector4.Transform(viewPos, projMtx);
                        projPos /= projPos.W;
                        if ((Math.Abs(projPos.X /*/ projPos.Z */- param.MouseX) < 0.02f) && (Math.Abs(projPos.Y /*/ projPos.Z */- param.MouseY) < 0.02f))
                        {
                            Console.WriteLine(part.Size0 + " Size");
                            break;
                        }
                    }
                }
            }

            if (Game.InputDevice.IsKeyDown(Keys.Q))
            {
                Pause();
            }

            if (Game.InputDevice.IsKeyDown(Keys.B))
            {

                AddMaxParticles();
            }


        }


        /// <summary>
        /// 
        /// </summary>
        void SwapParticleBuffers()
        {
            var temp = simulationBufferDst;
            simulationBufferDst = simulationBufferSrc;
            simulationBufferSrc = temp;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="stereoEye"></param>
        public override void Draw(GameTime gameTime, Fusion.Graphics.StereoEye stereoEye)
        {
            var ui = Game.GetService<UserInterface>();
            ui.Draw(gameTime, StereoEye.Mono);

            Game.GraphicsDevice.ClearBackbuffer(Color.Black, 1, 0);
            var device = Game.GraphicsDevice;
            var cam = Game.GetService<OrbitCamera>();
            var inp = Game.InputDevice;

            int w = Game.GraphicsDevice.DisplayBounds.Width;
            int h = Game.GraphicsDevice.DisplayBounds.Height;

            param.View = cam.GetViewMatrix(stereoEye);
            param.Projection = cam.GetProjectionMatrix(stereoEye);
            param.MaxParticles = 0;
            param.DeltaTime = gameTime.ElapsedSec;
            param.LinkSize = linkSize;



            device.ComputeShaderConstants[0] = paramsCB;
            device.VertexShaderConstants[0] = paramsCB;
            device.GeometryShaderConstants[0] = paramsCB;
            device.PixelShaderConstants[0] = paramsCB;


            device.PixelShaderSamplers[0] = SamplerState.LinearWrap;


            //	Simulate : ------------------------------------------------------------------------
            

            param.MaxParticles = injectionCount;
            paramsCB.SetData(param);


            device.ComputeShaderConstants[0] = paramsCB;

            if (state == State.RUN)
            {

                for (int i = 0; i < 20; i++)
                {
                    // calculate accelerations: ---------------------------------------------------
                    device.SetCSRWBuffer(0, simulationBufferSrc, MaxSimulatedParticles);

                    device.ComputeShaderResources[2] = linksPtrBuffer;
                    device.ComputeShaderResources[3] = linksBuffer;

                    param.MaxParticles = MaxSimulatedParticles;
                    paramsCB.SetData(param);

                    device.ComputeShaderConstants[0] = paramsCB;

                    device.PipelineState = factory[(int)Flags.SIMULATION | (int)cfg.IType];


                    device.Dispatch(MathUtil.IntDivUp(MaxSimulatedParticles, BlockSize));


                    // move particles: ------------------------------------------------------------
                    device.SetCSRWBuffer(0, simulationBufferSrc, MaxSimulatedParticles);
                    device.ComputeShaderConstants[0] = paramsCB;

                    device.PipelineState = factory[(int)Flags.MOVE | (int)cfg.IType];
                    device.Dispatch(MathUtil.IntDivUp(MaxSimulatedParticles, BlockSize));

                }
            }
            

            // draw points: ------------------------------------------------------------------------

            device.PipelineState = factory[(int)Flags.POINT];
            device.PixelShaderResources[0] = texture;
            device.SetCSRWBuffer(0, null);
            device.GeometryShaderResources[1] = simulationBufferSrc;
            device.Draw(MaxSimulatedParticles, 0);
            device.Draw(ParticleList.Count, 0);


            // draw lines: --------------------------------------------------------------------------

            device.PipelineState = factory[(int)Flags.LINE];

            device.GeometryShaderResources[1] = simulationBufferSrc;
            device.GeometryShaderResources[3] = linksBuffer;

            device.Draw(linkList.Count, 0);

            ui.Draw(gameTime, stereoEye);

            // --------------------------------------------------------------------------------------

            var sb = Game.GetService<SpriteBatch>();
            sb.Begin();

            sb.End();

            var debStr = Game.GetService<DebugStrings>();

            debStr.Add(Color.Yellow, "drawing " + ParticleList.Count + " points");
            debStr.Add(Color.Yellow, "drawing " + linkList.Count + " lines");

            base.Draw(gameTime, stereoEye);
        }

    }

}

