﻿using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Direct2D1;
using Avalonia.Direct2D1.Media;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Rendering;

using Vortice.D3DCompiler;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using AlphaMode = Vortice.DCommon.AlphaMode;
using Buffer = Vortice.Direct3D11.ID3D11Buffer;
using Factory2 = Vortice.DXGI.IDXGIFactory2;
using InputElement = Vortice.Direct3D11.InputElementDescription;
using PixelFormat = Vortice.DCommon.PixelFormat;

namespace Direct3DInteropSample
{
    public class MainWindow : Window
    {
        private ID3D11Texture2D _backBuffer;
        private ID3D11RenderTargetView _renderView;
        private ID3D11Texture2D _depthBuffer;
        private ID3D11DepthStencilView _depthView;
        private readonly IDXGISwapChain _swapChain;
        private SwapChainDescription1 _desc;
        private Matrix4x4 _proj = Matrix4x4.Identity;
        private readonly Matrix4x4 _view;
        private Buffer _contantBuffer;
        private ID2D1DeviceContext _deviceContext;
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
                BufferUsage = Usage.RenderTargetOutput
            };

            using (var adaper = Direct2D1Platform.DxgiDevice.GetAdapter())
            {
                using (var factory = adaper.GetParent<Factory2>())
                {
                    _swapChain = factory.CreateSwapChainForHwnd(Direct2D1Platform.DxgiDevice, PlatformImpl?.Handle.Handle ?? IntPtr.Zero, _desc);
                }
            }

            _deviceContext = Direct2D1Platform.Direct2D1Device.CreateDeviceContext(DeviceContextOptions.None);
            _deviceContext.Dpi = new Vortice.Mathematics.Size(96, 96);

            CreateMesh();

            _view = Matrix4x4Extensions.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);

            this.GetObservable(ClientSizeProperty).Subscribe(Resize);

            Resize(ClientSize);

            AvaloniaXamlLoader.Load(this);

            Background = Avalonia.Media.Brushes.Transparent;
        }


        protected override void HandlePaint(Rect rect)
        {
            var viewProj = Matrix4x4.Multiply(_view, _proj);
            var context = Direct2D1Platform.Direct3D11Device.ImmediateContext;

            // Clear views
            context.ClearDepthStencilView(_depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(_renderView, Vortice.Mathematics.Colors.White);

            // Update WorldViewProj Matrix
            var worldViewProj = Matrix4x4.CreateRotationX((float)_model.RotationX) * Matrix4x4.CreateRotationY((float)_model.RotationY)
                                                                          * Matrix4x4.CreateRotationZ((float)_model.RotationZ)
                                                                          * Matrix4x4.CreateScale((float)_model.Zoom)
                                                                          * viewProj;
            worldViewProj = Matrix4x4.Transpose(worldViewProj);
            context.UpdateSubresource(worldViewProj, _contantBuffer);

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
            var vertexShaderByteCode = Compiler.CompileFromFile("MiniCube.fx", "VS", "vs_4_0");
            var vertexShader = device.CreateVertexShader(vertexShaderByteCode);

            var pixelShaderByteCode = Compiler.CompileFromFile("MiniCube.fx", "PS", "ps_4_0");
            var pixelShader = device.CreatePixelShader(pixelShaderByteCode);

            Compiler.GetInputSignatureBlob(
                vertexShaderByteCode.BufferPointer, 
                vertexShaderByteCode.BufferSize, 
                out Blob signature).CheckError();

            var inputElements = new[]
                                {
                                    new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0)
                                };

            // Layout from VertexShader input signature
            var layout = device.CreateInputLayout(
                inputElements,
                signature
                );

            // Instantiate Vertex buffer from vertex data
            var vertices = device.CreateBuffer(
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
                }
                );

            // Create Constant Buffer
            _contantBuffer = device.CreateBuffer(Unsafe.SizeOf<Matrix>(), BindFlags.ConstantBuffer);

            var context = Direct2D1Platform.Direct3D11Device.ImmediateContext;

            // Prepare All the stages
            context.IASetInputLayout(layout);
            context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            context.IASetVertexBuffer(0, vertices, Unsafe.SizeOf<Vector4>() * 2, 0);
            context.VSSetConstantBuffer(0, _contantBuffer);
            context.VSSetShader(vertexShader);
            context.PSSetShader(pixelShader);
        }

        private void Resize(Size size)
        {
            _deviceContext.Dispose();
            _deviceContext = null;
            _backBuffer.Dispose();
            _backBuffer = null;
            _renderView.Dispose();
            _renderView = null;
            _depthBuffer.Dispose();
            _depthBuffer = null;
            _depthView.Dispose();
            _depthView = null;
            var context = Direct2D1Platform.Direct3D11Device.ImmediateContext;

            // Resize the backbuffer
            _swapChain.ResizeBuffers(0, 0, 0, Format.Unknown, SwapChainFlags.None);

            // Get the backbuffer from the swapchain
            _backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);

            // Renderview on the backbuffer
            _renderView = Direct2D1Platform.Direct3D11Device.CreateRenderTargetView(_backBuffer);

            // Create the depth buffer
            _depthBuffer = Direct2D1Platform.Direct3D11Device.CreateTexture2D(
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
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                });

            // Create the depth buffer view
            _depthView = Direct2D1Platform.Direct3D11Device.CreateDepthStencilView(_depthBuffer);

            // Setup targets and viewport for rendering
            context.RSSetViewport(0, 0, (float)size.Width, (float)size.Height);
            context.OMSetRenderTargets(_renderView, _depthView);

            // Setup new projection matrix with correct aspect ratio
            _proj = Matrix4x4Extensions.PerspectiveFovLH((float)Math.PI / 4.0f, (float)(size.Width / size.Height), 0.1f, 100.0f);

            using (var dxgiBackBuffer = _swapChain.GetBuffer<IDXGISurface>(0))
            {
                var renderTarget = Direct2D1Platform.Direct2D1Factory.CreateDxgiSurfaceRenderTarget(
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

                _deviceContext = renderTarget.QueryInterface<ID2D1DeviceContext>();

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
