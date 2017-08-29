using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Direct2D1.Media;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using SharpDX.Mathematics;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Buffer = SharpDX.Direct3D11.Buffer;
using DeviceContext = SharpDX.Direct3D11.DeviceContext;
using Factory1 = SharpDX.DXGI.Factory1;
using InputElement = SharpDX.Direct3D11.InputElement;
using Matrix = SharpDX.Matrix;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;

namespace Direct3DInteropSample
{
    class MainWindow : Window
    {
        private SharpDX.Direct3D11.Device _d3dDevice;
        private SharpDX.DXGI.Device _dxgiDevice;
        Texture2D backBuffer = null;
        RenderTargetView renderView = null;
        Texture2D depthBuffer = null;
        DepthStencilView depthView = null;
        private readonly SwapChain _swapChain;
        private SwapChainDescription _desc;
        private Matrix _proj = Matrix.Identity;
        private Matrix _view;
        private Buffer _contantBuffer;
        private SharpDX.Direct2D1.Device _d2dDevice;
        private SharpDX.Direct2D1.DeviceContext _d2dContext;
        private RenderTarget _d2dRenderTarget;
        private MainWindowViewModel _model;

        public MainWindow()
        {
            _dxgiDevice = AvaloniaLocator.Current.GetService<SharpDX.DXGI.Device>();
            _d3dDevice = _dxgiDevice.QueryInterface<SharpDX.Direct3D11.Device>();
            _d2dDevice = AvaloniaLocator.Current.GetService<SharpDX.Direct2D1.Device>();
            DataContext = _model = new MainWindowViewModel();
            _desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription =
                   new ModeDescription((int)ClientSize.Width, (int)ClientSize.Height,
                            new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = PlatformImpl?.Handle.Handle ?? IntPtr.Zero,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            _swapChain = new SwapChain(new Factory1(), _d3dDevice, _desc);

            _d2dContext = new SharpDX.Direct2D1.DeviceContext(_d2dDevice, DeviceContextOptions.None)
            {
                DotsPerInch = new Size2F(96, 96)
            };

            CreateMesh();
            _view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            this.GetObservable(ClientSizeProperty).Subscribe(Resize);
            Resize(ClientSize);
            AvaloniaXamlLoader.Load(this);
            Background = Avalonia.Media.Brushes.Transparent;
        }


        protected override void HandlePaint(Rect rect)
        {
            var viewProj = Matrix.Multiply(_view, _proj);
            var context = _d3dDevice.ImmediateContext;
            // Clear views
            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(renderView, Color.White);

            var time = 50;
            // Update WorldViewProj Matrix
            var worldViewProj = Matrix.RotationX((float) _model.RotationX) * Matrix.RotationY((float) _model.RotationY) *
                                Matrix.RotationZ((float) _model.RotationZ)
                                * Matrix.Scaling((float) _model.Zoom) * viewProj;
            worldViewProj.Transpose();
            context.UpdateSubresource(ref worldViewProj, _contantBuffer);

            // Draw the cube
            context.Draw(36, 0);
            base.HandlePaint(rect);
            // Present!
            _swapChain.Present(0, PresentFlags.None);
        }

        
        void CreateMesh()
        {
            var device = _d3dDevice;
            // Compile Vertex and Pixel shaders
            var vertexShaderByteCode = ShaderBytecode.CompileFromFile("MiniCube.fx", "VS", "vs_4_0");
            var vertexShader = new VertexShader(device, vertexShaderByteCode);

            var pixelShaderByteCode = ShaderBytecode.CompileFromFile("MiniCube.fx", "PS", "ps_4_0");
            var pixelShader = new PixelShader(device, pixelShaderByteCode);

            var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
            // Layout from VertexShader input signature
            var layout = new InputLayout(device, signature, new[]
                    {
                        new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                        new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0)
                    });
            
            // Instantiate Vertex buiffer from vertex data
            var vertices = Buffer.Create(device, BindFlags.VertexBuffer, new[]
                                  {
                                      new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), // Front
                                      new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                                      new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                                      new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                                      new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                                      new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),

                                      new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), // BACK
                                      new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                                      new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                                      new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                                      new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                                      new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),

                                      new Vector4(-1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), // Top
                                      new Vector4(-1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                                      new Vector4( 1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                                      new Vector4(-1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                                      new Vector4( 1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                                      new Vector4( 1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),

                                      new Vector4(-1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f), // Bottom
                                      new Vector4( 1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                                      new Vector4(-1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                                      new Vector4(-1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                                      new Vector4( 1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                                      new Vector4( 1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),

                                      new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f), // Left
                                      new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                                      new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                                      new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                                      new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                                      new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),

                                      new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f), // Right
                                      new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                                      new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                                      new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                                      new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                                      new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                            });

            // Create Constant Buffer
            _contantBuffer = new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            var context = _d3dDevice.ImmediateContext;

            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, Utilities.SizeOf<Vector4>() * 2, 0));
            context.VertexShader.SetConstantBuffer(0, _contantBuffer);
            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);
        }

        void Resize(Size size)
        {
            Utilities.Dispose(ref _d2dRenderTarget);
            Utilities.Dispose(ref backBuffer);
            Utilities.Dispose(ref renderView);
            Utilities.Dispose(ref depthBuffer);
            Utilities.Dispose(ref depthView);
            var context = _d3dDevice.ImmediateContext;
            // Resize the backbuffer
            _swapChain.ResizeBuffers(_desc.BufferCount, (int)size.Width, (int)size.Height, Format.Unknown, SwapChainFlags.None);

            // Get the backbuffer from the swapchain
            backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);

            // Renderview on the backbuffer
            renderView = new RenderTargetView(_d3dDevice, backBuffer);

            // Create the depth buffer
            depthBuffer = new Texture2D(_d3dDevice, new Texture2DDescription()
            {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = (int)size.Width,
                Height = (int)size.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });

            // Create the depth buffer view
            depthView = new DepthStencilView(_d3dDevice, depthBuffer);

            // Setup targets and viewport for rendering
            context.Rasterizer.SetViewport(new Viewport(0, 0, (int)size.Width, (int)size.Height, 0.0f, 1.0f));
            context.OutputMerger.SetTargets(depthView, renderView);

            // Setup new projection matrix with correct aspect ratio
            _proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, (float)(size.Width / size.Height), 0.1f, 100.0f);

            using (var dxgiBackBuffer = _swapChain.GetBackBuffer<Surface>(0))
            {
                _d2dRenderTarget = new RenderTarget(AvaloniaLocator.Current.GetService<SharpDX.Direct2D1.Factory>()
                    , dxgiBackBuffer, new RenderTargetProperties
                    {
                        DpiX = 96,
                        DpiY = 96,
                        Type = RenderTargetType.Default,
                        PixelFormat = new PixelFormat(Format.Unknown, AlphaMode.Premultiplied)
                    });
            }

        }

        class D3DRenderTarget: IRenderTarget
        {
            private readonly MainWindow _window;

            public D3DRenderTarget(MainWindow window)
            {
                _window = window;
            }
            public void Dispose()
            {
                
            }

            public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
            {
                return new DrawingContextImpl(visualBrushRenderer, null, _window._d2dRenderTarget,
                    AvaloniaLocator.Current.GetService<SharpDX.DirectWrite.Factory>(),
                    AvaloniaLocator.Current.GetService<ImagingFactory>());
            }
        }


        protected override IRenderTarget CreateRenderTarget() => new D3DRenderTarget(this);
    }
}
