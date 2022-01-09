using System.Collections.Generic;

namespace Sandbox;

public class MainViewModel
{

    public List<ImageSource> Images { get; } = new ( new []
    {
        new ImageSource("Images/Nikon-D500-Sample-Images-3.jpg"),
        new ImageSource("Images/nikon-d7500-sample-images-3.jpg"),
        new ImageSource("Images/R.jfif"),
        new ImageSource("Images/R.png")
    });
}
