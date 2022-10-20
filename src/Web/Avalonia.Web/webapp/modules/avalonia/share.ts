import { StorageItem } from "../storage/storageItem";
export class ShareData {
    constructor(public title: string, public files: File[]) { }
}

export class ShareHelper {
    public static async shareFileList(title: string, files: StorageItem[]) {
        const fileArray = new Array<File>();
        let sent = false;
        for (let i = 0; i < files.length; i++) {
            await (files[i].handle as FileSystemFileHandle).getFile().then(async f => {
                fileArray.push(f);

                if (!sent && fileArray.length === files.length) {
                    sent = true;
                    const shareData = new ShareData(title, fileArray);

                    if (navigator.canShare(shareData)) {
                        await navigator.share(shareData);
                    }
                }
            });
        }
    }
}
