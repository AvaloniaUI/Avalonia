export class GeneralHelpers {
    public static itemsArrayAt(instance: any, key: any): any[] {
        const items = instance[key];
        if (!items) {
            return [];
        }

        const retItems = [];
        for (let i = 0; i < items.length; i++) {
            retItems[i] = items[i];
        }
        return retItems;
    }

    public static itemAt(instance: any, key: any): any {
        const item = instance[key];
        return item;
    }

    public static callMethod(instance: any, name: string /*, args */): any {
        const args = Array.prototype.slice.call(arguments, 2);
        return instance[name].apply(instance, args);
    }
}
