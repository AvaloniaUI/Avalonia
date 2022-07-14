﻿using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Avalonia.Web.Blazor.Interop
{
    internal class JSModuleInterop : IDisposable
    {
        private readonly Task<IJSUnmarshalledObjectReference> moduleTask;
        private IJSUnmarshalledObjectReference? module;

        public JSModuleInterop(IJSRuntime js, string filename)
        {
            if (js is not IJSInProcessRuntime)
                throw new NotSupportedException("SkiaSharp currently only works on Web Assembly.");

            moduleTask = js.InvokeAsync<IJSUnmarshalledObjectReference>("import", filename).AsTask();
        }

        public async Task ImportAsync()
        {
            module = await moduleTask;
        }

        public void Dispose()
        {
            OnDisposingModule();
            Module.Dispose();
        }

        protected IJSUnmarshalledObjectReference Module =>
            module ?? throw new InvalidOperationException("Make sure to run ImportAsync() first.");

        protected void Invoke(string identifier, params object?[]? args) =>
            Module.InvokeVoid(identifier, args);

        protected TValue Invoke<TValue>(string identifier, params object?[]? args) =>
            Module.Invoke<TValue>(identifier, args);

        protected ValueTask InvokeAsync(string identifier, params object?[]? args) =>
            Module.InvokeVoidAsync(identifier, args);

        protected ValueTask<TValue> InvokeAsync<TValue>(string identifier, params object?[]? args) =>
            Module.InvokeAsync<TValue>(identifier, args);

        protected virtual void OnDisposingModule() { }
    }
}
