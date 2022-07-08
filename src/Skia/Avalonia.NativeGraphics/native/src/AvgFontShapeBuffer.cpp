#include "AvgFontShapeBuffer.h"

AvgFontShapeBuffer::AvgFontShapeBuffer(IAvgGlyphTypeface* typeface)
{
    _buffer = hb_buffer_create();
    hb_buffer_set_direction(_buffer, HB_DIRECTION_LTR);
    _typeface = dynamic_cast<AvgGlyphTypeface*>(typeface);
}

AvgFontShapeBuffer::~AvgFontShapeBuffer()
{
    hb_buffer_destroy(_buffer);
}

int AvgFontShapeBuffer::GetLength()
{
    return hb_buffer_get_length(_buffer);
}

void AvgFontShapeBuffer::GuessSegmentProperties()
{
    hb_buffer_guess_segment_properties(_buffer);
}

void AvgFontShapeBuffer::SetDirection(int direction)
{
    hb_buffer_set_direction(_buffer, (hb_direction_t)direction);
}

void AvgFontShapeBuffer::SetLanguage(void* langauge)
{
    hb_buffer_set_language(_buffer, (hb_language_t)langauge);
}

void AvgFontShapeBuffer::AddUtf16(void* utf16, int length, int itemOffset, int itemLength)
{
    hb_buffer_add_utf16(_buffer, (const uint16_t*)utf16, length, itemOffset, itemLength);
}

void AvgFontShapeBuffer::Shape()
{
    hb_shape(_typeface->_font, _buffer, NULL, 0);
}

void AvgFontShapeBuffer::GetScale(int* x, int* y)
{
    hb_font_get_scale(_typeface->_font, x, y);
}

void* AvgFontShapeBuffer::GetGlyphInfoSpan(unsigned int* length)
{
    hb_glyph_info_t* info = hb_buffer_get_glyph_infos(_buffer, length);
    return info;
}

void* AvgFontShapeBuffer::GetGlyphPositionSpan(unsigned int* length)
{
    auto ptr = hb_buffer_get_glyph_positions(_buffer, length);
    return ptr;
}
