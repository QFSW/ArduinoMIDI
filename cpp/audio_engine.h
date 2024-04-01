#pragma once

#include <inttypes.h>

#define AUDIO_PIN 8

class AudioEngine
{
public:
    void start();
    void tick();

private:
    int decompress_frequency(int note_index) const;
    int decompress_duration(int note_index) const;

    uint32_t decompress(const uint8_t* buffer, uint32_t bitIndex, uint8_t numBits) const;

    int _curr_note_index;
    int _curr_note_remaining;
    int _last_tick;
};