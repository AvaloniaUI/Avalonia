// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Direct2D1;
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

using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Buffer = SharpDX.Direct3D11.Buffer;
using DeviceContext = SharpDX.Direct2D1.DeviceContext;
using Factory2 = SharpDX.DXGI.Factory2;
using InputElement = SharpDX.Direct3D11.InputElement;
using Matrix = SharpDX.Matrix;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;
using Resource = SharpDX.Direct3D11.Resource;

namespace Direct3DInteropSample
{
    public class MainWindow : Window
    {
        Texture2D _backBuffer;
        RenderTargetView _renderView;
        Texture2D _depthBuffer;
        DepthStencilView _depthView;
        private readonly SwapChain _swapChain;
        private SwapChainDescription1 _desc;
        private Matrix _proj = Matrix.Identity;
        private readonly Matrix _view;
        private Buffer _contantBuffer;
        private DeviceContext _deviceContext;
        private readonly MainWindowViewModel _model;

        public MainWindow()
        {
            DataContext = _model = new MainWindowViewModel();

            _desc = new SwapChainDescription1()
            {
                BufferCount = 1,
                Width = (int)ClientSize.Width,
                Height = (int)ClientSize.Height,
                Format = Format.R8G8B8A8_UNorm,                
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            using (var factory = Direct2D1Platform.DxgiDevice.Adapter.GetParent<Factory2>())
            {
                _swapChain = new SwapChain1(factory, Direct2D1Platform.DxgiDevice, PlatformImpl?.Handle.Handle ?? IntPtr.Zero, ref _desc);
            }              

            _deviceContext = new DeviceContext(Direct2D1Platform.Direct2D1Device, DeviceContextOptions.None)
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
            var context = Direct2D1Platform.Direct3D11Device.ImmediateContext;

            // Clear views
            context.ClearDepthStencilView(_depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(_renderView, Color.White);

            // Update WorldViewProj Matrix
            var worldViewProj = Matrix.RotationX((float)_model.RotationX) * Matrix.RotationY((float)_model.RotationY)
                                                                          * Matrix.RotationZ((float)_model.RotationZ)
                                                                          * Matrix.Scaling((float)_model.Zoom)
                                                                          * viewProj;
            worldViewProj.Transpose();
            context.UpdateSubresource(ref worldViewProj, _contantBuffer);

            // Draw the cube
            context.Draw(36, 0);
            base.HandlePaint(rect);

            // Present!
            _swapChain.Present(0, PresentFlags.None);
        }

        private void CreateMesh()
        {
            var device = Direct2D1Platform.Direct3D11Device;

            // Compile Vertex and Pixel shaders
            var vertexShaderByteCode = ShaderBytecode.CompileFromFile("MiniCube.fx", "VS", "vs_4_0");
            var vertexShader = new VertexShader(device, vertexShaderByteCode);

            var pixelShaderByteCode = ShaderBytecode.CompileFromFile("MiniCube.fx", "PS", "ps_4_0");
            var pixelShader = new PixelShader(device, pixelShaderByteCode);

            var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode);

            var inputElements = new[]
                                {
                                    new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0)
                                };

            // Layout from VertexShader input signature
            var layout = new InputLayout(
                device,
                signature,
                inputElements);

            // Instantiate Vertex buffer from vertex data
            var vertices = Buffer.Create(
                device,
                BindFlags.VertexBuffer,
                new[]
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

                    new Vector4(-1.0f, -1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f), // Bottom
                    new Vector4( 1.0f, -1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                    new Vector4(-1.0f, -1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                    new Vector4(-1.0f, -1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                    new Vector4( 1.0f, -1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                    new Vector4( 1.0f, -1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),

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

            var context = Direct2D1Platform.Direct3D11Device.ImmediateContext;

            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, Utilities.SizeOf<Vector4>() * 2, 0));
            context.VertexShader.SetConstantBuffer(0, _contantBuffer);
            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);
        }

        private void Resize(Size size)
        {
            Utilities.Dispose(ref _deviceContext);
            Utilities.Dispose(ref _backBuffer);
            Utilities.Dispose(ref _renderView);
            Utilities.Dispose(ref _depthBuffer);
            Utilities.Dispose(ref _depthView);
            var context = Direct2D1Platform.Direct3D11Device.ImmediateContext;

            // Resize the backbuffer
            _swapChain.ResizeBuffers(0, 0, 0, Format.Unknown, SwapChainFlags.None);

            // Get the backbuffer from the swapchain
            _backBuffer = Resource.FromSwapChain<Texture2D>(_swapChain, 0);

            // Renderview on the backbuffer
            _renderView = new RenderTargetView(Direct2D1Platform.Direct3D11Device, _backBuffer);

            // Create the depth buffer
            _depthBuffer = new Texture2D(
                Direct2D1Platform.Direct3D11Device,
                new Texture2DDescription()
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
            _depthView = new DepthStencilView(Direct2D1Platform.Direct3D11Device, _depthBuffer);

            // Setup targets and viewport for rendering
            context.Rasterizer.SetViewport(new Viewport(0, 0, (int)size.Width, (int)size.Height, 0.0f, 1.0f));
            context.OutputMerger.SetTargets(_depthView, _renderView);

            // Setup new projection matrix with correct aspect ratio
            _proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, (float)(size.Width / size.Height), 0.1f, 100.0f);

            using (var dxgiBackBuffer = _swapChain.GetBackBuffer<Surface>(0))
            {
                var renderTarget = new SharpDX.Direct2D1.RenderTarget(
                    Direct2D1Platform.Direct2D1Factory,
                    dxgiBackBuffer,
                    new RenderTargetProperties
                    {
                        DpiX = 96,
                        DpiY = 96,
                        Type = RenderTargetType.Default,
                        PixelFormat = new PixelFormat(
                            Format.Unknown,
                            AlphaMode.Premultiplied)
                    });

                _deviceContext = renderTarget.QueryInterface<DeviceContext>();

                renderTarget.Dispose();
            }
        }

        private class D3DRenderTarget : IRenderTarget
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
                return new DrawingContextImpl(visualBrushRenderer, null, _window._deviceContext);
            }
        }


        protected override IRenderTarget CreateRenderTarget() => new D3DRenderTarget(this);
    }
}
