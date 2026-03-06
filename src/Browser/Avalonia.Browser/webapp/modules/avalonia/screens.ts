import { JsExports } from "./jsExports";

type SingleScreen = Screen & { window: Window; availLeft: number; availTop: number };
type ScreenDetailedEx = ScreenDetailed & { availLeft: number; availTop: number };
type BrowserScreen = ScreenDetailedEx | SingleScreen;
enum ScreenOrientation {
    None,
    Landscape = 1,
    Portrait = 2,
    LandscapeFlipped = 4,
    PortraitFlipped = 8
}

export class ScreenHelper {
    static detailedScreens?: ScreenDetails;

    private static raiseOnChanged() {
        JsExports.DomHelper.ScreensChanged();
    }

    public static async checkPermissions(globalThis: Window): Promise<void> {
        // If previous session already granted "window-management" permissions, just re-request details, before they are used.
        const { state } = await globalThis.navigator.permissions.query({ name: "window-management" } as any);
        if (state === "granted") {
            await this.requestDetailedScreens(globalThis);
        }
    }

    public static subscribeOnChanged(globalThis: Window) {
        if (this.detailedScreens) {
            globalThis.screen.removeEventListener("change", this.raiseOnChanged);
            this.detailedScreens.addEventListener("screenschange", this.raiseOnChanged);

            // When any screen was added, we re-subscribe on all of them to keep it simpler.
            // Just like in C#, it's safer to re-subscribe if handler is the same function - it will trigger it once.
            for (const screen of this.detailedScreens.screens) {
                screen.addEventListener("change", this.raiseOnChanged);
            }
        } else {
            globalThis.screen.addEventListener("change", this.raiseOnChanged);
        }
    }

    public static getAllScreens(globalThis: Window): BrowserScreen[] {
        if (this.detailedScreens) {
            return this.detailedScreens.screens as ScreenDetailedEx[];
        } else {
            const singleScreen = Object.assign(globalThis.screen, { window: globalThis }) as SingleScreen;
            return [singleScreen];
        }
    }

    public static async requestDetailedScreens(globalThis: Window): Promise<boolean> {
        if (this.detailedScreens) {
            return true;
        }
        if ("getScreenDetails" in globalThis) {
            this.detailedScreens = await globalThis.getScreenDetails();
            if (this.detailedScreens) {
                this.subscribeOnChanged(globalThis);
                globalThis.setTimeout(this.raiseOnChanged, 1);
                return true;
            }
        }
        return false;
    }

    static getDisplayName(screen: BrowserScreen) {
        return (screen as ScreenDetailed)?.label;
    }

    static getScaling(screen: BrowserScreen) {
        if ("devicePixelRatio" in screen) {
            return screen.devicePixelRatio;
        }
        if ("window" in screen) {
            return screen.window.devicePixelRatio;
        }
        return 1;
    }

    static getBounds(screen: BrowserScreen): number[] {
        const width = screen.width;
        const height = screen.height;

        if ("left" in screen && "top" in screen) {
            return [screen.left, screen.top, width, height];
        } else if ("availLeft" in screen && "availTop" in screen) {
            // If webapp doesn't have "window-management" perms, "left" and "top" will be undefined.
            // To keep getBounds consistent getWorkingArea, while still usable, fallback to availLeft and availTop values.
            return [screen.availLeft, screen.availTop, width, height];
        }

        return [0, 0, width, height];
    }

    static getWorkingArea(screen: BrowserScreen): number[] {
        const width = screen.availWidth;
        const height = screen.availHeight;

        if ("availLeft" in screen && "availTop" in screen) {
            return [screen.availLeft, screen.availTop, width, height];
        }
        return [0, 0, width, height];
    }

    static isCurrent(screen: BrowserScreen): boolean {
        if (this.detailedScreens) {
            return this.detailedScreens.currentScreen === screen;
        }

        // If detailedScreens were not requested - we have a single screen which always is a current one.
        return true;
    }

    static isPrimary(screen: BrowserScreen): boolean {
        if ("isPrimary" in screen) {
            return screen.isPrimary;
        }

        // If detailedScreens were not requested - we have a single screen which always is a current one, and we assume it's a primary one as well.
        return true;
    }

    /* eslint-disable indent */
    static getCurrentOrientation(screen: BrowserScreen): ScreenOrientation {
        switch (screen.orientation.type) {
            case "landscape-primary":
                return ScreenOrientation.Landscape;
            case "landscape-secondary":
                return ScreenOrientation.LandscapeFlipped;
            case "portrait-primary":
                return ScreenOrientation.Portrait;
            case "portrait-secondary":
                return ScreenOrientation.PortraitFlipped;
        }
    }
}
