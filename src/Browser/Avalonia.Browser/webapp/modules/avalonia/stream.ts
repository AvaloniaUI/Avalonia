import FileSystemWritableFileStream from "native-file-system-adapter/types/src/FileSystemWritableFileStream";
import { IMemoryView } from "../../types/dotnet";

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

    public static async write(stream: FileSystemWritableFileStream, span: IMemoryView, offset: number, count: number) {
        // Copy before awaiting: the view points into WASM memory, which can be detached by memory growth
        // or moved by GC once the segment is unpinned, while the write itself completes asynchronously.
        // A copy is required anyway when the heap is a SharedArrayBuffer (WasmEnableThreads).
        let buffer: Uint8Array;
        try {
            buffer = span.slice(offset, offset + count) as Uint8Array;
        } finally {
            span.dispose();
        }

        return await stream.write(buffer);
    }

    public static byteLength(stream: Blob) {
        return stream.size;
    }

    public static async readInto(stream: Blob, offset: number, target: IMemoryView): Promise<number> {
        try {
            const buffer = await stream.slice(offset, offset + target.length).arrayBuffer();
            target.set(new Uint8Array(buffer));
            return buffer.byteLength;
        } finally {
            target.dispose();
        }
    }
}
