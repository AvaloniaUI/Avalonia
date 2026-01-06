using System.Numerics;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using static Silk.NET.Core.Native.SilkMarshal;

namespace GpuInterop.D3DDemo;

public static class D3DContent
{
    private static D3DCompiler s_compiler = D3DCompiler.GetApi();

    public static unsafe ComPtr<ID3D11Buffer> CreateMesh(ComPtr<ID3D11Device> device)
    {
        // Compile Vertex and Pixel shaders
        using var vertexShaderByteCode = CompileShader("D3DDemo\\MiniCube.fx", "VS", "vs_4_0");
        using ComPtr<ID3D11VertexShader> vertexShader = default;
        ThrowHResult(device.CreateVertexShader(
            vertexShaderByteCode.GetBufferPointer(),
            vertexShaderByteCode.GetBufferSize(),
            (ID3D11ClassLinkage*)null,
            vertexShader.GetAddressOf()));

        using var pixelShaderByteCode = CompileShader("D3DDemo\\MiniCube.fx", "PS", "ps_4_0");
        using ComPtr<ID3D11PixelShader> pixelShader = default;
        ThrowHResult(device.CreatePixelShader(
            pixelShaderByteCode.GetBufferPointer(),
            pixelShaderByteCode.GetBufferSize(),
            (ID3D11ClassLinkage*)null,
            pixelShader.GetAddressOf()));

        using ComPtr<ID3D10Blob> vertexShaderSignature = default;
        ThrowHResult(s_compiler.GetInputSignatureBlob(
            vertexShaderByteCode.GetBufferPointer(),
            vertexShaderByteCode.GetBufferSize(),
            vertexShaderSignature.GetAddressOf()));

        // Layout from VertexShader input signature
        using ComPtr<ID3D11InputLayout> inputLayout = default;
        var positionNamePtr = StringToPtr("POSITION", NativeStringEncoding.LPStr);
        var colorNamePtr = StringToPtr("COLOR", NativeStringEncoding.LPStr);
        try
        {
            const int inputCount = 2;
            var inputs = stackalloc InputElementDesc[inputCount]
            {
                new InputElementDesc((byte*)positionNamePtr, 0, Format.FormatR32G32B32A32Float, 0, 0),
                new InputElementDesc((byte*)colorNamePtr, 0, Format.FormatR32G32B32A32Float, 0, 16)
            };

            ThrowHResult(device.CreateInputLayout(
                inputs,
                inputCount,
                vertexShaderSignature.GetBufferPointer(),
                vertexShaderSignature.GetBufferSize(),
                inputLayout.GetAddressOf()));
        }
        finally
        {
            FreeString(positionNamePtr, NativeStringEncoding.LPStr);
            FreeString(colorNamePtr, NativeStringEncoding.LPStr);
        }

        // Instantiate Vertex buffer from vertex data
        var vertices = new[]
        {
            new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), // Front
            new Vector4(-1.0f, 1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            new Vector4(1.0f, 1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            new Vector4(1.0f, 1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            new Vector4(1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            new Vector4(-1.0f, -1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), // BACK
            new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            new Vector4(-1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            new Vector4(-1.0f, -1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            new Vector4(1.0f, -1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            new Vector4(-1.0f, 1.0f, -1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), // Top
            new Vector4(-1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
            new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
            new Vector4(-1.0f, 1.0f, -1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
            new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
            new Vector4(1.0f, 1.0f, -1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
            new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f), // Bottom
            new Vector4(1.0f, -1.0f, 1.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
            new Vector4(-1.0f, -1.0f, 1.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
            new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
            new Vector4(1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
            new Vector4(1.0f, -1.0f, 1.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
            new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f), // Left
            new Vector4(-1.0f, -1.0f, 1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
            new Vector4(-1.0f, 1.0f, 1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
            new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
            new Vector4(-1.0f, 1.0f, 1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
            new Vector4(-1.0f, 1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
            new Vector4(1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f), // Right
            new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
            new Vector4(1.0f, -1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
            new Vector4(1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
            new Vector4(1.0f, 1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
            new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
        };

        using ComPtr<ID3D11Buffer> vertexBuffer = default;

        fixed (Vector4* verticesPtr = vertices)
        {
            var vertexBufferDesc = new BufferDesc
            {
                ByteWidth = (uint)(sizeof(Vector4) * vertices.Length),
                Usage = Usage.Default,
                BindFlags = (uint)BindFlag.VertexBuffer,
                CPUAccessFlags = (uint)CpuAccessFlag.None,
                MiscFlags = (uint)ResourceMiscFlag.None,
                StructureByteStride = 0
            };
            var subresourceData = new SubresourceData(verticesPtr);
            ThrowHResult(device.CreateBuffer(&vertexBufferDesc, &subresourceData, vertexBuffer.GetAddressOf()));
        }

        // Create Constant Buffer
        ComPtr<ID3D11Buffer> constantBuffer = default;
        var constantBufferDesc = new BufferDesc
        {
            ByteWidth = (uint)sizeof(Matrix4x4),
            Usage = Usage.Default,
            BindFlags = (uint)BindFlag.ConstantBuffer,
            CPUAccessFlags = (uint)CpuAccessFlag.None,
            MiscFlags = (uint)ResourceMiscFlag.None,
            StructureByteStride = 0
        };
        ThrowHResult(device.CreateBuffer(&constantBufferDesc, (SubresourceData*)null, constantBuffer.GetAddressOf()));

        // Prepare All the stages
        using ComPtr<ID3D11DeviceContext> context = default;
        device.GetImmediateContext(context.GetAddressOf());

        context.IASetInputLayout(inputLayout);
        context.IASetPrimitiveTopology(D3DPrimitiveTopology.D3D10PrimitiveTopologyTrianglelist);
        var stride = (uint)(sizeof(Vector4) * 2);
        var offset = 0u;
        context.IASetVertexBuffers(0, 1, &vertexBuffer.Handle, &stride, &offset);
        context.VSSetConstantBuffers(0, 1, &constantBuffer.Handle);
        context.VSSetShader(vertexShader, (ID3D11ClassInstance**)null, 0);
        context.PSSetShader(pixelShader, (ID3D11ClassInstance**)null, 0);

        return constantBuffer;
    }

    private static unsafe ComPtr<ID3D10Blob> CompileShader(string fileName, string entryPoint, string profile)
    {
        ComPtr<ID3D10Blob> blob = default;
        ThrowHResult(s_compiler.CompileFromFile(fileName, null, null, entryPoint, profile, 0u, 0u, blob.GetAddressOf(), null));
        return blob;
    }
}
