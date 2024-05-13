import FileSystemWritableFileStream from "native-file-system-adapter/types/src/FileSystemWritableFileStream";

const sharedArrayBufferDefined = typeof SharedArrayBuffer !== "undefined";
export function isSharedArrayBuffer(buffer: any): buffer is SharedArrayBuffer {
    // BEWARE: In some cases, `instanceof SharedArrayBuffer` returns false even though buffer is an SAB.
    // Patch adapted from https://github.com/emscripten-core/emscripten/pull/16994
    // See also https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Symbol/toStringTag
    return sharedArrayBufferDefined && buffer[Symbol.toStringTag] === "SharedArrayBuffer";
}

export class StreamHelper {
    public static async seek(stream: FileSystemWritableFileStream, position: number) {
        return await stream.seek(position);
    }

    public static async truncate(stream: FileSystemWritableFileStream, size: number) {
        return await stream.truncate(size);
    }

    public static async close(stream: FileSystemWritableFileStream) {
        return await stream.close();
    }

    public static async write(stream: FileSystemWritableFileStream, span: any, offset: number, count: number) {
        const heap8 = globalThis.getDotnetRuntime(0)?.localHeapViewU8();

        let buffer: Uint8Array;
        if (span._pointer > 0 && span._length > 0 && heap8 && !isSharedArrayBuffer(heap8.buffer)) {
            // Attempt to use undocumented access to the HEAP8 directly
            // Note, SharedArrayBuffer cannot be used with ImageData (when WasmEnableThreads = true).
            buffer = new Uint8Array(heap8.buffer, span._pointer as number + offset, count);
        } else {
            // Or fallback to the normal API that does multiple array copies.
            const copy = new Uint8Array(count);
            span.copyTo(copy, offset);
            buffer = span;
        }

        return await stream.write(buffer);
    }

    public static byteLength(stream: Blob) {
        return stream.size;
    }

    public static async sliceArrayBuffer(stream: Blob, offset: number, count: number) {
        const buffer = await stream.slice(offset, offset + count).arrayBuffer();
        return new Uint8Array(buffer);
    }

    public static toMemoryView(buffer: Uint8Array): Uint8Array {
        return buffer;
    }
}
