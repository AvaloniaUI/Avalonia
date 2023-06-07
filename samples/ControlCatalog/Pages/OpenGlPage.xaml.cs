using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Platform.Interop;
using Avalonia.Threading;
using static Avalonia.OpenGL.GlConsts;
// ReSharper disable StringLiteralTypo

namespace ControlCatalog.Pages
{
    public class OpenGlPage : UserControl
    {

    }

    public class OpenGlPageControl : OpenGlControlBase
    {
        private float _yaw;

        public static readonly DirectProperty<OpenGlPageControl, float> YawProperty =
            AvaloniaProperty.RegisterDirect<OpenGlPageControl, float>("Yaw", o => o.Yaw, (o, v) => o.Yaw = v);

        public float Yaw
        {
            get => _yaw;
            set => SetAndRaise(YawProperty, ref _yaw, value);
        }

        private float _pitch;

        public static readonly DirectProperty<OpenGlPageControl, float> PitchProperty =
            AvaloniaProperty.RegisterDirect<OpenGlPageControl, float>("Pitch", o => o.Pitch, (o, v) => o.Pitch = v);

        public float Pitch
        {
            get => _pitch;
            set => SetAndRaise(PitchProperty, ref _pitch, value);
        }


        private float _roll;

        public static readonly DirectProperty<OpenGlPageControl, float> RollProperty =
            AvaloniaProperty.RegisterDirect<OpenGlPageControl, float>("Roll", o => o.Roll, (o, v) => o.Roll = v);

        public float Roll
        {
            get => _roll;
            set => SetAndRaise(RollProperty, ref _roll, value);
        }


        private float _disco;

        public static readonly DirectProperty<OpenGlPageControl, float> DiscoProperty =
            AvaloniaProperty.RegisterDirect<OpenGlPageControl, float>("Disco", o => o.Disco, (o, v) => o.Disco = v);

        public float Disco
        {
            get => _disco;
            set => SetAndRaise(DiscoProperty, ref _disco, value);
        }

        private string _info = string.Empty;

        public static readonly DirectProperty<OpenGlPageControl, string> InfoProperty =
            AvaloniaProperty.RegisterDirect<OpenGlPageControl, string>("Info", o => o.Info, (o, v) => o.Info = v);

        public string Info
        {
            get => _info;
            private set => SetAndRaise(InfoProperty, ref _info, value);
        }
        
        private int _vertexShader;
        private int _fragmentShader;
        private int _shaderProgram;
        private int _vertexBufferObject;
        private int _indexBufferObject;
        private int _vertexArrayObject;

        private string GetShader(bool fragment, string shader)
        {
            var version = (GlVersion.Type == GlProfileType.OpenGL ?
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 150 : 120 :
                100);
            var data = "#version " + version + "\n";
            if (GlVersion.Type == GlProfileType.OpenGLES)
                data += "precision mediump float;\n";
            if (version >= 150)
            {
                shader = shader.Replace("attribute", "in");
                if (fragment)
                    shader = shader
                        .Replace("varying", "in")
                        .Replace("//DECLAREGLFRAG", "out vec4 outFragColor;")
                        .Replace("gl_FragColor", "outFragColor");
                else
                    shader = shader.Replace("varying", "out");
            }

            data += shader;

            return data;
        }


        private string VertexShaderSource => GetShader(false, @"
        attribute vec3 aPos;
        attribute vec3 aNormal;
        uniform mat4 uModel;
        uniform mat4 uProjection;
        uniform mat4 uView;

        varying vec3 FragPos;
        varying vec3 VecPos;  
        varying vec3 Normal;
        uniform float uTime;
        uniform float uDisco;
        void main()
        {
            float discoScale = sin(uTime * 10.0) / 10.0;
            float distortionX = 1.0 + uDisco * cos(uTime * 20.0) / 10.0;
            
            float scale = 1.0 + uDisco * discoScale;
            
            vec3 scaledPos = aPos;
            scaledPos.x = scaledPos.x * distortionX;
            
            scaledPos *= scale;
            gl_Position = uProjection * uView * uModel * vec4(scaledPos, 1.0);
            FragPos = vec3(uModel * vec4(aPos, 1.0));
            VecPos = aPos;
            Normal = normalize(vec3(uModel * vec4(aNormal, 1.0)));
        }
");

        private string FragmentShaderSource => GetShader(true, @"
        varying vec3 FragPos; 
        varying vec3 VecPos; 
        varying vec3 Normal;
        uniform float uMaxY;
        uniform float uMinY;
        uniform float uTime;
        uniform float uDisco;
        //DECLAREGLFRAG

        void main()
        {
            float y = (VecPos.y - uMinY) / (uMaxY - uMinY);
            float c = cos(atan(VecPos.x, VecPos.z) * 20.0 + uTime * 40.0 + y * 50.0);
            float s = sin(-atan(VecPos.z, VecPos.x) * 20.0 - uTime * 20.0 - y * 30.0);

            vec3 discoColor = vec3(
                0.5 + abs(0.5 - y) * cos(uTime * 10.0),
                0.25 + (smoothstep(0.3, 0.8, y) * (0.5 - c / 4.0)),
                0.25 + abs((smoothstep(0.1, 0.4, y) * (0.5 - s / 4.0))));

            vec3 objectColor = vec3((1.0 - y), 0.40 +  y / 4.0, y * 0.75 + 0.25);
            objectColor = objectColor * (1.0 - uDisco) + discoColor * uDisco;

            float ambientStrength = 0.3;
            vec3 lightColor = vec3(1.0, 1.0, 1.0);
            vec3 lightPos = vec3(uMaxY * 2.0, uMaxY * 2.0, uMaxY * 2.0);
            vec3 ambient = ambientStrength * lightColor;


            vec3 norm = normalize(Normal);
            vec3 lightDir = normalize(lightPos - FragPos);  

            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * lightColor;

            vec3 result = (ambient + diffuse) * objectColor;
            gl_FragColor = vec4(result, 1.0);

        }
");

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
        }

        private readonly Vertex[] _points;
        private readonly ushort[] _indices;
        private readonly float _minY;
        private readonly float _maxY;


        public OpenGlPageControl()
        {
            var name = typeof(OpenGlPage).Assembly.GetManifestResourceNames().First(x => x.Contains("teapot.bin"));
            using (var sr = new BinaryReader(typeof(OpenGlPage).Assembly.GetManifestResourceStream(name)!))
            {
                var buf = new byte[sr.ReadInt32()];
                sr.Read(buf, 0, buf.Length);
                var points = new float[buf.Length / 4];
                Buffer.BlockCopy(buf, 0, points, 0, buf.Length);
                buf = new byte[sr.ReadInt32()];
                sr.Read(buf, 0, buf.Length);
                _indices = new ushort[buf.Length / 2];
                Buffer.BlockCopy(buf, 0, _indices, 0, buf.Length);
                _points = new Vertex[points.Length / 3];
                for (var primitive = 0; primitive < points.Length / 3; primitive++)
                {
                    var srci = primitive * 3;
                    _points[primitive] = new Vertex
                    {
                        Position = new Vector3(points[srci], points[srci + 1], points[srci + 2])
                    };
                }

                for (int i = 0; i < _indices.Length; i += 3)
                {
                    Vector3 a = _points[_indices[i]].Position;
                    Vector3 b = _points[_indices[i + 1]].Position;
                    Vector3 c = _points[_indices[i + 2]].Position;
                    var normal = Vector3.Normalize(Vector3.Cross(c - b, a - b));

                    _points[_indices[i]].Normal += normal;
                    _points[_indices[i + 1]].Normal += normal;
                    _points[_indices[i + 2]].Normal += normal;
                }

                for (int i = 0; i < _points.Length; i++)
                {
                    _points[i].Normal = Vector3.Normalize(_points[i].Normal);
                    _maxY = Math.Max(_maxY, _points[i].Position.Y);
                    _minY = Math.Min(_minY, _points[i].Position.Y);
                }
            }

        }

        private static void CheckError(GlInterface gl)
        {
            int err;
            while ((err = gl.GetError()) != GL_NO_ERROR)
                Console.WriteLine(err);
        }

        protected override unsafe void OnOpenGlInit(GlInterface GL)
        {
            CheckError(GL);

            Info = $"Renderer: {GL.GetString(GL_RENDERER)} Version: {GL.GetString(GL_VERSION)}";
            
            // Load the source of the vertex shader and compile it.
            _vertexShader = GL.CreateShader(GL_VERTEX_SHADER);
            Console.WriteLine(GL.CompileShaderAndGetError(_vertexShader, VertexShaderSource));

            // Load the source of the fragment shader and compile it.
            _fragmentShader = GL.CreateShader(GL_FRAGMENT_SHADER);
            Console.WriteLine(GL.CompileShaderAndGetError(_fragmentShader, FragmentShaderSource));

            // Create the shader program, attach the vertex and fragment shaders and link the program.
            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, _vertexShader);
            GL.AttachShader(_shaderProgram, _fragmentShader);
            const int positionLocation = 0;
            const int normalLocation = 1;
            GL.BindAttribLocationString(_shaderProgram, positionLocation, "aPos");
            GL.BindAttribLocationString(_shaderProgram, normalLocation, "aNormal");
            Console.WriteLine(GL.LinkProgramAndGetError(_shaderProgram));
            CheckError(GL);

            // Create the vertex buffer object (VBO) for the vertex data.
            _vertexBufferObject = GL.GenBuffer();
            // Bind the VBO and copy the vertex data into it.
            GL.BindBuffer(GL_ARRAY_BUFFER, _vertexBufferObject);
            CheckError(GL);
            var vertexSize = Marshal.SizeOf<Vertex>();
            fixed (void* pdata = _points)
                GL.BufferData(GL_ARRAY_BUFFER, new IntPtr(_points.Length * vertexSize),
                    new IntPtr(pdata), GL_STATIC_DRAW);

            _indexBufferObject = GL.GenBuffer();
            GL.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _indexBufferObject);
            CheckError(GL);
            fixed (void* pdata = _indices)
                GL.BufferData(GL_ELEMENT_ARRAY_BUFFER, new IntPtr(_indices.Length * sizeof(ushort)), new IntPtr(pdata),
                    GL_STATIC_DRAW);
            CheckError(GL);
            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);
            CheckError(GL);
            GL.VertexAttribPointer(positionLocation, 3, GL_FLOAT,
                0, vertexSize, IntPtr.Zero);
            GL.VertexAttribPointer(normalLocation, 3, GL_FLOAT,
                0, vertexSize, new IntPtr(12));
            GL.EnableVertexAttribArray(positionLocation);
            GL.EnableVertexAttribArray(normalLocation);
            CheckError(GL);

        }

        protected override void OnOpenGlDeinit(GlInterface GL)
        {
            // Unbind everything
            GL.BindBuffer(GL_ARRAY_BUFFER, 0);
            GL.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);

            // Delete all resources.
            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteBuffer(_indexBufferObject);
            GL.DeleteVertexArray(_vertexArrayObject);
            GL.DeleteProgram(_shaderProgram);
            GL.DeleteShader(_fragmentShader);
            GL.DeleteShader(_vertexShader);
        }

        static Stopwatch St = Stopwatch.StartNew();
        protected override unsafe void OnOpenGlRender(GlInterface gl, int fb)
        {
            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
            gl.Enable(GL_DEPTH_TEST);
            gl.Viewport(0, 0, (int)Bounds.Width, (int)Bounds.Height);
            var GL = gl;

            GL.BindBuffer(GL_ARRAY_BUFFER, _vertexBufferObject);
            GL.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _indexBufferObject);
            GL.BindVertexArray(_vertexArrayObject);
            GL.UseProgram(_shaderProgram);
            CheckError(GL);
            var projection =
                Matrix4x4.CreatePerspectiveFieldOfView((float)(Math.PI / 4), (float)(Bounds.Width / Bounds.Height),
                    0.01f, 1000);


            var view = Matrix4x4.CreateLookAt(new Vector3(25, 25, 25), new Vector3(), new Vector3(0, 1, 0));
            var model = Matrix4x4.CreateFromYawPitchRoll(_yaw, _pitch, _roll);
            var modelLoc = GL.GetUniformLocationString(_shaderProgram, "uModel");
            var viewLoc = GL.GetUniformLocationString(_shaderProgram, "uView");
            var projectionLoc = GL.GetUniformLocationString(_shaderProgram, "uProjection");
            var maxYLoc = GL.GetUniformLocationString(_shaderProgram, "uMaxY");
            var minYLoc = GL.GetUniformLocationString(_shaderProgram, "uMinY");
            var timeLoc = GL.GetUniformLocationString(_shaderProgram, "uTime");
            var discoLoc = GL.GetUniformLocationString(_shaderProgram, "uDisco");
            GL.UniformMatrix4fv(modelLoc, 1, false, &model);
            GL.UniformMatrix4fv(viewLoc, 1, false, &view);
            GL.UniformMatrix4fv(projectionLoc, 1, false, &projection);
            GL.Uniform1f(maxYLoc, _maxY);
            GL.Uniform1f(minYLoc, _minY);
            GL.Uniform1f(timeLoc, (float)St.Elapsed.TotalSeconds);
            GL.Uniform1f(discoLoc, _disco);
            CheckError(GL);
            GL.DrawElements(GL_TRIANGLES, _indices.Length, GL_UNSIGNED_SHORT, IntPtr.Zero);

            CheckError(GL);
            if (_disco > 0.01)
                RequestNextFrameRendering();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == YawProperty || change.Property == RollProperty || change.Property == PitchProperty ||
                change.Property == DiscoProperty)
                RequestNextFrameRendering();
            base.OnPropertyChanged(change);
        }
    }
}
