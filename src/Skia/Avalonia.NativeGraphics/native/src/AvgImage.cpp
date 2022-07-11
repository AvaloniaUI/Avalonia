#include "AvgImage.h"

AvgImage::AvgImage(sk_sp<SkData> data)
{
    _image = SkImage::MakeFromEncoded(data);
}

AvgImage::~AvgImage()
{

}


AvgVector AvgImage::getSize()
{
    AvgVector v;

    v.X = _image->bounds().width();
    v.Y = _image->bounds().height();

    return v;
}
