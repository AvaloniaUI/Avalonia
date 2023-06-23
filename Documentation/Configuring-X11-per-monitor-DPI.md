Monitor DPI is calculated from values provided by XRANDR extension. These might be not accurate for your particular monitor, so you can override scaling factors for particular monitors via environment variable.

1) `xrandr --listactivemonitors` will give you output like this:
```
Monitors: 1
 0: +*eDP-1 1920/344x1080/194+0+0  eDP-1
```

`eDP-1`, `HDMI-1`, `DP-1` are output names that your can configure DPI for.

2) Add `AVALONIA_SCREEN_SCALE_FACTORS` environment variable to your `/etc/profile`, `$HOME/.profile` or other suitable location and relogin.

Example:
```
AVALONIA_SCREEN_SCALE_FACTORS='eDP-1=2;HDMI-1=1;DP-1=1.5'
```
this will set eDP-1 to 192 DPI, HDMI-1 to 96 DPI and DP-1 to 144 DPI.
