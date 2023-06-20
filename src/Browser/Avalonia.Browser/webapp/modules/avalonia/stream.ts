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

    public static async write(stream: FileSystemWritableFileStream, span: IMemoryView) {
        const array = new Uint8Array(span.byteLength);
        span.copyTo(array);

        return await stream.write(array);
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
